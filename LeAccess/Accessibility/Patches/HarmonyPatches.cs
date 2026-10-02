using LeAccess.I18n;
using ChatView;
using HarmonyLib;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Audio;
using LeAccess.Accessibility.Utils;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Il2CppSystem;

namespace LeAccess.Accessibility.Patches;

/// <summary>
/// Harmony patches for UI accessibility.
/// </summary>
public static class HarmonyPatches
{
    [HarmonyPatch(typeof(UIPanel), nameof(UIPanel.Open))]
    public static class UIPanelOpenPatch
    {
        public static void Prefix(UIPanel __instance, out bool __state)
        {
            __state = __instance?.isOpen == true;
        }

        public static void Postfix(UIPanel __instance, bool __state)
        {
            try
            {
                if (__state || __instance?.isOpen != true || __instance.instance == null ||
                    !UiVisibility.IsVisible(__instance.instance)) return;
                ElementAnnouncer.ResetFocusIfOwned(__instance.instance);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(UIPanel), nameof(UIPanel.Close))]
    public static class UIPanelClosePatch
    {
        public static void Prefix(UIPanel __instance, out bool __state)
        {
            __state = __instance?.isOpen == true;
        }

        public static void Postfix(UIPanel __instance, bool __state)
        {
            try
            {
                if (!__state || __instance == null || __instance.isOpen) return;
                ElementAnnouncer.ResetFocusIfOwned(__instance.instance);
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

                var stateText = ModText.Get(value ? "state.on" : "state.off");
                ScreenReader.Speak(stateText, true);
            }
            catch { }
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
                    {
                        Plugin.Log.LogInfo($"[Submit] VerticalNavigable submit on: {selected.gameObject.name}, navigable={__instance.name}");
                        ElementAnnouncer.HandleSubmitForGameObject(selected.gameObject);
                    }
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Patch ControllerNotification.ShowNotification to announce popups with title and message.
    /// Examples: "You are offline", "Connection lost", etc.
    /// </summary>
    [HarmonyPatch(typeof(ControllerNotification), nameof(ControllerNotification.ShowNotification))]
    public static class ControllerNotificationShowPatch
    {
        public static void Postfix(ControllerNotification __instance) => NotificationAnnouncer.TrackController(__instance);
    }

    /// <summary>
    /// Patch UIQuickNotificationText.Show to announce quick hints.
    /// Examples: "Press RT to submit", "Hold A to confirm", etc.
    /// </summary>
    [HarmonyPatch(typeof(UIQuickNotificationText), nameof(UIQuickNotificationText.Show))]
    public static class UIQuickNotificationTextShowPatch
    {
        public static void Postfix(UIQuickNotificationText __instance) => NotificationAnnouncer.TrackQuick(__instance);
    }

    /// <summary>
    /// Patch ScreenKeyboardManager.OnAppeared to announce when the on-screen keyboard appears.
    /// </summary>
    [HarmonyPatch(typeof(ScreenKeyboardManager), nameof(ScreenKeyboardManager.OnAppeared))]
    public static class ScreenKeyboardManagerOnAppearedPatch
    {
        public static void Postfix(ScreenKeyboardManager __instance)
        {
            try
            {
                // Get placeholder text if available
                string placeholder = __instance?._substitutePlaceholderText?.text;
                string announcement = ModText.Get("keyboard.open");

                if (!string.IsNullOrEmpty(placeholder))
                    announcement = ModText.Format("keyboard.open_named", placeholder);

                Plugin.Log.LogInfo($"[ScreenKeyboard] {announcement}");
                ScreenReader.Speak(announcement, true);
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"ScreenKeyboardManager error: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(MultiButtonsActionPrompt), nameof(MultiButtonsActionPrompt.UpdatePrompt), typeof(bool))]
    public static class MultiButtonsActionPromptUpdatePatch
    {
        public static void Postfix(MultiButtonsActionPrompt __instance) => ButtonPromptAnnouncer.Track(__instance);
    }

    [HarmonyPatch(typeof(MultiButtonsActionPrompt), nameof(MultiButtonsActionPrompt.OnEnable))]
    public static class MultiButtonsActionPromptEnablePatch
    {
        public static void Postfix(MultiButtonsActionPrompt __instance) => ButtonPromptAnnouncer.Track(__instance);
    }

    [HarmonyPatch(typeof(MultiButtonsActionPrompt), nameof(MultiButtonsActionPrompt.OnDestroy))]
    public static class MultiButtonsActionPromptDestroyPatch
    {
        public static void Prefix(MultiButtonsActionPrompt __instance) => ButtonPromptAnnouncer.Remove(__instance);
    }

}
