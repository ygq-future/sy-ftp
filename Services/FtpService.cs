using FluentFTP;
using static FluentFTP.Helpers.Enums;
using sy_ftp.Models;
using Renci.SshNet;
using SftpClient = Renci.SshNet.SftpClient;

namespace sy_ftp.Services;

public class FtpService : IFtpService, IDisposable
{
    private AsyncFtpClient? _ftpClient;
    private SftpClient? _sftpClient;
    private bool _isSftp;

    public bool IsConnected => _isSftp
        ? _sftpClient?.IsConnected ?? false
        : _ftpClient?.IsConnected ?? false;

    public async Task ConnectAsync(FtpHost host, CancellationToken ct = default)
    {
        await DisconnectAsync(ct);

        _isSftp = host.Port == 22;

        if (_isSftp)
        {
            _sftpClient = new SftpClient(host.Host, host.Port, host.Username, host.Password);
            await _sftpClient.ConnectAsync(ct);
        }
        else
        {
            _ftpClient = new AsyncFtpClient(host.Host, host.Username, host.Password, host.Port);
            _ftpClient.Config.EncryptionMode = FtpEncryptionMode.Auto;
            await _ftpClient.AutoConnect(ct);
        }
    }

    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_sftpClient is not null)
        {
            if (_sftpClient.IsConnected)
                _sftpClient.Disconnect();
            _sftpClient.Dispose();
            _sftpClient = null;
        }

        if (_ftpClient is not null)
        {
            if (_ftpClient.IsConnected)
                await _ftpClient.Disconnect(ct);
            _ftpClient.Dispose();
            _ftpClient = null;
        }

        _isSftp = false;
    }

    public async Task<IReadOnlyList<RemoteFile>> ListDirectoryAsync(string path = "/", CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            var items = await Task.Run(() => _sftpClient!.ListDirectory(path), ct);
            return items
                .Where(i => i.Name is not "." and not "..")
                .Select(i => new RemoteFile(
                    i.Name,
                    i.FullName,
                    i.Length,
                    i.IsDirectory,
                    i.LastWriteTime))
                .ToArray();
        }
        else
        {
            EnsureFtpConnected();
            var items = await _ftpClient!.GetListing(path, ct);
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
    }

    public async Task DownloadFileAsync(string remotePath, string localPath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            await using var fs = File.Create(localPath);
            await Task.Run(() =>
            {
                _sftpClient!.DownloadFile(remotePath, fs, progress is not null
                    ? downloaded =>
                    {
                        // SSH.NET callback gives ulong bytes downloaded; we just fire the progress
                        progress.Report(0);
                    }
                    : null);
            }, ct);
        }
        else
        {
            EnsureFtpConnected();
            var p = progress is not null
                ? new Progress<FtpProgress>(fp => progress.Report(fp.Progress))
                : null;
            var status = await _ftpClient!.DownloadFile(localPath, remotePath, FtpLocalExists.Overwrite, token: ct, progress: p);
            if (!status.IsSuccess())
                throw new InvalidOperationException($"Download failed: {remotePath}");
        }
    }

    public async Task UploadFileAsync(string localPath, string remotePath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            await using var fs = File.OpenRead(localPath);
            await Task.Run(() =>
            {
                _sftpClient!.UploadFile(fs, remotePath, true, progress is not null
                    ? uploaded => progress.Report(0)
                    : null);
            }, ct);
        }
        else
        {
            EnsureFtpConnected();
            var p = progress is not null
                ? new Progress<FtpProgress>(fp => progress.Report(fp.Progress))
                : null;
            var status = await _ftpClient!.UploadFile(localPath, remotePath, FtpRemoteExists.Overwrite, token: ct, progress: p);
            if (!status.IsSuccess())
                throw new InvalidOperationException($"Upload failed: {localPath}");
        }
    }

    public async Task DeleteFileAsync(string remotePath, CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            await Task.Run(() => _sftpClient!.DeleteFile(remotePath), ct);
        }
        else
        {
            EnsureFtpConnected();
            await _ftpClient!.DeleteFile(remotePath, ct);
        }
    }

    public async Task CreateDirectoryAsync(string remotePath, CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            await Task.Run(() => _sftpClient!.CreateDirectory(remotePath), ct);
        }
        else
        {
            EnsureFtpConnected();
            await _ftpClient!.CreateDirectory(remotePath, ct);
        }
    }

    public async Task<string> GetWorkingDirectoryAsync(CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            return await Task.Run(() => _sftpClient!.WorkingDirectory, ct);
        }
        else
        {
            EnsureFtpConnected();
            return await _ftpClient!.GetWorkingDirectory(ct);
        }
    }

    public async Task ChangeDirectoryAsync(string remotePath, CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            await Task.Run(() => _sftpClient!.ChangeDirectory(remotePath), ct);
        }
        else
        {
            EnsureFtpConnected();
            await _ftpClient!.SetWorkingDirectory(remotePath, ct);
        }
    }

    public async Task<bool> FileExistsAsync(string remotePath, CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            return await Task.Run(() => _sftpClient!.Exists(remotePath), ct);
        }
        else
        {
            EnsureFtpConnected();
            return await _ftpClient!.FileExists(remotePath, ct);
        }
    }

    public void Dispose()
    {
        _sftpClient?.Dispose();
        _ftpClient?.Dispose();
    }

    private void EnsureSftpConnected()
    {
        if (_sftpClient is not { IsConnected: true })
            throw new InvalidOperationException("Not connected to SFTP server.");
    }

    private void EnsureFtpConnected()
    {
        if (_ftpClient is not { IsConnected: true })
            throw new InvalidOperationException("Not connected to FTP server.");
    }
}
