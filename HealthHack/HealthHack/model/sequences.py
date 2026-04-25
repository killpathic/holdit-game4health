def generate_sequence(base_pattern, length):
    seq = []
    i = 0
    while len(seq) < length:
        seq.append(base_pattern[i % len(base_pattern)])
        i += 1
    return seq

SEQUENCES = {
    "parkinson": {
        "mild":     generate_sequence([0, 1, 0, 0], 20),
        "moderate": generate_sequence([0, 0, 1, 0, 0], 25),
        "severe":   generate_sequence([0, 0, 0, 1, 0, 0, 0], 30),
    },
    "stroke": {
        "early":    generate_sequence([0, 0, 0, 0, 1], 15),
        "mid":      generate_sequence([0, 0, 0, 1, 0], 20),
        "advanced": generate_sequence([0, 0, 1, 0, 0], 25),
    },
    "atrophy": {
        "early":    generate_sequence([0, 0, 0, 1], 15),
        "mid":      generate_sequence([0, 0, 1, 0], 20),
        "advanced": generate_sequence([0, 1, 0, 1, 0], 25),
    },
    "sports_injury": {
        "early":    generate_sequence([0, 1, 0, 0, 1], 15),
        "mid":      generate_sequence([1, 0, 1, 0, 1], 20),
        "advanced": generate_sequence([1, 1, 0, 1, 1, 0], 25),
    },
    "healthy": {
        "normal":      generate_sequence([1, 0, 1, 0, 1], 15),
        "working_out": generate_sequence([1, 1, 0, 1, 1], 20),
        "advanced":    generate_sequence([1, 1, 1, 0, 1, 1], 25),
    },
}