# SY-FTP Icon Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the dated indigo-and-arrows app icon with a Big Sur-style folder icon on a violet→pink gradient tile, carrying a small white transfer badge in the bottom-right.

**Architecture:** Introduce a hand-authored SVG (`scripts/icon.svg`) as the single source of truth. Replace the existing Pillow-only drawing script with a thin rasterizer (`scripts/generate_icons.py`) that uses `cairosvg` to render the SVG at each target size, then packs the results into `.ico` and `.icns` via Pillow.

**Tech Stack:** Python 3, `cairosvg` (adds Cairo native dep), Pillow, plain SVG 1.1 (gradients + `feDropShadow` + `feGaussianBlur`).

**Spec:** [`docs/superpowers/specs/2026-05-12-icon-redesign-design.md`](../specs/2026-05-12-icon-redesign-design.md)

---

## File Structure

| File | Responsibility | Change |
|------|----------------|--------|
| `scripts/icon.svg` | Vector source of truth for the icon design | **Create** |
| `scripts/generate_icons.py` | Rasterize SVG to per-size PNG, pack `.ico` and `.icns` | **Rewrite** (replaces all Pillow drawing logic) |
| `scripts/requirements.txt` | Pin the Python deps the script needs | **Create** |
| `Assets/sy-ftp.ico`, `sy-ftp.icns`, `sy-ftp.png`, `sy-ftp-512.png` | Final app icons referenced by `sy-ftp.csproj` | **Regenerate** |
| `Assets/icons/{16..1024}.png` | Linux per-size PNG set | **Regenerate** |

No `.csproj`, `.axaml`, or C# code is modified — file paths are unchanged, so the Avalonia project picks up the new artwork automatically.

---

## Task 1: Add Python dependencies

**Files:**
- Create: `scripts/requirements.txt`

- [ ] **Step 1: Create `scripts/requirements.txt`**

```
cairosvg>=2.7
Pillow>=10.0
```

- [ ] **Step 2: Install the dependencies**

Run (from repo root):
```bash
python -m pip install -r scripts/requirements.txt
```

Expected: installs `cairosvg`, `cairocffi`, `Pillow`, and their transitive deps. On Windows, if install fails with `OSError: no library called "cairo-2" was found`, install the GTK / Cairo runtime (e.g. `pipx install cairosvg` bundles it, or install `https://github.com/tschoonj/GTK-for-Windows-Runtime-Environment-Installer/releases`). Document the fix only if it triggers — do not pre-install.

- [ ] **Step 3: Verify cairosvg import works**

Run:
```bash
python -c "import cairosvg; print(cairosvg.__version__)"
```

Expected: prints a version number (e.g. `2.7.1`), no exception.

- [ ] **Step 4: Commit**

```bash
git add scripts/requirements.txt
git commit -m "chore: pin icon-generation deps (cairosvg, Pillow)"
```

---

## Task 2: Author the source SVG

**Files:**
- Create: `scripts/icon.svg`

- [ ] **Step 1: Write `scripts/icon.svg`**

Full file contents (1024×1024 artboard; all geometry values are in SVG user units):

