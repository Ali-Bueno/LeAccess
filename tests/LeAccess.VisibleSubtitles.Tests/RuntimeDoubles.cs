internal class FloatingTextLineUI : UnityEngine.Component
{ internal bool isActiveAndEnabled=true; internal TMPro.TMP_Text textComponent; }
internal class FloatingBark : UnityEngine.Component
{ internal bool isActiveAndEnabled=true; internal float secondsLeft=5; internal FloatingTextLineUI FloatingUI; internal UnityEngine.Vector3 offset; }
internal class FloatingBarkUI : FloatingTextLineUI { internal FloatingBark bark; }
internal class CinematicSubtitlePanel : UnityEngine.Component
{
    internal static CinematicSubtitlePanel Instance;
    internal bool isActiveAndEnabled=true, IsActive=true;
    internal UnityEngine.CanvasGroup _canvasGroup;
    internal TMPro.TMP_Text _subtitleText;
}
internal class Cinematic : UnityEngine.Object { }
internal class CharacterSelectionVideoManager : UnityEngine.Component
{
    internal static CharacterSelectionVideoManager instance;
    internal bool isActiveAndEnabled = true, IsPlaying = true;
    internal Cinematic currentCinematic = new();
    internal int currentSubtitleIndex = 1;
    internal UnityEngine.CanvasGroup cv;
    internal UnityEngine.GameObject holder;
    internal TMPro.TMP_Text subtitleText;
}
namespace UnityEngine { internal class CanvasGroup : Component { internal float alpha=1; } }
namespace LeAccess.Accessibility.Dialogue
{ internal static class DialogueAnnouncer { internal static bool HasActiveContent; } }
namespace LeAccess.Accessibility.ScreenContent
{ internal static class ScreenContentAnnouncer { internal static bool HasActiveContent; } }
