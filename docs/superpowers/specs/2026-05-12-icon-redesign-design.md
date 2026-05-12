# SY-FTP Icon Redesign — Design Spec

**Date:** 2026-05-12
**Status:** Approved (pending implementation)
**Owner:** icon pipeline (`scripts/`, `Assets/`)

## 1. Motivation

The current app icon (indigo rounded square + thick white up/down arrows + blurred glass highlight) feels dated — it reads as a 2015-era iOS skeuomorph. Two problems:

1. **Palette.** The flat indigo `#4050B5` is muted and conservative; it does not stand out in a modern dock alongside competitors (WinSCP, FileZilla, Cyberduck — all blue/gray).
2. **Symbol weight.** The oversized arrow pair dominates the silhouette and conveys only "transfer", under-selling the app's file-management features.

The redesign targets a macOS Big Sur / Tahoe visual language: a distinct rounded tile, a recognizable central object, and a small overlayed badge that adds a second meaning without fighting the main symbol.

## 2. Visual Design

### 2.1 Base tile

- Square with corner radius = 23% of edge length (matches Big Sur squircle proportions).
- Diagonal linear gradient, top-left to bottom-right:
  - `#7C3AED` (violet-600)
  - `#EC4899` (pink-500)
- Inner rim: 1px stroke, white @ 20% opacity, inset 1px — reads as a faint glass edge.
- Outer ambient glow: the same base gradient, heavily blurred and expanded, behind the tile. Provides "lift" against dark docks.

### 2.2 Folder (primary subject)

Centered, occupying ~62% of the tile width.

- Fill: vertical gradient, amber-300 `#FCD34D` (top) → amber-500 `#F59E0B` (bottom).
- Structure:
  - **Back panel:** rounded rectangle with a small trapezoidal tab on its top-left — the classic manila-folder silhouette.
  - **Front panel:** a slightly larger rounded rectangle, drawn *in front* of the back panel with its top edge ~8% of the folder height below the tab line, so the tab and a sliver of the back panel remain visible. This creates the "slightly open, contents peeking out" reading.
- Details:
  - 1px white highlight @ 30% along the top edge of the front panel.
  - Subtle inner shadow at the bottom of the front panel (dark amber, low opacity) to suggest thickness.
- Cast shadow behind the folder: color `#1E0B3C` (deep violet, not pure black) @ 40% opacity, Gaussian blur ~4% of canvas, offset +2% vertically. Blends with the gradient rather than dirtying it.

### 2.3 Transfer badge

A small overlay chip pinned to the bottom-right of the tile, following the Xcode / App Store icon convention.

- Shape: white circle, diameter ≈ 38% of tile edge, positioned so its center sits near `(0.78·S, 0.78·S)`. Slight overhang past the folder is desirable.
- Border: 1px white stroke; drop shadow `#1E0B3C` @ 30%, blur 3% of canvas, offset +1.5% vertically.
- Content: two parallel arrows, one pointing up, one pointing down, side-by-side inside the circle.
  - Arrow fill: the same violet → pink diagonal gradient used on the tile (visual echo).
  - Proportions (relative to circle diameter):
    - arrow total height: 55%
    - stem width: 14% of arrow height
    - arrowhead width: 42% of arrow height, height: 40% of arrow height
    - gap between the two arrows: 18% of circle diameter
  - All terminals rounded via SVG `stroke-linejoin="round"` on stroked paths, or path geometry with rounded corners if filled.

### 2.4 Palette reference

| Token | Hex | Usage |
|-------|-----|-------|
| `violet-600` | `#7C3AED` | tile gradient top-left, badge arrows |
| `pink-500` | `#EC4899` | tile gradient bottom-right, badge arrows |
| `amber-300` | `#FCD34D` | folder fill top |
| `amber-500` | `#F59E0B` | folder fill bottom |
| `violet-950` | `#1E0B3C` | all shadows (tinted, not black) |
| `white` | `#FFFFFF` | rim, highlight, badge fill |

No other colors introduced.

## 3. Technical Approach

Current pipeline (`scripts/generate_icons.py`) is Pillow-only and produces soft edges at small sizes due to raster-first drawing. Replace with:

### 3.1 Source of truth: SVG

- New file: `scripts/icon.svg` — hand-authored vector, single artboard 1024×1024.
- All shapes, gradients, blurs, and shadows expressed as native SVG (linear/radial gradients, `feGaussianBlur`, `feDropShadow`).
- Editing the icon = editing one SVG file. The Python script does not contain drawing logic.

### 3.2 Rasterizer: cairosvg

- `scripts/generate_icons.py` rewrites to:
  1. Load `icon.svg` via `cairosvg.svg2png(...)`, rendering at each target size.
  2. Wrap the resulting PNG bytes in `PIL.Image`.
  3. Write per-size PNGs; package `.ico` via `Pillow.save(format='ICO', sizes=[...])`; package `.icns` via `Pillow.save(format='ICNS')`.
- New Python deps: `cairosvg` (added to a `requirements.txt` or script docstring). Pillow remains.

### 3.3 Output contract (unchanged)

```
Assets/sy-ftp.ico              # Windows multi-size: 16,24,32,48,64,128,256
Assets/sy-ftp.icns             # macOS up to 1024
Assets/sy-ftp.png              # 256 generic
Assets/sy-ftp-512.png          # 512 generic
Assets/icons/{16,24,32,48,64,128,256,512,1024}.png  # Linux set
```

Paths match what `sy-ftp.csproj` and the current README reference, so no project wiring changes are needed.

## 4. Non-Goals

- No change to in-app toolbar / sidebar Phosphor icons; this spec is strictly about the application launcher icon.
- No change to splash screen, About dialog graphics, or README banners.
- No new accent colors introduced into the Avalonia theme — the icon palette is intentionally independent from the user's in-app accent color.

## 5. Risk & Verification

### 5.1 Risks

- **cairosvg install friction on Windows.** `cairosvg` depends on a Cairo native binary. Mitigation: document the `pip install cairosvg` prerequisite in the script docstring; if Cairo is missing, fall back instructions reference the prebuilt wheels (or `pipx`).
- **Small-size legibility of the badge.** At 16×16 the circle + two arrows may become unreadable. Mitigation: render the 16/24 sizes with the badge slightly enlarged (a separate SVG variant or size-aware rendering flag) if visual inspection shows mush. Decide during implementation after first render.

### 5.2 Verification steps

1. Run `python scripts/generate_icons.py`.
2. Visually inspect `Assets/icons/16.png`, `32.png`, `256.png` — confirm:
   - folder is unambiguous at 16
   - badge is legible at 32+
   - no aliasing artifacts at 256
3. Confirm file list under `Assets/` matches section 3.3 (no missing size, no stray orphan files from the old pipeline).
4. `dotnet build` — confirm project still builds and references resolve.
5. Run the app (`dotnet run`) — confirm the window icon and taskbar icon both display the new design.

## 6. Out-of-Scope Follow-ups (not part of this spec)

- A dark-mode / monochrome variant for Windows taskbar unplated rendering.
- An animated version for splash/loading screens.
- App Store / Microsoft Store marketing assets.
