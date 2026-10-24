using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace VNotch.Services;

public record AppNotificationInfo(string AppName, string Title, string Message, string AppId);

public sealed class SystemNotificationListenerService : IDisposable
{
    private const string LogCategory = "NOTIF-LISTENER";
    private UserNotificationListener? _listener;
    private bool _isListening;
    private uint _lastNotificationId;

    public event Action<AppNotificationInfo>? NotificationReceived;

    public bool IsSupported => ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener");

    public async Task<bool> StartListeningAsync()
    {
        if (!IsSupported) return false;

        try
        {
            _listener = UserNotificationListener.Current;
            var accessStatus = await _listener.RequestAccessAsync();
            if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
            {
                RuntimeLog.Log(LogCategory, $"Notification access: {accessStatus}");
                return false;
            }

            _listener.NotificationChanged += OnNotificationChanged;
            _isListening = true;

            var existing = await _listener.GetNotificationsAsync(NotificationKinds.Toast);
            if (existing != null && existing.Count > 0)
            {
                _lastNotificationId = existing.Max(n => n.Id);
            }

            return true;
        }
        catch (Exception ex)
        {
            RuntimeLog.Log(LogCategory, $"Start error: {ex.Message}");
            return false;
        }
    }

    private void OnNotificationChanged(UserNotificationListener sender, UserNotificationChangedEventArgs args)
    {
        if (args.ChangeKind != UserNotificationChangedKind.Added) return;

        try
        {
            var notif = sender.GetNotification(args.UserNotificationId);
            if (notif == null) return;
            if (notif.Id <= _lastNotificationId) return;
            _lastNotificationId = notif.Id;

            string appDisplayName = notif.AppInfo?.DisplayInfo?.DisplayName ?? "";
            string appId = notif.AppInfo?.Id ?? "";

            bool isDiscord = appDisplayName.Contains("Discord", StringComparison.OrdinalIgnoreCase) ||
                             appId.Contains("Discord", StringComparison.OrdinalIgnoreCase);
            bool isWhatsApp = appDisplayName.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) ||
                              appId.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase);

            string title = "";
            string message = "";

            var binding = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
            if (binding != null)
            {
                var textElements = binding.GetTextElements();
                var list = textElements.Select(t => t.Text).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                if (list.Count > 0) title = list[0];
                if (list.Count > 1) message = string.Join(" ", list.Skip(1));

                if (!isDiscord && !isWhatsApp)
                {
                    if (title.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) ||
                        message.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase))
                    {
                        isWhatsApp = true;
                    }
                }
            }

            if (!isDiscord && !isWhatsApp) return;

            string resolvedApp = isDiscord ? "Discord" : "WhatsApp";
            NotificationReceived?.Invoke(new AppNotificationInfo(resolvedApp, title, message, appId));
        }
        catch (Exception ex)
        {
            RuntimeLog.Log(LogCategory, $"Process notification error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (_listener != null && _isListening)
        {
            try { _listener.NotificationChanged -= OnNotificationChanged; } catch { }
            _isListening = false;
        }
    }
}
