"""
Sales film drawn per frame from the playhead.

Same idea as the prompt pack: one timeline, a canvas that evaluates
keyframes at time t, then ffmpeg. No After Effects file.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from functools import lru_cache
from pathlib import Path

import cv2
import numpy as np
from PIL import Image, ImageDraw, ImageFont

from timeline import BEATS, DURATION, FPS, HEIGHT, WIDTH

ROOT = Path(__file__).resolve().parents[1]
PROMO = Path(__file__).resolve().parent
OUT = PROMO / "out"

PLATES = {
    "facade": Path(r"C:\Users\solan\Documents\Codex\2026-10-18\c\outputs\Blender\Fachada_Cycles.png"),
    "sala": Path(r"C:\Users\solan\Documents\Codex\2026-10-18\c\outputs\Blender\Interior_Cycles.png"),
    "cocina": ROOT / "Captures" / "Video_Scout_Piso2.png",
    "night": ROOT / "Captures" / "Presentacion_Noche_Fachada.png",
}

FONT_DIR = Path(r"C:\Windows\Fonts")
GEORGIA_B = str(FONT_DIR / "georgiab.ttf")
GEORGIA_I = str(FONT_DIR / "georgiai.ttf")
SEGOE = str(FONT_DIR / "segoeui.ttf")
SEGOE_L = str(FONT_DIR / "segoeuil.ttf")
BAHN = str(FONT_DIR / "bahnschrift.ttf")

INK = np.array([16, 14, 12], dtype=np.float32)  # BGR
CREAM = (246, 240, 232, 255)
CREAM_SOFT = (246, 240, 232, 210)
BRONZE = (212, 168, 122, 255)
BRONZE_DIM = (186, 146, 108, 255)


def clamp01(t: float) -> float:
    return 0.0 if t < 0 else 1.0 if t > 1 else t


def seg(t: float, t0: float, t1: float) -> float:
    if t1 <= t0:
        return 1.0 if t >= t1 else 0.0
    return clamp01((t - t0) / (t1 - t0))


def ease_out_cubic(t: float) -> float:
    t = clamp01(t)
    u = 1 - t
    return 1 - u * u * u


def ease_out_expo(t: float) -> float:
    t = clamp01(t)
    return 1.0 if t >= 1 else 1 - 2 ** (-10 * t)


def ease_in_out(t: float) -> float:
    t = clamp01(t)
    return t * t * (3 - 2 * t)


def ease_out_back(t: float) -> float:
    t = clamp01(t)
    c1 = 1.70158
    c3 = c1 + 1
    return 1 + c3 * (t - 1) ** 3 + c1 * (t - 1) ** 2


def lerp(a: float, b: float, t: float) -> float:
    return a + (b - a) * t


@lru_cache(maxsize=32)
def font(path: str, size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(path, size)


@lru_cache(maxsize=400)
def text_sprite(text: str, path: str, size: int, fill: tuple, tracking: int) -> np.ndarray:
    face = font(path, size)
    ascent, descent = face.getmetrics()
    if not text:
        return np.zeros((4, 4, 4), dtype=np.uint8)
    widths = [face.getlength(ch) for ch in text]
    width = int(sum(widths) + tracking * (len(text) - 1) + 8)
    height = ascent + descent + 8
    image = Image.new("RGBA", (max(width, 1), height), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    x = 2
    for ch, cw in zip(text, widths):
        draw.text((x, 2), ch, font=face, fill=fill)
        x += cw + tracking
    return np.asarray(image)


def blit(dst: np.ndarray, sprite: np.ndarray, x: float, y: float, opacity: float = 1.0) -> None:
    if opacity <= 0.004 or sprite.size == 0:
        return
    h, w = sprite.shape[:2]
    x0, y0 = int(round(x)), int(round(y))
    x1, y1 = x0 + w, y0 + h
    cx0, cy0 = max(0, x0), max(0, y0)
    cx1, cy1 = min(dst.shape[1], x1), min(dst.shape[0], y1)
    if cx0 >= cx1 or cy0 >= cy1:
        return
    sx0, sy0 = cx0 - x0, cy0 - y0
    crop = sprite[sy0 : sy0 + (cy1 - cy0), sx0 : sx0 + (cx1 - cx0)]
    alpha = crop[:, :, 3:4].astype(np.float32) * (opacity / 255.0)
    rgb = crop[:, :, :3][:, :, ::-1].astype(np.float32)
    roi = dst[cy0:cy1, cx0:cx1].astype(np.float32)
    dst[cy0:cy1, cx0:cx1] = np.clip(roi * (1 - alpha) + rgb * alpha, 0, 255).astype(np.uint8)


def text_size(sprite: np.ndarray) -> tuple[int, int]:
    return sprite.shape[1], sprite.shape[0]


def grade(bgr: np.ndarray, contrast: float, warmth: float, lift: float) -> np.ndarray:
    f = bgr.astype(np.float32)
    f = (f - 128.0) * contrast + 128.0 + lift
    f[:, :, 2] += warmth
    f[:, :, 0] -= warmth * 0.45
    return np.clip(f, 0, 255).astype(np.uint8)


def load_plate(path: Path, contrast: float, warmth: float, lift: float) -> np.ndarray:
    image = cv2.imread(str(path), cv2.IMREAD_COLOR)
    if image is None:
        raise FileNotFoundError(path)
    h, w = image.shape[:2]
    longest = max(h, w)
    if longest > 2400:
        scale = 2400 / longest
        image = cv2.resize(image, (int(w * scale), int(h * scale)), interpolation=cv2.INTER_AREA)
    return grade(image, contrast, warmth, lift)


def cover(image: np.ndarray, zoom: float, fx: float, fy: float) -> np.ndarray:
    h, w = image.shape[:2]
    aspect = WIDTH / HEIGHT
    if w / h > aspect:
        base_h = h
        base_w = int(round(h * aspect))
    else:
        base_w = w
        base_h = int(round(w / aspect))
    vw = max(2, int(base_w / zoom))
    vh = max(2, int(base_h / zoom))
    vw = min(vw, w)
    vh = min(vh, h)
    cx = int(fx * w)
    cy = int(fy * h)
    x0 = int(np.clip(cx - vw // 2, 0, w - vw))
    y0 = int(np.clip(cy - vh // 2, 0, h - vh))
    crop = image[y0 : y0 + vh, x0 : x0 + vw]
    interp = cv2.INTER_AREA if (vw > WIDTH and vh > HEIGHT) else cv2.INTER_LINEAR
    return cv2.resize(crop, (WIDTH, HEIGHT), interpolation=interp)


def mix(a: np.ndarray, b: np.ndarray, alpha: np.ndarray | float) -> np.ndarray:
    aa = a.astype(np.float32)
    bb = b.astype(np.float32)
    return np.clip(aa * (1 - alpha) + bb * alpha, 0, 255).astype(np.uint8)


def wipe_x(a: np.ndarray, b: np.ndarray, p: float, soft: float = 70) -> np.ndarray:
    p = clamp01(p)
    if p <= 0:
        return a
    if p >= 1:
        return b
    edge = p * (WIDTH + soft) - soft
    dist = edge - np.arange(WIDTH, dtype=np.float32)
    alpha = np.clip(dist / soft + 0.5, 0, 1).astype(np.float32)
    return mix(a, b, alpha[None, :, None])


def fade(a: np.ndarray, b: np.ndarray, p: float) -> np.ndarray:
    p = ease_in_out(p)
    if p <= 0:
        return a
    if p >= 1:
        return b
    return mix(a, b, p)


VIGNETTE = None
GRAIN = None
INK_BG = None


def prepare_look() -> None:
    global VIGNETTE, GRAIN, INK_BG
    yy = np.linspace(-1, 1, HEIGHT, dtype=np.float32)[:, None]
    xx = np.linspace(-1, 1, WIDTH, dtype=np.float32)[None, :]
    radius = np.sqrt((xx * 1.05) ** 2 + (yy * 1.15) ** 2)
    VIGNETTE = np.clip(1 - np.clip(radius - 0.55, 0, 1) ** 1.35 * 0.55, 0.45, 1).astype(np.float32)
    rng = np.random.default_rng(4)
    GRAIN = rng.integers(0, 255, (HEIGHT + 40, WIDTH + 40), dtype=np.uint8)
    glow = np.clip(1 - np.sqrt((xx * 1.15) ** 2 + ((yy - 0.08) * 1.05) ** 2), 0, 1) ** 2
    bg = INK + glow[:, :, None] * np.array([14, 26, 42], dtype=np.float32)
    INK_BG = np.clip(bg, 0, 255).astype(np.uint8)


def finish(frame: np.ndarray, t: float) -> np.ndarray:
    f = frame.astype(np.float32)
    f *= VIGNETTE[:, :, None]
    shift = int(t * 17) % 30
    grain = GRAIN[shift : shift + HEIGHT, shift : shift + WIDTH].astype(np.float32)
    f += (grain.astype(np.float32) - 128)[:, :, None] * 0.045
    # Playhead: a 2 px rule that is the comp clock, bronze on ink.
    play = clamp01(t / DURATION)
    x = int(play * WIDTH)
    f[HEIGHT - 4 : HEIGHT - 2, :x, 0] = 122
    f[HEIGHT - 4 : HEIGHT - 2, :x, 1] = 168
    f[HEIGHT - 4 : HEIGHT - 2, :x, 2] = 212
    return np.clip(f, 0, 255).astype(np.uint8)


def scrim(frame: np.ndarray, top: float = 0.0, bottom: float = 0.62) -> np.ndarray:
    f = frame.astype(np.float32)
    if top > 0:
        reach = int(HEIGHT * 0.38)
        ramp = np.linspace(top, 0, reach, dtype=np.float32)[:, None, None]
        f[:reach] *= 1 - ramp
    if bottom > 0:
        start = int(HEIGHT * 0.58)
        ramp = np.linspace(0, bottom, HEIGHT - start, dtype=np.float32)[:, None, None]
        f[start:] *= 1 - ramp
    return np.clip(f, 0, 255).astype(np.uint8)


def rounded_panel(w: int, h: int, rgba: tuple, radius: int = 18) -> np.ndarray:
    image = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle((0, 0, w - 1, h - 1), radius=radius, fill=rgba)
    return np.asarray(image)


class Film:
    def __init__(self) -> None:
        self.facade = load_plate(PLATES["facade"], 1.05, 8, 4)
        self.sala = load_plate(PLATES["sala"], 1.04, 6, 6)
        self.cocina = load_plate(PLATES["cocina"], 1.06, 5, 8)
        self.night = load_plate(PLATES["night"], 1.12, 4, 18)

    def plate(self, name: str, t: float, t0: float, t1: float, z0: float, z1: float, fx: float, fy: float) -> np.ndarray:
        u = ease_in_out(seg(t, t0, t1))
        image = getattr(self, name)
        return cover(image, lerp(z0, z1, u), fx, fy)

    def title_card(self, t: float) -> np.ndarray:
        frame = INK_BG.copy()
        b = BEATS
        # Pen draws a rule, then the wordmark lands. Tracking eases like a text animator.
        line_p = ease_out_expo(seg(t, b["line"], b["line"] + 0.55))
        rule_w = int(lerp(0, 280, line_p))
        y = 430
        x = WIDTH // 2 - rule_w // 2
        frame[y : y + 2, x : x + rule_w] = (122, 168, 212)

        eye_p = ease_out_cubic(seg(t, b["eyebrow"], b["eyebrow"] + 0.45))
        tracking = int(lerp(16, 6, eye_p))
        eye = text_sprite("PROYECTO RESIDENCIAL", SEGOE_L, 22, BRONZE, tracking)
        ew, eh = text_size(eye)
        blit(frame, eye, (WIDTH - ew) / 2, 458, eye_p)

        title_p = ease_out_expo(seg(t, b["title"], b["title"] + 0.5))
        dos = text_sprite("DOS", GEORGIA_B, 148, CREAM, 0)
        dw, dh = text_size(dos)
        blit(frame, dos, (WIDTH - dw) / 2, lerp(560, 508, title_p), title_p)

        sub_p = ease_out_cubic(seg(t, b["title"] + 0.18, b["title"] + 0.7))
        track = int(lerp(22, 8, sub_p))
        word = text_sprite("APARTAMENTOS", GEORGIA_B, 52, CREAM, track)
        ww, wh = text_size(word)
        blit(frame, word, (WIDTH - ww) / 2, 680, sub_p)

        line2 = ease_out_cubic(seg(t, b["subtitle"], b["subtitle"] + 0.4))
        tag = text_sprite("Un edificio. Dos hogares.", GEORGIA_I, 32, CREAM_SOFT, 0)
        tw, th = text_size(tag)
        blit(frame, tag, (WIDTH - tw) / 2, lerp(790, 768, line2), line2)
        return frame

    def label(self, frame: np.ndarray, index: str, name: str, t: float, t0: float) -> None:
        p = ease_out_expo(seg(t, t0, t0 + 0.4))
        x = lerp(48, 88, p)
        idx = text_sprite(index, BAHN, 28, BRONZE, 1)
        blit(frame, idx, x, 78, p)
        iw, _ = text_size(idx)
        title = text_sprite(name, SEGOE_L, 26, CREAM, 4)
        blit(frame, title, x + iw + 18, 80, p)
        # Tick under the index.
        tick_w = int(lerp(0, 42, p))
        yy = 118
        xx = int(x)
        frame[yy : yy + 2, xx : xx + tick_w] = (122, 168, 212)

    def caption(self, frame: np.ndarray, line: str, detail: str, t: float, t0: float) -> None:
        p = ease_out_cubic(seg(t, t0, t0 + 0.45))
        y = lerp(940, 900, p)
        head = text_sprite(line, GEORGIA_I, 40, CREAM, 0)
        blit(frame, head, 88, y, p)
        sub = text_sprite(detail, SEGOE_L, 22, BRONZE_DIM, 1)
        blit(frame, sub, 90, y + 58, p * 0.95)

    def chip(self, frame: np.ndarray, copy: str, t: float, t0: float, y: float) -> None:
        p = ease_out_back(seg(t, t0, t0 + 0.42))
        opacity = ease_out_cubic(seg(t, t0, t0 + 0.28))
        sprite = text_sprite(copy, SEGOE, 22, CREAM, 0)
        tw, th = text_size(sprite)
        pad_x, pad_y = 22, 12
        panel_w = tw + pad_x * 2
        panel = rounded_panel(panel_w, th + pad_y, (22, 18, 15, 210), 8)
        right = 1832
        x = lerp(right + 48, right - panel_w, min(p, 1))
        blit(frame, panel, x, y, opacity)
        blit(frame, sprite, x + pad_x, y + 4, opacity)

    def stats(self, frame: np.ndarray, t: float) -> None:
        t0 = BEATS["stats"]
        p = ease_out_expo(seg(t, t0, t0 + 0.5))
        if p <= 0:
            return
        panel = rounded_panel(430, 360, (18, 15, 12, 200), 6)
        x = lerp(1980, 1410, p)
        y = 250
        blit(frame, panel, x, y, min(p * 1.4, 1))
        rows = (
            ("2", "NIVELES"),
            ("2", "APARTAMENTOS"),
            ("2", "DORMITORIOS C/U"),
            ("VR", "A ESCALA REAL"),
        )
        for i, (num, label) in enumerate(rows):
            delay = t0 + 0.08 * i
            rp = ease_out_cubic(seg(t, delay, delay + 0.35))
            number = text_sprite(num, BAHN, 40, BRONZE, 0)
            name = text_sprite(label, SEGOE_L, 20, CREAM, 1)
            blit(frame, number, x + 32, y + 32 + i * 82, rp)
            blit(frame, name, x + 128, y + 44 + i * 82, rp)

    def end_card(self, t: float) -> np.ndarray:
        frame = INK_BG.copy()
        b = BEATS["end"]
        p = ease_out_expo(seg(t, b, b + 0.45))
        kicker = text_sprite("VIVIENDA", SEGOE_L, 20, BRONZE, 8)
        kw, _ = text_size(kicker)
        blit(frame, kicker, (WIDTH - kw) / 2, 300, p)

        rule = ease_out_expo(seg(t, b + 0.1, b + 0.5))
        rw = int(lerp(0, 120, rule))
        frame[352 : 354, WIDTH // 2 - rw // 2 : WIDTH // 2 + rw // 2] = (122, 168, 212)

        head_p = ease_out_cubic(seg(t, b + 0.15, b + 0.6))
        head = text_sprite("Visítala a escala real.", GEORGIA_I, 58, CREAM, 0)
        hw, _ = text_size(head)
        blit(frame, head, (WIDTH - hw) / 2, lerp(430, 400, head_p), head_p)

        sub_p = ease_out_cubic(seg(t, b + 0.4, b + 0.85))
        sub = text_sprite("Recorrido en realidad virtual", SEGOE_L, 28, CREAM_SOFT, 1)
        sw, _ = text_size(sub)
        blit(frame, sub, (WIDTH - sw) / 2, 500, sub_p)

        foot_p = ease_out_cubic(seg(t, b + 0.65, b + 1.05))
        foot = text_sprite("Apartamento 1    ·    Apartamento 2", SEGOE, 22, BRONZE_DIM, 1)
        fw, _ = text_size(foot)
        blit(frame, foot, (WIDTH - fw) / 2, 620, foot_p)
        return frame

    def facade_shot(self, t: float) -> np.ndarray:
        frame = self.plate("facade", t, BEATS["facade"], BEATS["sala"], 1.02, 1.10, 0.56, 0.46)
        frame = scrim(frame, top=0.28, bottom=0.72)
        self.label(frame, "01", "FACHADA", t, BEATS["facade_label"])
        self.caption(
            frame,
            "Piedra, madera y vidrio.",
            "Dos niveles  ·  Dos hogares",
            t,
            BEATS["facade_line"],
        )
        return frame

    def sala_shot(self, t: float) -> np.ndarray:
        frame = self.plate("sala", t, BEATS["sala"], BEATS["cocina"], 1.04, 1.12, 0.34, 0.58)
        frame = scrim(frame, top=0.2, bottom=0.55)
        self.label(frame, "02", "SALA", t, BEATS["sala_label"])
        self.caption(frame, "Luz de día. Nogal y lino.", "Sala, cocina y dos dormitorios", t, BEATS["sala_label"] + 0.15)
        self.chip(frame, "Sala", t, BEATS["chip0"], 186)
        self.chip(frame, "Cocina", t, BEATS["chip1"], 258)
        self.chip(frame, "2 dormitorios", t, BEATS["chip2"], 330)
        return frame

    def cocina_shot(self, t: float) -> np.ndarray:
        frame = self.plate("cocina", t, BEATS["cocina"], BEATS["night"], 1.06, 1.14, 0.58, 0.52)
        frame = scrim(frame, top=0.5, bottom=0.58)
        self.label(frame, "03", "COCINA ABIERTA", t, BEATS["cocina_label"])
        self.caption(frame, "Nogal y microcemento.", "La sala sigue de un lado", t, BEATS["cocina_label"] + 0.12)
        self.stats(frame, t)
        return frame

    def night_shot(self, t: float) -> np.ndarray:
        frame = self.plate("night", t, BEATS["night"], BEATS["end"], 1.04, 1.09, 0.50, 0.46)
        frame = scrim(frame, top=0.35, bottom=0.7)
        self.label(frame, "04", "DÍA Y NOCHE", t, BEATS["night_title"])
        self.caption(
            frame,
            "El recorrido no se apaga.",
            "Misma casa  ·  Otra luz",
            t,
            BEATS["night_title"] + 0.12,
        )
        return frame

    def frame(self, t: float) -> np.ndarray:
        b = BEATS
        if t < b["facade"]:
            image = self.title_card(t)
        elif t < b["sala"]:
            image = wipe_x(self.title_card(b["facade"] - 0.02), self.facade_shot(t), seg(t, b["facade"], b["facade"] + 0.55))
        elif t < b["cocina"]:
            held = self.facade_shot(b["sala"] - 0.02)
            image = wipe_x(held, self.sala_shot(t), seg(t, b["sala"], b["sala"] + 0.5))
        elif t < b["night"]:
            held = self.sala_shot(b["cocina"] - 0.02)
            image = wipe_x(held, self.cocina_shot(t), seg(t, b["cocina"], b["cocina"] + 0.5))
        elif t < b["end"]:
            held = self.cocina_shot(b["night"] - 0.02)
            image = fade(held, self.night_shot(t), seg(t, b["night"], b["night"] + 0.55))
        else:
            held = self.night_shot(b["end"] - 0.02)
            image = fade(held, self.end_card(t), seg(t, b["end"], b["end"] + 0.5))
        return finish(image, t)


def render_stills(film: Film, folder: Path) -> None:
    folder.mkdir(parents=True, exist_ok=True)
    times = [1.7, 2.6, 4.8, 8.2, 11.8, 14.6, 17.2]
    for t in times:
        frame = film.frame(t)
        path = folder / f"still_{t:.1f}.png"
        cv2.imwrite(str(path), frame)
        print(path)


def render_movie(film: Film, wav: Path, mp4: Path) -> None:
    mp4.parent.mkdir(parents=True, exist_ok=True)
    cmd = [
        "ffmpeg",
        "-y",
        "-f",
        "rawvideo",
        "-pix_fmt",
        "bgr24",
        "-s",
        f"{WIDTH}x{HEIGHT}",
        "-r",
        str(FPS),
        "-i",
        "pipe:0",
        "-i",
        str(wav),
        "-c:v",
        "libx264",
        "-preset",
        "medium",
        "-crf",
        "17",
        "-pix_fmt",
        "yuv420p",
        "-c:a",
        "aac",
        "-b:a",
        "320k",
        "-shortest",
        "-movflags",
        "+faststart",
        str(mp4),
    ]
    log_path = OUT / "ffmpeg.log"
    log = open(log_path, "w", encoding="utf-8")
    proc = subprocess.Popen(cmd, stdin=subprocess.PIPE, stderr=log)
    assert proc.stdin is not None
    n = int(round(DURATION * FPS))
    try:
        for i in range(n):
            frame = np.ascontiguousarray(film.frame(i / FPS))
            proc.stdin.write(frame.tobytes())
            if i % 30 == 0:
                print(f"frame {i}/{n}", flush=True)
        proc.stdin.close()
        proc.stdin = None
    except Exception:
        proc.kill()
        raise
    finally:
        if proc.stdin is not None:
            proc.stdin.close()
    code = proc.wait()
    log.close()
    if code != 0:
        err = log_path.read_text(encoding="utf-8", errors="replace")
        print(err[-2000:], file=sys.stderr)
        raise SystemExit(code)
    print(mp4)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--stills", action="store_true")
    parser.add_argument("--movie", action="store_true")
    args = parser.parse_args()
    prepare_look()
    film = Film()
    if args.stills or not args.movie:
        render_stills(film, OUT / "stills")
    if args.movie:
        from sound import render_wav

        wav = OUT / "score.wav"
        render_wav(str(wav))
        render_movie(film, wav, OUT / "Vivienda_Promo.mp4")


if __name__ == "__main__":
    main()
