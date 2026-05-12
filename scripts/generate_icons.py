"""Generate SY-FTP icons from scripts/icon.svg.

Pipeline: SVG (source of truth) -> cairosvg raster -> PNG set -> .ico / .icns.

Prerequisites:
    pip install -r scripts/requirements.txt

Run from repo root:
    python scripts/generate_icons.py
"""
from __future__ import annotations

import io
from pathlib import Path

import cairosvg
from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
SVG_SRC = ROOT / "scripts" / "icon.svg"
OUT_ASSETS = ROOT / "Assets"
OUT_LINUX = OUT_ASSETS / "icons"

SIZES = [16, 24, 32, 48, 64, 128, 256, 512, 1024]
ICO_SIZES = [(256, 256), (128, 128), (64, 64), (48, 48),
             (32, 32), (24, 24), (16, 16)]


def render(size: int) -> Image.Image:
    """Rasterize the source SVG to a square RGBA PNG at the requested edge size."""
    png_bytes = cairosvg.svg2png(
        url=str(SVG_SRC),
        output_width=size,
        output_height=size,
    )
    return Image.open(io.BytesIO(png_bytes)).convert("RGBA")


def main() -> None:
    if not SVG_SRC.exists():
        raise SystemExit(f"source SVG missing: {SVG_SRC}")

    OUT_ASSETS.mkdir(exist_ok=True)
    OUT_LINUX.mkdir(parents=True, exist_ok=True)

    renders = {s: render(s) for s in SIZES}

    for s in SIZES:
        renders[s].save(OUT_LINUX / f"{s}.png", "PNG")

    renders[256].save(OUT_ASSETS / "sy-ftp.png", "PNG")
    renders[512].save(OUT_ASSETS / "sy-ftp-512.png", "PNG")
    renders[256].save(OUT_ASSETS / "sy-ftp.ico", format="ICO", sizes=ICO_SIZES)
    renders[1024].save(OUT_ASSETS / "sy-ftp.icns", format="ICNS")

    print("Generated:")
    for p in sorted(OUT_ASSETS.rglob("*")):
        if p.is_file():
            print(f"  {p.relative_to(ROOT)}  ({p.stat().st_size:,} bytes)")


if __name__ == "__main__":
    main()
