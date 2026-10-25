using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VNotch.Models;
using VNotch.Services;
using static VNotch.Services.AnimationPrimitives;
using static VNotch.Services.Win32Interop;

namespace VNotch;

public partial class MainWindow
{
    #region Media Controls

    private const string MediaCtrlLogTag = "MEDIA-CTRL";
    private bool _isPlaying = true;

    private async Task TogglePlayPauseInstantAsync()
    {
        try
        {
            if ((DateTime.UtcNow - _lastMediaActionTime).TotalMilliseconds < 250) return;
            _lastMediaActionTime = DateTime.UtcNow;

            WakeFromIdle();

            if (_isVolumeIndicatorActive)
            {
                DismissVolumeIndicatorImmediate(restoreMedia: true);
            }

            _isPlaying = !_viewModel.IsPlaying;
            UpdatePlayPauseIcon();

            if (!_isExpanded && !_isAnimating &&
                !_transitionCoordinator.IsTransitionActive &&
                _transitionCoordinator.CurrentView == NotchView.Compact)
            {
                PlayGestureMiddleClickFeedback();
            }

            if (_isExpanded && MediaPlayback.PlayPauseButton != null && MediaPlayback.PlayPauseButton.Visibility == Visibility.Visible)
            {
                PlayGentleButtonPressAnimation(MediaPlayback.PlayPauseButton);
            }
            else if (_isExpanded && InlinePlayPauseButton != null && InlinePlayPauseButton.Visibility == Visibility.Visible)
            {
                PlayGentleButtonPressAnimation(InlinePlayPauseButton);
            }

            _progressEngine.NotifyUserPlayPause(_isPlaying);
            await _viewModel.PlayPauseCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            RuntimeLog.Error(MediaCtrlLogTag, ex, "MiddleClick/PlayPause failed");
        }
    }

    private void PlayPauseButton_Click(object sender, MouseButtonEventArgs e) => MediaPlayback.PlayPauseButton_Click(sender, e);

    private void NextButton_Click(object sender, MouseButtonEventArgs e) => MediaPlayback.NextButton_Click(sender, e);

    private void PrevButton_Click(object sender, MouseButtonEventArgs e) => MediaPlayback.PrevButton_Click(sender, e);

    private bool? _presentedInlinePlaying;

    private void OnViewModelPlayPauseToggled(object? sender, bool isPlaying)
    {
        _lastMediaActionTime = DateTime.UtcNow;
        _isPlaying = isPlaying;
        UpdatePlayPauseIcon();
    }

    private void UpdatePlayPauseIcon()
    {
        RefreshPlaybackAccessibleNames();
        MediaPlayback.PresentPlayback(_isPlaying);
        if (_presentedInlinePlaying == _isPlaying) return;
        _presentedInlinePlaying = _isPlaying;
        var duration = TimeSpan.FromMilliseconds(180);

        if (_isPlaying)
        {
            AnimateIconSwitch(InlinePlayIcon, InlinePauseIcon, duration, _easeQuadInOut);
        }
        else
        {
            AnimateIconSwitch(InlinePauseIcon, InlinePlayIcon, duration, _easeQuadInOut);
        }
    }

