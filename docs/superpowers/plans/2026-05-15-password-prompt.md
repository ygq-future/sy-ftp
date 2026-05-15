# 临时密码输入功能实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 当 host 没有保存密码时，连接前弹出密码输入对话框，支持临时输入或持久化保存

**Architecture:** 创建独立的 PasswordDialog，在 MainWindowViewModel 中添加集中式密码检查方法 PromptPasswordIfNeededAsync，所有连接逻辑在调用 FtpService.ConnectAsync 前先检查密码

**Tech Stack:** Avalonia UI 12.x, CommunityToolkit.Mvvm, Semi.Avalonia, PhosphorIconsAvalonia

---

## File Structure

**New files:**
- `Views/PasswordDialog.axaml` - 密码输入对话框 XAML 视图
- `Views/PasswordDialog.axaml.cs` - 密码输入对话框代码隐藏

**Modified files:**
- `ViewModels/MainWindowViewModel.cs` - 添加 PromptPasswordIfNeededAsync 方法，修改 ConnectAsync 和 EnsureSessionAsync
- `Resources/Strings.cs` - 添加本地化字符串

---

### Task 1: 添加本地化字符串

**Files:**
- Modify: `Resources/Strings.cs:169` (En dictionary 末尾)
- Modify: `Resources/Strings.cs:333` (Zh dictionary 末尾)

- [ ] **Step 1: 在英文字典中添加密码对话框翻译键**

在 `Strings.cs` 的 `En` 字典中，在第 169 行 `["settings.btn.close"] = "Close",` 之后添加：

```csharp
        // Password dialog
        ["password.dialog.title"] = "Connect to {0}",
        ["password.dialog.label"] = "Password",
        ["password.dialog.remember"] = "Remember password",
        ["password.btn.cancel"] = "Cancel",
        ["password.btn.ok"] = "OK",
        ["status.cancelled"] = "Connection cancelled",
```

- [ ] **Step 2: 在中文字典中添加密码对话框翻译键**

在 `Strings.cs` 的 `Zh` 字典中，在第 333 行 `["settings.btn.close"] = "关闭",` 之后添加：

```csharp
        // Password dialog
        ["password.dialog.title"] = "连接到 {0}",
        ["password.dialog.label"] = "密码",
        ["password.dialog.remember"] = "记住密码",
        ["password.btn.cancel"] = "取消",
        ["password.btn.ok"] = "确定",
        ["status.cancelled"] = "连接已取消",
```

- [ ] **Step 3: 构建项目验证语法**

```bash
dotnet build
```

Expected: 构建成功，无编译错误

- [ ] **Step 4: 提交本地化字符串**

```bash
git add Resources/Strings.cs
git commit -m "feat: 添加密码对话框本地化字符串"
```

---

### Task 2: 创建 PasswordDialog XAML 视图

**Files:**
- Create: `Views/PasswordDialog.axaml`

- [ ] **Step 1: 创建 PasswordDialog.axaml 文件**

