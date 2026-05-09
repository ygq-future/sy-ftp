using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using sy_ftp.Models;

namespace sy_ftp.ViewModels;

public partial class HostManagerViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredHosts))]
    [NotifyPropertyChangedFor(nameof(DistinctTags))]
    [NotifyPropertyChangedFor(nameof(AllTagOptions))]
    private ObservableCollection<FtpHost> _hosts = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredHosts))]
    private FtpHost? _selectedHost;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredHosts))]
    private string _filterTag = AllTagsSentinel;

    public IEnumerable<FtpHost> FilteredHosts =>
        string.IsNullOrWhiteSpace(FilterTag) || FilterTag == AllTagsSentinel
            ? Hosts
            : Hosts.Where(h => h.TagList.Contains(FilterTag, StringComparer.OrdinalIgnoreCase));

    /// <summary>Distinct tag strings derived from all hosts (no "All" entry).</summary>
    public IEnumerable<string> DistinctTags =>
        Hosts.SelectMany(h => h.TagList)
             .Distinct(StringComparer.OrdinalIgnoreCase)
             .OrderBy(t => t);

    /// <summary>
    /// Full list for the ComboBox: "All tags" sentinel first, then every distinct tag.
    /// Selecting "All tags" (or null) shows all hosts.
    /// </summary>
    public const string AllTagsSentinel = "All tags";

    public IEnumerable<string> AllTagOptions =>
        new[] { AllTagsSentinel }.Concat(DistinctTags);

    public HostManagerViewModel()
    {
        // Default selection: show all hosts
        _filterTag = AllTagsSentinel;

        // Wire up reactivity: whenever Hosts items change, refresh computed properties.
        _hosts.CollectionChanged += OnHostsCollectionChanged;

        // ── Sample data for development / UI testing ──────────────────────────
        var samples = new[]
        {
            new FtpHost { Name = "Production Web",   Host = "ftp.example.com",    Port = 21,  Username = "webadmin",   Tags = "prod, web" },
            new FtpHost { Name = "Staging Server",   Host = "staging.example.com",Port = 21,  Username = "deploy",     Tags = "staging" },
            new FtpHost { Name = "Dev Box",          Host = "192.168.1.50",       Port = 2121,Username = "dev",        Tags = "dev, local" },
            new FtpHost { Name = "Backup Store",     Host = "backup.internal",    Port = 21,  Username = "backup",     Tags = "prod, backup" },
            new FtpHost { Name = "Media CDN",        Host = "media.cdn.example",  Port = 21,  Username = "media",      Tags = "prod" },
            new FtpHost { Name = "Anonymous Mirror", Host = "mirror.opensrc.org", Port = 21,  Username = "anonymous",  Tags = "" },
        };

        foreach (var h in samples)
            _hosts.Add(h);
    }

    // ── Collection-change handler ─────────────────────────────────────────────

    private void OnHostsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Re-subscribe to item-level changes when items are added.
        if (e.NewItems is not null)
        {
            foreach (FtpHost host in e.NewItems)
                host.PropertyChanged += OnHostPropertyChanged;
        }

        if (e.OldItems is not null)
        {
            foreach (FtpHost host in e.OldItems)
                host.PropertyChanged -= OnHostPropertyChanged;
        }

        RefreshDerivedProperties();
    }

    private void OnHostPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // A host's Tags (or any field) changed — refresh filters and tag lists.
        if (e.PropertyName is nameof(FtpHost.Tags)
                           or nameof(FtpHost.Name)
                           or nameof(FtpHost.Host))
        {
            RefreshDerivedProperties();
        }
    }

    partial void OnHostsChanged(ObservableCollection<FtpHost>? oldValue, ObservableCollection<FtpHost> newValue)
    {
        if (oldValue is not null)
            oldValue.CollectionChanged -= OnHostsCollectionChanged;

        newValue.CollectionChanged += OnHostsCollectionChanged;
    }

    private void RefreshDerivedProperties()
    {
        OnPropertyChanged(nameof(DistinctTags));
        OnPropertyChanged(nameof(AllTagOptions));
        OnPropertyChanged(nameof(FilteredHosts));
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void AddHost()
    {
        var host = new FtpHost { Name = "New Host" };
        Hosts.Add(host);
        SelectedHost = host;
    }

    [RelayCommand]
    private async Task EditHost(FtpHost? host)
    {
        if (host is null) return;
        try
        {
            var lifetime = (Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime)Avalonia.Application.Current!.ApplicationLifetime!;
            var mainWindow = lifetime.MainWindow;
            var dlg = new Views.HostEditWindow { DataContext = host };
            var result = await dlg.ShowDialog<bool?>(mainWindow);
            if (result == true)
            {
                RefreshDerivedProperties();
            }
            else
            {
                // still select host so details show
                SelectedHost = host;
            }
        }
        catch
        {
            // Fallback: select host if UI dialog cannot be shown
            SelectedHost = host;
        }
    }

    [RelayCommand]
    private void DeleteHost(FtpHost? host)
    {
        if (host is null) return;
        Hosts.Remove(host);
        if (SelectedHost == host)
            SelectedHost = Hosts.FirstOrDefault();
    }

    [RelayCommand]
    private void FilterByTag(string? tag)
    {
        FilterTag = tag ?? AllTagsSentinel;
    }
}
