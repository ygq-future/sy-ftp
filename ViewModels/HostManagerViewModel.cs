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

    public event EventHandler? HostDataChanged;

    public HostManagerViewModel()
    {
        _filterTag = AllTagsSentinel;
        _hosts.CollectionChanged += OnHostsCollectionChanged;
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
        HostDataChanged?.Invoke(this, EventArgs.Empty);
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AddHost()
    {
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime
                as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWindow = lifetime?.MainWindow;
            if (mainWindow is null) return;

            var host = new FtpHost();
            var dlg = new Views.HostEditWindow { DataContext = host, Title = "Add Host" };
            var result = await dlg.ShowDialog<bool?>(mainWindow);
            if (result == true)
            {
                Hosts.Add(host);
                SelectedHost = host;
                RefreshDerivedProperties();
            }
        }
        catch { }
    }

    [RelayCommand]
    private async Task EditHost(FtpHost? host)
    {
        if (host is null) return;
        try
        {
            var lifetime = Avalonia.Application.Current?.ApplicationLifetime
                as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            var mainWindow = lifetime?.MainWindow;
            if (mainWindow is null)
            {
                SelectedHost = host;
                return;
            }

            // Work on a clone so Cancel truly reverts changes
            var clone = host.Clone();
            var dlg = new Views.HostEditWindow { DataContext = clone, Title = "Edit Host" };
            var result = await dlg.ShowDialog<bool?>(mainWindow);
            if (result == true)
            {
                host.Name = clone.Name;
                host.Host = clone.Host;
                host.Port = clone.Port;
                host.Username = clone.Username;
                host.Password = clone.Password;
                host.Tags = clone.Tags;
                RefreshDerivedProperties();
            }
            SelectedHost = host;
        }
        catch
        {
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
