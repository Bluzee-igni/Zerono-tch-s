using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using VNotch.Controllers;
using VNotch.Services;
using static VNotch.Services.AnimationPrimitives;

namespace VNotch;

public enum CompactTransientAction
{
    None = 0,
    Volume,
    MicAlert,
    Clipboard,
    Notification
}

public enum CompactContentType
{
    StandbyClock,
    MediaLyrics,
    VolumeOsd,
    MicAlertOsd,
    CopiedOsd,
    ToastNotificationOsd
}

public partial class MainWindow
{
    public const double CompactStandbyWidth = 130.0;
    public const double CompactStandbyHeight = 32.0;
    public const double CompactActiveWidth = 360.0;
    public const double CompactActiveHeight = 36.0;

    private static readonly IEasingFunction _cubicEaseOut = new CubicEase { EasingMode = EasingMode.EaseOut };

    private int _compactWidthAnimationVersion = 0;
    private bool _compactPresentationGuardInstalled;
    private bool _enforcingCompactPresentation;
    private bool _isTransientOsdActive = false;
    private CompactTransientAction _activeTransientAction = CompactTransientAction.None;
    private UiScheduledTimer? _transientActionTimer;

    public bool IsTransientOsdActive => _isTransientOsdActive;

    public void SetTransientAction(CompactTransientAction action, TimeSpan duration)
    {
        bool wasAlreadyThisAction = _isTransientOsdActive && _activeTransientAction == action;
        _transientActionTimer?.Stop();

        if (action != CompactTransientAction.None)
        {
            _isTransientOsdActive = true;
            _activeTransientAction = action;
            _transientActionTimer = new UiScheduledTimer { Interval = duration };
            _transientActionTimer.Tick += (s, e) =>
            {
                _transientActionTimer?.Stop();
                _isTransientOsdActive = false;
                _activeTransientAction = CompactTransientAction.None;
                EvaluateAndRestoreNotchState();
            };
            _transientActionTimer.Start();
        }
        else
        {
            _isTransientOsdActive = false;
            _activeTransientAction = CompactTransientAction.None;
        }

        if (!wasAlreadyThisAction)
        {
            ApplyTransientVisuals(action);
        }
    }

    private void ApplyTransientVisuals(CompactTransientAction action)
    {
        switch (action)
        {
            case CompactTransientAction.Volume:
                SetCompactModeContent(CompactContentType.VolumeOsd);
                break;
            case CompactTransientAction.MicAlert:
                SetCompactModeContent(CompactContentType.MicAlertOsd);
                break;
            case CompactTransientAction.Clipboard:
                SetCompactModeContent(CompactContentType.CopiedOsd);
                break;
            case CompactTransientAction.Notification:
                SetCompactModeContent(CompactContentType.ToastNotificationOsd);
                break;
            case CompactTransientAction.None:
            default:
                EvaluateAndRestoreNotchState();
                break;
        }
    }

    private CompactContentType _currentCompactContentType = CompactContentType.StandbyClock;
    private UiScheduledTimer? _pausedMediaIdleTimer;

