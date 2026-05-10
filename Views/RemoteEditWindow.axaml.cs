using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace sy_ftp.Views;

public partial class RemoteEditWindow : Window
{
    private IDisposable? _textMate;
    private string? _currentScope;

    public RemoteEditWindow()
    {
        InitializeComponent();
        ApplyShadow();
        ActualThemeVariantChanged += (_, _) =>
        {
            ApplyShadow();
            UpdateTextMateTheme();
        };
    }

    public void Load(string fileName, string content)
    {
        TitleBlock.Text = fileName;
        Editor.Text = content;
        InstallGrammar(fileName);
    }

    private void InstallGrammar(string fileName)
    {
        _textMate?.Dispose();

        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        var registry = new RegistryOptions(isDark ? ThemeName.DarkPlus : ThemeName.LightPlus);

        dynamic installation = Editor.InstallTextMate(registry);
        _textMate = installation;

        var ext = Path.GetExtension(fileName);
        var lang = registry.GetLanguageByExtension(ext);
        if (lang is not null)
        {
            _currentScope = registry.GetScopeByLanguageId(lang.Id);
            installation.SetGrammar(_currentScope);
            LangLabel.Text = lang.Id;
        }
        else
        {
            _currentScope = null;
            LangLabel.Text = "plain text";
        }
    }

    private void UpdateTextMateTheme()
    {
        if (_textMate is null) return;
        var title = TitleBlock.Text ?? "";
        InstallGrammar(title);
    }

    private void ApplyShadow()
    {
        if (CardBorder is null) return;
        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        CardBorder.BoxShadow = isDark
            ? BoxShadows.Parse("0 0 24 0 #18FFFFFF")
            : BoxShadows.Parse("0 0 16 0 #0C000000");
    }

    private void OnTitleBarDrag(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close(null);
    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(null);
    private void OnSaveClick(object? sender, RoutedEventArgs e) => Close(Editor.Text);

    protected override void OnClosed(EventArgs e)
    {
        _textMate?.Dispose();
        base.OnClosed(e);
    }
}
