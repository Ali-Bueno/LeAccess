using HarmonyLib;
using LeAccess.Accessibility.Dialogue;
using PixelCrushers.DialogueSystem;
using PixelCrushers.DialogueSystem.TextMeshPro;

namespace LeAccess.Accessibility.Patches;

[HarmonyPatch(typeof(TextMeshProDialogueUI), nameof(TextMeshProDialogueUI.ShowSubtitle), typeof(Subtitle))]
internal static class DialogueSubtitleShownPatch
{
    private static void Postfix(TextMeshProDialogueUI __instance, Subtitle subtitle) =>
        DialogueAnnouncer.ShowSubtitle(__instance, subtitle);
}

[HarmonyPatch(typeof(TextMeshProDialogueUI), nameof(TextMeshProDialogueUI.HideSubtitle), typeof(Subtitle))]
internal static class DialogueSubtitleHiddenPatch
{
    private static void Postfix(TextMeshProDialogueUI __instance, Subtitle subtitle) =>
        DialogueAnnouncer.HideSubtitle(__instance, subtitle);
}

[HarmonyPatch(typeof(TextMeshProDialogueUI), nameof(TextMeshProDialogueUI.ShowResponses))]
internal static class DialogueResponsesShownPatch
{
    private static void Postfix(TextMeshProDialogueUI __instance) => DialogueAnnouncer.ShowResponses(__instance);
}

[HarmonyPatch(typeof(TextMeshProDialogueUI), nameof(TextMeshProDialogueUI.HideResponses))]
internal static class DialogueResponsesHiddenPatch
{
    private static void Postfix(TextMeshProDialogueUI __instance) => DialogueAnnouncer.HideResponses(__instance);
}

[HarmonyPatch(typeof(DialogueSystemNavigable), nameof(DialogueSystemNavigable.ConversationEnd))]
internal static class DialogueConversationEndedPatch
{
    private static void Postfix() => DialogueAnnouncer.Reset();
}