创建文件 `Views/PasswordDialog.axaml`，内容如下：

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:pia="using:PhosphorIconsAvalonia.Markup"
        xmlns:l="using:sy_ftp.Markup"
        x:Class="sy_ftp.Views.PasswordDialog"
        Width="360" MinHeight="240" SizeToContent="Height"
        WindowDecorations="None"
        CanResize="False"
        WindowStartupLocation="CenterOwner"
        Background="Transparent">

    <Window.Styles>
        <!-- Dialog card entry animation: fade + slight scale-up -->
        <Style Selector="Border.dialog-card">
            <Setter Property="RenderTransformOrigin" Value="50%,50%" />
            <Style.Animations>
                <Animation Duration="0:0:0.22" Easing="CubicEaseOut" FillMode="Forward">
                    <KeyFrame Cue="0%">
                        <Setter Property="Opacity" Value="0.0" />
                        <Setter Property="ScaleTransform.ScaleX" Value="0.94" />
                        <Setter Property="ScaleTransform.ScaleY" Value="0.94" />
                    </KeyFrame>
                    <KeyFrame Cue="100%">
                        <Setter Property="Opacity" Value="1.0" />
                        <Setter Property="ScaleTransform.ScaleX" Value="1.0" />
                        <Setter Property="ScaleTransform.ScaleY" Value="1.0" />
                    </KeyFrame>
                </Animation>
            </Style.Animations>
        </Style>

        <!-- Dialog buttons: smooth hover transitions -->
        <Style Selector="Button.dialog-btn-primary /template/ ContentPresenter">
            <Setter Property="Transitions">
                <Transitions>
                    <BrushTransition Property="Background" Duration="0:0:0.18" />
                </Transitions>
            </Setter>
        </Style>
        <Style Selector="Button.dialog-btn-primary:pointerover /template/ ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource SemiColorPrimaryPointerover}" />
        </Style>
        <Style Selector="Button.dialog-btn-primary:pressed /template/ ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource SemiColorPrimaryActive}" />
        </Style>

        <Style Selector="Button.dialog-btn-outline /template/ ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource SemiColorFill1Start}" />
            <Setter Property="Transitions">
                <Transitions>
                    <BrushTransition Property="Background" Duration="0:0:0.18" />
                </Transitions>
            </Setter>
        </Style>
        <Style Selector="Button.dialog-btn-outline:pointerover /template/ ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource SemiColorFill1}" />
        </Style>
        <Style Selector="Button.dialog-btn-outline:pressed /template/ ContentPresenter">
            <Setter Property="Background" Value="{DynamicResource SemiColorFill2}" />
        </Style>
    </Window.Styles>

    <Border x:Name="CardBorder"
            Classes="dialog-card"
            CornerRadius="12" Margin="8" Padding="24"
            Background="{DynamicResource SemiColorBackground1}"
            BorderBrush="{DynamicResource SemiColorBorder}"
            BorderThickness="1"
            PointerPressed="OnCardDrag">
        <Border.RenderTransform>
            <ScaleTransform ScaleX="1" ScaleY="1" />
        </Border.RenderTransform>
        <Grid RowDefinitions="Auto,Auto,Auto,Auto">

            <!-- Header -->
            <StackPanel Orientation="Horizontal" Spacing="10" Margin="0,0,0,12">
                <PathIcon Data="{pia:IconGeometry Icon=lock_key, IconType=regular}"
                          Width="20" Height="20"
                          Foreground="{DynamicResource SemiColorPrimary}" />
                <TextBlock x:Name="TitleBlock" Text="Connect to Host"
                           FontSize="16" FontWeight="SemiBold"
                           Foreground="{DynamicResource SemiColorText0}"
                           VerticalAlignment="Center" />
            </StackPanel>

            <!-- Password Input -->
            <StackPanel Grid.Row="1" Spacing="2">
                <TextBlock x:Name="LabelBlock" FontSize="12"
                           Foreground="{DynamicResource SemiColorText2}" />
                <TextBox x:Name="PasswordBox"
                         PasswordChar="●"
                         KeyDown="OnPasswordKeyDown"
                         Background="{DynamicResource SemiColorFill0}"
                         BorderBrush="{DynamicResource SemiColorBorder}"
                         CornerRadius="6" Padding="10,7" />
                <TextBlock x:Name="ErrorText"
                           IsVisible="False"
                           FontSize="11" Margin="2,2,0,0"
                           Foreground="{DynamicResource SemiColorDanger}" />
            </StackPanel>

            <!-- Remember Checkbox -->
            <CheckBox Grid.Row="2" x:Name="RememberCheckBox"
                      Margin="0,12,0,0"
                      Foreground="{DynamicResource SemiColorText1}" />

            <!-- Buttons -->
            <StackPanel Grid.Row="3" Orientation="Horizontal"
                        HorizontalAlignment="Right" Spacing="8" Margin="0,16,0,0">
                <Button x:Name="CancelButton" Click="Cancel_Click"
                        Classes="dialog-btn-outline"
                        Background="Transparent"
                        BorderBrush="{DynamicResource SemiColorBorder}"
                        BorderThickness="1"
                        Foreground="{DynamicResource SemiColorText1}"
                        CornerRadius="6" Padding="14,6" />
                <Button x:Name="OkButton" Click="Ok_Click"
                        Classes="dialog-btn-primary"
                        Background="{DynamicResource SemiColorPrimary}"
                        Foreground="White"
                        BorderThickness="0"
                        CornerRadius="6" Padding="14,6" />
            </StackPanel>

        </Grid>
    </Border>
</Window>
```

- [ ] **Step 2: 构建项目验证 XAML 语法**

```bash
dotnet build
```

Expected: 构建失败，提示 `PasswordDialog` 类不存在（这是预期的，下一个任务会创建代码隐藏文件）

- [ ] **Step 3: 提交 XAML 文件**

```bash
git add Views/PasswordDialog.axaml
git commit -m "feat: 添加密码对话框 XAML 视图"
```

---

### Task 3: 创建 PasswordDialog 代码隐藏

**Files:**
- Create: `Views/PasswordDialog.axaml.cs`

- [ ] **Step 1: 创建 PasswordDialog.axaml.cs 文件**

创建文件 `Views/PasswordDialog.axaml.cs`，内容如下：

```csharp
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using sy_ftp.Services;

