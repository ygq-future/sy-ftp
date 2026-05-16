# Configuration Optimization and Feature Enhancements Design

**Date:** 2026-05-15  
**Version:** 1.0.3  
**Status:** Approved

## Overview

This design document covers four optimization and enhancement tasks for SY-FTP:
1. Merge all configuration files into a single `settings.json`
2. Add an "About" section to the Settings panel
3. Fix Transfer To panel disconnect/reconnect bugs
4. Add Owner and Permissions columns to the file browser

## 1. Configuration File Unification

### Goal
Consolidate all application configuration into a single `settings.json` file located at `%LocalAppData%/SY-FTP/settings.json`.

### Current State
- `settings.json` - theme, accent color, language, paths (managed by `SettingsService`)
- `config.json` - host list and window state (managed by `App.LoadConfig/SaveConfig`)
- Legacy `theme.json` and `accent.json` (already migrated by `SettingsService`)

### Design

#### Data Model Extension
Extend `Models/AppSettings.cs`:
```csharp
public class AppSettings
{
    public string Theme { get; set; } = "Default";
    public string AccentColor { get; set; } = "#4050B5";
    public string Language { get; set; } = "en";
    public string? DefaultDownloadPath { get; set; }
    public string? DefaultDataPath { get; set; }
    public List<FtpHost> Hosts { get; set; } = new();  // NEW
}
```

#### Migration Strategy
In `Services/SettingsService.cs`:
1. When `Load()` is called, check if `settings.json` exists and has a non-empty `Hosts` list
2. If `Hosts` is empty or null, attempt to load from legacy `config.json` (via `App.LoadConfig()`)
3. If legacy config exists, copy `Hosts` to `AppSettings.Hosts` and save
4. Optionally delete or rename `config.json` to `config.json.backup` after successful migration

#### API Changes
Remove `App.LoadConfig()` and `App.SaveConfig()` methods. Replace all calls with:
- Read: `SettingsService.Current.Hosts`
- Write: Modify `SettingsService.Current.Hosts`, then call `SettingsService.Save()`

#### Affected Components
- `Models/AppSettings.cs` - add `Hosts` property
- `Services/SettingsService.cs` - add migration logic in `Load()`, expose `Hosts` via `Current`
- `App.axaml.cs` - remove `LoadConfig/SaveConfig/ConfigFile`, update references
- `ViewModels/HostManagerViewModel.cs` - change host CRUD to use `SettingsService.Current.Hosts` and `SettingsService.Save()`
- `ViewModels/MainWindowViewModel.cs` - update host loading to use `SettingsService.Current.Hosts`

### Why
- Single source of truth for all configuration
- Consistent access pattern through `SettingsService`
- Easier backup and restore for users
- Reduces file I/O operations

---

## 2. About Section in Settings

### Goal
Add an "About" section to the Settings window displaying software information, version, license, and links.

### Design

#### Version Management
Add to `sy-ftp.csproj`:
```xml
<PropertyGroup>
    <Version>1.0.3</Version>
</PropertyGroup>
```

Create `Helpers/VersionHelper.cs`:
```csharp
public static class VersionHelper
{
    public static string GetVersion()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        return version != null 
            ? $"{version.Major}.{version.Minor}.{version.Build}" 
            : "1.0.3";
    }
}
```

#### UI Layout
Add fourth navigation item in `Views/SettingsWindow.axaml` sidebar:
- Icon: `info` (Phosphor Icons, regular style)
- Label: "settings.section.about" (localized)

Content panel layout (centered card style):
```
┌─────────────────────────────────────┐
│         [App Icon - 64x64]          │
│                                     │
│            SY-FTP                   │
│          Version 1.0.3              │
│                                     │
│  跨平台轻量级 FTP / SFTP 客户端      │
│  面向开发者，专注远程文件管理与      │
│         即时编辑                    │
│                                     │
│  Developer: ygq-future              │
│  License: Apache-2.0                │
│                                     │
│  [GitHub Repository Button]         │
└─────────────────────────────────────┘
```

#### ViewModel Extension
In `ViewModels/SettingsViewModel.cs`:
- Add `IsAboutSelected` property (computed from `SelectedSectionIndex == 3`)
- Add `OpenGitHubCommand` that opens `https://github.com/ygq-future/sy-ftp` in default browser