```xml
<?xml version="1.0" encoding="UTF-8"?>
<!-- SY-FTP app icon. Source of truth; edit here, then re-run scripts/generate_icons.py. -->
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 1024" width="1024" height="1024">
  <defs>
    <!-- Tile gradient: violet (top-left) → pink (bottom-right). -->
    <linearGradient id="tileGrad" x1="0" y1="0" x2="1024" y2="1024" gradientUnits="userSpaceOnUse">
      <stop offset="0" stop-color="#7C3AED"/>
      <stop offset="1" stop-color="#EC4899"/>
    </linearGradient>

    <!-- Folder gradient: amber-300 → amber-500, top to bottom of folder region. -->
    <linearGradient id="folderGrad" x1="0" y1="290" x2="0" y2="775" gradientUnits="userSpaceOnUse">
      <stop offset="0" stop-color="#FCD34D"/>
      <stop offset="1" stop-color="#F59E0B"/>
    </linearGradient>

    <!-- Tinted drop shadow behind the folder (violet-950, not black). -->
    <filter id="folderShadow" x="-20%" y="-20%" width="140%" height="140%">
      <feDropShadow dx="0" dy="22" stdDeviation="18" flood-color="#1E0B3C" flood-opacity="0.4"/>
    </filter>

    <!-- Drop shadow behind the badge circle. -->
    <filter id="badgeShadow" x="-30%" y="-30%" width="160%" height="160%">
      <feDropShadow dx="0" dy="14" stdDeviation="14" flood-color="#1E0B3C" flood-opacity="0.32"/>
    </filter>

    <!-- Outer ambient glow behind the tile (lives in the 40px artboard padding). -->
    <filter id="tileGlow" x="-10%" y="-10%" width="120%" height="120%">
      <feGaussianBlur in="SourceGraphic" stdDeviation="28"/>
    </filter>

    <!-- Clip everything drawn on top of the tile to the tile's rounded-square. -->
    <clipPath id="tileClip">
      <rect x="40" y="40" width="944" height="944" rx="217" ry="217"/>
    </clipPath>
  </defs>

  <!-- 0. Outer ambient glow: a blurred, slightly expanded copy of the tile shape. -->
  <rect x="24" y="32" width="976" height="976" rx="233" ry="233"
        fill="url(#tileGrad)" fill-opacity="0.55" filter="url(#tileGlow)"/>

  <!-- 1. Base tile (rounded squircle) -->
  <rect x="40" y="40" width="944" height="944" rx="217" ry="217" fill="url(#tileGrad)"/>

  <!-- 2. Inner rim: faint white stroke inset by 1px for a glass-edge feel. -->
  <rect x="42" y="42" width="940" height="940" rx="215" ry="215"
        fill="none" stroke="#FFFFFF" stroke-opacity="0.2" stroke-width="2"/>

  <!-- 3. Soft frosted highlight across the top of the tile. -->
  <g clip-path="url(#tileClip)">
    <ellipse cx="512" cy="60" rx="560" ry="230" fill="#FFFFFF" fill-opacity="0.12"/>
  </g>

  <!-- 4. Folder (back panel with tab + front panel), tinted drop shadow. -->
  <g clip-path="url(#tileClip)" filter="url(#folderShadow)">
    <!-- Back panel: rounded rectangle with a trapezoidal tab on top-left. -->
    <path d="M 212 290
             L 340 290
             L 380 330
             L 822 330
             Q 842 330 842 350
             L 842 740
             Q 842 760 822 760
             L 212 760
             Q 192 760 192 740
             L 192 310
             Q 192 290 212 290 Z"
          fill="url(#folderGrad)"/>
    <!-- Front panel: slightly larger rounded rectangle in front of back. -->
    <rect x="180" y="370" width="674" height="405" rx="28" ry="28" fill="url(#folderGrad)"/>
  </g>

  <!-- 5. Front-panel top highlight (drawn after shadow so it stays crisp). -->
  <rect x="186" y="370" width="662" height="3" rx="1.5" ry="1.5"
        fill="#FFFFFF" fill-opacity="0.32"/>

  <!-- 6. Transfer badge: white circle pinned to bottom-right with drop shadow. -->
  <g filter="url(#badgeShadow)">
    <circle cx="799" cy="799" r="194" fill="#FFFFFF"/>
  </g>
  <!-- Subtle inner border on the badge -->
  <circle cx="799" cy="799" r="192" fill="none"
          stroke="#1E0B3C" stroke-opacity="0.08" stroke-width="2"/>

  <!-- 7. Up arrow inside badge, at center (719.5, 799). -->
  <!--    tip → right-head corner → stem right → stem right bottom → stem left bottom → stem left → left-head corner → back to tip -->
  <path d="M 719.5 692.5
           L 764 777.5
           L 734.5 777.5
           L 734.5 905.5
           L 704.5 905.5
           L 704.5 777.5
           L 675 777.5 Z"
        fill="url(#tileGrad)"
        stroke="url(#tileGrad)" stroke-width="6" stroke-linejoin="round"/>

  <!-- 8. Down arrow inside badge, at center (878.5, 799). -->
  <path d="M 878.5 905.5
           L 834 820.5
           L 863.5 820.5
           L 863.5 692.5
           L 893.5 692.5
           L 893.5 820.5
           L 923 820.5 Z"
        fill="url(#tileGrad)"
        stroke="url(#tileGrad)" stroke-width="6" stroke-linejoin="round"/>
</svg>
```

Design key (reference only — do not add to the file):

| Layer | Source section |
|-------|----------------|
| 1. Base tile | Spec §2.1 |
| 2. Inner rim | Spec §2.1 |
| 3. Highlight | Spec §2.1 (interpreted: `glass-edge feel` + subtle top gloss) |
| 4. Folder back + front | Spec §2.2 |
| 5. Top highlight | Spec §2.2 |
| 6. Badge circle + shadow | Spec §2.3 |
| 7–8. Arrows | Spec §2.3 |

- [ ] **Step 2: Sanity-check the SVG renders at all**

Run:
```bash
python -c "import cairosvg; cairosvg.svg2png(url='scripts/icon.svg', output_width=256, output_height=256, write_to='/tmp/icon-preview.png')"
```

Expected: creates `/tmp/icon-preview.png` (256×256) with no exception. Open it in an image viewer and confirm:
- you see a rounded-corner violet→pink tile filling the frame
- a golden/amber folder with a visible tab is centered
- a white circle with two parallel arrows sits in the bottom-right
- no obvious clipping, no black background, no transparent gaps inside the tile

If on Windows, use `%TEMP%\icon-preview.png` instead of `/tmp/icon-preview.png`.

- [ ] **Step 3: Commit the SVG**

