using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FE2IONative;

public sealed class MainForm : Form
{
    private enum DeathAction { QuietenBgm, StopBgm, Disable }
    private enum LeaveAction { StopBgm, Disable }

    private readonly TextBox _usernameBox = new();
    private readonly Button _connectButton = new();
    private readonly Label _statusLabel = new();
    private readonly Label _nowPlayingLabel = new();
    private readonly TrackBar _volumeSlider = new();
    private readonly Label _volumeValueLabel = new();
    private readonly CheckBox _muteBox = new();

    private readonly GroupBox _deathGroup = new();
    private readonly RadioButton _deathQuieten = new();
    private readonly RadioButton _deathStop = new();
    private readonly RadioButton _deathDisable = new();

    private readonly GroupBox _leaveGroup = new();
    private readonly RadioButton _leaveStop = new();
    private readonly RadioButton _leaveDisable = new();

    private readonly TextBox _logBox = new();

    private AudioPlayer? _player;
    private Fe2IoClient? _client;
    private CancellationTokenSource? _cts;
    private bool _sessionActive; // true while an in-game round is running (between "bgm" and "left")

    private const string ServerUrl = "ws://client.fe2.io:8081";

    public MainForm()
    {
        Text = "FE2.IO Native";
        Width = 480;
        Height = 470;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        var usernameLabel = new Label { Text = "Enter Username:", Location = new Point(12, 15), AutoSize = true };
        _usernameBox.Location = new Point(12, 35);
        _usernameBox.Width = 250;

        _connectButton.Text = "Connect";
        _connectButton.Location = new Point(270, 34);
        _connectButton.Width = 100;
        _connectButton.Click += ConnectButton_Click;

        _statusLabel.Text = "Disconnected";
        _statusLabel.Location = new Point(12, 65);
        _statusLabel.AutoSize = true;
        _statusLabel.ForeColor = Color.DarkRed;

        _nowPlayingLabel.Text = "Now playing: -";
        _nowPlayingLabel.Location = new Point(12, 88);
        _nowPlayingLabel.AutoSize = true;

        var volumeLabel = new Label { Text = "Volume:", Location = new Point(12, 118), AutoSize = true };
        _volumeSlider.Minimum = 0;
        _volumeSlider.Maximum = 100;
        _volumeSlider.Value = 70;
        _volumeSlider.Location = new Point(75, 110);
        _volumeSlider.Width = 205;
        _volumeSlider.TickFrequency = 10;
        _volumeSlider.Enabled = false;
        _volumeSlider.Scroll += VolumeSlider_Scroll;

        _volumeValueLabel.Text = "70%";
        _volumeValueLabel.Location = new Point(290, 118);
        _volumeValueLabel.AutoSize = true;

        _muteBox.Text = "Mute";
        _muteBox.Location = new Point(350, 116);
        _muteBox.AutoSize = true;
        _muteBox.Enabled = false;
        _muteBox.CheckedChanged += MuteBox_CheckedChanged;

        // --- On Death options ---
        _deathGroup.Text = "On Death:";
        _deathGroup.Location = new Point(12, 150);
        _deathGroup.Size = new Size(440, 45);

        _deathQuieten.Text = "Quieten BGM";
        _deathQuieten.Location = new Point(10, 18);
        _deathQuieten.AutoSize = true;
        _deathQuieten.Checked = true;

        _deathStop.Text = "Stop BGM";
        _deathStop.Location = new Point(150, 18);
        _deathStop.AutoSize = true;

        _deathDisable.Text = "Disable";
        _deathDisable.Location = new Point(260, 18);
        _deathDisable.AutoSize = true;

        _deathGroup.Controls.AddRange(new Control[] { _deathQuieten, _deathStop, _deathDisable });

        // --- Leaving Game options ---
        _leaveGroup.Text = "Leaving Game:";
        _leaveGroup.Location = new Point(12, 200);
        _leaveGroup.Size = new Size(440, 45);

        _leaveStop.Text = "Stop BGM";
        _leaveStop.Location = new Point(10, 18);
        _leaveStop.AutoSize = true;
        _leaveStop.Checked = true;

        _leaveDisable.Text = "Disable";
        _leaveDisable.Location = new Point(150, 18);
        _leaveDisable.AutoSize = true;

        _leaveGroup.Controls.AddRange(new Control[] { _leaveStop, _leaveDisable });

        _logBox.Multiline = true;
        _logBox.ReadOnly = true;
        _logBox.ScrollBars = ScrollBars.Vertical;
        _logBox.Location = new Point(12, 255);
        _logBox.Size = new Size(440, 165);
        _logBox.Font = new Font("Consolas", 8.5f);

        Controls.AddRange(new Control[]
        {
            usernameLabel, _usernameBox, _connectButton,
            _statusLabel, _nowPlayingLabel,
            volumeLabel, _volumeSlider, _volumeValueLabel, _muteBox,
            _deathGroup, _leaveGroup,
            _logBox
        });

        FormClosing += (_, _) => Disconnect();
    }

    private DeathAction SelectedDeathAction =>
        _deathStop.Checked ? DeathAction.StopBgm :
        _deathDisable.Checked ? DeathAction.Disable :
        DeathAction.QuietenBgm;

