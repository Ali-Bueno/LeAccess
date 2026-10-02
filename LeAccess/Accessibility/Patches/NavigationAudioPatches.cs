using HarmonyLib;
using LeAccess.Accessibility.Navigation;

namespace LeAccess.Accessibility.Patches;

[HarmonyPatch(typeof(DialogueSystemNavigable), nameof(DialogueSystemNavigable.ConversationStart))]
internal static class RadarConversationStartPatch
{
    private static void Postfix(DialogueSystemNavigable __instance) =>
        NavigationAudioState.ConversationStarted(__instance);
}

[HarmonyPatch(typeof(DialogueSystemNavigable), nameof(DialogueSystemNavigable.ConversationEnd))]
internal static class RadarConversationEndPatch
{
    private static void Postfix(DialogueSystemNavigable __instance) =>
        NavigationAudioState.ConversationEnded(__instance);
}