#### Localization Strings
Add to localization files:
- `settings.section.about` - "About" / "关于"
- `settings.about.developer` - "Developer" / "开发者"
- `settings.about.license` - "License" / "许可证"
- `settings.about.github` - "GitHub Repository" / "GitHub 仓库"
- `settings.about.description` - (full description text)

### Why
- Users can easily find version information for bug reports
- Provides proper attribution and license information
- GitHub link encourages community engagement
- Follows standard desktop application patterns

---

## 3. Transfer To Panel Bug Fix

### Problem
When user selects current host in Transfer To panel (self-transfer), then clicks Disconnect:
1. Both main panel and transfer panel disconnect
2. Clicking Connect reconnects, but main panel directory doesn't refresh
3. Clicking "Transfer Here" shows "source host is no longer connected" error

### Root Cause
Transfer panel shares connection sessions via `_mainVm.Sessions` dictionary. Disconnect affects both panels, but reconnect doesn't trigger directory refresh in main panel.

### Design

#### Independent State Management
In `Views/TransferBrowserDialog.axaml.cs`:

Add state tracking:
```csharp
private bool _isConnected;  // Track connection state independently
```

#### Disconnect Behavior
When user clicks Disconnect button:
1. Clear directory list (`DirList.ItemsSource = null`)
2. Set `_isConnected = false`
3. Set `_destFtp = null`
4. Disable "Transfer Here" button
5. Show "Not Connected" status message
6. Update UI to show Connect button

#### Connect Behavior
When user clicks Connect button:
1. Call `_mainVm.EnsureSessionAsync(_destHost, CancellationToken.None)`
2. If successful, set `_destFtp = session.Ftp`
3. Set `_isConnected = true`
4. Call `LoadPathAsync(session.CurrentPath ?? "/")` to refresh directory
5. Enable "Transfer Here" button
6. Update UI to show Disconnect button

#### Transfer Validation
Before executing "Transfer Here":
1. Check if `_sourceFtp` is still valid (source connection alive)
2. Check if `_destFtp` is still valid (destination connection alive)
3. If either is null or disconnected, show clear error message:
   - "Source host is no longer connected" if source is down
   - "Destination host is not connected" if destination is down
4. Only proceed if both connections are valid

#### UI Updates
In `Views/TransferBrowserDialog.axaml`:
- Bind "Transfer Here" button `IsEnabled` to connection state
- Add visual indicator for connection status (green dot when connected)
- Ensure Connect/Disconnect buttons toggle based on `_isConnected` state

### Why
- Independent state management prevents cross-panel interference
- Clear validation prevents confusing error messages
- Proper cleanup ensures UI reflects actual connection state
- Users have full control over transfer panel connections

---

## 4. Owner and Permissions Columns

### Goal
Display file owner and Unix-style permissions in the file browser list.

### Design

#### Data Model Extension
Extend `Models/RemoteFile.cs`:
```csharp
public record RemoteFile(
    string Name,
    string FullPath,
    long Size,
    bool IsDirectory,
    DateTimeOffset LastModified,
    string? Owner,        // Format: "owner:group" or "N/A"
    string? Permissions   // Format: "rwxr-xr-x" or "N/A"
)
{
    public bool IsParentEntry => Name == "..";
}
```

#### Data Extraction

**For FluentFTP (FTP/FTPS):**
In `Services/FtpService.cs`, when processing `FtpListItem`:
1. Check if `item.OwnerPermissions`, `item.GroupPermissions`, `item.OthersPermissions` exist
2. Convert to string format: `rwxr-xr-x`
   - Owner: read(4) + write(2) + execute(1) → "rwx" or "r--" etc.
   - Group: same logic
   - Others: same logic
3. For owner info, check if FluentFTP provides owner/group fields (may need to parse `item.RawListing`)
4. If unavailable, set to "N/A"