    private async void InlinePlayPauseButton_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        try
        {
            _lastMediaActionTime = DateTime.UtcNow;
            _isPlaying = !_viewModel.IsPlaying;
            UpdatePlayPauseIcon();
            PlayButtonPressAnimation(InlinePlayPauseButton);

            _progressEngine.NotifyUserPlayPause(_isPlaying);

            await _viewModel.PlayPauseCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            RuntimeLog.Error(MediaCtrlLogTag, ex, "InlinePlayPause failed");
        }
    }

    private async void InlineNextButton_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        try
        {
            PlayButtonPressAnimation(InlineNextButton);
            PlayNextSkipAnimation(InlineNextArrow0, InlineNextArrow1);

            OptimisticPrepareForNextTrack();
            await _viewModel.NextTrackCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            RuntimeLog.Error(MediaCtrlLogTag, ex, "InlineNext failed");
        }
    }

    private async void InlinePrevButton_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        try
        {
            PlayButtonPressAnimation(InlinePrevButton);
            PlayPrevSkipAnimation(InlinePrevArrow0, InlinePrevArrow2);

            PrepareForPreviousTrackRequest();
            await _viewModel.PreviousTrackCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            RuntimeLog.Error(MediaCtrlLogTag, ex, "InlinePrev failed");
        }
    }

    private void PrepareForPreviousTrackRequest() => MediaPlayback.PrepareForPreviousTrackRequest();
    private void OptimisticPrepareForNextTrack() => MediaPlayback.OptimisticPrepareForNextTrack();

    private void ThumbnailBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        OpenCurrentMediaSourceFromThumbnail();
    }

    private void CompactThumbnailBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_settings.EnableGestureControls && !_isExpanded && !_isMusicExpanded &&
            _currentMediaInfo != null && _currentMediaInfo.IsAnyMediaPlaying && !_isAnimating)
        {
            return;
        }

        e.Handled = true;
        if (!_isExpanded && !_isAnimating)
        {
            ExpandNotch();
        }
    }

    private void OpenCurrentMediaSourceFromThumbnail()
    {
        var info = _currentMediaInfo;
        if (info == null || !info.IsAnyMediaPlaying) return;
        Task.Run(() => MediaWindowActivator.TryActivateForMedia(info), System.Threading.CancellationToken.None)
            .SafeFireAndForget("MEDIA-ACTIVATE");
    }

    private void MediaButton_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Border button && button.RenderTransform is ScaleTransform transform)
        {
            button.CacheMode ??= new System.Windows.Media.BitmapCache(1.5);

            var animX = new DoubleAnimation(transform.ScaleX, 1.18, _dur150) { EasingFunction = _easeQuadOut };
            var animY = new DoubleAnimation(transform.ScaleY, 1.18, _dur150) { EasingFunction = _easeQuadOut };
            Timeline.SetDesiredFrameRate(animX, VNotch.Services.AnimationConfig.TargetFps);
            Timeline.SetDesiredFrameRate(animY, VNotch.Services.AnimationConfig.TargetFps);
            transform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
        }
    }

    private void MediaButton_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Border button && button.RenderTransform is ScaleTransform transform)
        {
            var animX = new DoubleAnimation(transform.ScaleX, 1.0, _dur200) { EasingFunction = _easeQuadOut };
            var animY = new DoubleAnimation(transform.ScaleY, 1.0, _dur200) { EasingFunction = _easeQuadOut };
            Timeline.SetDesiredFrameRate(animX, VNotch.Services.AnimationConfig.TargetFps);
            Timeline.SetDesiredFrameRate(animY, VNotch.Services.AnimationConfig.TargetFps);

            animX.Completed += (_, _) =>
            {
                if (!button.IsMouseOver)
                    button.CacheMode = null;
            };

            transform.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
            transform.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
        }
    }

    #endregion

    #region Volume Control

    private float _currentVolume = 0.5f;
    private const float VolumeScrollStep = 0.05f;
    private const double CompactVolumeImmediateWheelDelta = 120.0;
    private const double CompactVolumeSmoothActivationDelta = 240.0;
    private const double CompactVolumeWheelSessionResetMs = 450.0;
    private const double CompactVolumeClickSuppressMs = 600.0;
    private bool _isVolumeIndicatorActive = false;
    private bool _volumeSynced = false;
    private double _compactVolumeWheelAccumulator = 0;
    private DateTime _lastCompactVolumeWheelUtc = DateTime.MinValue;
    private DateTime _suppressCompactVolumeWheelUntilUtc = DateTime.MinValue;
    private bool _isCompactVolumeWheelActive = false;

    private void SuppressCompactVolumeWheelForClick()
    {
        _suppressCompactVolumeWheelUntilUtc = DateTime.UtcNow.AddMilliseconds(CompactVolumeClickSuppressMs);
        ResetCompactVolumeWheelIntent();
    }

    private void ResetCompactVolumeWheelIntent()
    {
        ++_volumeBaselineVersion;
        _volumeBaselinePending = false;
        _compactVolumeWheelAccumulator = 0;
        _lastCompactVolumeWheelUtc = DateTime.MinValue;
        _isCompactVolumeWheelActive = false;
    }

    private bool TryGetCompactVolumeWheelDelta(int rawDelta, out int volumeDelta)
    {
        volumeDelta = 0;
        if (rawDelta == 0) return false;

        var now = DateTime.UtcNow;
        if (now < _suppressCompactVolumeWheelUntilUtc)
        {
            ResetCompactVolumeWheelIntent();
            return false;
        }

        if ((now - _lastCompactVolumeWheelUtc).TotalMilliseconds > CompactVolumeWheelSessionResetMs)
        {
            _compactVolumeWheelAccumulator = 0;
            _isCompactVolumeWheelActive = false;
        }
        _lastCompactVolumeWheelUtc = now;

        if (_isVolumeIndicatorActive || _isCompactVolumeWheelActive)
        {
            _isCompactVolumeWheelActive = true;
            volumeDelta = rawDelta;
            return true;
        }

        if (Math.Abs(rawDelta) >= CompactVolumeImmediateWheelDelta)
        {
            _isCompactVolumeWheelActive = true;
            volumeDelta = rawDelta;
            return true;
        }

        if (_compactVolumeWheelAccumulator != 0 &&
            Math.Sign(_compactVolumeWheelAccumulator) != Math.Sign(rawDelta))
        {
            _compactVolumeWheelAccumulator = 0;
        }

        _compactVolumeWheelAccumulator += rawDelta;
        if (Math.Abs(_compactVolumeWheelAccumulator) < CompactVolumeSmoothActivationDelta)
        {
            return false;
        }

        _compactVolumeWheelAccumulator = 0;
        _isCompactVolumeWheelActive = true;
        volumeDelta = Math.Sign(rawDelta) * (int)CompactVolumeImmediateWheelDelta;
        return true;
    }

    private bool _volumeBaselinePending;
    private int _volumeBaselineVersion;
    private DateTime _volumeScrollSyncAfterUtc = DateTime.MinValue;

    private static void SetVolumeFill(ScaleTransform scale, double volume, bool animate = false)
    {
        double target = Math.Clamp(volume, 0, 1);
        double current = scale.ScaleX;
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        scale.ScaleX = target;
        if (!animate || AnimationConfig.ReduceMotion || Math.Abs(current - target) < 0.0001) return;

        // Retarget from the visible fill while keeping the final value on the
        // base property, so a drag or keyboard update can take over immediately.
        var animation = MakeAnim(current, target, _dur150, _easeAppleOut);
        animation.FillBehavior = FillBehavior.Stop;
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private void AdjustVolumeByScroll(int delta)
    {
        if (!_volumeSynced)
        {
            // Query baseline volume off UI thread to avoid cold COM query stalls,
            // applying volume state once known to prevent flicker.
            if (_volumeBaselinePending) return;
            _volumeBaselinePending = true;
            int version = ++_volumeBaselineVersion;

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                bool ok = _mediaService.TryGetCurrentSessionVolume(out float vol, out bool _);
                Dispatcher.BeginInvoke(() =>
                {
                    if (version != _volumeBaselineVersion) return;
                    _volumeBaselinePending = false;
                    if (_isExpanded || _isAnimating ||
                        DateTime.UtcNow < _suppressCompactVolumeWheelUntilUtc) return;
                    if (ok) _currentVolume = vol;
                    _volumeSynced = true;
                    ApplyVolumeStep(delta);
                });
            });
            return;
        }

        ApplyVolumeStep(delta);
    }

    private void ApplyVolumeStep(int delta)
    {
        _volumeInteractionVersion++;
        _volumeScrollSyncAfterUtc = AnimationConfig.ReduceMotion ? DateTime.MinValue : DateTime.UtcNow.Add(_dur150.TimeSpan);
        float step = (delta / 120f) * VolumeScrollStep;
        float newVolume = Math.Clamp(_currentVolume + step, 0f, 1f);
        _currentVolume = newVolume;

        ShowVolumeIndicator(newVolume);

        float volumeToSet = newVolume;
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            _mediaService.TrySetCurrentSessionVolume(volumeToSet);
        });

        if (_isExpanded)
        {
            SetVolumeFill(VolumeBarScale, newVolume, animate: true);
            UpdateVolumeIcon(newVolume, false);
        }
    }
    private static readonly Geometry _volHighGeometry = Geometry.Parse("M3,9V15H7L12,20V4L7,9H3M16.5,12C16.5,10.23 15.5,8.71 14,7.97V16.02C15.5,15.29 16.5,13.77 16.5,12M14,3.23V5.29C16.89,6.15 19,8.83 19,12C19,15.17 16.89,17.85 14,18.71V20.77C18.01,19.86 21,16.28 21,12C21,7.72 18.01,4.14 14,3.23Z");
    private static readonly Geometry _volLowGeometry = Geometry.Parse("M3,9V15H7L12,20V4L7,9H3M16.5,12C16.5,10.23 15.5,8.71 14,7.97V16.02C15.5,15.29 16.5,13.77 16.5,12Z");
    private static readonly Geometry _volMuteGeometry = Geometry.Parse("M12,4L9.91,6.09L12,8.18M4.27,3L3,4.27L7.73,9H3V15H7L12,20V13.27L16.25,17.53C15.58,18.04 14.83,18.46 14,18.7V20.77C15.38,20.45 16.63,19.82 17.68,18.96L19.73,21L21,19.73L4.27,3M19,12C19,12.94 18.8,13.82 18.46,14.64L19.97,16.15C20.62,14.91 21,13.5 21,12C21,7.72 18.01,4.14 14,3.23V5.29C16.89,6.15 19,8.83 19,12M16.5,12C16.5,10.23 15.5,8.71 14,7.97V10.18L16.45,12.63C16.5,12.43 16.5,12.21 16.5,12Z");

    private void ShowVolumeIndicator(float volume, bool isMuted = false)
    {
        if (_isExpanded || _isAnimating) return;
        if (!_settings.EnableVolumeOsd) return;

        string volIcon = isMuted || volume <= 0.001f ? "🔇" : volume < 0.5f ? "🔉" : "🔊";
        string volPercent = isMuted ? "Mute" : $"{(int)Math.Round(volume * 100)}%";

        if (CompactActionIconBorder != null)
        {
            CompactActionIconBorder.Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
        }

        if (CompactActionIconPath != null)
        {
            CompactActionIconPath.Data = isMuted || volume <= 0.001f ? _volMuteGeometry : volume < 0.5f ? _volLowGeometry : _volHighGeometry;
            CompactActionIconPath.Fill = Brushes.White;
        }

        if (CompactVolumePercentText != null)
        {
            CompactVolumePercentText.Text = volPercent;
        }

        if (CompactVolumeFill != null)
        {
            double fillW = Math.Max(0, Math.Min(1.0, isMuted ? 0 : volume)) * 100.0;
            CompactVolumeFill.Width = fillW;
        }

        if (VolumeOsdText != null) VolumeOsdText.Text = volPercent;
        if (VolumeOsdIcon != null) VolumeOsdIcon.Text = volIcon;

        SetTransientAction(CompactTransientAction.Volume, TimeSpan.FromMilliseconds(1500));
    }

    private int _lastVolumeInt = -1;
    private bool? _lastMuteState = null;
    private bool _hasReceivedInitialVolume = false;

    public void OnHardwareVolumeChanged(float volume, bool isMuted)
    {
        if (_cleanedUp) return;
        Dispatcher.BeginInvoke(() =>
        {
            int newVolumeInt = (int)Math.Round(volume * 100);
            _currentVolume = volume;

            if (_isExpanded)
            {
                SetVolumeFill(VolumeBarScale, isMuted ? 0 : volume, animate: true);
                UpdateVolumeIcon(volume, isMuted);
            }

            if (!_hasReceivedInitialVolume)
            {
                _hasReceivedInitialVolume = true;
                _lastVolumeInt = newVolumeInt;
                _lastMuteState = isMuted;
                return;
            }

            bool volumeChanged = newVolumeInt != _lastVolumeInt;
            bool muteChanged = isMuted != _lastMuteState;

            if (!volumeChanged && !muteChanged)
            {
                return;
            }

            _lastVolumeInt = newVolumeInt;
            _lastMuteState = isMuted;

            ShowVolumeIndicator(volume, isMuted);
        });
    }

    private void HideVolumeIndicator()
    {
        ResetCompactVolumeWheelIntent();
        _isVolumeIndicatorActive = false;
        _volumeSynced = false;
        _isDraggingVolumeIndicator = false;
        EvaluateAndRestoreNotchState();
    }

    private void DismissVolumeIndicatorImmediate(bool restoreMedia = false, bool animateExit = false)
    {
        ResetCompactVolumeWheelIntent();
        _isVolumeIndicatorActive = false;
        _volumeSynced = false;
        _isDraggingVolumeIndicator = false;
        EvaluateAndRestoreNotchState();
    }

    #region Volume Indicator Drag

    private bool _isDraggingVolumeIndicator = false;

    private void VolumeIndicator_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _isDraggingVolumeIndicator = true;
        VolumeIndicatorContainer.CaptureMouse();
        SetVolumeFromIndicatorPosition(e);

        _transientActionTimer?.Stop();
    }

    private void VolumeIndicator_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDraggingVolumeIndicator || e.LeftButton != MouseButtonState.Pressed) return;
        e.Handled = true;
        SetVolumeFromIndicatorPosition(e);
    }

    private void VolumeIndicator_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDraggingVolumeIndicator) return;
        e.Handled = true;
        _isDraggingVolumeIndicator = false;
        VolumeIndicatorContainer.ReleaseMouseCapture();

        _transientActionTimer?.Stop();
        _transientActionTimer?.Start();
    }

    private void SetVolumeFromIndicatorPosition(MouseEventArgs e)
    {
        _volumeInteractionVersion++;
        var pos = e.GetPosition(VolumeIndicatorContainer);
        double containerWidth = VolumeIndicatorContainer.ActualWidth;
        if (containerWidth <= 0) containerWidth = _collapsedWidth - 32;

        double position = FlowDirection == FlowDirection.RightToLeft ? containerWidth - pos.X : pos.X;
        float newVolume = (float)Math.Clamp(position / containerWidth, 0.0, 1.0);
        _currentVolume = newVolume;
        SetVolumeFill(VolumeIndicatorScale, newVolume);

        float volumeToSet = newVolume;
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            _mediaService.TrySetCurrentSessionVolume(volumeToSet);
        });

        if (_isExpanded)
        {
            SetVolumeFill(VolumeBarScale, newVolume);
            UpdateVolumeIcon(newVolume, false);
        }
    }

    #endregion

    private void VolumeIcon_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        int request = ++_volumeInteractionVersion;
        // Every click is a toggle, so retain each one while serializing driver
        // calls. Concurrent read/modify/write operations can otherwise lose clicks.
        _mediaVolumeWrites.Post($"mute:{request}", () =>
        {
            if (!_mediaService.TryToggleCurrentSessionMute()) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _volumeInteractionVersion++;
                if (_volumeReadInFlight)
                    _pendingMuteRefreshVersion = _volumeInteractionVersion;
                else
                    SyncVolumeFromActiveSession();
            }));
        });
    }

    private void VolumeIcon_MouseEnter(object sender, MouseEventArgs e)
    {
        var animX = new DoubleAnimation(1, 1.2, _dur150) { EasingFunction = _easeQuadOut };
        var animY = new DoubleAnimation(1, 1.2, _dur150) { EasingFunction = _easeQuadOut };
        System.Windows.Media.Animation.Timeline.SetDesiredFrameRate(animX, VNotch.Services.AnimationConfig.TargetFps);
        VolumeIconScale.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
        System.Windows.Media.Animation.Timeline.SetDesiredFrameRate(animY, VNotch.Services.AnimationConfig.TargetFps);
        VolumeIconScale.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
    }

    private void VolumeIcon_MouseLeave(object sender, MouseEventArgs e)
    {
        var animX = new DoubleAnimation(VolumeIconScale.ScaleX, 1, _dur150) { EasingFunction = _easeQuadOut };
        var animY = new DoubleAnimation(VolumeIconScale.ScaleY, 1, _dur150) { EasingFunction = _easeQuadOut };
        System.Windows.Media.Animation.Timeline.SetDesiredFrameRate(animX, VNotch.Services.AnimationConfig.TargetFps);
        VolumeIconScale.BeginAnimation(ScaleTransform.ScaleXProperty, animX);
        System.Windows.Media.Animation.Timeline.SetDesiredFrameRate(animY, VNotch.Services.AnimationConfig.TargetFps);
        VolumeIconScale.BeginAnimation(ScaleTransform.ScaleYProperty, animY);
    }

    private void VolumeBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
        _isDraggingVolume = true;
        VolumeBarContainer.CaptureMouse();
        SetVolumeFromMousePosition(e);
    }

    private void VolumeBar_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        if (e.Delta == 0 || _isDraggingVolume || _isAnimating) return;
        ApplyVolumeStep(e.Delta);
    }

    private void VolumeBar_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDraggingVolume && e.LeftButton == MouseButtonState.Pressed)
        {
            SetVolumeFromMousePosition(e);
        }
    }

    private void VolumeBar_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDraggingVolume)
        {
            _isDraggingVolume = false;
            VolumeBarContainer.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void SetVolumeFromMousePosition(MouseEventArgs e)
    {
        _volumeInteractionVersion++;
        const double volumeBarWidth = 100.0;
        var pos = e.GetPosition(VolumeBarContainer);
        float newVolume = (float)Math.Clamp(pos.X / volumeBarWidth, 0.0, 1.0);

        _currentVolume = newVolume;
        SetVolumeFill(VolumeBarScale, newVolume);
        UpdateVolumeIcon(newVolume, false);

        float volumeToSet = newVolume;
        System.Threading.ThreadPool.QueueUserWorkItem(_ =>
        {
            _mediaService.TrySetCurrentSessionVolume(volumeToSet);
        });
    }

    private bool _volumeReadInFlight;
    private int? _pendingMuteRefreshVersion;
    private int _volumeInteractionVersion;

    private async void SyncVolumeFromActiveSession()
    {
        if (_isDraggingVolume || _volumeReadInFlight) return;

        _volumeReadInFlight = true;
        int version = _volumeInteractionVersion;
        try
        {
            var result = await Task.Run(() =>
            {
                bool success = _mediaService.TryGetCurrentSessionVolume(out float volume, out bool muted);
                return (success, volume, muted);
            });
            if (_isDraggingVolume || version != _volumeInteractionVersion) return;

            if (result.success)
            {
                _currentVolume = result.volume;
                SetVolumeFill(VolumeBarScale, _currentVolume);
                UpdateVolumeIcon(_currentVolume, result.muted);
            }
        }
        catch (Exception ex)
        {
            RuntimeLog.Error(MediaCtrlLogTag, ex, "Read session volume failed");
        }
        finally
        {
            _volumeReadInFlight = false;
            int? pendingVersion = _pendingMuteRefreshVersion;
            _pendingMuteRefreshVersion = null;
            if (pendingVersion == _volumeInteractionVersion)
                SyncVolumeFromActiveSession();
        }
    }

    private void UpdateVolumeIcon(float volume, bool isMuted)
    {
        _viewModel.UpdateVolumeState(volume, isMuted);
    }

    private void NotchCloseButton_Click(object sender, MouseButtonEventArgs e)
    {
        CollapseAll();
    }

    private void NotchContext_HideUntilHover_Click(object sender, RoutedEventArgs e)
    {
        CollapseAll();
        if (NotchContainer != null)
        {
            NotchContainer.BeginAnimation(OpacityProperty, null);
            NotchContainer.Opacity = 0;
            NotchContainer.IsHitTestVisible = false;
        }
    }

    private void NotchContext_RestartState_Click(object sender, RoutedEventArgs e)
    {
        _isTransientOsdActive = false;
        _activeTransientAction = CompactTransientAction.None;
        _transientActionTimer?.Stop();
        EvaluateAndRestoreNotchState();
    }

    private void NotchContext_ExitApp_Click(object sender, RoutedEventArgs e)
    {
        Exit_Click(sender, e);
    }

    #endregion
}