    public void SetCompactModeContent(CompactContentType type)
    {
        if (_isExpanded) return;

        bool isMicMuted = _micMonitorService?.IsMuted ?? false;
        _currentCompactContentType = type;

        if (type == CompactContentType.StandbyClock)
        {
            if (MusicCompactContent != null)
            {
                MusicCompactContent.Visibility = Visibility.Collapsed;
                MusicCompactContent.Opacity = 0;
            }

            if (CollapsedContent != null)
            {
                CollapsedContent.Visibility = Visibility.Visible;
                CollapsedContent.Opacity = 1;
            }

            if (StandbyClockText != null)
            {
                StandbyClockText.Text = DateTime.Now.ToString("HH:mm");
            }

            if (StandbyMicBadge != null)
            {
                StandbyMicBadge.Visibility = isMicMuted ? Visibility.Visible : Visibility.Collapsed;
            }

            SuppressPrivacyDot();
            TransitionToCompactSize(_collapsedWidth, _collapsedHeight);
            return;
        }

        if (CollapsedContent != null)
        {
            CollapsedContent.Visibility = Visibility.Collapsed;
            CollapsedContent.Opacity = 0;
        }

        if (MusicCompactContent != null)
        {
            MusicCompactContent.Visibility = Visibility.Visible;
            MusicCompactContent.Opacity = 1;
        }

        if (MicMutedBadge != null)
        {
            MicMutedBadge.Visibility = isMicMuted ? Visibility.Visible : Visibility.Collapsed;
        }

        if (type == CompactContentType.MediaLyrics)
        {
            if (CompactActionIconBorder != null) CompactActionIconBorder.Visibility = Visibility.Collapsed;
            if (CompactVolumeOsd != null) CompactVolumeOsd.Visibility = Visibility.Collapsed;
            if (CompactActionText != null) CompactActionText.Visibility = Visibility.Collapsed;
            if (CompactCopiedHost != null) CompactCopiedHost.Visibility = Visibility.Collapsed;
            if (ClipboardCopiedText != null) ClipboardCopiedText.Visibility = Visibility.Collapsed;
            if (ClipboardCheckIcon != null) ClipboardCheckIcon.Visibility = Visibility.Collapsed;
            if (CompactToastHost != null) CompactToastHost.Visibility = Visibility.Collapsed;

            if (CompactThumbnailBorder != null)
            {
                CompactThumbnailBorder.Visibility = Visibility.Visible;
                CompactThumbnailBorder.Opacity = 1;
                if (_currentMediaInfo?.Thumbnail != null)
                {
                    CompactThumbnail.Source = _currentMediaInfo.Thumbnail;
                }
            }

            if (CompactLyricsHost != null)
            {
                CompactLyricsHost.Visibility = Visibility.Visible;
                CompactLyricsHost.Opacity = 1;
            }

            if (!string.IsNullOrWhiteSpace(_lastCompactLyricLine))
            {
                if (CompactTrackFallbackText != null)
                {
                    CompactTrackFallbackText.BeginAnimation(OpacityProperty, null);
                    CompactTrackFallbackText.Opacity = 0;
                    CompactTrackFallbackText.Visibility = Visibility.Collapsed;
                    CompactTrackFallbackText.Text = "";
                }
                if (CompactLyricText != null)
                {
                    CompactLyricText.Visibility = Visibility.Visible;
                    CompactLyricText.Text = _lastCompactLyricLine;
                    CompactLyricText.Opacity = 1;
                }
            }
            else
            {
                if (CompactLyricText != null)
                {
                    CompactLyricText.BeginAnimation(OpacityProperty, null);
                    CompactLyricText.Opacity = 0;
                    CompactLyricText.Visibility = Visibility.Collapsed;
                    CompactLyricText.Text = "";
                }
                string fallback = "";
                if (_currentMediaInfo != null && !string.IsNullOrWhiteSpace(_currentMediaInfo.CurrentTrack))
                {
                    fallback = string.IsNullOrWhiteSpace(_currentMediaInfo.CurrentArtist)
                        ? _currentMediaInfo.CurrentTrack
                        : $"{_currentMediaInfo.CurrentTrack} • {_currentMediaInfo.CurrentArtist}";
                }
                if (CompactTrackFallbackText != null)
                {
                    CompactTrackFallbackText.Visibility = Visibility.Visible;
                    CompactTrackFallbackText.Text = fallback;
                    CompactTrackFallbackText.Opacity = 1;
                }
            }

            if (MusicViz != null)
            {
                MusicViz.Visibility = Visibility.Visible;
                MusicViz.Opacity = 1;
                MusicViz.IsPlaying = _currentMediaInfo?.IsPlaying ?? false;
                MusicViz.SetCopiedFeedback(false);
                if (_currentMediaInfo?.IsPlaying == true)
                {
                    ShowMusicVisualizer(duration: _dur100);
                }
            }

            double targetActiveW = _collapsedWidth;
            if (!string.IsNullOrWhiteSpace(_lastCompactLyricLine))
            {
                double textW = MeasureCompactTextWidth(_lastCompactLyricLine);
                double requiredWidth = textW + (isMicMuted ? 150 : 120);
                double maxScreenWidth = Math.Max(750, SystemParameters.PrimaryScreenWidth * 0.70);
                targetActiveW = Math.Max(_collapsedWidth, Math.Min(maxScreenWidth, requiredWidth));
            }
            TransitionToCompactSize(targetActiveW, _collapsedHeight);
            return;
        }

        SuppressPrivacyDot();
        if (CompactThumbnailBorder != null) CompactThumbnailBorder.Visibility = Visibility.Collapsed;
        if (CompactLyricsHost != null) CompactLyricsHost.Visibility = Visibility.Collapsed;
        if (CompactLyricText != null) CompactLyricText.Opacity = 0;
        if (CompactTrackFallbackText != null) CompactTrackFallbackText.Opacity = 0;
        if (MusicViz != null)
        {
            MusicViz.BeginAnimation(OpacityProperty, null);
            MusicViz.Opacity = 0;
            MusicViz.Visibility = Visibility.Collapsed;
        }

        if (CompactActionIconBorder != null) CompactActionIconBorder.Visibility = Visibility.Collapsed;
        if (CompactVolumeOsd != null) CompactVolumeOsd.Visibility = Visibility.Collapsed;
        if (CompactActionText != null) CompactActionText.Visibility = Visibility.Collapsed;
        if (CompactCopiedHost != null) CompactCopiedHost.Visibility = Visibility.Collapsed;
        if (ClipboardCopiedText != null) ClipboardCopiedText.Visibility = Visibility.Collapsed;
        if (ClipboardCheckIcon != null) ClipboardCheckIcon.Visibility = Visibility.Collapsed;
        if (CompactToastHost != null) CompactToastHost.Visibility = Visibility.Collapsed;

        double targetOsdWidth;
        switch (type)
        {
            case CompactContentType.VolumeOsd:
                if (CompactActionIconBorder != null) CompactActionIconBorder.Visibility = Visibility.Visible;
                if (CompactVolumeOsd != null) CompactVolumeOsd.Visibility = Visibility.Visible;
                targetOsdWidth = 280;
                break;

            case CompactContentType.MicAlertOsd:
                if (CompactActionIconBorder != null) CompactActionIconBorder.Visibility = Visibility.Visible;
                if (CompactActionText != null) CompactActionText.Visibility = Visibility.Visible;
                double micTextWidth = MeasureCompactTextWidth(CompactActionText?.Text ?? "Microphone Muted");
                targetOsdWidth = Math.Clamp(micTextWidth + 85, 220, 300);
                break;

            case CompactContentType.CopiedOsd:
                if (CompactCopiedHost != null) CompactCopiedHost.Visibility = Visibility.Visible;
                double snippetW = CompactCopiedSnippet != null && !string.IsNullOrEmpty(CompactCopiedSnippet.Text)
                    ? MeasureCompactTextWidth(CompactCopiedSnippet.Text)
                    : 0;
                targetOsdWidth = Math.Clamp(MeasureCompactTextWidth("Copied") + snippetW + 90, 200, 340);
                break;

            case CompactContentType.ToastNotificationOsd:
                if (CompactActionIconBorder != null) CompactActionIconBorder.Visibility = Visibility.Visible;
                if (CompactToastHost != null) CompactToastHost.Visibility = Visibility.Visible;
                targetOsdWidth = Math.Clamp(MeasureCompactTextWidth(CompactToastMsg?.Text ?? "") + 110, 260, 360);
                break;

            default:
                targetOsdWidth = _collapsedWidth;
                break;
        }

        TransitionToCompactSize(targetOsdWidth, CompactActiveHeight);
    }

