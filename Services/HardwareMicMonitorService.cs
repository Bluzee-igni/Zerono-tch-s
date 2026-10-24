using System;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace VNotch.Services;

public sealed class HardwareMicMonitorService : IDisposable, IMMNotificationClient
{
    private const string LogCategory = "MIC-MONITOR";
    private readonly object _sync = new();
    private MMDeviceEnumerator? _enumerator;
    private MMDevice? _captureDevice;
    private AudioEndpointVolume? _captureVolume;
    private bool _disposed;
    private bool _lastMuteState;

    public event Action<bool>? MicMuteChanged;

    public bool IsMuted
    {
        get
        {
            lock (_sync)
            {
                try
                {
                    return _captureVolume?.Mute ?? false;
                }
                catch
                {
                    return false;
                }
            }
        }
    }

    public HardwareMicMonitorService()
    {
        Initialize();
    }

    private void Initialize()
    {
        lock (_sync)
        {
            if (_disposed) return;
            ReleaseCurrentDeviceLocked();

            try
            {
                _enumerator ??= new MMDeviceEnumerator();
                try
                {
                    Marshal.ThrowExceptionForHR(_enumerator.RegisterEndpointNotificationCallback(this));
                }
                catch { }

                _captureDevice = _enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Multimedia);
                _captureVolume = _captureDevice.AudioEndpointVolume;
                _captureVolume.OnVolumeNotification += OnVolumeNotification;
                _lastMuteState = _captureVolume.Mute;
            }
            catch (Exception ex)
            {
                RuntimeLog.Log(LogCategory, $"Init error: {ex.Message}");
            }
        }
    }

    private void OnVolumeNotification(AudioVolumeNotificationData data)
    {
        bool isMuted = data.Muted;
        if (isMuted != _lastMuteState)
        {
            _lastMuteState = isMuted;
            MicMuteChanged?.Invoke(isMuted);
        }
    }

    public void ToggleMute()
    {
        lock (_sync)
        {
            if (_disposed || _captureVolume == null) return;
            try
            {
                _captureVolume.Mute = !_captureVolume.Mute;
            }
            catch (Exception ex)
            {
                RuntimeLog.Log(LogCategory, $"ToggleMute error: {ex.Message}");
            }
        }
    }

    public void SetMute(bool mute)
    {
        lock (_sync)
        {
            if (_disposed || _captureVolume == null) return;
            try
            {
                _captureVolume.Mute = mute;
            }
            catch (Exception ex)
            {
                RuntimeLog.Log(LogCategory, $"SetMute error: {ex.Message}");
            }
        }
    }

    private void ReleaseCurrentDeviceLocked()
    {
        if (_captureVolume != null)
        {
            try { _captureVolume.OnVolumeNotification -= OnVolumeNotification; } catch { }
            _captureVolume = null;
        }
        if (_captureDevice != null)
        {
            try { _captureDevice.Dispose(); } catch { }
            _captureDevice = null;
        }
    }

    public void OnDefaultDeviceChanged(DataFlow flow, Role role, string defaultDeviceId)
    {
        if (flow == DataFlow.Capture && role == Role.Multimedia)
        {
            Initialize();
            MicMuteChanged?.Invoke(IsMuted);
        }
    }

    public void OnDeviceStateChanged(string deviceId, DeviceState newState) { }
    public void OnDeviceAdded(string deviceId) { }
    public void OnDeviceRemoved(string deviceId) { }
    public void OnPropertyValueChanged(string deviceId, PropertyKey key) { }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            ReleaseCurrentDeviceLocked();
            if (_enumerator != null)
            {
                try { _enumerator.UnregisterEndpointNotificationCallback(this); } catch { }
                try { _enumerator.Dispose(); } catch { }
                _enumerator = null;
            }
        }
    }
}
