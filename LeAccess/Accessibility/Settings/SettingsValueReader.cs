using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.Utils;
using UnityEngine.UI;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Settings;

internal static class SettingsValueReader
{
    internal static string ReadSlider(Slider slider)
    {
        var owner = slider.GetComponentInParent<SliderInput>();
        if (!IsAlive(owner) || !IsAlive(owner._slider) || owner._slider.Pointer != slider.Pointer) return null;
        var number = owner._numberInput;
        if (!IsAlive(number) || !number.transform.IsChildOf(owner.transform)) return null;
        var input = number._input;
        if (!IsAlive(input) || !input.isActiveAndEnabled || !input.transform.IsChildOf(number.transform) ||
            !UiVisibility.IsVisible(input.gameObject)) return null;
        string value = TextUtils.CleanText(input.text);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