namespace sy_ftp.Views;

public partial class PasswordDialog : Window
{
    private readonly LocalizationService _loc = LocalizationService.Instance;

    public PasswordDialog()
    {
        InitializeComponent();
        ApplyShadow();
        ActualThemeVariantChanged += (_, _) => ApplyShadow();
        
        Opened += (_, _) =>
        {
            PasswordBox.Focus();
        };
        
        PasswordBox.TextChanged += (_, _) =>
        {
            if (!string.IsNullOrWhiteSpace(PasswordBox.Text))
                SetError(false);
        };

        LabelBlock.Text = _loc.Tr("password.dialog.label");
        RememberCheckBox.Content = _loc.Tr("password.dialog.remember");
        CancelButton.Content = _loc.Tr("password.btn.cancel");
        OkButton.Content = _loc.Tr("password.btn.ok");
    }

    private void ApplyShadow()
    {
        if (CardBorder is null) return;
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        CardBorder.BoxShadow = isDark
            ? BoxShadows.Parse("0 0 24 0 #18FFFFFF")
            : BoxShadows.Parse("0 0 16 0 #0C000000");
    }

    public string HostName
    {
        get => TitleBlock.Text ?? "";
        set
        {
            var title = _loc.Tr("password.dialog.title", value);
            TitleBlock.Text = title;
            Title = title;
        }
    }

    public string Password { get; private set; } = string.Empty;

    public bool RememberPassword { get; private set; }

    private void SetError(bool hasError, string? message = null)
    {
        ErrorText.IsVisible = hasError;
        ErrorText.Text = message ?? _loc.Tr("input.error.required");
        if (hasError)
        {
            if (!PasswordBox.Classes.Contains("error")) PasswordBox.Classes.Add("error");
        }
        else
        {
            PasswordBox.Classes.Remove("error");
        }
    }

    private void Ok_Click(object? sender, RoutedEventArgs e)
    {
        var text = PasswordBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            SetError(true);
            PasswordBox.Focus();
            return;
        }
        SetError(false);
        Password = text;
        RememberPassword = RememberCheckBox.IsChecked == true;
        Close(true);
    }

    private void OnPasswordKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Ok_Click(sender, e);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(false);
        }
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnCardDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is TextBox or Button or CheckBox) return;
        BeginMoveDrag(e);
    }
}
```

- [ ] **Step 2: 构建项目验证代码**

```bash
dotnet build
```

Expected: 构建成功，无编译错误

- [ ] **Step 3: 运行应用验证对话框可以实例化**

```bash
dotnet run
```

Expected: 应用正常启动（对话框尚未集成到连接流程，所以不会显示）

- [ ] **Step 4: 提交代码隐藏文件**

```bash
git add Views/PasswordDialog.axaml.cs
git commit -m "feat: 添加密码对话框代码隐藏逻辑"
```

---

### Task 4: 在 MainWindowViewModel 中添加 PromptPasswordIfNeededAsync 方法

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs:186` (在 OpenSettingsAsync 方法之后)

- [ ] **Step 1: 添加 PromptPasswordIfNeededAsync 方法**

在 `MainWindowViewModel.cs` 的第 186 行 `OpenSettingsAsync` 方法之后添加：

```csharp
    private async Task<string?> PromptPasswordIfNeededAsync(FtpHost host)
    {
        if (!string.IsNullOrEmpty(host.Password))
            return host.Password;

        var lifetime = Application.Current?.ApplicationLifetime
            as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
        var mainWindow = lifetime?.MainWindow;
        if (mainWindow is null) return null;

        var dialog = new Views.PasswordDialog { HostName = host.Name };
        var result = await dialog.ShowDialog<bool>(mainWindow);

        if (!result) return null;

        var password = dialog.Password;

        if (dialog.RememberPassword)
        {
            host.Password = password;
            SaveConfig();
        }

        return password;
    }
```

- [ ] **Step 2: 构建项目验证语法**

```bash
dotnet build
```

Expected: 构建成功，无编译错误

- [ ] **Step 3: 提交 PromptPasswordIfNeededAsync 方法**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "feat: 添加密码提示检查方法"
```

---

### Task 5: 修改 ConnectAsync 集成密码检查

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs:188-226` (ConnectAsync 方法)

- [ ] **Step 1: 修改 ConnectAsync 方法添加密码检查**

在 `MainWindowViewModel.cs` 中，找到 `ConnectAsync` 方法（约第 188 行），将其替换为：

