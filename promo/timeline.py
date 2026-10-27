"""Single clock for the sales film. Picture and sound both read this."""

FPS = 30
DURATION = 18.0
WIDTH = 1920
HEIGHT = 1080
BPM = 120

# Landings sit on musical beats (0.5 s at 120 BPM).
BEATS = {
    "line": 0.50,
    "eyebrow": 1.00,
    "title": 1.50,
    "subtitle": 2.00,
    "facade": 3.00,
    "facade_label": 3.50,
    "facade_line": 4.00,
    "sala": 6.50,
    "sala_label": 7.00,
    "chip0": 7.50,
    "chip1": 8.00,
    "chip2": 8.50,
    "cocina": 10.00,
    "cocina_label": 10.50,
    "stats": 11.00,
    "night": 13.50,
    "night_title": 14.00,
    "end": 16.00,
}

# Each id is a synthesized hit. The picture change and the hit share a time.
SOUND = [
    {"t": 0.50, "id": "tick"},
    {"t": 1.00, "id": "blip"},
    {"t": 1.50, "id": "chord_soft"},
    {"t": 2.00, "id": "blip"},
    {"t": 3.00, "id": "whoosh"},
    {"t": 3.00, "id": "drop"},
    {"t": 3.50, "id": "bell"},
    {"t": 4.00, "id": "bell_low"},
    {"t": 6.50, "id": "whoosh"},
    {"t": 7.00, "id": "bell"},
    {"t": 7.50, "id": "tick"},
    {"t": 8.00, "id": "tick"},
    {"t": 8.50, "id": "tick"},
    {"t": 10.00, "id": "whoosh"},
    {"t": 10.50, "id": "bell"},
    {"t": 11.00, "id": "stats"},
    {"t": 13.50, "id": "riser"},
    {"t": 14.00, "id": "bell_low"},
    {"t": 16.00, "id": "resolve"},
]
