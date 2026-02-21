using AppNotify.Models;
using Microsoft.Toolkit.Uwp.Notifications;

namespace AppNotify.Services;

public sealed class NotificationService
{
    public void SendStatusChangeNotification(string appName, AppStatus newStatus)
    {
        var builder = new ToastContentBuilder()
            .AddText("App Status Changed")
            .AddText($"{appName} is now: {newStatus.DisplayName()}");

        if (newStatus == AppStatus.PendingDeveloperRelease)
        {
            builder.AddText("🎉 Ready for Release!");
        }

        builder.Show();
    }
}
