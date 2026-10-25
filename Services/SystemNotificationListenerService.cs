using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Automation;
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
    private AutomationEventHandler? _automationWindowOpenedHandler;
    private bool _isAutomationListening;

    public event Action<AppNotificationInfo>? NotificationReceived;

    public bool IsSupported => ApiInformation.IsTypePresent("Windows.UI.Notifications.Management.UserNotificationListener");

    public async Task<bool> StartListeningAsync()
    {
        if (!IsSupported)
        {
            StartAutomationFallback();
            return true;
        }

        try
        {
            _listener = UserNotificationListener.Current;
            var accessStatus = await _listener.RequestAccessAsync();
            if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
            {
                RuntimeLog.Log(LogCategory, $"Notification access: {accessStatus}, falling back to UI Automation");
                StartAutomationFallback();
                return true;
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
            RuntimeLog.Log(LogCategory, $"Start error: {ex.Message}, falling back to UI Automation");
            StartAutomationFallback();
            return true;
        }
    }

    private void StartAutomationFallback()
    {
        if (_isAutomationListening) return;
        try
        {
            _automationWindowOpenedHandler = OnAutomationWindowOpened;
            Automation.AddAutomationEventHandler(
                WindowPattern.WindowOpenedEvent,
                AutomationElement.RootElement,
                TreeScope.Children,
                _automationWindowOpenedHandler);
            _isAutomationListening = true;
            RuntimeLog.Log(LogCategory, "UI Automation toast fallback started");
        }
        catch (Exception ex)
        {
            RuntimeLog.Log(LogCategory, $"UI Automation fallback error: {ex.Message}");
        }
    }

    private void OnAutomationWindowOpened(object sender, AutomationEventArgs e)
    {
        Task.Run(() =>
        {
            try
            {
                if (sender is not AutomationElement element) return;
                string className = element.Current.ClassName ?? "";
                string name = element.Current.Name ?? "";

                bool isCandidate = className.Contains("Windows.UI.Core.CoreWindow", StringComparison.OrdinalIgnoreCase) ||
                                   className.Contains("Toast", StringComparison.OrdinalIgnoreCase) ||
                                   name.Contains("notification", StringComparison.OrdinalIgnoreCase);

                if (!isCandidate) return;

                var textElements = element.FindAll(
                    TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Text));

                if (textElements == null || textElements.Count == 0) return;

                var texts = new List<string>();
                for (int i = 0; i < textElements.Count; i++)
                {
                    string txt = textElements[i].Current.Name;
                    if (!string.IsNullOrWhiteSpace(txt)) texts.Add(txt.Trim());
                }

                if (texts.Count == 0) return;

                bool isDiscord = texts.Any(t => t.Contains("Discord", StringComparison.OrdinalIgnoreCase));
                bool isWhatsApp = texts.Any(t => t.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase));

                if (!isDiscord && !isWhatsApp) return;

                string resolvedApp = isDiscord ? "Discord" : "WhatsApp";
                string title = texts.Count > 0 ? texts[0] : resolvedApp;
                string message = texts.Count > 1 ? string.Join(" ", texts.Skip(1)) : "";

                NotificationReceived?.Invoke(new AppNotificationInfo(resolvedApp, title, message, resolvedApp));
            }
            catch { }
        });
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

            string title = "";
            string message = "";

            var binding = notif.Notification.Visual.GetBinding(KnownNotificationBindings.ToastGeneric);
            if (binding != null)
            {
                var textElements = binding.GetTextElements();
                var list = textElements.Select(t => t.Text).Where(t => !string.IsNullOrWhiteSpace(t)).ToList();
                if (list.Count > 0) title = list[0];
                if (list.Count > 1) message = string.Join(" ", list.Skip(1));
            }

            bool isDiscord = appDisplayName.Contains("Discord", StringComparison.OrdinalIgnoreCase) ||
                             appId.Contains("Discord", StringComparison.OrdinalIgnoreCase) ||
                             title.Contains("Discord", StringComparison.OrdinalIgnoreCase) ||
                             message.Contains("Discord", StringComparison.OrdinalIgnoreCase);

            bool isWhatsApp = appDisplayName.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) ||
                              appId.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) ||
                              title.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) ||
                              message.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase);

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

        if (_isAutomationListening && _automationWindowOpenedHandler != null)
        {
            try
            {
                Automation.RemoveAutomationEventHandler(
                    WindowPattern.WindowOpenedEvent,
                    AutomationElement.RootElement,
                    _automationWindowOpenedHandler);
            }
            catch { }
            _isAutomationListening = false;
        }
    }
}