```csharp
    [RelayCommand]
    private async Task ConnectAsync(CancellationToken ct)
    {
        var host = HostManager.SelectedHost;
        if (host is null) return;

        if (_sessions.TryGetValue(host.Id, out var existing))
        {
            await FileBrowser.ActivateSessionAsync(existing, ct);
            IsConnected = true;
            StatusText = Loc.Tr("status.connected", host.Name);
            return;
        }

        var password = await PromptPasswordIfNeededAsync(host);
        if (password is null)
        {
            StatusText = Loc.Tr("status.cancelled");
            return;
        }

        IsBusy = true;
        StatusText = Loc.Tr("status.connecting");
        var ftp = new FtpService();
        try
        {
            var originalPassword = host.Password;
            if (string.IsNullOrEmpty(host.Password))
                host.Password = password;
            
            await ftp.ConnectAsync(host, ct);
            
            if (string.IsNullOrEmpty(originalPassword))
                host.Password = originalPassword;

            var homeDir = await ftp.GetWorkingDirectoryAsync(ct);
            var session = new HostSession { HostId = host.Id, Host = host, Ftp = ftp, CurrentPath = homeDir };
            _sessions[host.Id] = session;
            host.IsConnected = true;

            await FileBrowser.ActivateSessionAsync(session, ct);
            IsConnected = true;
            StatusText = Loc.Tr("status.connected", host.Name);
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

- [ ] **Step 2: 构建项目验证语法**

```bash
dotnet build
```

Expected: 构建成功，无编译错误

- [ ] **Step 3: 运行应用测试密码提示功能**

```bash
taskkill /F /IM sy-ftp.exe /T 2>$null; dotnet build && dotnet run
```

Expected: 应用启动，选择一个没有密码的 host 并点击连接，应该弹出密码输入对话框

- [ ] **Step 4: 手动测试场景**

测试以下场景：
1. 输入密码但不勾选 Remember，点击确定 → 应该成功连接
2. 断开连接，再次连接 → 应该再次弹出密码对话框
3. 输入密码并勾选 Remember，点击确定 → 应该成功连接
4. 断开连接，再次连接 → 应该直接连接，不弹出对话框
5. 点击取消 → 状态栏显示"连接已取消"

- [ ] **Step 5: 提交 ConnectAsync 修改**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "feat: 在 ConnectAsync 中集成密码检查"
```

---

### Task 6: 修改 EnsureSessionAsync 集成密码检查

**Files:**
- Modify: `ViewModels/MainWindowViewModel.cs:263-275` (EnsureSessionAsync 方法)

- [ ] **Step 1: 修改 EnsureSessionAsync 方法添加密码检查**

在 `MainWindowViewModel.cs` 中，找到 `EnsureSessionAsync` 方法（约第 263 行），将其替换为：

```csharp
    public async Task<HostSession> EnsureSessionAsync(FtpHost host, CancellationToken ct)
    {
        if (_sessions.TryGetValue(host.Id, out var existing))
            return existing;

        var password = await PromptPasswordIfNeededAsync(host);
        if (password is null)
            throw new OperationCanceledException("User cancelled password input");

        var ftp = new FtpService();
        
        var originalPassword = host.Password;
        if (string.IsNullOrEmpty(host.Password))
            host.Password = password;
        
        await ftp.ConnectAsync(host, ct);
        
        if (string.IsNullOrEmpty(originalPassword))
            host.Password = originalPassword;

        var homeDir = await ftp.GetWorkingDirectoryAsync(ct);
        var session = new HostSession { HostId = host.Id, Host = host, Ftp = ftp, CurrentPath = homeDir };
        _sessions[host.Id] = session;
        host.IsConnected = true;
        return session;
    }
```

- [ ] **Step 2: 构建项目验证语法**

```bash
dotnet build
```

Expected: 构建成功，无编译错误

- [ ] **Step 3: 运行应用测试 Transfer To 功能**

```bash
taskkill /F /IM sy-ftp.exe /T 2>$null; dotnet build && dotnet run
```

Expected: 应用启动，连接一个 host，选择文件，右键选择"Transfer to..."，选择一个没有密码的目标 host 并点击连接，应该弹出密码输入对话框

- [ ] **Step 4: 手动测试 Transfer To 场景**

测试以下场景：
1. Transfer To 对话框中选择无密码 host → 点击连接 → 应该弹出密码对话框
2. 输入密码并勾选 Remember → 应该成功连接并显示目录列表
3. 点击取消 → 应该关闭对话框，不显示错误

- [ ] **Step 5: 提交 EnsureSessionAsync 修改**

