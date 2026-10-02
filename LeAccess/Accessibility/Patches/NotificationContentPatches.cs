using HarmonyLib;
using LeAccess.Accessibility.Announcements;
using LE.UI.NotificationSystem;

namespace LeAccess.Accessibility.Patches;

internal static class NotificationContentPatches
{
    [HarmonyPatch(typeof(OverChatNotification), nameof(OverChatNotification.ShowNotification))]
    internal static class OverChatShow
    {
        private static void Postfix(OverChatNotification __instance) => NotificationAnnouncer.TrackChat(__instance);
    }

    [HarmonyPatch(typeof(LocationNotificationManager), nameof(LocationNotificationManager.ActivateNotification))]
    internal static class LocationShow
    {
        private static void Postfix(LocationNotificationManager __instance) => NotificationAnnouncer.TrackLocation(__instance);
    }

    [HarmonyPatch(typeof(Notifications), nameof(Notifications.Awake))]
    internal static class GameplayNotificationsReady
    {
        private static void Postfix(Notifications __instance) => NotificationAnnouncer.TrackGameplay(__instance);
    }

    [HarmonyPatch(typeof(NotificationSystem), nameof(NotificationSystem.ShowOnChannel),
        typeof(NotificationChannel), typeof(NotificationBase), typeof(NotificationData))]
    internal static class ChannelNotificationShown
    {
        private static void Postfix(NotificationBase __result) => NotificationAnnouncer.GameplayDelivered(__result);
    }

}
