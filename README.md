# SY-FTP

> 跨平台轻量级 FTP 客户端 — 极致简洁，聚焦远程文件管理。

[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Avalonia](https://img.shields.io/badge/Avalonia-11-8E44AD)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)

## 特性

- **主机管理** — 新增、编辑、删除 FTP 主机，支持**标签（Tag）**分类与筛选。
- **远程浏览** — 单窗口显示远程文件列表，支持目录深度导航与实时刷新。
- **拖拽上传** — 从系统资源管理器直接拖拽文件/文件夹至窗口即可上传。
- **右键下载** — 选中远程文件，右键下载到本地。
- **🔥 远程编辑** — 右键远程文件选择"本地编辑"，自动下载到临时目录并用系统默认编辑器打开；保存后自动回传至 FTP 服务器。
- **窗口置顶** — 一键开关，随时将窗口固定在最上层。
- **跨平台** — 支持 Windows、Linux、macOS。

## 远程编辑流程

```
右键远程文件 → 下载至 Temp → 调用系统编辑器 → 监听保存 → 自动上传覆盖
```

## 技术栈

| 技术 | 说明 |
|------|------|
| [Avalonia UI](https://avaloniaui.net/) | 跨平台 UI 框架 (MVVM) |
| [FluentAvalonia](https://github.com/amwx/FluentAvalonia) | Win11 Fluent Design 控件库 |
| [FluentFTP](https://github.com/robinrodricks/FluentFTP) | FTP 协议客户端 |
| C# 13 / .NET 10 | 开发语言与运行时 |

## 快速开始

### 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### 克隆 & 运行

```bash
git clone https://github.com/ygq-future/sy-ftp.git
cd sy-ftp
dotnet run
```

## 项目结构

```
SY-FTP/
├── Models/          # 数据实体 (FtpHost, RemoteFile, AppConfig)
├── ViewModels/      # MVVM 视图模型
├── Views/           # XAML 布局
├── Services/        # FTP 与文件监控服务
└── Assets/          # 图标与资源
```

## 许可

Apache-2.0 License
