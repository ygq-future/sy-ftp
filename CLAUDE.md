# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build          # build
dotnet run            # build + run
```

If a previous instance is still running, kill it first to avoid file-lock errors during build:
```bash
taskkill /F /IM sy-ftp.exe /T 2>$null; dotnet build && dotnet run
```

No test project exists yet.

## Architecture

**Stack**: .NET 10 / C# 13 / Avalonia UI 12.x / Semi.Avalonia (theme) / PhosphorIconsAvalonia (icons) / FluentFTP (FTP) / SSH.NET (SFTP) / CommunityToolkit.Mvvm 8.x (source generators).

**MVVM pattern** — Views in `Views/`, ViewModels in `ViewModels/`, Models in `Models/`. `ViewLocator.cs` maps ViewModel → View by convention (replace "ViewModel" with "View" in the type name, resolve via reflection).

**Root composition**: `App.axaml.cs` creates `MainWindow` with `MainWindowViewModel` as DataContext. `MainWindowViewModel` owns `HostManagerViewModel` and `FileBrowserViewModel` as children. There is no DI container — dependencies (`FtpService`, `FileWatcherService`) are `new`-ed directly or injected through an overloaded constructor.

**ViewModelBase** extends `ObservableObject` from CommunityToolkit. All ViewModels use the source-generator pattern: `[ObservableProperty]` on private fields generates public properties and `OnPropertyChanged` partial methods. `[RelayCommand]` on private methods generates `CommandNameCommand` IRelayCommand properties.

**FTP layer**: `FtpService : IFtpService` wraps `AsyncFtpClient` from FluentFTP (also handles SFTP via SSH.NET). `FileWatcherService` wraps `FileSystemWatcher` with a 500ms debounce mechanism for the remote-edit feature (download → watch temp file → re-upload on save).

**Drag-drop**: Implemented in `MainWindow.axaml.cs` code-behind (not in ViewModels). Upload: `DragDropHelper.GetDroppedFiles()` extracts paths from `DragEventArgs`, then `FileBrowserViewModel.UploadViaDragDropAsync` handles recursive directory upload. Download: downloads to `%TEMP%/SY-FTP/{guid}`, wraps as `DataTransferItem`, then calls `DragDrop.DoDragDropAsync` to initiate OS drag-out. Also includes rubber-band multi-select via an overlay `Canvas` + `SelectionRect Border` with `Ctrl`-click toggling.

**Converters** (4 total, all registered in `MainWindow.axaml` resources):
| Converter | Role |
|-----------|------|
| `BoolInverter` | `!bool` for visibility toggling |
| `FileSizeConverter` | Formats `long` bytes → human-readable (`B`, `KB`, `MB`, `GB`) |
| `HexToBrushConverter` | Hex string → `SolidColorBrush` for accent color swatches |
| `NotNullOrEmptyConverter` | `!string.IsNullOrEmpty(s)` for error-message visibility |

**Theme**: Semi.Avalonia via `<semi:SemiTheme />` in App.axaml. Light/dark preference persisted to `%LocalAppData%/SY-FTP/theme.json`. Accent color persisted separately to `accent.json` in the same directory. `App.ApplyAccentColor(hex)` computes HSL from the hex value and rewrites `SemiColorPrimary*` resource entries in the active theme dictionary — this runs on startup and on every theme/accent change. All colors must use `{DynamicResource SemiColor*}` tokens — never hardcode colors.

**Compiled bindings**: `AvaloniaUseCompiledBindingsByDefault` is enabled. Views declare `x:DataType` for compile-time binding verification.

## Key files

| File | Role |
|------|------|
| `App.axaml.cs` | App init: loads/saves theme + accent color, rewrites `SemiColorPrimary*` resources via HSL |
| `ViewModels/MainWindowViewModel.cs` | Top-level VM: connect/disconnect, theme toggle, topmost toggle, accent color, status |
| `ViewModels/HostManagerViewModel.cs` | Host CRUD, tag filtering |
| `ViewModels/FileBrowserViewModel.cs` | Remote directory listing, nav, download, upload, remote edit, breadcrumb |
| `Services/FtpService.cs` | AsyncFtpClient + SSH.NET wrapper — all FTP/SFTP I/O |
| `Services/FileWatcherService.cs` | FileSystemWatcher + debounce for remote-edit auto-reupload |
| `Views/MainWindow.axaml` | Single-window layout: toolbar + sidebar + content + status bar cards |
| `Views/MainWindow.axaml.cs` | Drag-drop (upload + download), rubber-band multi-select, window topmost toggle |
| `Views/HostEditWindow.axaml` | Add/edit host dialog (clone-then-edit pattern) |
| `Views/InputDialog.axaml` | Generic input dialog for new file/folder name entry |
| `Models/FtpHost.cs` | Host entity with `TagList` (comma-separated tags parsed from `Tags`) |
| `Models/PathSegment.cs` | Breadcrumb path segment with `IsOverflow` / `ShowSeparator` for overflow folding |
| `Helpers/FtpPathHelper.cs` | P/Invoke `SHGetKnownFolderPath` for Windows download folder; `Ensure()` creates `%UserProfile%\Downloads\SY-FTP` |
| `Helpers/DragDropHelper.cs` | Extracts local file paths from `DragEventArgs` |
| `README_AI.md` | Exhaustive UI design system reference — Semi color tokens, icon mappings, elevation/radius system, card layout specs |

## Workflow rules

- **After every code change** (edit, write, etc.), build + run the app so the user can visually verify the result.
- **CRITICAL: Stop after build + run.** Do NOT continue analyzing, brainstorming, speculating, or preparing the next change. Output a brief summary of what changed, then wait silently for the user's explicit confirmation or next instruction. Do not propose follow-up fixes or ask "should I also do X" — just stop and wait.
- **Do NOT chain multiple speculative fixes** without user confirmation. After making a change, wait for the user to confirm whether the fix works before trying an alternative approach. Do not guess and switch solutions on your own — the user must validate each change first.
- **Use web search when stuck**: If you encounter an unfamiliar technology, API, or can't decide on a solution, use `WebSearch` / `WebFetch` to look up the latest official documentation and solutions before guessing or using outdated knowledge. Prefer official docs, recent blog posts, and Stack Overflow answers from the current year.

## UI constraints (from README_AI.md)

- All `Border` elements must set explicit `CornerRadius` (min 4px, never 0).
- Panel-level cards: `CornerRadius="12"`, `BoxShadow="0 2 8 0 #1A000000"`.
- Nested interactive cards: `CornerRadius="8"`, `BoxShadow="0 1 3 0 #14000000"`.
- Icons: Phosphor Icons via `{pia:IconGeometry Icon=name, IconType=regular|fill}`. Directories use `folder_simple` + `SemiColorPrimary`; files use `file` + `SemiColorText2`.
- Buttons: `Classes="Primary"` for main action, `Theme="{StaticResource SolidButton}"` / `OutlineButton` / `BorderlessButton` for variants. At most one `Primary` per action area.
- Empty states: centered icon + guidance text in `SemiColorText2`.
- Loading states: `ProgressBar IsIndeterminate="True"` with optional overlay.
