# SY-FTP — 开发任务清单（基于当前进度与代码风格）

说明：按优先级排列。已完成项标注 DONE。每项尽量包含验收条件。

## P0 — 核心骨架（必须最先完成）

### 1. MainWindow 布局 — DONE
- 卡片式布局：工具栏 / 侧栏 / 内容 / 状态栏
- 无边框窗口设计（`WindowDecorations="BorderOnly"`），自定义标题栏（最小化/最大化/关闭）
- 主题切换按钮（亮色/暗色），`Semi.Avalonia` 主题集成
- 窗口置顶开关按钮
- SFTP 连接按钮（`connect-primary` 样式，accent 主色调，disabled 置灰）
- 面板卡片 `CornerRadius="12"` + `BoxShadow`，嵌套卡片 `CornerRadius="8"`
- 全局使用 `{DynamicResource SemiColor*}` 颜色令牌，不硬编码颜色

### 2. HostManager UI — DONE
- 主机列表：卡片式项目（`CornerRadius="8"` + `BoxShadow`），选中高亮（Primary 边框 + PrimaryLight 背景）
- Tag 筛选：ComboBox 下拉，`"All tags"` 哨兵值显示全部，从所有主机 Tags 中提取去重 Tag 列表
- ContextMenu（Edit/Delete）：主题色背景，圆角菜单项，悬浮/选中使用 `SemiColorPrimaryLight`
- ComboBox 下拉面板背景色适配主题（代码后置 `WirePopupBackgrounds()` 直接设置）
- ContextMenu 下拉面板背景色适配主题
- 空状态引导文字
- 示例数据：wsl (172.23.30.234:22)

### 3. Host 编辑对话框 — DONE
- 无边框窗口（`WindowDecorations="None"`，`Background="Transparent"`）
- 卡片面板 + 主题自适应阴影（亮色：`0 0 16 0 #0C000000`，暗色：`0 0 24 0 #18FFFFFF`）
- 卡片空白区域可拖拽移动（`BeginMoveDrag`），TextBox/Button 区域不拦截拖拽
- 密码输入框字符掩码（`PasswordChar="•"`）
- TextBox 聚焦时边框高亮为 accent 主色调（代码后置 `WireTextBoxFocus()`）
- 内联错误提示横幅（warning 图标 + 错误文本），必填项校验（Name / Host）
- Cancel 按钮：outlined 样式（accent 边框）；Save 按钮：filled 样式（accent 背景）
- Clone 编辑模式：Cancel 时真正还原，不修改原对象
- 默认端口 22（SFTP）
- Add 按钮打开对话框，确认后才添加到列表

## P1 — 远程文件浏览

### 4. 文件列表视图 — DONE
- 列：图标 | Name | Size | LastModified
- 目录/文件图标区分：`folder_simple`（Primary 色） vs `file`（Text2 色）
- 智能文件大小显示（`FileSizeConverter`）：< 1KB → N B，< 1MB → N.N KB，< 1GB → N.N MB，>= 1GB → N.N GB，目录不显示大小
- 紧凑项目间距（`Padding="10,3"`）
- 悬浮高亮：`SemiColorPrimaryLight` 背景 + `CornerRadius="6"` 圆角
- 选中高亮：`SemiColorPrimaryLight` 背景
- 默认排序：文件夹优先，然后按名称正序
- 路径面包屑（`CurrentPath`，Consolas 等宽字体）
- 加载遮罩层（`ProgressBar IsIndeterminate`）
- 空状态：云图标 + "Select a host and click Connect" 引导文字

### 5. 连接与导航联动 — DONE
- 选中主机 Connect → 加载根目录；Disconnect → 清空视图 + 重置为 `/`
- 双击文件夹 → 进入子目录（`NavigateCommand`）
- 排序栏：Name / Size / Modified 三列，点击切换排序，再次点击切换正/倒序
- 排序指示器：当前排序列显示 ▲（正序）或 ▼（倒序），非活跃列不显示箭头
- 状态栏：连接指示灯（绿/红圆点）、状态文字、加载进度条、文件计数

## P2 — 文件传输

