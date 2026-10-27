"""Score synthesized from timeline.SOUND. No samples, no voice."""

from __future__ import annotations

import numpy as np

from timeline import BPM, DURATION, SOUND

SR = 48000


def _place(buf: np.ndarray, t: float, sig: np.ndarray, pan: float = 0.0) -> None:
    i = int(round(t * SR))
    if i >= len(buf) or len(sig) == 0:
        return
    n = min(len(sig), len(buf) - i)
    left = np.cos((pan + 1) * np.pi / 4)
    right = np.sin((pan + 1) * np.pi / 4)
    buf[i : i + n, 0] += sig[:n] * left
    buf[i : i + n, 1] += sig[:n] * right


def _kick(rng: np.random.Generator) -> np.ndarray:
    dur = 0.22
    tt = np.arange(int(dur * SR)) / SR
    freq = 92 * np.exp(-tt * 22) + 38
    phase = 2 * np.pi * np.cumsum(freq) / SR
    body = np.sin(phase) * np.exp(-tt * 10)
    click = rng.standard_normal(len(tt)) * np.exp(-tt * 180) * 0.15
    return (body + click).astype(np.float32)


def _hat(rng: np.random.Generator, open_hat: bool = False) -> np.ndarray:
    dur = 0.09 if open_hat else 0.035
    tt = np.arange(int(dur * SR)) / SR
    noise = rng.standard_normal(len(tt)).astype(np.float32)
    acc = 0.0
    high = np.empty_like(noise)
    for i, x in enumerate(noise):
        acc += 0.35 * (x - acc)
        high[i] = x - acc
    decay = 28 if open_hat else 70
    return high * np.exp(-tt * decay) * (0.22 if open_hat else 0.16)


def _whoosh(rng: np.random.Generator) -> np.ndarray:
    dur = 0.55
    tt = np.arange(int(dur * SR)) / SR
    noise = rng.standard_normal(len(tt)).astype(np.float32)
    from scipy.signal import lfilter

    band = lfilter([0.2], [1.0, -0.8], noise).astype(np.float32)
    env = np.sin(np.pi * np.clip(tt / dur, 0, 1)) ** 1.4
    return band * env * 0.28


def _tone(freq: float, dur: float, decay: float, amp: float, partial: float = 0.18) -> np.ndarray:
    tt = np.arange(int(dur * SR)) / SR
    sig = np.sin(2 * np.pi * freq * tt)
    sig += partial * np.sin(2 * np.pi * freq * 2 * tt)
    sig += 0.06 * np.sin(2 * np.pi * freq * 3 * tt)
    return (sig * np.exp(-tt * decay) * amp).astype(np.float32)


def _bell(freq: float) -> np.ndarray:
    return _tone(freq, 1.15, 3.2, 0.16, partial=0.35)


def _sum(*parts: np.ndarray) -> np.ndarray:
    n = max(len(p) for p in parts)
    out = np.zeros(n, dtype=np.float32)
    for part in parts:
        out[: len(part)] += part
    return out


def _blip() -> np.ndarray:
    return _tone(880, 0.09, 28, 0.12, partial=0.0)


def _tick() -> np.ndarray:
    return _tone(1560, 0.045, 60, 0.08, partial=0.0)


def _pad(n: int) -> np.ndarray:
    tt = np.arange(n) / SR
    swell = np.clip((tt - 0.3) / 1.6, 0, 1)
    swell *= np.clip((DURATION - tt) / 1.4, 0, 1)
    bed = np.zeros((n, 2), dtype=np.float32)
    voices = (
        (174.61, 0.045, 0.0),
        (220.00, 0.030, 0.15),
        (261.63, 0.022, -0.1),
        (329.63, 0.012, 0.25),
    )
    for freq, amp, detune in voices:
        left = np.sin(2 * np.pi * freq * tt) * swell * amp
        right = np.sin(2 * np.pi * freq * (1 + detune * 0.004) * tt + 0.4) * swell * amp
        bed[:, 0] += left
        bed[:, 1] += right
    noise = np.random.default_rng(7).standard_normal((n, 2)).astype(np.float32)
    from scipy.signal import lfilter

    air = lfilter([0.01], [1.0, -0.99], noise, axis=0).astype(np.float32)
    bed += air * swell[:, None] * 0.015
    return bed


