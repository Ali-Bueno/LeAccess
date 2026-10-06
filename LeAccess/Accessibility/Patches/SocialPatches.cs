using HarmonyLib;
using LeAccess.Accessibility.Social;
using LE.Services;

namespace LeAccess.Accessibility.Patches;

// Party and friend results reach the chat only here (native 0x1c9ec80; no relative branch in its first 16 bytes).
// See reference/social-reading.md#feedback.
[HarmonyPatch(typeof(SystemMessageService), nameof(SystemMessageService.SubmitSystemMessage), typeof(string))]
internal static class SocialSystemMessagePatch
{
    private static void Postfix(string content) => SocialAccessibility.SystemMessageSubmitted(content);
}
