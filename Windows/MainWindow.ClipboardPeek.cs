using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using VNotch.Services;
using static VNotch.Services.AnimationPrimitives;

namespace VNotch;

public partial class MainWindow
{
    #region Clipboard Peek

    private bool _isClipboardPeekActive = false;
    private UiScheduledTimer? _clipboardRevertTimer;

    private void PlayClipboardPeek()
    {

        if (!_isExpanded && !_isAnimating)
        {
            NotchScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            NotchScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            NotchShadowScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            NotchShadowScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);

            var peakTime = TimeSpan.FromMilliseconds(100);
            var endTime = TimeSpan.FromMilliseconds(500);

            var bounceX = new DoubleAnimationUsingKeyFrames();
            bounceX.KeyFrames.Add(new EasingDoubleKeyFrame(1.06,
                KeyTime.FromTimeSpan(peakTime), _easeQuadOut));
            bounceX.KeyFrames.Add(new EasingDoubleKeyFrame(1.0,
                KeyTime.FromTimeSpan(endTime), _easeSoftSpring));
            Timeline.SetDesiredFrameRate(bounceX, VNotch.Services.AnimationConfig.TargetFps);

            var bounceY = new DoubleAnimationUsingKeyFrames();
            bounceY.KeyFrames.Add(new EasingDoubleKeyFrame(0.95,
                KeyTime.FromTimeSpan(peakTime), _easeQuadOut));
            bounceY.KeyFrames.Add(new EasingDoubleKeyFrame(1.0,
                KeyTime.FromTimeSpan(endTime), _easeSoftSpring));
            Timeline.SetDesiredFrameRate(bounceY, VNotch.Services.AnimationConfig.TargetFps);

            NotchScale.BeginAnimation(ScaleTransform.ScaleXProperty, bounceX);
            NotchScale.BeginAnimation(ScaleTransform.ScaleYProperty, bounceY);
            NotchShadowScale.BeginAnimation(ScaleTransform.ScaleXProperty, bounceX);
            NotchShadowScale.BeginAnimation(ScaleTransform.ScaleYProperty, bounceY);
        }

        if (!_isExpanded)
        {
            ShowClipboardCopiedState();
        }
    }
    private void ShowClipboardCopiedState()
    {
        if (!_settings.EnableClipboardToast) return;
        string snippet = "";
        try
        {
            if (Clipboard.ContainsText())
            {
                string text = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    text = text.Replace("\r", " ").Replace("\n", " ").Trim();
                    if (text.Length > 28)
                    {
                        text = text.Substring(0, 25) + "...";
                    }
                    snippet = $"\"{text}\"";
                }
            }
            else if (Clipboard.ContainsImage())
            {
                snippet = "Image";
            }
            else if (Clipboard.ContainsFileDropList())
            {
                snippet = "Files";
            }
        }
        catch { }

        if (CompactCopiedSnippet != null)
        {
            if (!string.IsNullOrEmpty(snippet))
            {
                CompactCopiedSnippet.Text = snippet;
                CompactCopiedSnippet.Visibility = Visibility.Visible;
                if (CompactCopiedSeparator != null) CompactCopiedSeparator.Visibility = Visibility.Visible;
            }
            else
            {
                CompactCopiedSnippet.Text = "";
                CompactCopiedSnippet.Visibility = Visibility.Collapsed;
                if (CompactCopiedSeparator != null) CompactCopiedSeparator.Visibility = Visibility.Collapsed;
            }
        }

        if (ClipboardCopiedText != null)
        {
            ClipboardCopiedText.Text = Loc.Get("clipboard.copied").TrimStart('✓', ' ');
        }
        SetTransientAction(CompactTransientAction.Clipboard, TimeSpan.FromMilliseconds(1600));
    }
    private void RevertClipboardCopiedState()
    {
        EvaluateAndRestoreNotchState();
    }

    private void CancelClipboardPeekImmediate()
    {
        EvaluateAndRestoreNotchState();
    }

    private void StopClipboardRevertTimer()
    {
        var timer = _clipboardRevertTimer;
        _clipboardRevertTimer = null;
        timer?.Stop();
    }

    #endregion
}
