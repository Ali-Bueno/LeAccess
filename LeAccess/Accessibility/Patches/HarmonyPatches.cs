using ChatView;
using HarmonyLib;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace LeAccess.Accessibility.Patches;

/// <summary>
/// Harmony patches for UI accessibility.
/// </summary>
public static class HarmonyPatches
{
    [HarmonyPatch(typeof(UIPanel), nameof(UIPanel.Open))]
    public static class UIPanelOpenPatch
    {
        public static void Postfix(UIPanel __instance)
        {
            try
            {
                if (__instance?.instance == null) return;
                var cleanName = TextUtils.CleanPanelName(__instance.instance.name);
                if (!string.IsNullOrEmpty(cleanName))
                    ScreenReader.Speak($"{cleanName} opened");
                TextUtils.DisableVirtualMouse();
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(UIPanel), nameof(UIPanel.Close))]
    public static class UIPanelClosePatch
    {
        public static void Prefix(UIPanel __instance)
        {
            try
            {
                if (__instance?.isOpen != true) return;
                var cleanName = TextUtils.CleanPanelName(__instance?.instance?.name);
                if (!string.IsNullOrEmpty(cleanName))
                    ScreenReader.Speak($"{cleanName} closed");
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(ControllerFocusCursor), nameof(ControllerFocusCursor.Show), typeof(RectTransform), typeof(int))]
    public static class ControllerFocusCursorShowPatch
    {
        public static void Postfix(RectTransform rectTransform)
        {
            if (rectTransform != null)
                ElementAnnouncer.AnnounceElement(rectTransform.gameObject);
        }
    }

    [HarmonyPatch(typeof(ControllerFocusCursor), nameof(ControllerFocusCursor.Show),
        typeof(RectTransform), typeof(ControllerCursorPreset), typeof(bool), typeof(bool), typeof(float), typeof(int))]
    public static class ControllerFocusCursorShowPatch2
    {
        public static void Postfix(RectTransform element)
        {
            if (element != null)
                ElementAnnouncer.AnnounceElement(element.gameObject);
        }
    }

    [HarmonyPatch(typeof(EpochInputManager), nameof(EpochInputManager.SetActiveVirtualMouse))]
    public static class DisableVirtualMousePatch
    {
        public static bool Prefix(ref bool active) { active = false; return true; }
    }

    [HarmonyPatch(typeof(EpochInputManager), nameof(EpochInputManager.VirtualMouseIsEnabled))]
    public static class VirtualMouseIsEnabledPatch
    {
        public static bool Prefix(ref bool __result) { __result = false; return false; }
    }

    /// <summary>
    /// Patch Toggle.isOn setter to detect state changes and re-announce.
    /// </summary>
    [HarmonyPatch(typeof(Toggle), nameof(Toggle.isOn), MethodType.Setter)]
    public static class ToggleIsOnSetterPatch
    {
        public static void Postfix(Toggle __instance, bool value)
        {
            ElementAnnouncer.HandleToggleValueChange(__instance, value, "setter");
        }
    }

    /// <summary>
    /// Patch Toggle.SetIsOnWithoutNotify to detect state changes (some UI uses this instead of setter).
    /// </summary>
    [HarmonyPatch(typeof(Toggle), nameof(Toggle.SetIsOnWithoutNotify))]
    public static class ToggleSetIsOnWithoutNotifyPatch
    {
        public static void Postfix(Toggle __instance, bool value)
        {
            ElementAnnouncer.HandleToggleValueChange(__instance, value, "SetIsOnWithoutNotify");
        }
    }

    /// <summary>
    /// Patch ChatToggleField.IsActive setter for chat options.
    /// </summary>
    [HarmonyPatch(typeof(ChatToggleField), nameof(ChatToggleField.IsActive), MethodType.Setter)]
    public static class ChatToggleFieldIsActivePatch
    {
        public static void Postfix(ChatToggleField __instance, bool value)
        {
            try
            {
                if (__instance == null || __instance.gameObject == null) return;

                var instanceId = __instance.GetInstanceID();

                if (ElementAnnouncer.TogglePreviousStates.TryGetValue(instanceId, out var previousValue))
                {
                    if (previousValue == value) return;
                }

                ElementAnnouncer.TogglePreviousStates[instanceId] = value;

                bool isCurrentlyFocused = ElementAnnouncer.IsRelatedToFocusedElement(__instance.gameObject);
                if (!isCurrentlyFocused) return;

                var stateText = value ? "On" : "Off";
                ScreenReader.Speak(stateText, true);
            }
            catch { }
        }
    }

    /// <summary>
    /// Patch PrioritizedNavigable.HandleSubmitInput to detect A/X button presses on custom toggles.
    /// </summary>
    [HarmonyPatch(typeof(PrioritizedNavigable), nameof(PrioritizedNavigable.HandleSubmitInput))]
    public static class PrioritizedNavigableHandleSubmitInputPatch
    {
        public static void Postfix(PrioritizedNavigable __instance)
        {
            try
            {
                if (__instance == null) return;

                var activeSelectable = __instance.ActiveSelectable;
                if (activeSelectable == null) return;

                var go = activeSelectable.gameObject;
                if (go == null) return;

                // Check for custom toggle components
                var moveToAttack = go.GetComponent<ActiveWhileMoveToAttackEnabled>()
                    ?? go.GetComponentInParent<ActiveWhileMoveToAttackEnabled>();
                if (moveToAttack != null)
                {
                    var stateText = moveToAttack.moveAttackEnabled ? "On" : "Off";
                    ScreenReader.Speak(stateText, true);
                    return;
                }

                var friendNotification = go.GetComponent<ActiveWhileFriendAddNotificationEnabled>()
                    ?? go.GetComponentInParent<ActiveWhileFriendAddNotificationEnabled>();
                if (friendNotification != null)
                {
                    var stateText = friendNotification.hideUnknownSocialRequests ? "On" : "Off";
                    ScreenReader.Speak(stateText, true);
                    return;
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"HandleSubmitInput error: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Patch VerticalNavigable.HandleSubmitInput - settings panels often use this.
    /// </summary>
    [HarmonyPatch(typeof(VerticalNavigable), nameof(VerticalNavigable.HandleSubmitInput))]
    public static class VerticalNavigableHandleSubmitInputPatch
    {
        public static void Postfix(VerticalNavigable __instance)
        {
            try
            {
                if (__instance == null) return;
                var selectables = __instance.selectables;
                var idx = __instance.currentlySelectedIdx;
                if (selectables != null && idx >= 0 && idx < selectables.Length)
                {
                    var selected = selectables[idx];
                    if (selected != null)
                        ElementAnnouncer.HandleSubmitForGameObject(selected.gameObject);
                }
            }
            catch { }
        }
    }
}
