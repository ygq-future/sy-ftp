# 项目说明文档 (README_AI.md)

**AI 指示：请在阅读此文档后，基于 Avalonia MVVM 模式和 C# 强类型规范进行代码生成和逻辑建议。**

## 1. 项目概览

- **名称**: sy-ftp
- **定位**: 跨平台（Windows/Linux/macOS）轻量级 FTP 客户端。
- **核心理念**: 极致简洁、现代 UI、单窗口聚焦远程文件管理。
- **技术栈**:
  - 框架: Avalonia UI (MVVM 模式)
  - 开发语言: C# 13 / .NET 10
  - 关键库: FluentAvalonia (提供现代 Win11 风格控件), FluentFTP (底层协议处理)。

## 2. 功能清单 (Functional Requirements)

### A. 主机管理
- 支持 FTP 主机的新增、编辑、删除。
- 分类系统: 支持为不同主机设置 Tag（标签），支持按标签筛选。

### B. 远程文件浏览
- 单窗口设计: 仅显示远程服务器的文件列表（List/Grid View）。
- 支持目录深度导航。
- 实时刷新远程目录状态。

### C. 文件传输逻辑
- 上传: 支持从系统资源管理器直接拖拽 (Drag & Drop) 文件/文件夹至窗口进行上传。
- 下载: 选中文件右键下载至本地。

### D. 核心特性：远程编辑 (Hot Feature)
1. 用户右键点击远程文件，选择"本地编辑"。
2. **静默下载**: 自动下载该文件至系统临时目录（Temp Path）。
3. **调用关联程序**: 使用系统默认编辑器打开该临时文件。
4. **实时监控**: 监听临时文件的 FileSystemWatcher 事件。
5. **自动同步**: 检测到保存行为后，自动将修改后的文件覆盖回远程 FTP。

### E. 交互与 UI
- 窗口置顶: 提供开关，允许窗口固定在最上层。
- 设计语言: 遵循 Fluent Design，简洁无多余元素。

## 3. 技术实现架构 (Architectural Reference)

### 项目结构

| 路径 | 职责 |
|------|------|
| `Models/` | 包含 `FtpHost`, `RemoteFile`, `AppConfig` 等实体 |
| `ViewModels/` | 核心逻辑控制器 |
| `ViewModels/MainWindowViewModel` | 核心逻辑控制器 |
| `ViewModels/HostManagerViewModel` | 处理主机增删改查 |
| `ViewModels/FileBrowserViewModel` | 处理文件列表渲染与 FTP 指令 |
| `Views/` | XAML 布局文件 |
| `Services/` | 服务层 |
| `Services/IFtpService` | 封装 FluentFTP 的异步操作 |
| `Services/IFileWatcherService` | 负责监控临时文件修改 |

### 关键逻辑片段 (AI 预设)

- **拖拽实现**: 拦截 `DragDrop.DropEvent` 并解析 `DataObject`。
- **置顶实现**: 绑定 `Window` 的 `Topmost` 属性。
- **远程编辑逻辑流**:
  ```
  RemotePath -> LocalTempPath -> Process.Start -> Watcher_Changed -> FTP_Upload
  ```

## 4. AI 协助约束

- **代码风格**: 优先使用 C# 最新语法的简洁写法（如 File-scoped namespaces, Primary constructors）。
- **异步规范**: 所有的网络与 IO 操作必须使用 `async/await`，禁止阻塞 UI 线程。
- **UI 响应式**: 列表加载需具备 Loading 状态。
