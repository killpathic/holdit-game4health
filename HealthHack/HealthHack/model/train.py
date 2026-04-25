import numpy as np
import pandas as pd
from sklearn.ensemble import RandomForestClassifier
from sklearn.multioutput import MultiOutputClassifier
from sklearn.preprocessing import LabelEncoder
import joblib
import random
import sys
import os

sys.path.append(os.path.dirname(os.path.abspath(__file__)))
from sequences import SEQUENCES

random.seed(42)
np.random.seed(42)

SEQ_LEN = 30

CONDITION_SEVERITY = {
    "parkinson":     ["mild", "moderate", "severe"],
    "stroke":        ["early", "mid", "advanced"],
    "atrophy":       ["early", "mid", "advanced"],
    "sports_injury": ["early", "mid", "advanced"],
    "healthy":       ["normal", "working_out", "advanced"],
}

def age_to_severity(condition, age):
    if condition == "parkinson":
        if age < 50:   return "mild"
        elif age < 65: return "moderate"
        else:          return "severe"
    elif condition == "healthy":
        if age > 50:   return "normal"
        return random.choice(["normal", "working_out", "advanced"])
    else:
        if age < 40:   return "advanced"
        elif age < 65: return "mid"
        else:          return "early"

def add_noise(sequence, noise_prob=0.05):
    return [1 - b if random.random() < noise_prob else b for b in sequence]

def generate_dataset(n=2000):
    rows = []
    conditions = list(CONDITION_SEVERITY.keys())

    for _ in range(n):
        condition    = random.choice(conditions)
        age          = random.randint(15, 85)
        max_duration = round(random.uniform(0.5, 10.0), 2)
        severity     = age_to_severity(condition, age)
        base_seq     = SEQUENCES[condition][severity]
        seq          = add_noise(base_seq)
        seq          = seq + [0] * (SEQ_LEN - len(seq))

        rows.append({
            "age":          age,
            "condition":    condition,
            "max_duration": max_duration,
            **{f"s{i}": seq[i] for i in range(SEQ_LEN)},
        })

    return pd.DataFrame(rows)


df = generate_dataset(2000)

le = LabelEncoder()
df["condition_enc"] = le.fit_transform(df["condition"])

FEATURES = ["age", "condition_enc", "max_duration"]
TARGETS  = [f"s{i}" for i in range(SEQ_LEN)]

X = df[FEATURES]
y = df[TARGETS]

model = MultiOutputClassifier(
    RandomForestClassifier(n_estimators=200, max_depth=10, random_state=42)
)
model.fit(X, y)

joblib.dump(model, "model.pkl")
joblib.dump(le,    "label_encoder.pkl")

print("Model trained and saved.")
print(f"Conditions: {list(le.classes_)}")

# quick sanity check
test_cases = [
    {"age": 70, "condition": "parkinson","status" : "mild",     "max_duration": 2.0},
    {"age": 30, "condition": "sports_injury", "status": "moderate", "max_duration": 8.0},
    {"age": 25, "condition": "healthy",       "status": "normal", "max_duration": 9.0},
]

print("\n── Sanity Check ──")
for case in test_cases:
    enc = le.transform([case["condition"]])[0]
    inp = pd.DataFrame([{"age": case["age"], "condition_enc": enc, "max_duration": case["max_duration"]}])
    pred = model.predict(inp)[0].tolist()
    print(f"  {case['condition']:15} age {case['age']} → {pred}")