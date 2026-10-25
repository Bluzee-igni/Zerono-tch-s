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

    private static readonly Geometry _micMutedGeometry = Geometry.Parse("M19,11C19,12.19 18.66,13.3 18.1,14.28L16.87,13.05C17.14,12.43 17.3,11.74 17.3,11H19M15,11.16L9,5.18V5A3,3 0 0,1 12,2A3,3 0 0,1 15,5V11L15,11.16M4.27,3L21,19.73L19.73,21L15.54,16.81C14.77,17.27 13.91,17.58 13,17.72V21H11V17.72C7.72,17.23 5,14.41 5,11H6.7C6.7,14 9.24,16.1 12,16.1C12.81,16.1 13.6,15.91 14.31,15.58L12.65,13.92L12,14A3,3 0 0,1 9,11V10.28L3,4.27L4.27,3Z");
    private static readonly Geometry _micActiveGeometry = Geometry.Parse("M12,2A3,3 0 0,1 15,5V11A3,3 0 0,1 12,14A3,3 0 0,1 9,11V5A3,3 0 0,1 12,2M19,11C19,14.53 16.39,17.44 13,17.93V21H11V17.93C7.61,17.44 5,14.53 5,11H7A5,5 0 0,0 12,16A5,5 0 0,0 17,11H19Z");
    private static readonly Geometry _chatBubbleGeometry = Geometry.Parse("M20,2H4A2,2 0 0,0 2,4V22L6,18H20A2,2 0 0,0 22,16V4A2,2 0 0,0 20,2M6,9H18V11H6V9M14,14H6V12H14V14M18,8H6V6H18V8Z");
    private static readonly Geometry _whatsappGeometry = Geometry.Parse("M12.04 2C6.58 2 2.13 6.45 2.13 11.91C2.13 13.66 2.59 15.36 3.45 16.86L2.05 22L7.3 20.62C8.75 21.41 10.38 21.83 12.04 21.83C17.5 21.83 21.95 17.38 21.95 11.92C21.95 9.27 20.92 6.78 19.05 4.91C17.18 3.03 14.69 2 12.04 2M12.05 3.67C14.25 3.67 16.31 4.53 17.87 6.09C19.42 7.65 20.28 9.72 20.28 11.92C20.28 16.46 16.58 20.15 12.04 20.15C10.56 20.15 9.11 19.76 7.85 19L7.55 18.83L4.43 19.65L5.26 16.61L5.06 16.29C4.24 15 3.8 13.47 3.8 11.91C3.81 7.37 7.5 3.67 12.05 3.67Z");

    private void ShowMicAlert(bool isMuted)
    {
        if (_isExpanded || _isAnimating) return;
        if (!_settings.EnableMicMuteAlert) return;

        if (CompactActionIconBorder != null)
        {
            CompactActionIconBorder.Background = isMuted
                ? new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44))
                : new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
        }

        if (CompactActionIconPath != null)
        {
            CompactActionIconPath.Data = isMuted ? _micMutedGeometry : _micActiveGeometry;
            CompactActionIconPath.Fill = Brushes.White;
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
        bool isWa = notif.AppName.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase);

        if (CompactActionIconBorder != null)
        {
            CompactActionIconBorder.Background = isWa
                ? new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E))
                : new SolidColorBrush(Color.FromRgb(0x58, 0x65, 0xF2));
        }

        if (CompactActionIconPath != null)
        {
            CompactActionIconPath.Data = isWa ? _whatsappGeometry : _chatBubbleGeometry;
            CompactActionIconPath.Fill = Brushes.White;
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