### 6. 拖拽上传 (Drag & Drop) — DONE
- `DragDropHelper` 提取本地文件路径
- 支持多文件 / 文件夹递归上传
- `UploadViaDragDropAsync` → 单文件上传 → 目录递归创建 + 上传
- 上传进度可见；不阻塞 UI（async/await）
- 验收：从系统资源管理器拖拽文件/文件夹到窗口即可上传，上传后自动刷新列表

### 7. 文件下载 — DONE
- 右键菜单触发下载
- 弹出保存路径选择对话框
- 显示下载进度
- 验收：大文件下载不阻塞 UI，进度条实时更新

## P3 — 远程编辑（关键特性）

### 8. 远程编辑完整实现 — DONE
- 右键远程文件 → 下载到 `%TEMP%/SY-FTP/` → 调用系统默认编辑器打开
- `FileWatcherService` 监听临时文件变更（500ms 防抖）→ 保存后自动上传覆盖
- 状态栏显示最后同步时间
- 验收：本地编辑保存后自动回传，状态栏更新时间戳

## P4 — 体验与持久化

### 9. 窗口置顶开关 — DONE
- 工具栏 Topmost 切换按钮（`icon-toggle` 样式），图标高亮表示置顶状态
- 验收：一键开关，窗口始终在最上层或恢复正常

### 10. 应用配置持久化（AppConfig） — DONE
- 持久化内容：主机列表、窗口位置/大小、主题选择、置顶状态
- 保存位置：`%LocalAppData%/SY-FTP/`
- 验收：重启应用后恢复上次的主机列表和界面状态

## P5 — 健壮性与错误处理

### 11. 异常处理与用户提示 — DONE
- 连接失败 / 超时 / 传输失败通过 `ErrorMessage` 浮层（2 秒自动消失）展示，不崩溃
- `FileBrowserViewModel.EditRemoteAsync`、`MainWindow.OnItemOnlineEditClick` 增加 catch-all，统一写入 `ErrorMessage`
- 空目录 / 无权限等边缘情况的用户提示
- 验收：各种异常场景均有提示且应用不闪退

### 12. 单元验证（手动测试清单） — DONE
- 端到端测试流程：连接 → 列文件 → 下载 → 远程编辑 → 上传
- 交叉验证 SFTP（端口 22）和 FTP（其他端口）两种后端
- 验收：完整走通一次端到端流程，无异常

### 13. 跨平台兼容性（Windows / Linux / macOS） — DONE
- `Program.cs`：`Win32PlatformOptions` 仅在 `OperatingSystem.IsWindows()` 下应用，避免 Linux/macOS 启动异常
- `FtpPathHelper`：Windows 使用 `SHGetKnownFolderPath`（P/Invoke 已 guard），其他平台 fallback 到 `~/Downloads`
- 等宽字体：`Consolas,Menlo,DejaVu Sans Mono,Courier New,monospace` 多候选，覆盖三平台
- `Process.Start { UseShellExecute = true }` 在三平台均可调默认编辑器（Windows: ShellExecute；Linux: xdg-open；macOS: open）
- `Environment.SpecialFolder.LocalApplicationData` 三平台均映射到合理目录：Windows `%LocalAppData%`、Linux `~/.local/share`、macOS `~/Library/Application Support`
- `FileSystemWatcher`、`Path.Combine`、`Directory.*`、`File.*` 为 .NET 标准库跨平台 API
- 验收：代码中所有平台特定 API 均已 guard 或使用跨平台 fallback

## 技术要点

- **SFTP/FTP 双后端**：端口 22 → SSH.NET `SftpClient`；其他端口 → FluentFTP `AsyncFtpClient`（`EncryptionMode=Auto`）
- **MVVM**：CommunityToolkit.Mvvm 源生成器（`[ObservableProperty]` / `[RelayCommand]`）
- **编译绑定**：`AvaloniaUseCompiledBindingsByDefault` 全局开启，View 声明 `x:DataType`
- **代码风格**：不加注释（除非 WHY 不显然），不引入未请求的抽象，简洁优先

## 开发流程遵守
- 每次改动后使用powershell执行：`taskkill /F /IM sy-ftp.exe /T`，然后再执行：`dotnet build && dotnet run`
- 小步快跑：每完成一项提交并在 PR 描述写明验收步骤
