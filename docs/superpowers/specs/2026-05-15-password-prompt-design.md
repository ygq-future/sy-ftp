# 临时密码输入功能设计

**日期：** 2026-05-15  
**状态：** 已批准

## 概述

当 FTP host 配置中没有保存密码时（`Password` 字段为空），在任何需要连接的场景下都弹出密码输入对话框，允许用户临时输入密码或选择持久化保存。

## 需求

### 功能需求

1. **密码检查触发时机**：在任何需要连接的场景（主动连接、切换 host、Transfer To 等），如果目标 host 的 `Password` 为空，则弹出密码输入对话框
2. **用户取消处理**：如果用户点击"取消"，在状态栏显示"连接已取消"，不显示错误
3. **Remember 选项**：用户可以勾选"Remember"复选框，将本次输入的密码持久化到 host 配置文件
4. **密码持久化**：勾选 Remember 后，密码立即保存到配置文件（与手动编辑 host 保存密码的行为一致）

### 非功能需求

1. 密码输入框使用遮罩显示（`PasswordRevealMode="Hidden"`）
2. 对话框样式与现有 `InputDialog` 保持一致（卡片动画、按钮样式、拖拽移动）
3. 支持键盘快捷键（Enter 确认、Escape 取消）

## 架构设计

### 方案选择

采用**集中式密码检查方案**：

- 在 `MainWindowViewModel` 中添加 `PromptPasswordIfNeededAsync` 方法
- 所有连接逻辑在调用 `FtpService.ConnectAsync` 之前都先调用此方法
- 优点：逻辑集中、易于维护、覆盖所有连接场景
- 缺点：ViewModel 需要持有窗口引用（已有先例：`OpenSettingsAsync`）

### 组件结构

```
UI 层
├── PasswordDialog.axaml          # 密码输入对话框视图
└── PasswordDialog.axaml.cs       # 对话框代码隐藏

ViewModel 层
└── MainWindowViewModel.cs
    └── PromptPasswordIfNeededAsync(FtpHost)  # 密码检查和提示逻辑

集成点
├── ConnectAsync                  # 主动连接
└── EnsureSessionAsync            # Transfer To 等场景
```

## 详细设计

### 1. PasswordDialog UI

**文件：** `Views/PasswordDialog.axaml` / `.axaml.cs`

**布局：**
- 标题：显示 host 名称（如"连接到 {host.Name}"）
- 密码输入框：`TextBox` with `PasswordChar="●"` 或使用 Avalonia 的密码控件
- Remember 复选框：`CheckBox` with label "记住密码"
- 按钮：取消（outline）、确定（primary）

**样式继承：**
- 卡片容器：`CornerRadius="12"`, `BoxShadow`, 入场动画
- 按钮样式：复用 `InputDialog` 的 `dialog-btn-primary` / `dialog-btn-outline`
- 拖拽移动：`PointerPressed` 事件处理

**公开属性：**
```csharp
public string HostName { get; set; }           // 显示在标题中
public string Password { get; private set; }   // 用户输入的密码
public bool RememberPassword { get; private set; }  // Remember 复选框状态
```

**返回值：**
- 用户点击"确定"：`Close(true)`，调用方可通过 `Password` 和 `RememberPassword` 获取结果
- 用户点击"取消"或 Escape：`Close(false)`

### 2. MainWindowViewModel 集成

**新增方法：**

```csharp
/// <summary>
/// 检查 host 是否需要密码，如果需要则弹出对话框。
/// </summary>
/// <returns>最终使用的密码，如果用户取消则返回 null</returns>
private async Task<string?> PromptPasswordIfNeededAsync(FtpHost host)
{
    // 如果已有密码，直接返回
    if (!string.IsNullOrEmpty(host.Password))
        return host.Password;

    // 弹出密码输入对话框
    var lifetime = Application.Current?.ApplicationLifetime
        as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
    var mainWindow = lifetime?.MainWindow;
    if (mainWindow is null) return null;

    var dialog = new Views.PasswordDialog { HostName = host.Name };
    var result = await dialog.ShowDialog<bool>(mainWindow);

    if (!result) return null;  // 用户取消

    var password = dialog.Password;

    // 如果用户勾选 Remember，持久化密码
    if (dialog.RememberPassword)
    {
        host.Password = password;
        SaveConfig();
    }

    return password;
}
```

**修改 ConnectAsync：**

```csharp
[RelayCommand]
private async Task ConnectAsync(CancellationToken ct)
{
    var host = HostManager.SelectedHost;
    if (host is null) return;

    // 已连接场景保持不变
    if (_sessions.TryGetValue(host.Id, out var existing))
    {
        await FileBrowser.ActivateSessionAsync(existing, ct);
        IsConnected = true;
        StatusText = Loc.Tr("status.connected", host.Name);
        return;
    }

    // 检查密码
    var password = await PromptPasswordIfNeededAsync(host);
    if (password is null)
    {
        StatusText = Loc.Tr("status.cancelled");  // 新增翻译键
        return;
    }

    IsBusy = true;
    StatusText = Loc.Tr("status.connecting");
    var ftp = new FtpService();
    try
    {
        // 使用临时 host 副本（包含密码）进行连接
        var hostWithPassword = host.Password == password ? host : new FtpHost
        {
            Host = host.Host,
            Port = host.Port,
            Username = host.Username,
            Password = password,
        };
        await ftp.ConnectAsync(hostWithPassword, ct);
        
        // ... 其余连接逻辑保持不变
    }
    catch (Exception ex)
    {
        try { await ftp.DisconnectAsync(CancellationToken.None); } catch { }
        StatusText = Loc.Tr("status.error", ex.Message);
    }
    finally
    {
        IsBusy = false;
    }
}
```

**修改 EnsureSessionAsync：**

同样在连接前调用 `PromptPasswordIfNeededAsync`，如果返回 `null` 则抛出 `OperationCanceledException`。

### 3. 本地化支持

**新增翻译键：**

```json
{
  "password.dialog.title": "连接到 {0}",
  "password.dialog.label": "密码",
  "password.dialog.remember": "记住密码",
  "status.cancelled": "连接已取消"
}
```

### 4. 数据流

```
用户触发连接
    ↓
MainWindowViewModel.ConnectAsync
    ↓
检查 host.Password 是否为空
    ↓ (为空)
弹出 PasswordDialog
    ↓
用户输入密码 + 勾选 Remember
    ↓
PromptPasswordIfNeededAsync 返回密码
    ↓
如果 Remember = true，写入 host.Password 并 SaveConfig()
    ↓
使用密码调用 FtpService.ConnectAsync
    ↓
连接成功，创建 session
```

## 错误处理

1. **用户取消输入**：`PromptPasswordIfNeededAsync` 返回 `null`，调用方设置状态为"连接已取消"并提前返回
2. **密码错误**：`FtpService.ConnectAsync` 抛出异常，显示错误信息（现有逻辑）
3. **窗口引用为空**：返回 `null`，视为取消操作

## 测试场景

1. **主动连接无密码 host**：弹出对话框，输入密码后成功连接
2. **勾选 Remember**：密码保存到配置文件，下次连接不再提示
3. **不勾选 Remember**：本次连接成功，断开后重新连接仍需输入密码
4. **用户取消输入**：状态栏显示"连接已取消"，不显示错误
5. **Transfer To 无密码 host**：同样弹出对话框
6. **密码错误**：显示连接错误，用户可以重新尝试连接（再次弹出对话框）

## 未来扩展

1. 添加"显示密码"按钮（眼睛图标）
2. 支持密钥文件认证（SFTP）
3. 记住密码的过期策略（如 30 天后重新输入）