```bash
git add scripts/icon.svg
git commit -m "feat(icons): add SVG source for Big Sur-style folder icon"
```

---

## Task 3: Rewrite the icon generator to consume the SVG

**Files:**
- Modify: `scripts/generate_icons.py` (replace all contents)

- [ ] **Step 1: Overwrite `scripts/generate_icons.py` with the new rasterizer**

```python
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
```

- [ ] **Step 2: Run the generator**

```bash
python scripts/generate_icons.py
```

Expected output (file sizes will vary):
```
Generated:
  Assets/icons/1024.png  (~220,000 bytes)
  Assets/icons/128.png   (~9,000 bytes)
  Assets/icons/16.png    (~600 bytes)
  ... (all sizes listed)
  Assets/sy-ftp-512.png  (~80,000 bytes)
  Assets/sy-ftp.icns     (~350,000 bytes)
  Assets/sy-ftp.ico      (~140,000 bytes)
  Assets/sy-ftp.png      (~25,000 bytes)
```

No Python exceptions.

- [ ] **Step 3: Programmatic file-set verification**

Run (single command — confirms every expected file exists and is non-empty):
```bash
python -c "
from pathlib import Path
root = Path('Assets')
expected = [
    'sy-ftp.ico', 'sy-ftp.icns', 'sy-ftp.png', 'sy-ftp-512.png',
    *(f'icons/{s}.png' for s in (16, 24, 32, 48, 64, 128, 256, 512, 1024)),
]
for rel in expected:
    p = root / rel
    assert p.exists(), f'missing: {p}'
    assert p.stat().st_size > 0, f'empty: {p}'
    print(f'  OK {p} ({p.stat().st_size:,} bytes)')
print('all icon artifacts present')
"
```

Expected: every file prints `OK ...`, final line `all icon artifacts present`. Any `AssertionError` means the generator failed silently on that file — investigate before moving on.

- [ ] **Step 4: Visual inspection at critical sizes**

Open `Assets/icons/16.png`, `Assets/icons/32.png`, and `Assets/icons/256.png` in an image viewer (or Explorer thumbnail view).

Acceptance checklist:
- **16 px:** the folder silhouette is still recognizable as a folder (tab visible, even if badge becomes a blob). Violet→pink gradient is visible on the tile.
- **32 px:** badge circle is distinct from the folder, two arrows legible as "up and down" shapes.
- **256 px:** folder has clean edges, tab is clearly visible, front-panel top highlight is a crisp line, badge arrows are clean without ragged corners.

If 16/24 are unreadable, STOP — do not commit. Note the issue and raise with the user; do not silently tweak the SVG without going back through the brainstorming/design loop.

- [ ] **Step 5: Commit the regenerated assets + new script**

```bash
git add scripts/generate_icons.py Assets/
git commit -m "feat(icons): regenerate app icons from SVG pipeline"
```

Expected: commit touches `scripts/generate_icons.py` plus every file listed in the verification step. `git status` clean afterwards.

---

## Task 4: Build and visually verify in-app

**Files:** (no source edits — sanity pass only)

- [ ] **Step 1: Stop any running app instance**

```bash
taskkill //F //IM sy-ftp.exe //T 2>/dev/null || true
```

(Per `CLAUDE.md` — avoids file-lock errors on rebuild.)

- [ ] **Step 2: Build the project**

```bash
dotnet build
```

Expected: `Build succeeded` with 0 errors. Warnings unchanged from baseline.

- [ ] **Step 3: Run the app**

```bash
dotnet run
```

Expected: app window opens. Verify:
- **Window titlebar icon** shows the new violet→pink tile with amber folder.
- **Taskbar icon** (hover the app in the Windows taskbar) shows the new icon cleanly.
- No regression in app startup behavior.

- [ ] **Step 4: Close the app and confirm**

Close the window. No commit needed here — this task is verification only.

---

## Verification Summary

After Task 4, the following are true:
- `scripts/icon.svg` exists and is the single editing surface for the icon.
- `scripts/generate_icons.py` is ~45 lines, has no drawing logic, just rasterizes + packs.
- Every file under `Assets/` (except the old leftover paths in `README.md` / `README_AI.md`) is regenerated from the new pipeline.
- The app displays the new icon in both the titlebar and the Windows taskbar.
- Three commits on `main`:
  1. `chore: pin icon-generation deps (cairosvg, Pillow)`
  2. `feat(icons): add SVG source for Big Sur-style folder icon`
  3. `feat(icons): regenerate app icons from SVG pipeline`

---

## Out-of-Scope Notes

- `README_AI.md` / `README.md` may still reference the old icon color scheme. That documentation pass is deliberately out of scope here — keep this plan focused on the asset pipeline.
- A dark-mode monochrome taskbar variant (mentioned in spec §6) is a follow-up.
- No Avalonia theme changes — the in-app accent color remains user-configurable and independent from the launcher icon.