    public void TransitionToCompactSize(double targetWidth, double targetHeight)
    {
        if (_isExpanded || NotchBorder == null) return;

        double currentW = NotchBorder.ActualWidth > 0 ? NotchBorder.ActualWidth : NotchBorder.Width;
        if (double.IsNaN(currentW) || currentW <= 0) currentW = targetWidth;

        double currentH = NotchBorder.ActualHeight > 0 ? NotchBorder.ActualHeight : NotchBorder.Height;
        if (double.IsNaN(currentH) || currentH <= 0) currentH = targetHeight;

        if (Math.Abs(currentW - targetWidth) < 0.5 && Math.Abs(currentH - targetHeight) < 0.5)
        {
            return;
        }

        if (Math.Abs(currentW - targetWidth) > 0.5)
        {
            var widthAnim = new DoubleAnimation
            {
                From = currentW,
                To = targetWidth,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = _cubicEaseOut
            };
            Timeline.SetDesiredFrameRate(widthAnim, AnimationConfig.TargetFps);
            widthAnim.Completed += (_, _) =>
            {
                NotchBorder.Width = targetWidth;
                NotchBorder.BeginAnimation(WidthProperty, null);
            };
            NotchBorder.BeginAnimation(WidthProperty, widthAnim, HandoffBehavior.SnapshotAndReplace);
        }
        else
        {
            NotchBorder.Width = targetWidth;
        }

        if (Math.Abs(currentH - targetHeight) > 0.5)
        {
            var heightAnim = new DoubleAnimation
            {
                From = currentH,
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = _cubicEaseOut
            };
            Timeline.SetDesiredFrameRate(heightAnim, AnimationConfig.TargetFps);
            heightAnim.Completed += (_, _) =>
            {
                NotchBorder.Height = targetHeight;
                NotchBorder.BeginAnimation(HeightProperty, null);
            };
            NotchBorder.BeginAnimation(HeightProperty, heightAnim, HandoffBehavior.SnapshotAndReplace);
        }
        else
        {
            NotchBorder.Height = targetHeight;
        }
    }

