using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VNotch.Controllers;
using VNotch.Services;
using static VNotch.Services.AnimationPrimitives;
using static VNotch.Services.Win32Interop;

namespace VNotch;

public partial class MainWindow
{
    private HardwareMicMonitorService? _micMonitorService;
    private SystemNotificationListenerService? _notificationListenerService;
    private AppNotificationInfo? _currentAppToast;

    private void InitializeSystemAlerts()
    {
        try
        {
            _micMonitorService = new HardwareMicMonitorService();
            _micMonitorService.MicMuteChanged += OnHardwareMicMuteChanged;
            UpdateMicMutedBadge(_micMonitorService.IsMuted);
        }
        catch (Exception ex)
        {
            RuntimeLog.Log("MIC", $"Init error: {ex.Message}");
        }

        try
        {
            _notificationListenerService = new SystemNotificationListenerService();
            _notificationListenerService.NotificationReceived += OnSystemNotificationReceived;
            _notificationListenerService.StartListeningAsync().SafeFireAndForget("NOTIF-START");
        }
        catch (Exception ex)
        {
            RuntimeLog.Log("NOTIF", $"Init error: {ex.Message}");
        }
    }

    private void OnHardwareMicMuteChanged(bool isMuted)
    {
        if (_cleanedUp) return;
        Dispatcher.BeginInvoke(() =>
        {
            UpdateMicMutedBadge(isMuted);
            ShowMicAlert(isMuted);
        });
    }

    private void UpdateMicMutedBadge(bool isMuted)
    {
        if (MicMutedBadge == null) return;
        bool showBadge = isMuted && _settings.EnableMicMuteAlert;
        MicMutedBadge.Visibility = showBadge ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MicMutedBadge_Click(object sender, MouseButtonEventArgs e)
    {
        _micMonitorService?.ToggleMute();
    }

    private void ShowMicAlert(bool isMuted)
    {
        if (_isExpanded || _isAnimating) return;
        if (!_settings.EnableMicMuteAlert) return;

        if (CompactActionIconText != null)
        {
            CompactActionIconText.Text = isMuted ? "🎙️" : "🟢";
            CompactActionIconText.Foreground = Brushes.White;
        }
        if (CompactActionText != null)
        {
            CompactActionText.Text = isMuted ? "Microphone Muted" : "Microphone Active";
            CompactActionText.Foreground = isMuted
                ? new SolidColorBrush(Color.FromRgb(0xFF, 0x6B, 0x6B))
                : new SolidColorBrush(Color.FromRgb(0x30, 0xD1, 0x58));
        }

        SetTransientAction(CompactTransientAction.MicAlert, TimeSpan.FromMilliseconds(1500));
    }

    private void DismissMicAlert()
    {
        EvaluateAndRestoreNotchState();
    }

    private void CancelMicAlertImmediate()
    {
        _transientActionTimer?.Stop();
        _activeTransientAction = CompactTransientAction.None;
        EvaluateAndRestoreNotchState();
    }

    private void OnSystemNotificationReceived(AppNotificationInfo notif)
    {
        if (_cleanedUp) return;
        Dispatcher.BeginInvoke(() =>
        {
            ShowAppToast(notif);
        });
    }

    private void ShowAppToast(AppNotificationInfo notif)
    {
        if (_isExpanded || _isAnimating) return;
        if (!_settings.EnableAppNotifications) return;

        _currentAppToast = notif;
        if (CompactActionIconText != null)
        {
            CompactActionIconText.Text = notif.AppName.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase) ? "💬" : "🎮";
        }
        if (CompactToastApp != null)
        {
            CompactToastApp.Text = string.IsNullOrWhiteSpace(notif.Title) ? notif.AppName : notif.Title;
        }
        if (CompactToastMsg != null)
        {
            CompactToastMsg.Text = string.IsNullOrWhiteSpace(notif.Message) ? "New message" : notif.Message;
        }

        SetTransientAction(CompactTransientAction.Notification, TimeSpan.FromMilliseconds(3000));
    }

    private void DismissAppToast()
    {
        EvaluateAndRestoreNotchState();
    }

    private void CancelAppToastImmediate()
    {
        _transientActionTimer?.Stop();
        _activeTransientAction = CompactTransientAction.None;
        EvaluateAndRestoreNotchState();
    }

    private void AppToastNotification_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_currentAppToast != null)
        {
            try
            {
                string target = _currentAppToast.AppName;
                var proc = Process.GetProcesses()
                    .FirstOrDefault(p => p.ProcessName.Contains(target, StringComparison.OrdinalIgnoreCase));
                if (proc != null && proc.MainWindowHandle != IntPtr.Zero)
                {
                    SetForegroundWindow(proc.MainWindowHandle);
                }
            }
            catch { }
        }
        DismissAppToast();
    }

    private void RestoreCompactPresentationAfterAlert()
    {
        EvaluateAndRestoreNotchState();
    }

    private void DisposeSystemAlerts()
    {
        _micMonitorService?.Dispose();
        _notificationListenerService?.Dispose();
    }
}
