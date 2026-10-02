using HarmonyLib;
using LE.UI.Login.UnityUI;
using LeAccess.Accessibility.Login;

namespace LeAccess.Accessibility.Patches;

[HarmonyPatch(typeof(LandingZonePanel), nameof(LandingZonePanel.OnPlayOfflineClicked))]
internal static class OfflineStartupClickPatch
{
    private static bool Prefix(LandingZonePanel __instance) => LoginTransitionGuard.MayExecute(__instance, online: false);
}

[HarmonyPatch(typeof(LandingZonePanel), nameof(LandingZonePanel.OnPlayOnlineClicked))]
internal static class OnlineStartupClickPatch
{
    private static bool Prefix(LandingZonePanel __instance) => LoginTransitionGuard.MayExecute(__instance, online: true);
}
