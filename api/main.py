from contextlib import asynccontextmanager
from datetime import datetime
from typing import Literal

from fastapi import FastAPI, HTTPException, Depends
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel, Field
from pydantic_settings import BaseSettings, SettingsConfigDict
from sqlalchemy import (
    Column, DateTime, Float, Integer, String, JSON, create_engine
)
from sqlalchemy.orm import DeclarativeBase, Session, sessionmaker


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    database_url: str
    secret_key: str
    cors_origins: str = ""


settings = Settings()

engine = create_engine(settings.database_url, pool_pre_ping=True)
SessionLocal = sessionmaker(bind=engine, autoflush=False, autocommit=False)


class Base(DeclarativeBase):
    pass


class RehabSession(Base):
    __tablename__ = "rehab_sessions"

    id = Column(Integer, primary_key=True, index=True)
    age = Column(Integer, nullable=False)
    condition = Column(String(32), nullable=False)
    status = Column(String(32), nullable=False)
    max_duration = Column(Float, nullable=False)
    sequence = Column(JSON, nullable=False)
    created_at = Column(DateTime, default=datetime.utcnow, nullable=False)


VALID_CONDITIONS = ["parkinson", "stroke", "atrophy", "sports_injury", "healthy"]
VALID_STATUSES = ["mild", "moderate", "severe", "early", "mid", "advanced", "normal", "working_out"]
SEQ_LEN = 30


@asynccontextmanager
async def lifespan(app: FastAPI):
    Base.metadata.create_all(bind=engine)
    yield


app = FastAPI(
    title="HOLD IT — Game4Health API",
    version="0.1.0",
    lifespan=lifespan,
)

_cors_origins = [o.strip() for o in settings.cors_origins.split(",") if o.strip()]
if _cors_origins:
    app.add_middleware(
        CORSMiddleware,
        allow_origins=_cors_origins,
        allow_credentials=False,
        allow_methods=["GET", "POST"],
        allow_headers=["*"],
    )


def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


class SequenceResponse(BaseModel):
    sequence: list[int]
    length: int
    condition: str
    status: str
    age: int
    legend: dict[str, str]


class SessionCreate(BaseModel):
    age: int = Field(ge=5, le=100)
    condition: Literal["parkinson", "stroke", "atrophy", "sports_injury", "healthy"]
    status: Literal["mild", "moderate", "severe", "early", "mid", "advanced", "normal", "working_out"]
    max_duration: float = Field(ge=0.5, le=10.0)
    sequence: list[int]


class SessionRead(SessionCreate):
    id: int
    created_at: datetime


@app.get("/health")
def health():
    return {"status": "ok"}


@app.get("/unity", response_model=SequenceResponse)
def get_sequence(age: int, condition: str, status: str, max_duration: float):
    if condition not in VALID_CONDITIONS:
        raise HTTPException(status_code=400, detail=f"Invalid condition. Valid: {VALID_CONDITIONS}")
    if status not in VALID_STATUSES:
        raise HTTPException(status_code=400, detail=f"Invalid status. Valid: {VALID_STATUSES}")
    if not (5 <= age <= 100):
        raise HTTPException(status_code=400, detail="Age must be between 5 and 100")
    if not (0.5 <= max_duration <= 10.0):
        raise HTTPException(status_code=400, detail="max_duration must be between 0.5 and 10.0")

    # Stub sequence — replace with real ml inference once ml/ is wired up.
    sequence = [(i + age) % 2 for i in range(SEQ_LEN)]

    return SequenceResponse(
        sequence=sequence,
        length=len(sequence),
        condition=condition,
        status=status,
        age=age,
        legend={
            "0": "light contraction 0.4s–4s",
            "1": "hard contraction  4s–10s",
        },
    )


@app.post("/sessions", response_model=SessionRead)
def create_session(payload: SessionCreate, db: Session = Depends(get_db)):
    for v in payload.sequence:
        if v not in (0, 1):
            raise HTTPException(status_code=400, detail="Sequence must contain only 0 and 1")

    row = RehabSession(
        age=payload.age,
        condition=payload.condition,
        status=payload.status,
        max_duration=payload.max_duration,
        sequence=payload.sequence,
    )
    db.add(row)
    db.commit()
    db.refresh(row)
    return SessionRead(
        id=row.id,
        age=row.age,
        condition=row.condition,
        status=row.status,
        max_duration=row.max_duration,
        sequence=row.sequence,
        created_at=row.created_at,
    )


@app.get("/sessions/{session_id}", response_model=SessionRead)
def get_session(session_id: int, db: Session = Depends(get_db)):
    row = db.get(RehabSession, session_id)
    if row is None:
        raise HTTPException(status_code=404, detail="session not found")
    return SessionRead(
        id=row.id,
        age=row.age,
        condition=row.condition,
        status=row.status,
        max_duration=row.max_duration,
        sequence=row.sequence,
        created_at=row.created_at,
    )
