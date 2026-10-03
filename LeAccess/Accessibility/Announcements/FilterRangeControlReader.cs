using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;
using LeAccess.I18n;
using TMPro;
using UnityEngine;
using static LeAccess.Accessibility.Navigation.NavigationObjectLifetime;

namespace LeAccess.Accessibility.Announcements;

internal static class FilterRangeControlReader
{
    internal static bool TryRead(GameObject element, out string text, out bool unavailable)
    {
        text = null;
        unavailable = false;
        try
        {
            var input = element.GetComponentInParent<TMP_InputField>();
            if (!IsAlive(input) || !input.isActiveAndEnabled || !UiVisibility.IsVisible(input.gameObject)) return false;
            var number = element.GetComponentInParent<NumberInput>();
            var range = element.GetComponentInParent<InputRange>();
            if (!IsAlive(number) || !number.isActiveAndEnabled || !IsAlive(number._input) || number._input.Pointer != input.Pointer ||
                !IsAlive(range) || !range.isActiveAndEnabled || !Within(input.gameObject, range.gameObject) ||
                !Matches(range._minInput, number) && !Matches(range._maxInput, number)) return false;
            var potential = range.GetComponentInParent<PotentialConditionUI>();
            var affixCount = range.GetComponentInParent<AffixCountConditionUI>();
            GameObject owner = null;
            if (IsAlive(potential) && potential.isActiveAndEnabled &&
                (Matches(potential._legendaryPotentialRange, range) || Matches(potential._forgingPotentialRange, range) ||
                 Matches(potential._weaversWillRange, range) || Matches(potential._weaversTouchRange, range)))
                owner = potential.gameObject;
            else if (IsAlive(affixCount) && affixCount.isActiveAndEnabled &&
                     (Matches(affixCount._prefixesRange, range) || Matches(affixCount._suffixesRange, range)))
                owner = affixCount.gameObject;
            if (owner == null) return false;
            var parent = range.transform.parent;
            if (!IsAlive(parent) || !Within(parent.gameObject, owner)) return false;
            string heading = null;
            for (int index = 0; index < range.transform.GetSiblingIndex(); index++)
            {
                var label = parent.GetChild(index).GetComponent<TMP_Text>();
                if (!TextUtils.IsVisibleLabel(label)) continue;
                string candidate = TextUtils.CleanText(label.text);
                if (string.IsNullOrWhiteSpace(candidate)) continue;
                if (heading != null) return false;
                heading = candidate;
            }
            var placeholder = input.placeholder?.TryCast<TMP_Text>();
            if (heading == null || !IsAlive(placeholder) || !Within(placeholder.gameObject, input.gameObject)) return false;
            string role = TextUtils.CleanText(placeholder.text);
            if (string.IsNullOrWhiteSpace(role)) return false;
            string value = TextUtils.CleanText(input.text);
            string bound = string.IsNullOrWhiteSpace(value) ? ModText.Format("control.empty_field", role) :
                ModText.Format("control.field_value", role, value);
            text = ScreenContentTracker.Combine(new[] { heading, bound });
            unavailable = !input.IsInteractable();
            return true;
        }
        catch (System.Exception) { text = null; unavailable = false; return false; }
    }

    private static bool Matches(UnityEngine.Object first, UnityEngine.Object second) =>
        IsAlive(first) && IsAlive(second) && first.Pointer == second.Pointer;

    private static bool Within(GameObject element, GameObject owner) =>
        IsAlive(element) && IsAlive(owner) && UiVisibility.IsVisible(owner) && element.transform.IsChildOf(owner.transform);
}
