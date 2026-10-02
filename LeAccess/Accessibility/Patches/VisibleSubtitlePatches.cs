using HarmonyLib;
using LeAccess.Accessibility.Dialogue;
using LE.Services;
using PixelCrushers.DialogueSystem;

namespace LeAccess.Accessibility.Patches;

[HarmonyPatch(typeof(SystemMessageService), nameof(SystemMessageService.SubmitNpcMessage), typeof(string), typeof(string))]
internal static class SubmittedNpcSubtitlePatch
{
    private static void Postfix(string content, string sender) =>
        VisibleSubtitleAnnouncer.NpcMessageSubmitted(content, sender);
}

[HarmonyPatch(typeof(FloatingBark), nameof(FloatingBark.Bark), typeof(string), typeof(string))]
internal static class FloatingBarkTextShownPatch
{
    private static void Postfix(FloatingBark __instance) => VisibleSubtitleAnnouncer.BarkShown(__instance);
}

[HarmonyPatch(typeof(FloatingBark), nameof(FloatingBark.Bark), typeof(Subtitle))]
internal static class FloatingBarkSubtitleShownPatch
{
    private static void Postfix(FloatingBark __instance) => VisibleSubtitleAnnouncer.BarkShown(__instance);
}

[HarmonyPatch(typeof(FloatingBark), nameof(FloatingBark.Hide))]
internal static class FloatingBarkHiddenPatch
{
    private static void Postfix(FloatingBark __instance) => VisibleSubtitleAnnouncer.BarkHidden(__instance);
}
