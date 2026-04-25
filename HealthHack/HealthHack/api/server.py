from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
import joblib
import pandas as pd
import os

app = FastAPI()

BASE  = os.path.dirname(os.path.abspath(__file__))
model = joblib.load(os.path.join(BASE, "../model/model.pkl"))
le    = joblib.load(os.path.join(BASE, "../model/label_encoder.pkl"))

SEQ_LEN = 30

VALID_CONDITIONS = ["parkinson", "stroke", "atrophy", "sports_injury", "healthy"]
VALID_STATUSES   = ["mild", "moderate", "severe", "early", "mid", "advanced", "normal", "working_out"]

# ── GET: game engine sends player data, ML returns sequence ──

@app.get("/unity")
def get_sequence(age: int, condition: str, status: str, max_duration: float):
    if condition not in VALID_CONDITIONS:
        raise HTTPException(status_code=400, detail=f"Invalid condition. Valid: {VALID_CONDITIONS}")
    if status not in VALID_STATUSES:
        raise HTTPException(status_code=400, detail=f"Invalid status. Valid: {VALID_STATUSES}")
    if not (5 <= age <= 100):
        raise HTTPException(status_code=400, detail="Age must be between 5 and 100")
    if not (0.5 <= max_duration <= 10.0):
        raise HTTPException(status_code=400, detail="max_duration must be between 0.5 and 10.0")

    condition_enc = le.transform([condition])[0]

    input_df = pd.DataFrame([{
        "age":           age,
        "condition_enc": condition_enc,
        "max_duration":  max_duration,
    }])

    sequence = model.predict(input_df)[0].tolist()

    return {
        "sequence":  sequence,
        "length":    len(sequence),
        "condition": condition,
        "status":    status,
        "age":       age,
        "legend": {
            "0": "light contraction 0.4s–4s",
            "1": "hard contraction  4s–10s",
        }
    }

# ── POST: ML sends back only the sequence to game engine ──

class SequenceResult(BaseModel):
    sequence: list[int]

@app.post("/unity/result")
def receive_sequence(result: SequenceResult):
    for val in result.sequence:
        if val not in [0, 1]:
            raise HTTPException(status_code=400, detail="Sequence must contain only 0 and 1")
    return {
        "received": True,
        "sequence": result.sequence,
        "length":   len(result.sequence),
    }

@app.get("/health")
def health():
    return {"status": "ok"}



if __name__ == "__main__":
    import uvicorn
    uvicorn.run("server:app", host="0.0.0.0", port=8000, reload=True)