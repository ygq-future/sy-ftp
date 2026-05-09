using FluentFTP;
using static FluentFTP.Helpers.Enums;
using sy_ftp.Models;

namespace sy_ftp.Services;

public class FtpService : IFtpService, IDisposable
{
    private AsyncFtpClient? _client;

    public bool IsConnected => _client?.IsConnected ?? false;

    public async Task ConnectAsync(FtpHost host, CancellationToken ct = default)
    {
        if (_client?.IsConnected == true)
            await DisconnectAsync(ct);

        _client = new AsyncFtpClient(host.Host, host.Username, host.Password, host.Port);
        await _client.AutoConnect(ct);
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_client is not null)
        {
            await _client.Disconnect(ct);
            _client.Dispose();
            _client = null;
        }
    }

    public async Task<IReadOnlyList<RemoteFile>> ListDirectoryAsync(string path = "/", CancellationToken ct = default)
    {
        EnsureConnected();
        var items = await _client!.GetListing(path, ct);
        return items
            .Where(i => i.Name is not "." and not "..")
            .Select(i => new RemoteFile(
                i.Name,
                i.FullName,
                i.Size,
                i.Type == FtpObjectType.Directory,
                i.RawModified))
            .ToArray();
    }

    public async Task DownloadFileAsync(string remotePath, string localPath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        EnsureConnected();
        var p = progress is not null
            ? new Progress<FtpProgress>(fp => progress.Report(fp.Progress))
            : null;
        var status = await _client!.DownloadFile(localPath, remotePath, FtpLocalExists.Overwrite, token: ct, progress: p);
        if (!status.IsSuccess())
            throw new InvalidOperationException($"Download failed: {remotePath}");
    }

    public async Task UploadFileAsync(string localPath, string remotePath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        EnsureConnected();
        var p = progress is not null
            ? new Progress<FtpProgress>(fp => progress.Report(fp.Progress))
            : null;
        var status = await _client!.UploadFile(localPath, remotePath, FtpRemoteExists.Overwrite, token: ct, progress: p);
        if (!status.IsSuccess())
            throw new InvalidOperationException($"Upload failed: {localPath}");
    }

    public async Task DeleteFileAsync(string remotePath, CancellationToken ct = default)
    {
        EnsureConnected();
        await _client!.DeleteFile(remotePath, ct);
    }

    public async Task CreateDirectoryAsync(string remotePath, CancellationToken ct = default)
    {
        EnsureConnected();
        await _client!.CreateDirectory(remotePath, ct);
    }

    public async Task<string> GetWorkingDirectoryAsync(CancellationToken ct = default)
    {
        EnsureConnected();
        return await _client!.GetWorkingDirectory(ct);
    }

    public async Task ChangeDirectoryAsync(string remotePath, CancellationToken ct = default)
    {
        EnsureConnected();
        await _client!.SetWorkingDirectory(remotePath, ct);
    }

    public async Task<bool> FileExistsAsync(string remotePath, CancellationToken ct = default)
    {
        EnsureConnected();
        return await _client!.FileExists(remotePath, ct);
    }

    private void EnsureConnected()
    {
        if (_client is not { IsConnected: true })
            throw new InvalidOperationException("Not connected to FTP server.");
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
