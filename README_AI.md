# sy-ftp — 设计与实现参考（README_AI.md）

简短说明：本文件为开发者与 AI 合作时的指引，包含架构概览、UI 设计代{线}、编码规范与关键实现要点。遵循 Avalonia MVVM、CommunityToolkit.Mvvm 源代码生成与 Semi.Avalonia 主题体系。

## 项目概览
- 名称：sy-ftp
- 目标：跨平台、轻量、单窗口 FTP 客户端，专注远程文件管理与便捷远程编辑
- 技术栈：.NET 10 / C# 13, Avalonia UI 12.x, Semi.Avalonia, PhosphorIconsAvalonia, FluentFTP, CommunityToolkit.Mvvm

## 快速实现要点（一目了然）
- UI：卡片式（Toolbar / Sidebar / Content / StatusBar），全局 8px 间距，面板 CornerRadius=12，子卡片 CornerRadius=8
- 主题：使用 SemiColor Token（禁止硬编码色值）；通过 Application.Current.RequestedThemeVariant 切换，偏好保存在 %LocalAppData%/sy-ftp/theme.json
- 图标：使用 PhosphorIconsAvalonia（pia:IconGeometry）；目录用 folder_simple+SemiColorPrimary，文件用 file+SemiColorText2
- 异步：所有网络/IO 使用 async/await，禁止阻塞 UI 线程
- 编码风格：File-scoped namespace、简洁语法、使用 CommunityToolkit 源生成（[ObservableProperty]/[RelayCommand]）

## 关键模块
- Models/: FtpHost, RemoteFile, AppConfig
- ViewModels/: MainWindowViewModel, HostManagerViewModel, FileBrowserViewModel
- Views/: XAML 布局（使用 Compiled Bindings）
- Services/: FtpService (FluentFTP 封装), FileWatcherService (远程编辑自动上传)
- Helpers/: DragDropHelper, Result

## 核心功能流程（简要）
- 连接流程：HostSelect → ConnectAsync → 加载根目录 → 更新状态栏
- 远程编辑：RemotePath → 下载到 %TEMP%/sy-ftp → Process.Start 打开 → FileWatcher 监听变更 → 保存后自动上传 → 可选清理临时文件
- 拖拽上传：解析拖拽路径（支持文件夹递归）、并行上传、展示进度

## UI 设计与约束（必须遵守）
- 颜色：全部使用 {DynamicResource SemiColor*} Token
- 圆角：所有 Border 必须显式 CornerRadius（最小 4px）
- 卡片阴影：L1/L2/L3 层级（L2 用于主面板）
- 按钮：使用 Classes 与 Theme（Primary/SolidButton 用于关键操作）
- 图标：优先 regular 描边，按钮内可用 fill 强调

## 验证与运行
- 杀进程（避免锁文件）：taskkill /F /IM sy-ftp.exe /T
- 构建：dotnet build
- 运行：dotnet run

## 给 AI 的请求约束（简短）
- 提交的代码必须使用 async/await，避免任何同步阻塞 IO
- 不要改变设计 token 名称或引入新的 UI 库
- 保持 ViewModels 轻量，复杂逻辑放入 Services

