"""Generate SY-FTP icons for Windows (.ico), macOS (.icns), and Linux (PNG set).

Design: glassmorphism indigo — matches the app default accent (#4050B5).
Gradient body (light indigo -> deep indigo), frosted top highlight, inner rim,
soft ambient glow, and two white transfer arrows with fully rounded corners
(blur + smooth-threshold applied on the arrow alpha channel).

Run from repo root:
    python scripts/generate_icons.py
"""
from __future__ import annotations
from pathlib import Path
from PIL import Image, ImageDraw, ImageFilter, ImageChops

ROOT = Path(__file__).resolve().parent.parent
OUT_ASSETS = ROOT / "Assets"
OUT_LINUX = OUT_ASSETS / "icons"
OUT_ASSETS.mkdir(exist_ok=True)
OUT_LINUX.mkdir(parents=True, exist_ok=True)

# Palette tuned around the app's default accent #4050B5 (Indigo).
C_TL = (104, 120, 201)  # #6878C9 — lighter indigo (top-left)
C_BR = (38, 49, 122)    # #26317A — deeper indigo (bottom-right)
GLOW = (64, 80, 181)    # #4050B5 — accent base (ambient halo)
FG = (255, 255, 255)


def lerp(a, b, t):
    return int(a * (1 - t) + b * t)


def diagonal_gradient(size: int, c1, c2) -> Image.Image:
    img = Image.new("RGBA", (size, size))
    px = img.load()
    denom = 2 * (size - 1) or 1
    for y in range(size):
        for x in range(size):
            t = (x + y) / denom
            px[x, y] = (lerp(c1[0], c2[0], t),
                        lerp(c1[1], c2[1], t),
                        lerp(c1[2], c2[2], t),
                        255)
    return img


def round_alpha(alpha: Image.Image, blur_radius: float,
                center: int = 128, spread: int = 22) -> Image.Image:
    """Smooth a hard-edged alpha mask into one with rounded, antialiased edges.

    Gaussian blur converts every sharp corner into a soft ramp; a smooth
    threshold then snaps it back to a crisp but *curved* edge. The smaller the
    threshold `spread`, the harder the final edge.
    """
    blurred = alpha.filter(ImageFilter.GaussianBlur(radius=blur_radius))
    scale = 255 / (2 * spread)

    def ramp(v):
        return int(max(0, min(255, (v - (center - spread)) * scale)))

    return blurred.point(ramp)


