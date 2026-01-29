using HarmonyLib;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LeAccess.Accessibility;

/// <summary>
/// Harmony patches for UI accessibility.
/// </summary>
public static class UIPatches
{
    /// <summary>
    /// Patch for UIPanel.Open - announces when panels open.
    /// </summary>
    [HarmonyPatch(typeof(UIPanel), nameof(UIPanel.Open))]
    public static class UIPanelOpenPatch
    {
        public static void Postfix(UIPanel __instance)
        {
            try
            {
                if (__instance?.instance == null) return;

                var panelName = __instance.instance.name;
                var cleanName = CleanPanelName(panelName);

                if (!string.IsNullOrEmpty(cleanName))
                {
                    ScreenReader.Speak($"{cleanName} opened");
                }

                DisableVirtualMouse();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogDebug($"UIPanelOpenPatch error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Patch for UIPanel.Close - announces when panels close.
    /// </summary>
    [HarmonyPatch(typeof(UIPanel), nameof(UIPanel.Close))]
    public static class UIPanelClosePatch
    {
        public static void Prefix(UIPanel __instance)
        {
            try
            {
                if (__instance?.isOpen != true) return;

                var panelName = __instance?.instance?.name;
                if (!string.IsNullOrEmpty(panelName))
                {
                    var cleanName = CleanPanelName(panelName);
                    if (!string.IsNullOrEmpty(cleanName))
                    {
                        ScreenReader.Speak($"{cleanName} closed");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogDebug($"UIPanelClosePatch error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Patch for ControllerFocusCursor.Show - announces when focus highlight moves to a new element.
    /// This is the visual highlight that shows which element is selected.
    /// </summary>
    [HarmonyPatch(typeof(ControllerFocusCursor), nameof(ControllerFocusCursor.Show), typeof(RectTransform), typeof(int))]
    public static class ControllerFocusCursorShowPatch
    {
        public static void Postfix(RectTransform rectTransform)
        {
            try
            {
                if (rectTransform == null) return;

                var go = rectTransform.gameObject;
                if (go == null) return;

                AnnounceGameObject(go);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogDebug($"ControllerFocusCursorShowPatch error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Another overload of Show with more parameters.
    /// </summary>
    [HarmonyPatch(typeof(ControllerFocusCursor), nameof(ControllerFocusCursor.Show),
        typeof(RectTransform), typeof(ControllerCursorPreset), typeof(bool), typeof(bool), typeof(float), typeof(int))]
    public static class ControllerFocusCursorShowPatch2
    {
        public static void Postfix(RectTransform element)
        {
            try
            {
                if (element == null) return;

                var go = element.gameObject;
                if (go == null) return;

                AnnounceGameObject(go);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogDebug($"ControllerFocusCursorShowPatch2 error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Patch to prevent virtual mouse from activating.
    /// </summary>
    [HarmonyPatch(typeof(EpochInputManager), nameof(EpochInputManager.SetActiveVirtualMouse))]
    public static class DisableVirtualMousePatch
    {
        public static bool Prefix(ref bool active)
        {
            active = false;
            return true;
        }
    }

    /// <summary>
    /// Patch to always report virtual mouse as disabled.
    /// </summary>
    [HarmonyPatch(typeof(EpochInputManager), nameof(EpochInputManager.VirtualMouseIsEnabled))]
    public static class VirtualMouseIsEnabledPatch
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    private static int _lastAnnouncedInstanceId;

    /// <summary>
    /// Announces a GameObject, avoiding duplicates.
    /// </summary>
    private static void AnnounceGameObject(GameObject go)
    {
        if (go == null) return;

        var instanceId = go.GetInstanceID();
        if (instanceId == _lastAnnouncedInstanceId) return;
        _lastAnnouncedInstanceId = instanceId;

        var text = GetElementText(go);
        if (!string.IsNullOrWhiteSpace(text))
        {
            ScreenReader.Speak(text);
        }
    }

    /// <summary>
    /// Gets readable text from a GameObject.
    /// </summary>
    private static string GetElementText(GameObject go)
    {
        // Try TextMeshPro first
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
        {
            return CleanText(tmp.text);
        }

        // Try legacy Text
        var legacyText = go.GetComponentInChildren<Text>();
        if (legacyText != null && !string.IsNullOrWhiteSpace(legacyText.text))
        {
            return CleanText(legacyText.text);
        }

        // Try getting text from parent (sometimes the highlight is on a child element)
        var parent = go.transform.parent;
        if (parent != null)
        {
            var parentTmp = parent.GetComponentInChildren<TextMeshProUGUI>();
            if (parentTmp != null && !string.IsNullOrWhiteSpace(parentTmp.text))
            {
                return CleanText(parentTmp.text);
            }
        }

        // Fallback to object name
        return CleanObjectName(go.name);
    }

    private static string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");
        text = text.Trim();
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

        return text;
    }

    private static string CleanObjectName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\(\d+\)$", "");
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\(Clone\)$", "");
        name = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        name = name.Replace("_", " ");

        return name.Trim();
    }

    private static void DisableVirtualMouse()
    {
        try
        {
            EpochInputManager.SetActiveVirtualMouse(false);
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogDebug($"DisableVirtualMouse error: {ex.Message}");
        }
    }

    private static string CleanPanelName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        name = name.Replace("Panel", "")
                   .Replace("(Clone)", "")
                   .Replace("UI", "")
                   .Trim();

        name = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");

        return name.Trim();
    }
}
