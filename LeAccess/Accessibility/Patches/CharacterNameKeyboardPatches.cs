using HarmonyLib;
using HeathenEngineering.UX.uGUIExtras;
using LeAccess.Accessibility.CharacterCreation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LeAccess.Accessibility.Patches;

internal static class CharacterNameKeyboardPatches
{
    [HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.OnSubmit))]
    internal static class Submit
    {
        public static void Prefix(TMP_InputField __instance) => CharacterNameKeyboard.RequestEditing(__instance);
    }

    [HarmonyPatch(typeof(TMP_InputField), nameof(TMP_InputField.OnPointerClick))]
    internal static class Click
    {
        public static void Prefix(TMP_InputField __instance, PointerEventData eventData)
        {
            if (eventData is { WasCollected: false } && eventData.button == PointerEventData.InputButton.Left)
                CharacterNameKeyboard.RequestEditing(__instance);
        }

        public static void Postfix(TMP_InputField __instance, PointerEventData eventData)
        {
            if (eventData is { WasCollected: false } && eventData.button == PointerEventData.InputButton.Left)
                CharacterNameKeyboard.CompleteClick(__instance);
        }
    }

    [HarmonyPatch(typeof(ScreenKeyboardManager), nameof(ScreenKeyboardManager.SelectedGameObject), MethodType.Setter)]
    internal static class SelectEditor
    {
        public static bool Prefix(ScreenKeyboardManager __instance, GameObject value) =>
            CharacterNameKeyboard.AllowSelection(__instance, value);
    }

    [HarmonyPatch(typeof(ScreenKeyboardManager), nameof(ScreenKeyboardManager.ShouldBeEnabled), MethodType.Getter)]
    internal static class Enable
    {
        public static void Postfix(ScreenKeyboardManager __instance, ref bool __result) =>
            __result = CharacterNameKeyboard.FilterEnabled(__instance, __result);
    }

    [HarmonyPatch(typeof(ScreenKeyboardManager), nameof(ScreenKeyboardManager.Disable))]
    internal static class Close
    {
        public static void Prefix(ScreenKeyboardManager __instance, out TMP_InputField __state) =>
            __state = CharacterNameKeyboard.BeginClose(__instance);

        public static void Postfix(ScreenKeyboardManager __instance, TMP_InputField __state) =>
            CharacterNameKeyboard.CompleteClose(__instance, __state);
    }

    [HarmonyPatch(typeof(ScreenKeyboardManager), nameof(ScreenKeyboardManager.OnFunctionPress))]
    internal static class Function
    {
        public static void Prefix(ScreenKeyboardManager __instance, ref KeyboardFunction keyboardFunction) =>
            keyboardFunction = CharacterNameKeyboard.BackAction(__instance, keyboardFunction);
    }

    [HarmonyPatch(typeof(ScreenKeyboardManager), nameof(ScreenKeyboardManager.OnCancel))]
    internal static class Cancel
    {
        public static bool Prefix(ScreenKeyboardManager __instance) => CharacterNameKeyboard.AllowCancel(__instance);
    }

    [HarmonyPatch(typeof(EpochInputManager), nameof(EpochInputManager.IsControllerActive), MethodType.Setter)]
    internal static class DeviceMode
    {
        public static void Prefix(bool value, out CharacterNameKeyboard.PhysicalTransfer __state) =>
            __state = CharacterNameKeyboard.CapturePhysicalTransfer(value);

        public static void Postfix(CharacterNameKeyboard.PhysicalTransfer __state) =>
            CharacterNameKeyboard.CompletePhysicalTransfer(__state);
    }
}
