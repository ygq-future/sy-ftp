# SY-FTP

> 跨平台轻量级 FTP / SFTP 客户端 — 面向开发者，专注远程文件管理与即时编辑。

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-12-8E44AD)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

## 亮点

- **双协议**：端口 22 自动走 SFTP（SSH.NET），其他端口走 FTP/FTPS（FluentFTP，`EncryptionMode=Auto`）。
- **开发者友好**：彩色实心文件图标（按扩展名区分）、等宽路径栏、快捷键、Ctrl 多选、橡皮筋选择、拖拽移动。
- **在线 / 离线编辑二选一**：在窗口内用 AvaloniaEdit 直接编辑，或下载到本地调默认编辑器、保存即自动回传。
- **多主机并行**：每个主机各自维护独立会话和工作目录，侧栏圆点指示连接状态。
- **主题 & 强调色**：亮色 / 暗色即时切换，8 种强调色可选，整体色板自动按 HSL 重算。
- **跨平台**：Windows / Linux / macOS 均通过原生渲染运行，Win32 专属选项已按平台守卫。

## 功能概览

### 主机管理
- 新增 / 编辑 / 删除主机，编辑走 Clone 模式，Cancel 真正回滚。
- 标签（Tag）逗号分隔，下拉筛选。新建主机未填标签时自动设为 `default`，保证筛选器始终命中。
- 主机列表、窗口置顶状态持久化到 `%LocalAppData%/SY-FTP/config.json`。

### 远程浏览
- 面包屑路径栏：支持溢出折叠（「…」弹出中间段）、就地编辑（双击或点铅笔）、一键复制路径（带 toast）。
- 列表列：图标 / Name / Size / Modified，点击列头切换排序列与正倒序。
- 文件夹优先、`..` 固定置顶。
- 文件图标按扩展名区分颜色（`.py` 深蓝、`.js` 黄、`.md` 蓝、`.sh` 绿、图片 / 音频 / 视频 / 压缩包各色等），文件夹统一主题色实心图标。

### 文件操作
- **上传**：从系统资源管理器拖入单文件、多文件或整个文件夹，目录结构递归保留。
- **下载**：右键下载到默认目录（`~/Downloads/SY-FTP`），或「Download to…」选择路径；进度条按字节聚合。
- **新建**：New File / New Folder 走输入框对话框。
- **删除**：单选或多选后右键 / Delete 键，带确认弹窗。
- **移动**：按住拖拽到目标文件夹（包括 `..`），松手即重命名到远端新路径。
- **跨主机传输**：右键「Transfer to…」打开浮层，在两个会话之间双向复制文件。

### 远程编辑（两种模式）
- **Remote Edit**：下载到 `%TEMP%/SY-FTP/` → 调系统默认编辑器 → `FileSystemWatcher` 监听（500ms 防抖）→ 自动回传。
- **Online Edit**：`RemoteEditWindow` 内置 AvaloniaEdit + TextMate 语法高亮，保存直接上传。
- 状态栏显示上次同步时间戳。

### 界面与体验
- 无边框窗口 + 自定义标题栏（最小 / 最大 / 关闭）。
- 窗口置顶开关，状态跨重启保留。
- 状态栏：连接圆点（绿 / 红）、当前主机名、加载指示器、文件计数。
- 错误浮层：连接 / 传输 / 编辑失败统一进 `ErrorMessage`，2 秒自动消失，不崩溃。

## 技术栈

| 技术 | 说明 |
|------|------|
| .NET 10 / C# 13 | 运行时与语言 |
| [Avalonia UI 12](https://avaloniaui.net/) | 跨平台 UI 框架（MVVM） |
| [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia) | 主题与控件样式令牌（`SemiColor*`） |
| [PhosphorIconsAvalonia](https://github.com/Phosphoricons/PhosphorIconsAvalonia) | 矢量图标（fill / regular 两种风格） |
| [CommunityToolkit.Mvvm 8](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) | `[ObservableProperty]` / `[RelayCommand]` 源生成器 |
| [FluentFTP](https://github.com/robinrodricks/FluentFTP) | FTP / FTPS 客户端 |
| [SSH.NET](https://github.com/sshnet/SSH.NET) | SFTP 客户端 |
| [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) | 内置代码编辑器（含 TextMate 语法高亮） |

## 快速开始

### 环境

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 构建 & 运行

```bash
git clone https://github.com/ygq-future/sy-ftp.git
cd sy-ftp
dotnet run
```

若上一个实例仍在运行导致文件锁定：

```powershell
taskkill /F /IM sy-ftp.exe /T; dotnet build; dotnet run
```

### 配置目录

应用数据写入 `Environment.SpecialFolder.LocalApplicationData` 下的 `SY-FTP/`：

| 平台 | 路径 |
|------|------|
| Windows | `%LocalAppData%\SY-FTP\` |
| Linux | `~/.local/share/SY-FTP/` |
| macOS | `~/Library/Application Support/SY-FTP/` |

内部文件：`config.json`（主机 + 窗口状态）、`theme.json`（亮 / 暗）、`accent.json`（强调色）。

## 项目结构

```
sy-ftp/
├── Models/           # FtpHost / RemoteFile / AppConfig / PathSegment 等实体
├── ViewModels/       # MainWindow / HostManager / FileBrowser / HostSession
├── Views/            # MainWindow + 编辑 / 输入 / 确认 / 传输 浮层
├── Services/         # FtpService（SFTP + FTP 双后端）、FileWatcherService
├── Helpers/          # FileIconHelper / FtpPathHelper / DragDropHelper
├── Converters/       # 绑定转换器（文件大小、图标、颜色等）
└── Assets/           # 图标与资源
```

## 跨平台说明

所有平台特定 API 均已守卫或提供跨平台 fallback：

- `Program.cs` 中的 `Win32PlatformOptions` 仅在 `OperatingSystem.IsWindows()` 下应用。
- `FtpPathHelper` 仅 Windows 下 P/Invoke `SHGetKnownFolderPath`，其他平台回退到 `~/Downloads`。
- 等宽字体：`Consolas, Menlo, DejaVu Sans Mono, Courier New, monospace`，三平台均有命中。
- `Process.Start { UseShellExecute = true }` 自动适配：Windows ShellExecute、Linux xdg-open、macOS open。

## 许可

Apache-2.0 License