    public void EvaluateAndRestoreNotchState()
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.BeginInvoke(EvaluateAndRestoreNotchState);
            return;
        }

        if (_cleanedUp || _isExpanded || _isAnimating) return;

        if (_isTransientOsdActive && _activeTransientAction != CompactTransientAction.None)
        {
            ApplyTransientVisuals(_activeTransientAction);
            return;
        }

        _isTransientOsdActive = false;
        _activeTransientAction = CompactTransientAction.None;

        bool hasMedia = _isMusicCompactMode && _currentMediaInfo != null && !string.IsNullOrWhiteSpace(_currentMediaInfo.CurrentTrack);
        bool isPlaying = hasMedia && _currentMediaInfo!.IsPlaying;

        if (isPlaying)
        {
            _pausedMediaIdleTimer?.Stop();
            SetCompactModeContent(CompactContentType.MediaLyrics);
        }
        else if (hasMedia)
        {
            if (_pausedMediaIdleTimer == null)
            {
                _pausedMediaIdleTimer = new UiScheduledTimer { Interval = TimeSpan.FromSeconds(10) };
                _pausedMediaIdleTimer.Tick += (s, e) =>
                {
                    _pausedMediaIdleTimer?.Stop();
                    if (_isExpanded || _isTransientOsdActive) return;
                    if (_currentMediaInfo?.IsPlaying != true)
                    {
                        SetCompactModeContent(CompactContentType.StandbyClock);
                    }
                };
            }

            if (!_pausedMediaIdleTimer.IsEnabled)
            {
                _pausedMediaIdleTimer.Start();
            }

            if (_currentCompactContentType != CompactContentType.StandbyClock)
            {
                SetCompactModeContent(CompactContentType.MediaLyrics);
            }
        }
        else
        {
            _pausedMediaIdleTimer?.Stop();
            SetCompactModeContent(CompactContentType.StandbyClock);
        }
    }

    private void EnsureCompactPresentationGuard()
    {
        if (_compactPresentationGuardInstalled) return;
        _compactPresentationGuardInstalled = true;
        foreach (var element in new UIElement[] { MusicViz, CompactThumbnailBorder,
            MusicCompactContent, CollapsedContent,
            ClipboardCheckIcon, ClipboardCopiedText, VolumeIndicatorContainer,
            BluetoothNotification, BluetoothDisconnectNotification, ChargingNotification })
            element.IsVisibleChanged += (_, _) => EnforceCompactPresentationOwner();
    }

    private static void HideCompactSurface(UIElement element)
    {
        element.BeginAnimation(OpacityProperty, null);
        element.Opacity = 0;
        element.Visibility = Visibility.Collapsed;
    }

    private void EnforceCompactPresentationOwner()
    {
        if (_enforcingCompactPresentation) return;
        _enforcingCompactPresentation = true;
        try
        {
            var owner = _compactPillArbiter.ActiveSlot;
            if (owner is CompactPillSlot.Bluetooth or CompactPillSlot.Charging or CompactPillSlot.Greeting)
            {
                HideCompactSurface(MusicCompactContent);
                HideCompactSurface(CollapsedContent);
                HideCompactSurface(MusicViz);
                HideCompactSurface(CompactThumbnailBorder);
            }
            if (owner != CompactPillSlot.Clipboard)
            {
                HideCompactSurface(ClipboardCheckIcon);
                HideCompactSurface(ClipboardCopiedText);
            }
            if (owner != CompactPillSlot.Volume)
                HideCompactSurface(VolumeIndicatorContainer);
            if (owner != CompactPillSlot.Bluetooth)
            {
                HideCompactSurface(BluetoothNotification);
                HideCompactSurface(BluetoothDisconnectNotification);
            }
            if (owner != CompactPillSlot.Charging) HideCompactSurface(ChargingNotification);
        }
        finally { _enforcingCompactPresentation = false; }
    }

    private void RestoreCompactMediaPresentation()
    {
        EvaluateAndRestoreNotchState();
    }

    private bool TryAcquireCompactSlot(CompactPillSlot slot, out int token)
    {
        WakeFromIdle();
        EnsureCompactPresentationGuard();

        var result = _compactPillArbiter.TryAcquire(slot);
        token = result.Token;
        if (!result.Won)
        {
            return false;
        }

        if (result.Preempted != CompactPillSlot.None)
        {
            CancelCompactSlotImmediate(result.Preempted);
        }
        EnforceCompactPresentationOwner();
        return true;
    }

    private void CancelCompactSlotImmediate(CompactPillSlot slot)
    {
        switch (slot)
        {
            case CompactPillSlot.Clipboard:
                CancelClipboardPeekImmediate();
                break;
            case CompactPillSlot.Volume:
                DismissVolumeIndicatorImmediate();
                break;
            case CompactPillSlot.Bluetooth:
                CancelBluetoothNotificationImmediate();
                break;
            case CompactPillSlot.Charging:
                CancelChargingGlanceImmediate();
                break;
            case CompactPillSlot.Greeting:
                break;
            case CompactPillSlot.Mic:
                CancelMicAlertImmediate();
                break;
            case CompactPillSlot.Notification:
                CancelAppToastImmediate();
                break;
        }
    }

    private void AnimateCompactWidth(double targetWidth, TimeSpan duration, IEasingFunction ease, int token)
        => AnimateCompactWidth(targetWidth, new Duration(duration), ease, token);

    private void AnimateCompactWidth(double targetWidth, Duration duration, IEasingFunction ease, int token)
    {
        int version = ++_compactWidthAnimationVersion;
        long revision = _compactPillArbiter.Revision;
        double fromWidth = NotchBorder.ActualWidth;
        if (double.IsNaN(fromWidth) || double.IsInfinity(fromWidth) || fromWidth <= 0)
        {
            fromWidth = _collapsedWidth;
        }

        double previousBaseWidth = (double)NotchBorder.GetAnimationBaseValue(WidthProperty);

        var anim = new DoubleAnimation
        {
            From = fromWidth,
            To = targetWidth,
            Duration = duration,
            EasingFunction = ease
        };
        Timeline.SetDesiredFrameRate(anim, AnimationConfig.TargetFps);

        anim.Completed += (_, _) =>
        {
            if (version != _compactWidthAnimationVersion) return;
            if (token == 0 && !_compactPillArbiter.CanRestoreMedia(revision)) return;

            bool returningToRest = Math.Abs(targetWidth - _collapsedWidth) < 0.5;
            bool canCommitTarget = token == 0
                || _compactPillArbiter.IsTokenCurrent(token)
                || (returningToRest && _compactPillArbiter.CanRestoreMedia(revision + 1));
            double finalWidth = canCommitTarget ? targetWidth : previousBaseWidth;

            NotchBorder.Width = finalWidth;
            NotchBorder.BeginAnimation(WidthProperty, null);
        };

        NotchBorder.BeginAnimation(WidthProperty, anim, HandoffBehavior.SnapshotAndReplace);
    }

    private bool IsCompactSlotStale(int token) => !_compactPillArbiter.IsTokenCurrent(token);
}
