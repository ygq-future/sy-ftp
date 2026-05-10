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
            var total = _sftpClient!.GetAttributes(remotePath).Size;
            await using var fs = File.Create(localPath);
            await Task.Run(() =>
            {
                _sftpClient!.DownloadFile(remotePath, fs, progress is not null
                    ? downloaded => progress.Report(total > 0 ? (double)downloaded / total * 100 : 0)
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

    public async Task DeleteDirectoryAsync(string remotePath, CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            await Task.Run(() => SftpRecursiveDelete(remotePath), ct);
        }
        else
        {
            EnsureFtpConnected();
            await _ftpClient!.DeleteDirectory(remotePath, FtpListOption.Recursive, ct);
        }
    }

    public async Task CreateDirectoryAsync(string remotePath, CancellationToken ct = default)
    {
        if (await DirectoryExistsAsync(remotePath, ct)) return;
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

    public async Task<bool> DirectoryExistsAsync(string remotePath, CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            return await Task.Run(() =>
            {
                try { return _sftpClient!.Exists(remotePath); }
                catch { return false; }
            }, ct);
        }
        else
        {
            EnsureFtpConnected();
            return await _ftpClient!.DirectoryExists(remotePath, ct);
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

    public async Task<long> GetFileSizeAsync(string remotePath, CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            return (await Task.Run(() => _sftpClient!.GetAttributes(remotePath), ct)).Size;
        }
        else
        {
            EnsureFtpConnected();
            return await _ftpClient!.GetFileSize(remotePath);
        }
    }

    public async Task<IReadOnlyList<RemoteFile>> ListDirectoryRecursiveAsync(string path, CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            var result = new List<RemoteFile>();
            await Task.Run(() => SftpRecursiveList(path, result), ct);
            return result;
        }
        else
        {
            EnsureFtpConnected();
            var items = await _ftpClient!.GetListing(path, FtpListOption.Recursive, ct);
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

    private void SftpRecursiveDelete(string path)
    {
        var items = _sftpClient!.ListDirectory(path);
        foreach (var i in items)
        {
            if (i.Name is "." or "..") continue;
            var full = i.FullName;
            if (i.IsDirectory)
                SftpRecursiveDelete(full);
            else
                _sftpClient!.DeleteFile(full);
        }
        _sftpClient!.DeleteDirectory(path);
    }

    private void SftpRecursiveList(string path, List<RemoteFile> result)
    {
        var items = _sftpClient!.ListDirectory(path);
        foreach (var i in items)
        {
            if (i.Name is "." or "..") continue;
            result.Add(new RemoteFile(i.Name, i.FullName, i.Length, i.IsDirectory, i.LastWriteTime));
            if (i.IsDirectory)
                SftpRecursiveList(i.FullName, result);
        }
    }

    public async Task DownloadDirectoryAsync(string remotePath, string localBasePath, IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var all = await ListDirectoryRecursiveAsync(remotePath, ct);
        var files = all.Where(f => !f.IsDirectory).ToList();
        var totalBytes = files.Sum(f => f.Size);
        long downloadedBytes = 0;

        // Recreate directory tree
        foreach (var dir in all.Where(f => f.IsDirectory))
        {
            var rel = dir.FullPath[remotePath.Length..].TrimStart('/');
            var localDir = Path.Combine(localBasePath, rel);
            Directory.CreateDirectory(localDir);
        }

        // Download files with aggregate progress
        var progressLock = new object();
        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();
            var rel = file.FullPath[remotePath.Length..].TrimStart('/');
            var localPath = Path.Combine(localBasePath, rel);
            var parent = Path.GetDirectoryName(localPath);
            if (parent is not null)
                Directory.CreateDirectory(parent);

            var fileSize = file.Size;
            long lastReported = 0;
            await DownloadFileAsync(file.FullPath, localPath,
                fileSize > 0 && progress is not null
                    ? new Progress<double>(pct =>
                    {
                        var fileBytes = (long)(fileSize * pct / 100);
                        var delta = fileBytes - lastReported;
                        if (delta <= 0) return;
                        lastReported = fileBytes;
                        long current;
                        lock (progressLock)
                        {
                            downloadedBytes += delta;
                            current = downloadedBytes;
                        }
                        progress.Report(totalBytes > 0 ? (double)current / totalBytes * 100 : 0);
                    })
                    : null, ct);
        }
    }

    public async Task MoveAsync(string fromPath, string toPath, CancellationToken ct = default)
    {
        if (_isSftp)
        {
            EnsureSftpConnected();
            await Task.Run(() => _sftpClient!.RenameFile(fromPath, toPath), ct);
        }
        else
        {
            EnsureFtpConnected();
            await _ftpClient!.MoveFile(fromPath, toPath, FtpRemoteExists.Skip, ct);
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