    private LeaveAction SelectedLeaveAction =>
        _leaveDisable.Checked ? LeaveAction.Disable : LeaveAction.StopBgm;

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        if (_client is not null)
        {
            Disconnect();
            return;
        }

        var username = _usernameBox.Text.Trim();
        if (string.IsNullOrEmpty(username))
        {
            MessageBox.Show(this, "Enter your Roblox username first.", "FE2.IO Native",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _usernameBox.Enabled = false;
        _connectButton.Text = "Connecting...";
        _connectButton.Enabled = false;

        _player = new AudioPlayer(_volumeSlider.Value);
        _client = new Fe2IoClient(ServerUrl, username);
        _cts = new CancellationTokenSource();

        _client.MessageReceived += HandleMessageAsync;

        try
        {
            await _client.ConnectAsync(_cts.Token);
            SetStatus("Connected (waiting for a game to sync to)", Color.DarkGreen);
            _connectButton.Text = "Disconnect";
            _connectButton.Enabled = true;
            Log("Connected to server.");

            _ = Task.Run(() => _client.RunAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            Log($"Connection error: {ex.Message}");
            SetStatus("Disconnected", Color.DarkRed);
            _usernameBox.Enabled = true;
            _connectButton.Text = "Connect";
            _connectButton.Enabled = true;
            _client = null;
        }
    }

    private void Disconnect()
    {
        _cts?.Cancel();
        _client?.Dispose();
        _player?.Dispose();
        _client = null;
        _player = null;
        _sessionActive = false;

        SetStatus("Disconnected", Color.DarkRed);
        SetNowPlaying("-");
        SetVolumeControlsEnabled(false);
        _usernameBox.Enabled = true;
        _connectButton.Text = "Connect";
        _connectButton.Enabled = true;
    }

    private async Task HandleMessageAsync(ServerMessage msg)
    {
        if (_player is null) return;

        switch (msg.MsgType)
        {
            case "bgm" when msg.AudioUrl is not null:
                if (!_sessionActive)
                {
                    _sessionActive = true;
                    SetVolumeControlsEnabled(true);
                    SetStatus("Connected - game session active", Color.DarkGreen);
                }
                await _player.PlayUrlAsync(msg.AudioUrl);
                SetNowPlaying(msg.AudioUrl);
                break;

            case "gameStatus" when msg.StatusType == "died":
                switch (SelectedDeathAction)
                {
                    case DeathAction.QuietenBgm:
                        _player.QuietenBgm();
                        SyncVolumeSlider();
                        Log("You died: BGM quietened.");
                        break;
                    case DeathAction.StopBgm:
                        _player.StopBgm();
                        SetNowPlaying("-");
                        Log("You died: BGM stopped.");
                        break;
                    case DeathAction.Disable:
                        Log("You died: death handling disabled, BGM unaffected.");
                        break;
                }
                break;

            case "gameStatus" when msg.StatusType == "left":
                if (SelectedLeaveAction == LeaveAction.StopBgm)
                {
                    _player.StopBgm();
                    SetNowPlaying("-");
                    Log("Left the game: BGM stopped.");
                }
                else
                {
                    Log("Left the game: leave handling disabled, BGM unaffected.");
                }

                _sessionActive = false;
                SetVolumeControlsEnabled(false);
                SetStatus("Connected (waiting for a game to sync to)", Color.DarkGreen);
                break;

            default:
                Log($"Unhandled message: msgType={msg.MsgType} statusType={msg.StatusType}");
                break;
        }
    }

    private void VolumeSlider_Scroll(object? sender, EventArgs e)
    {
        _player?.SetVolume(_volumeSlider.Value);
        _volumeValueLabel.Text = $"{_volumeSlider.Value}%";
        _muteBox.CheckedChanged -= MuteBox_CheckedChanged;
        _muteBox.Checked = false;
        _muteBox.CheckedChanged += MuteBox_CheckedChanged;
    }

    private void MuteBox_CheckedChanged(object? sender, EventArgs e)
    {
        _player?.ToggleMute();
        SyncVolumeSlider();
    }

    private void SetVolumeControlsEnabled(bool enabled)
    {
        if (InvokeRequired) { Invoke(() => SetVolumeControlsEnabled(enabled)); return; }
        _volumeSlider.Enabled = enabled;
        _muteBox.Enabled = enabled;
    }

    private void SyncVolumeSlider()
    {
        if (_player is null) return;
        var value = (int)Math.Round(_player.VolumePercent);
        if (InvokeRequired) { Invoke(SyncVolumeSlider); return; }
        _volumeSlider.Value = Math.Clamp(value, _volumeSlider.Minimum, _volumeSlider.Maximum);
        _volumeValueLabel.Text = $"{value}%";
    }

    private void SetStatus(string text, Color color)
    {
        if (InvokeRequired) { Invoke(() => SetStatus(text, color)); return; }
        _statusLabel.Text = text;
        _statusLabel.ForeColor = color;
    }

    private void SetNowPlaying(string text)
    {
        if (InvokeRequired) { Invoke(() => SetNowPlaying(text)); return; }
        _nowPlayingLabel.Text = $"Now playing: {text}";
    }

    private void Log(string text)
    {
        if (InvokeRequired) { Invoke(() => Log(text)); return; }
        _logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }
}