**For SSH.NET (SFTP):**
In `Services/FtpService.cs`, when processing `SftpFile`:
1. Use `file.OwnerUserId` and `file.GroupId` for owner (format: `userId:groupId`)
2. Use `file.Attributes.Permissions` (numeric) and convert to `rwxr-xr-x`:
   ```csharp
   string FormatPermissions(int mode)
   {
       char[] perms = new char[9];
       perms[0] = (mode & 0x100) != 0 ? 'r' : '-';  // Owner read
       perms[1] = (mode & 0x080) != 0 ? 'w' : '-';  // Owner write
       perms[2] = (mode & 0x040) != 0 ? 'x' : '-';  // Owner execute
       perms[3] = (mode & 0x020) != 0 ? 'r' : '-';  // Group read
       perms[4] = (mode & 0x010) != 0 ? 'w' : '-';  // Group write
       perms[5] = (mode & 0x008) != 0 ? 'x' : '-';  // Group execute
       perms[6] = (mode & 0x004) != 0 ? 'r' : '-';  // Others read
       perms[7] = (mode & 0x002) != 0 ? 'w' : '-';  // Others write
       perms[8] = (mode & 0x001) != 0 ? 'x' : '-';  // Others execute
       return new string(perms);
   }
   ```

**Fallback:**
If server doesn't provide this information, set both fields to "N/A".

#### UI Layout
In `Views/MainWindow.axaml`, update file list columns:

**New column order:**
1. Icon (existing)
2. Name (existing)
3. **Owner** (new) - width: 120px, header: "Owner"
4. **Permissions** (new) - width: 100px, header: "Permissions"
5. Size (existing)
6. Modified (existing)

**Column styling:**
- Owner: left-aligned, monospace font, `SemiColorText1`
- Permissions: left-aligned, monospace font, `SemiColorText1`
- Both columns should use `TextTrimming="CharacterEllipsis"` for overflow

#### Localization
Add column headers:
- `file.column.owner` - "Owner" / "所有者"
- `file.column.permissions` - "Permissions" / "权限"

#### Sorting
Update `ViewModels/FileBrowserViewModel.cs`:
- Add sorting support for Owner and Permissions columns
- Treat "N/A" as lowest priority in sort order

### Why
- Provides essential Unix file metadata for developers
- Helps users understand file access rights before editing
- Matches expectations from command-line FTP clients
- Graceful degradation with "N/A" for unsupported servers

---

## Implementation Order

1. **Configuration file unification** (foundation for other changes)
2. **Owner and Permissions columns** (data model changes affect other features)
3. **About section** (independent, low risk)
4. **Transfer To panel fix** (depends on stable configuration)

## Testing Checklist

### Configuration Unification
- [ ] Fresh install: settings.json created with default values
- [ ] Migration: existing config.json hosts imported to settings.json
- [ ] Host CRUD: add/edit/delete hosts persists correctly
- [ ] Backward compatibility: old config.json backed up after migration

### About Section
- [ ] Version number displays correctly (1.0.3)
- [ ] GitHub link opens in default browser
- [ ] All text properly localized (English + Chinese)
- [ ] Layout renders correctly on all platforms

### Transfer To Panel
- [ ] Disconnect clears directory list and disables Transfer Here
- [ ] Connect refreshes directory list
- [ ] Transfer Here validates both connections before proceeding
- [ ] Error messages are clear and accurate
- [ ] Self-transfer (same host) works correctly

### Owner and Permissions
- [ ] SFTP: owner and permissions display correctly
- [ ] FTP: owner and permissions display correctly (if supported)
- [ ] Unsupported servers: "N/A" displays correctly
- [ ] Columns are sortable
- [ ] Layout doesn't break with long owner names
- [ ] Monospace font renders correctly on all platforms

## Risks and Mitigations

**Risk:** Configuration migration fails, user loses host list  
**Mitigation:** Backup config.json before migration, add error handling to preserve original file

**Risk:** FluentFTP doesn't expose owner/permissions in a parseable way  
**Mitigation:** Parse `RawListing` string as fallback, display "N/A" if parsing fails

**Risk:** Transfer panel state gets out of sync with main panel  
**Mitigation:** Independent state tracking, explicit validation before operations

**Risk:** Version number doesn't update automatically  
**Mitigation:** Document manual update process in CLAUDE.md, consider CI/CD automation

## Success Criteria

- All configuration stored in single settings.json file
- Users can view software version and license information
- Transfer To panel handles disconnect/reconnect without errors
- File browser displays owner and permissions when available
- No data loss during configuration migration
- All features work on Windows, Linux, and macOS
