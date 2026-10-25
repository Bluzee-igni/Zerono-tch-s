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

    private int _micAlertToken;
    private bool _isMicAlertActive;
    private UiScheduledTimer? _micAlertDismissTimer;

    private int _appToastToken;
    private bool _isAppToastActive;
    private UiScheduledTimer? _appToastDismissTimer;
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
        MicMutedBadge.Visibility = isMuted ? Visibility.Visible : Visibility.Collapsed;
    }

    private void MicMutedBadge_Click(object sender, MouseButtonEventArgs e)
    {
        _micMonitorService?.ToggleMute();
    }

    private void ShowMicAlert(bool isMuted)
    {
        if (MicAlertNotification == null) return;
        if (_isExpanded || _isAnimating) return;

        if (!TryAcquireCompactSlot(CompactPillSlot.Mic, out int token))
        {
            return;
        }
        _micAlertToken = token;
        _isMicAlertActive = true;

        SuppressPrivacyDot();

        MicAlertIcon.Text = isMuted ? "🎙️" : "🟢";
        MicAlertText.Text = isMuted ? "Microphone Muted" : "Microphone Active";
        MicAlertText.Foreground = isMuted
            ? new SolidColorBrush(Color.FromRgb(0xFF, 0x45, 0x3A))
            : new SolidColorBrush(Color.FromRgb(0x30, 0xD1, 0x58));

        AnimateCompactWidth(_collapsedWidth + 24, _dur350, _easeExpOut6, _micAlertToken);

        MicAlertNotification.Visibility = Visibility.Visible;
        MicAlertNotification.Opacity = 0;
        MicAlertNotification.BeginAnimation(OpacityProperty, null);
        var fadeIn = MakeAnim(0.0, 1.0, _dur200, _easeQuadOut);
        MicAlertNotification.BeginAnimation(OpacityProperty, fadeIn);

        _micAlertDismissTimer ??= new UiScheduledTimer
        {
            Interval = TimeSpan.FromMilliseconds(2200)
        };
        _micAlertDismissTimer.Tick -= OnMicAlertDismissTick;
        _micAlertDismissTimer.Tick += OnMicAlertDismissTick;
        _micAlertDismissTimer.Stop();
        _micAlertDismissTimer.Start();
    }

    private void OnMicAlertDismissTick(object? sender, EventArgs e)
    {
        _micAlertDismissTimer?.Stop();
        DismissMicAlert();
    }

    private void DismissMicAlert()
    {
        if (!_isMicAlertActive || IsCompactSlotStale(_micAlertToken)) return;
        _isMicAlertActive = false;
        int token = _micAlertToken;
        _micAlertToken = 0;
        _compactPillArbiter.Release(token);

        RestorePrivacyDotVisibility();

        if (MicAlertNotification != null)
        {
            MicAlertNotification.BeginAnimation(OpacityProperty, null);
            var fadeOut = MakeAnim(1.0, 0.0, _dur200, _easeQuadOut);
            fadeOut.Completed += (_, _) =>
            {
                if (!_isMicAlertActive) MicAlertNotification.Visibility = Visibility.Collapsed;
            };
            MicAlertNotification.BeginAnimation(OpacityProperty, fadeOut);
        }

        RestoreCompactPresentationAfterAlert();
    }

    private void CancelMicAlertImmediate()
    {
        _micAlertDismissTimer?.Stop();
        _isMicAlertActive = false;
        _micAlertToken = 0;
        if (MicAlertNotification != null)
        {
            MicAlertNotification.BeginAnimation(OpacityProperty, null);
            MicAlertNotification.Opacity = 0;
            MicAlertNotification.Visibility = Visibility.Collapsed;
        }
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
        if (AppToastNotification == null) return;
        if (_isExpanded || _isAnimating) return;

        if (!TryAcquireCompactSlot(CompactPillSlot.Notification, out int token))
        {
            return;
        }
        _appToastToken = token;
        _isAppToastActive = true;
        _currentAppToast = notif;

        SuppressPrivacyDot();

        AppToastIcon.Text = notif.AppName.Equals("WhatsApp", StringComparison.OrdinalIgnoreCase) ? "💬" : "🎮";
        AppToastSender.Text = string.IsNullOrWhiteSpace(notif.Title) ? notif.AppName : notif.Title;
        AppToastMessage.Text = string.IsNullOrWhiteSpace(notif.Message) ? "New message" : notif.Message;

        AnimateCompactWidth(_collapsedWidth + 48, _dur350, _easeExpOut6, _appToastToken);

        AppToastNotification.Visibility = Visibility.Visible;
        AppToastNotification.Opacity = 0;
        AppToastNotification.BeginAnimation(OpacityProperty, null);
        var fadeIn = MakeAnim(0.0, 1.0, _dur200, _easeQuadOut);
        AppToastNotification.BeginAnimation(OpacityProperty, fadeIn);

        _appToastDismissTimer ??= new UiScheduledTimer
        {
            Interval = TimeSpan.FromMilliseconds(4500)
        };
        _appToastDismissTimer.Tick -= OnAppToastDismissTick;
        _appToastDismissTimer.Tick += OnAppToastDismissTick;
        _appToastDismissTimer.Stop();
        _appToastDismissTimer.Start();
    }

    private void OnAppToastDismissTick(object? sender, EventArgs e)
    {
        _appToastDismissTimer?.Stop();
        DismissAppToast();
    }

    private void DismissAppToast()
    {
        if (!_isAppToastActive || IsCompactSlotStale(_appToastToken)) return;
        _isAppToastActive = false;
        int token = _appToastToken;
        _appToastToken = 0;
        _compactPillArbiter.Release(token);

        RestorePrivacyDotVisibility();

        if (AppToastNotification != null)
        {
            AppToastNotification.BeginAnimation(OpacityProperty, null);
            var fadeOut = MakeAnim(1.0, 0.0, _dur200, _easeQuadOut);
            fadeOut.Completed += (_, _) =>
            {
                if (!_isAppToastActive) AppToastNotification.Visibility = Visibility.Collapsed;
            };
            AppToastNotification.BeginAnimation(OpacityProperty, fadeOut);
        }

        RestoreCompactPresentationAfterAlert();
    }

    private void CancelAppToastImmediate()
    {
        _appToastDismissTimer?.Stop();
        _isAppToastActive = false;
        _appToastToken = 0;
        if (AppToastNotification != null)
        {
            AppToastNotification.BeginAnimation(OpacityProperty, null);
            AppToastNotification.Opacity = 0;
            AppToastNotification.Visibility = Visibility.Collapsed;
        }
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
        if (_isExpanded || _isAnimating) return;

        AnimateCompactWidth(_collapsedWidth, _dur350, _easeExpOut6, 0);

        if (_isMusicCompactMode)
        {
            if (!_isClipboardPeekActive)
            {
                CompactThumbnailBorder.Visibility = Visibility.Visible;
                CompactThumbnailBorder.Opacity = 0;
                CompactThumbnailBorder.BeginAnimation(OpacityProperty, null);
                var thumbIn = MakeAnim(0.0, 1.0, _dur250, _easeQuadOut);
                CompactThumbnailBorder.BeginAnimation(OpacityProperty, thumbIn);
                ShowMusicVisualizer(duration: _dur100);
            }
        }
        else
        {
            CollapsedContent.Visibility = Visibility.Visible;
            CollapsedContent.Opacity = 0;
            CollapsedContent.BeginAnimation(OpacityProperty, null);
            var contentIn = MakeAnim(0.0, 1.0, _dur250, _easeQuadOut);
            CollapsedContent.BeginAnimation(OpacityProperty, contentIn);
        }
    }

    private void DisposeSystemAlerts()
    {
        _micMonitorService?.Dispose();
        _notificationListenerService?.Dispose();
        _micAlertDismissTimer?.Stop();
        _appToastDismissTimer?.Stop();
    }
}