def _bass_line(n: int, drop: float) -> np.ndarray:
    # F G A C, one note per beat, rubbery and short.
    notes = [87.31, 98.00, 110.00, 130.81]
    buf = np.zeros((n, 2), dtype=np.float32)
    beat = 60.0 / BPM
    t = drop
    i = 0
    while t < DURATION - 1.2:
        freq = notes[i % 4]
        sig = _tone(freq, beat * 0.92, 5.5, 0.20, partial=0.08)
        _place(buf, t, sig, pan=-0.05 if i % 2 == 0 else 0.05)
        t += beat
        i += 1
    return buf


def render_wav(path: str) -> None:
    rng = np.random.default_rng(11)
    n = int(DURATION * SR)
    mix = np.zeros((n, 2), dtype=np.float32)
    mix += _pad(n)

    drop = next(e["t"] for e in SOUND if e["id"] == "drop")
    mix += _bass_line(n, drop)

    beat = 60.0 / BPM
    t = drop
    step = 0
    kick = _kick(rng)
    hat = _hat(rng, False)
    oh = _hat(rng, True)
    while t < 15.85:
        _place(mix, t, kick * (0.72 if step % 4 in (0, 2) else 0.28), 0.0)
        if step % 2 == 1:
            _place(mix, t, hat * 0.85, 0.25)
        else:
            _place(mix, t + beat * 0.5, hat * 0.55, -0.2)
        if step % 8 == 4:
            _place(mix, t, oh * 0.7, 0.35)
        t += beat
        step += 1

    bells = {
        "bell": _bell(659.25),
        "bell_low": _bell(440.0),
        "blip": _blip(),
        "tick": _tick(),
        "whoosh": _whoosh(rng),
        "chord_soft": _sum(
            _tone(261.63, 1.6, 1.8, 0.10),
            _tone(329.63, 1.6, 1.8, 0.07),
            _tone(392.0, 1.8, 1.6, 0.06),
        ),
        "stats": _sum(_tone(523.25, 0.7, 4.0, 0.11), _tone(659.25, 0.8, 3.6, 0.08)),
        "riser": _whoosh(np.random.default_rng(3)) * 1.15,
        "resolve": _sum(
            _tone(261.63, 2.4, 1.1, 0.16),
            _tone(329.63, 2.4, 1.15, 0.12),
            _tone(392.00, 2.6, 1.05, 0.12),
            _tone(523.25, 2.2, 1.4, 0.06),
        ),
    }
    pans = {
        "whoosh": -0.15,
        "bell": 0.2,
        "bell_low": -0.1,
        "blip": 0.0,
        "tick": 0.1,
        "chord_soft": 0.0,
        "stats": 0.15,
        "riser": 0.0,
        "resolve": 0.0,
    }
    for event in SOUND:
        if event["id"] == "drop":
            continue
        sig = bells[event["id"]]
        _place(mix, event["t"], sig, pans.get(event["id"], 0.0))

    # Soft clip, then a ceiling at -1 dBFS.
    mix = np.tanh(mix * 1.35).astype(np.float32)
    peak = float(np.max(np.abs(mix)))
    ceiling = 10 ** (-1 / 20)
    if peak > 1e-6:
        mix *= min(1.0, ceiling / peak)

    pcm = np.clip(mix * 32767.0, -32767, 32767).astype(np.int16)
    import wave

    with wave.open(path, "wb") as wf:
        wf.setnchannels(2)
        wf.setsampwidth(2)
        wf.setframerate(SR)
        wf.writeframes(pcm.tobytes())

    hop = SR // 2
    print("loudness per 0.5s (dBFS peak of mean-square)")
    for i in range(0, n, hop):
        chunk = mix[i : i + hop]
        if len(chunk) == 0:
            continue
        rms = float(np.sqrt(np.mean(chunk**2) + 1e-12))
        db = 20 * np.log10(rms)
        print(f"  {i / SR:5.2f}s  {db:6.1f} dB")


if __name__ == "__main__":
    render_wav("score.wav")