```bash
git add ViewModels/MainWindowViewModel.cs
git commit -m "feat: 在 EnsureSessionAsync 中集成密码检查"
```

---

### Task 7: 最终集成测试

**Files:**
- Test: 整个应用的密码提示功能

- [ ] **Step 1: 创建测试用无密码 host**

运行应用，添加一个新的 host，填写名称、地址、端口、用户名，但**不填写密码**，保存

- [ ] **Step 2: 测试主动连接场景**

1. 选择无密码 host，点击"连接"按钮
2. 验证：弹出密码对话框，标题显示"连接到 {host名称}"
3. 输入错误密码，不勾选 Remember，点击确定
4. 验证：连接失败，状态栏显示错误信息
5. 再次点击"连接"按钮
6. 验证：再次弹出密码对话框（因为没有勾选 Remember）
7. 输入正确密码，勾选 Remember，点击确定
8. 验证：连接成功，文件列表显示

- [ ] **Step 3: 测试 Remember 功能**

1. 断开连接
2. 再次点击"连接"按钮
3. 验证：直接连接成功，不弹出密码对话框（因为密码已保存）

- [ ] **Step 4: 测试取消场景**

1. 编辑 host，清空密码字段，保存
2. 点击"连接"按钮
3. 弹出密码对话框后，点击"取消"按钮
4. 验证：状态栏显示"连接已取消"，不显示错误

- [ ] **Step 5: 测试 Transfer To 场景**

1. 连接一个有密码的 host A
2. 选择一个文件，右键选择"Transfer to..."
3. 在对话框中选择无密码的 host B，点击"连接"
4. 验证：弹出密码对话框
5. 输入密码，勾选 Remember，点击确定
6. 验证：连接成功，显示 host B 的目录列表
7. 主窗口侧边栏验证：host B 显示为已连接状态（绿色指示器）

- [ ] **Step 6: 测试键盘快捷键**

1. 编辑 host，清空密码，保存
2. 点击"连接"按钮
3. 在密码对话框中输入密码，按 Enter 键
4. 验证：对话框关闭，连接成功
5. 断开连接，再次点击"连接"
6. 在密码对话框中按 Escape 键
7. 验证：对话框关闭，状态栏显示"连接已取消"

- [ ] **Step 7: 测试多语言支持**

1. 打开设置，切换语言为中文
2. 编辑 host，清空密码，保存
3. 点击"连接"按钮
4. 验证：密码对话框标题、标签、按钮文本均为中文
5. 点击取消
6. 验证：状态栏显示"连接已取消"（中文）

- [ ] **Step 8: 最终构建验证**

```bash
dotnet build --configuration Release
```

Expected: Release 构建成功，无警告

- [ ] **Step 9: 创建最终提交**

```bash
git add -A
git commit -m "feat: 完成临时密码输入功能

- 添加 PasswordDialog 对话框（XAML + 代码隐藏）
- 在 MainWindowViewModel 中添加 PromptPasswordIfNeededAsync 方法
- 修改 ConnectAsync 和 EnsureSessionAsync 集成密码检查
- 添加本地化字符串支持（中英文）
- 支持 Remember 选项持久化密码
- 支持键盘快捷键（Enter 确认、Escape 取消）
- 覆盖所有连接场景（主动连接、Transfer To）"
```

---

## Self-Review Checklist

**Spec coverage:**
- ✅ 密码检查触发时机：Task 5 (ConnectAsync) + Task 6 (EnsureSessionAsync)
- ✅ 用户取消处理：Task 5 (status.cancelled)
- ✅ Remember 选项：Task 3 (UI) + Task 4 (逻辑)
- ✅ 密码持久化：Task 4 (SaveConfig)
- ✅ 密码遮罩显示：Task 2 (PasswordChar="●")
- ✅ 对话框样式一致性：Task 2 (复用 InputDialog 样式)
- ✅ 键盘快捷键：Task 3 (OnPasswordKeyDown)
- ✅ 本地化支持：Task 1

**Placeholder scan:**
- ✅ 无 TBD、TODO
- ✅ 所有代码块完整
- ✅ 所有命令具体
- ✅ 所有文件路径明确

**Type consistency:**
- ✅ `PromptPasswordIfNeededAsync` 返回 `Task<string?>`，所有调用点一致
- ✅ `PasswordDialog.Password` 为 `string`，`RememberPassword` 为 `bool`
- ✅ 本地化键名在所有任务中一致

**Architecture alignment:**
- ✅ 集中式密码检查方案（Task 4）
- ✅ 所有连接点集成（Task 5, Task 6）
- ✅ UI 层独立（Task 2, Task 3）
- ✅ 本地化支持（Task 1）
