using NAudio.Wave;

namespace FE2IONative;

/// <summary>
/// Downloads (and caches) track URLs sent by the server, then plays them
/// locally with NAudio. No browser / webview involved anywhere.
/// </summary>
public sealed class AudioPlayer : IDisposable
{
    private readonly HttpClient _http = new();
    private readonly string _cacheDir;
    private WaveOutEvent? _outputDevice;
    private AudioFileReader? _currentFile;
    private float _volume; // 0.0 - 1.0
    private float _volumeBeforeDeathOrMute = 1.0f;
    private bool _muted;

    public AudioPlayer(float initialVolumePercent)
    {
        _volume = Math.Clamp(initialVolumePercent, 0f, 100f) / 100f;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _cacheDir = Path.Combine(home, "fe2io-cache");
        Directory.CreateDirectory(_cacheDir);
    }

    public float VolumePercent => _volume * 100f;

    public void SetVolume(float percent)
    {
        _volume = Math.Clamp(percent, 0f, 100f) / 100f;
        if (_currentFile is not null)
            _currentFile.Volume = _volume;
        Log($"Changed volume to {_volume * 100:0}%");
    }

    public void VolumeStep(float deltaPercent)
    {
        SetVolume(VolumePercent + deltaPercent);
        _muted = false;
    }

    public void ToggleMute()
    {
        if (_muted)
        {
            SetVolume(_volumeBeforeDeathOrMute * 100f);
            _muted = false;
        }
        else
        {
            _volumeBeforeDeathOrMute = _volume;
            SetVolume(0f);
            _muted = true;
        }
    }

    /// <summary>"Quieten BGM" option: halve the volume, keep the track playing.</summary>
    public void QuietenBgm()
    {
        SetVolume(VolumePercent / 2f);
        Log("Quietened BGM");
    }

    /// <summary>"Stop BGM" option: stop the current track outright.</summary>
    public void StopBgm()
    {
        Stop();
        Log("Stopped BGM");
    }

    public void Stop()
    {
        _outputDevice?.Stop();
    }

    public async Task PlayUrlAsync(string url, CancellationToken ct = default)
    {
        Stop();
        DisposeCurrent();

        var path = await GetOrDownloadAsync(url, ct);

        _currentFile = new AudioFileReader(path) { Volume = _volume };
        _outputDevice = new WaveOutEvent();
        _outputDevice.Init(_currentFile);
        _outputDevice.Play();

        Log($"Playing {url}");
    }

    private async Task<string> GetOrDownloadAsync(string url, CancellationToken ct)
    {
        // Same filename-sanitizing approach as the Rust version.
        var invalid = new[] { '/', '<', '>', ':', '"', '\\', '|', '?', '*' };
        var fileName = new string(url.Where(c => !invalid.Contains(c)).ToArray());
        var path = Path.Combine(_cacheDir, fileName);

        if (!File.Exists(path))
        {
            var bytes = await _http.GetByteArrayAsync(url, ct);
            await File.WriteAllBytesAsync(path, bytes, ct);
        }

        return path;
    }

    private void DisposeCurrent()
    {
        _outputDevice?.Dispose();
        _outputDevice = null;
        _currentFile?.Dispose();
        _currentFile = null;
    }

    private static void Log(string status) => Console.WriteLine($"[audio] {status}");

    public void Dispose()
    {
        DisposeCurrent();
        _http.Dispose();
    }
}
