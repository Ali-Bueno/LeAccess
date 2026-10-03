namespace UnityEngine.UI
{
    internal sealed class Graphic : UnityEngine.Component { }
}

namespace PixelCrushers.DialogueSystem
{
    internal static class DialogueManager { internal static DialogueSystemController instance; }
    internal sealed class DialogueSystemController : UnityEngine.Component
    { internal ConversationController conversationController; internal NativeDialogueUI currentDialogueUI; }
    internal sealed class ConversationController { internal bool WasCollected; internal bool IsActive=true; internal ConversationState state; }
    internal sealed class ConversationState { internal bool WasCollected; internal Subtitle subtitle; }
    internal class NativeDialogueUI : UnityEngine.Component
    { internal T TryCast<T>() where T:class => this as T; }
    internal sealed class Subtitle
    {
        private static int _nextId;
        public readonly IntPtr Pointer = new(++_nextId);
        public bool WasCollected;
        public FormattedText formattedText;
    }

    internal sealed class FormattedText
    {
        public bool WasCollected;
        public string text;
    }
}

namespace PixelCrushers.DialogueSystem.TextMeshPro
{
    internal sealed class TextMeshProDialogueUI : PixelCrushers.DialogueSystem.NativeDialogueUI
    {
        public IntPtr Pointer => new(GetInstanceID());
        public bool isActiveAndEnabled = true;
        public TextMeshProDialogueControls dialogue;
        public TextMeshProSubtitleControls currentSubtitleControls;
    }

    internal sealed class TextMeshProDialogueControls
    {
        public TextMeshProSubtitleControls npcSubtitle;
        public bool WasCollected;
        public UnityEngine.UI.Graphic panel;
        public TextMeshProResponseMenuControls responseMenu;
    }

    internal sealed class TextMeshProSubtitleControls
    {
        private static int _next; public readonly IntPtr Pointer=new(++_next);
        public bool WasCollected;
        public UnityEngine.UI.Graphic panel;
        public TMPro.TMP_Text line;
        public TMPro.TMP_Text portraitName;
        public UnityEngine.UI.Button continueButton;
    }

    internal sealed class TextMeshProResponseMenuControls
    {
        public bool WasCollected;
        public bool isVisible = true;
        public UnityEngine.UI.Graphic panel;
        public TextMeshProSubtitleControls subtitleReminder;
    }
}
