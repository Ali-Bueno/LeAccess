using System.Collections.Generic;
using UnityEngine;

namespace LeAccess.Accessibility.Navigation;

internal static class NavigationAudioState
{
    private static readonly Dictionary<int, DialogueSystemNavigable> Conversations = new();
    private static readonly List<int> StaleConversations = new();

    public static string SuspensionReason => !Application.isFocused ? "game unfocused"
        : Time.timeScale <= 0f ? "game paused"
        : LE.UI.LoadingScreen.IsVisible ? "scene loading"
        : Conversations.Count != 0 ? "conversation active" : null;

    public static bool IsSuspended => SuspensionReason != null;

    public static void ConversationStarted(DialogueSystemNavigable navigable)
    {
        if (NavigationObjectLifetime.IsAlive(navigable)) Conversations[navigable.GetInstanceID()] = navigable;
    }

    public static void ConversationEnded(DialogueSystemNavigable navigable)
    {
        if (NavigationObjectLifetime.IsAlive(navigable)) Conversations.Remove(navigable.GetInstanceID());
    }

    public static void Refresh()
    {
        StaleConversations.Clear();
        foreach (var conversation in Conversations)
            if (!NavigationObjectLifetime.IsAlive(conversation.Value) || !conversation.Value.isActiveAndEnabled)
                StaleConversations.Add(conversation.Key);
        foreach (int id in StaleConversations) Conversations.Remove(id);
    }

    public static void Reset() => Conversations.Clear();
}
