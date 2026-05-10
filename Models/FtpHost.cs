using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace sy_ftp.Models;

public partial class FtpHost : ObservableObject
{
    public Guid Id { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _host = string.Empty;

    [ObservableProperty]
    private int _port = 22;

    [ObservableProperty]
    private string _username = "anonymous";

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _tags = string.Empty;

    [JsonIgnore]
    public string[] TagList => string.IsNullOrWhiteSpace(Tags)
        ? []
        : Tags.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    public FtpHost Clone() => new()
    {
        Name = Name,
        Host = Host,
        Port = Port,
        Username = Username,
        Password = Password,
        Tags = Tags
    };
}