def build_body(S: int) -> Image.Image:
    radius = int(S * 0.23)

    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        [(0, 0), (S - 1, S - 1)], radius=radius, fill=255)

    # Gradient body.
    body = diagonal_gradient(S, C_TL, C_BR)
    body.putalpha(mask)

    # Frosted glass highlight.
    glass = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    gd = ImageDraw.Draw(glass)
    gd.ellipse(
        [(int(-S * 0.1), int(-S * 0.55)),
         (int(S * 1.1), int(S * 0.55))],
        fill=(255, 255, 255, 72),
    )
    gd.ellipse(
        [(int(S * 0.08), int(S * 0.06)),
         (int(S * 0.55), int(S * 0.36))],
        fill=(255, 255, 255, 55),
    )
    glass = glass.filter(ImageFilter.GaussianBlur(radius=S * 0.04))
    glass.putalpha(ImageChops.multiply(glass.split()[3], mask))
    body = Image.alpha_composite(body, glass)

    # Inner rim.
    rim = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ImageDraw.Draw(rim).rounded_rectangle(
        [(1, 1), (S - 2, S - 2)],
        radius=radius - 1,
        outline=(255, 255, 255, 90),
        width=max(2, int(S * 0.006)),
    )
    rim.putalpha(ImageChops.multiply(rim.split()[3], mask))
    body = Image.alpha_composite(body, rim)

    # ── Arrows: draw hard-edged first, then round every corner. ──────────
    sharp = Image.new("L", (S, S), 0)
    sd = ImageDraw.Draw(sharp)
    cx = S // 2
    cy = S // 2
    arrow_h = int(S * 0.55)
    stem_w = int(S * 0.098)
    # Broader + shorter head makes the tip less acute → rounds cleaner.
    head_w = int(S * 0.28)
    head_h = int(S * 0.22)
    gap = int(S * 0.19)

    # Up arrow (left, points up).
    up_cx = cx - gap // 2 - stem_w // 2 - int(S * 0.015)
    up_top = cy - arrow_h // 2
    up_bot = cy + arrow_h // 2
    sd.rectangle(
        [(up_cx - stem_w // 2, up_top + head_h - int(S * 0.01)),
         (up_cx + stem_w // 2, up_bot)],
        fill=255,
    )
    sd.polygon(
        [(up_cx, up_top),
         (up_cx - head_w // 2, up_top + head_h),
         (up_cx + head_w // 2, up_top + head_h)],
        fill=255,
    )

    # Down arrow (right, points down).
    dn_cx = cx + gap // 2 + stem_w // 2 + int(S * 0.015)
    dn_top = cy - arrow_h // 2
    dn_bot = cy + arrow_h // 2
    sd.rectangle(
        [(dn_cx - stem_w // 2, dn_top),
         (dn_cx + stem_w // 2, dn_bot - head_h + int(S * 0.01))],
        fill=255,
    )
    sd.polygon(
        [(dn_cx, dn_bot),
         (dn_cx - head_w // 2, dn_bot - head_h),
         (dn_cx + head_w // 2, dn_bot - head_h)],
        fill=255,
    )

    # Round all corners of the arrow silhouette.
    rounded = round_alpha(sharp, blur_radius=S * 0.022, center=128, spread=24)
    arrows = Image.new("RGBA", (S, S), FG + (0,))
    arrows.putalpha(rounded)

    # Soft tinted shadow behind the arrows for depth.
    shadow_alpha = rounded.point(lambda a: int(a * 0.55))
    shadow = Image.new("RGBA", arrows.size, (10, 14, 70, 0))
    shadow.putalpha(shadow_alpha)
    shadow = shadow.filter(ImageFilter.GaussianBlur(radius=S * 0.018))
    offset_shadow = Image.new("RGBA", arrows.size, (0, 0, 0, 0))
    offset_shadow.alpha_composite(shadow, (int(S * 0.008), int(S * 0.014)))

    body = Image.alpha_composite(body, offset_shadow)
    body = Image.alpha_composite(body, arrows)

    return body


def render(size: int) -> Image.Image:
    scale = 4
    S = size * scale
    pad = int(S * 0.08)
    C = S + pad * 2

    canvas = Image.new("RGBA", (C, C), (0, 0, 0, 0))

    # Ambient outer glow.
    radius = int(S * 0.23)
    glow_src = Image.new("RGBA", (C, C), (0, 0, 0, 0))
    ImageDraw.Draw(glow_src).rounded_rectangle(
        [(pad, pad), (pad + S - 1, pad + S - 1)],
        radius=radius, fill=GLOW + (150,),
    )
    glow = glow_src.filter(ImageFilter.GaussianBlur(radius=S * 0.06))
    canvas = Image.alpha_composite(canvas, glow)

    body = build_body(S)
    canvas.alpha_composite(body, (pad, pad))

    final_padded = size + 2 * max(1, pad // scale)
    scaled = canvas.resize((final_padded, final_padded), Image.LANCZOS)
    offset = (scaled.width - size) // 2
    return scaled.crop((offset, offset, offset + size, offset + size))


def main():
    sizes = [16, 24, 32, 48, 64, 128, 256, 512, 1024]
    renders = {s: render(s) for s in sizes}

    for s in sizes:
        renders[s].save(OUT_LINUX / f"{s}.png", "PNG")

    renders[256].save(OUT_ASSETS / "sy-ftp.png", "PNG")
    renders[512].save(OUT_ASSETS / "sy-ftp-512.png", "PNG")

    ico_sizes = [(256, 256), (128, 128), (64, 64), (48, 48), (32, 32), (24, 24), (16, 16)]
    renders[256].save(OUT_ASSETS / "sy-ftp.ico", format="ICO", sizes=ico_sizes)

    renders[1024].save(OUT_ASSETS / "sy-ftp.icns", format="ICNS")

    print("Generated:")
    for p in sorted(OUT_ASSETS.rglob("*")):
        if p.is_file():
            print(f"  {p.relative_to(ROOT)}  ({p.stat().st_size:,} bytes)")


if __name__ == "__main__":
    main()
