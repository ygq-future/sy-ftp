# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build          # build
dotnet run            # build + run
```

If a previous instance is still running, kill it first to avoid file-lock errors during build:
```bash
taskkill /F /IM sy-ftp.exe /T 2>/dev/null; dotnet build && dotnet run
```

No test project exists yet.

## Architecture

**Stack**: .NET 10 / C# 13 / Avalonia UI 12.x / Semi.Avalonia (theme) / PhosphorIconsAvalonia (icons) / FluentFTP (FTP protocol) / CommunityToolkit.Mvvm 8.x (source generators).

**MVVM pattern** — Views in `Views/`, ViewModels in `ViewModels/`, Models in `Models/`. `ViewLocator.cs` maps ViewModel → View by convention (replace "ViewModel" with "View" in the type name, resolve via reflection).

**Root composition**: `App.axaml.cs` creates `MainWindow` with `MainWindowViewModel` as DataContext. `MainWindowViewModel` owns `HostManagerViewModel` and `FileBrowserViewModel` as children. There is no DI container — dependencies (`FtpService`, `FileWatcherService`) are `new`-ed directly or injected through an overloaded constructor.

**ViewModelBase** extends `ObservableObject` from CommunityToolkit. All ViewModels use the source-generator pattern: `[ObservableProperty]` on private fields generates public properties and `OnPropertyChanged` partial methods. `[RelayCommand]` on private methods generates `CommandNameCommand` IRelayCommand properties.

**FTP layer**: `FtpService : IFtpService` wraps `AsyncFtpClient` from FluentFTP. `FileWatcherService` wraps `FileSystemWatcher` with a 500ms debounce mechanism for the remote-edit feature (download → watch temp file → re-upload on save).

**Theme**: Semi.Avalonia via `<semi:SemiTheme />` in App.axaml. Light/dark preference persisted to `%LocalAppData%/sy-ftp/theme.json`. All colors must use `{DynamicResource SemiColor*}` tokens — never hardcode colors.

**Compiled bindings**: `AvaloniaUseCompiledBindingsByDefault` is enabled. Views declare `x:DataType` for compile-time binding verification.

## Key files

| File | Role |
|------|------|
| `ViewModels/MainWindowViewModel.cs` | Top-level VM: connect/disconnect, theme toggle, topmost toggle, status |
| `ViewModels/HostManagerViewModel.cs` | Host CRUD, tag filtering |
| `ViewModels/FileBrowserViewModel.cs` | Remote directory listing, nav, download, upload (drag-drop), remote edit |
| `Services/FtpService.cs` | AsyncFtpClient wrapper — all FTP I/O |
| `Services/FileWatcherService.cs` | FileSystemWatcher + debounce for remote-edit auto-reupload |
| `Views/MainWindow.axaml` | Single-window layout: toolbar + sidebar + content + status bar cards |
| `Models/FtpHost.cs` | Host entity with `TagList` (comma-separated tags parsed from `Tags`) |
| `Converters/BoolInverter.cs` | `!bool` converter used for icon visibility toggling |
| `Helpers/DragDropHelper.cs` | Extracts local file paths from `DragEventArgs` |
| `README_AI.md` | Exhaustive UI design system reference — Semi color tokens, icon mappings, elevation/radius system, card layout specs |

## Workflow rules

- **After every code change** (edit, write, etc.), build + run the app so the user can visually verify the result.
- **CRITICAL: Stop after build + run.** Do NOT continue analyzing, brainstorming, speculating, or preparing the next change. Output a brief summary of what changed, then wait silently for the user's explicit confirmation or next instruction. Do not propose follow-up fixes or ask "should I also do X" — just stop and wait.
- **Do NOT chain multiple speculative fixes** without user confirmation. After making a change, wait for the user to confirm whether the fix works before trying an alternative approach. Do not guess and switch solutions on your own — the user must validate each change first.

## UI constraints (from README_AI.md)

- All `Border` elements must set explicit `CornerRadius` (min 4px, never 0).
- Panel-level cards: `CornerRadius="12"`, `BoxShadow="0 2 8 0 #1A000000"`.
- Nested interactive cards: `CornerRadius="8"`, `BoxShadow="0 1 3 0 #14000000"`.
- Icons: Phosphor Icons via `{pia:IconGeometry Icon=name, IconType=regular|fill}`. Directories use `folder_simple` + `SemiColorPrimary`; files use `file` + `SemiColorText2`.
- Buttons: `Classes="Primary"` for main action, `Theme="{StaticResource SolidButton}"` / `OutlineButton` / `BorderlessButton` for variants. At most one `Primary` per action area.
- Empty states: centered icon + guidance text in `SemiColorText2`.
- Loading states: `ProgressBar IsIndeterminate="True"` with optional overlay.
