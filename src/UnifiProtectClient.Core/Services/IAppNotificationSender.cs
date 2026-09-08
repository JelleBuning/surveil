namespace UnifiProtectClient.Services;

internal interface IAppNotificationSender
{
    void Notify(string title, string? heroImagePath);
}
