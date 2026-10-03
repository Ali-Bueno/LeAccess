using LeAccess.Accessibility.Utils;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LeAccess.Accessibility.Announcements;

/// <summary>
/// Detects and extracts text from UI elements using multiple strategies.
/// </summary>
public static class TextDetector
{
    /// <summary>
    /// Tries to get label and description from InfoForTooltip component.
    /// </summary>
    public static void TryGetFromTooltipInfo(GameObject go, ref string label, ref string description)
    {
        try
        {
            var tooltip = go.GetComponent<InfoForTooltip>() ?? go.GetComponentInParent<InfoForTooltip>();
            if (tooltip == null) return;

            tooltip.SetText();

            var info = tooltip.tooltipInfo;
            if (info != null)
            {
                var nameText = info.nameText;
                if (!string.IsNullOrWhiteSpace(nameText))
                    label = TextUtils.CleanText(nameText);

                var mainText = info.mainText;
                if (!string.IsNullOrWhiteSpace(mainText))
                    description = TextUtils.CleanText(mainText);
            }
        }
        catch { }
    }

    /// <summary>
    /// Tries to get text from dropdown item when inside an expanded dropdown list.
    /// </summary>
    public static string TryGetDropdownItemText(GameObject go)
    {
        var toggle = go.GetComponent<Toggle>() ?? go.GetComponentInParent<Toggle>();
        if (toggle == null) return null;

        var current = go.transform;
        bool isInDropdownList = false;
        for (int i = 0; i < 6 && current != null; i++)
        {
            var name = current.name.ToLowerInvariant();
            if (name.Contains("dropdown list") || name.Contains("dropdownlist"))
            {
                isInDropdownList = true;
                break;
            }
            if (name.Contains("content") && current.parent?.name.ToLowerInvariant().Contains("viewport") == true)
            {
                isInDropdownList = true;
                break;
            }
            current = current.parent;
        }

        if (!isInDropdownList) return null;

        var itemText = TextUtils.GetTextFromHierarchy(go);
        if (!string.IsNullOrEmpty(itemText))
            return itemText;

        return null;
    }

    /// <summary>
    /// Finds label in the immediate container by searching siblings.
    /// </summary>
    public static void FindLabelInImmediateContainer(GameObject controlGo, ref string label, ref string description, string excludeValue)
    {
        var container = FindSingleOptionContainer(controlGo);
        if (container != null)
            FindTextsInSiblings(container, controlGo, excludeValue, ref label, ref description, 0);
    }

    /// <summary>
    /// Aggressive search for label and description using single option container detection.
    /// </summary>
    public static void FindLabelAndDescriptionAggressive(GameObject go, string excludeValue, ref string label, ref string description)
    {
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(excludeValue))
            seen.Add(excludeValue);
        if (!string.IsNullOrEmpty(label))
            seen.Add(label);
        if (!string.IsNullOrEmpty(description))
            seen.Add(description);

        var optionContainer = FindSingleOptionContainer(go);
        if (optionContainer == null)
            return;

        foreach (var text in ReadOrderedLabels(optionContainer))
        {
            if (string.IsNullOrEmpty(text) || seen.Contains(text)) continue;

            seen.Add(text);
            var textType = TextClassifier.ClassifyText(text);

            if (label == null && textType == TextType.Label)
                label = text;
            else if (description == null && textType == TextType.Description)
                description = text;

            if (label != null && description != null)
                return;
        }
    }

    /// <summary>
    /// Last resort search for any text in parent hierarchy.
    /// </summary>
    public static string FindAnyTextInParents(GameObject go, string excludeValue)
    {
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(excludeValue))
            seen.Add(excludeValue);

        var container = FindSingleOptionContainer(go);
        if (container == null) return null;
        foreach (string text in ReadOrderedLabels(container))
        {
            if (string.IsNullOrWhiteSpace(text) || seen.Contains(text)) continue;
            var kind = TextClassifier.ClassifyText(text);
            if (kind == TextType.Label || kind == TextType.Unknown) return text;
        }
        return null;
    }

    private static List<string> ReadOrderedLabels(Transform container)
    {
        var labels = new List<UiReadingOrder.Fragment>();
        foreach (var label in container.GetComponentsInChildren<TextMeshProUGUI>(false))
            if (TextUtils.IsVisibleLabel(label) && !string.IsNullOrWhiteSpace(label.text))
                labels.Add(new(label, TextUtils.CleanText(label.text)));
        return UiReadingOrder.Read(labels);
    }

    /// <summary>
    /// Finds a container that has exactly one control (toggle/slider/dropdown).
    /// </summary>
    public static Transform FindSingleOptionContainer(GameObject controlGo)
    {
        GameObject setting = null;
        var boolean = controlGo.GetComponentInParent<LE.Settings.BoolSettingUI>();
        if (boolean != null) setting = boolean._settingContainer;
        var numeric = controlGo.GetComponentInParent<LE.Settings.NumericSettingUI>();
        if (setting == null && numeric != null) setting = numeric._settingContainer;
        var spinner = controlGo.GetComponentInParent<LE.Settings.SpinnerSettingUI>();
        if (setting == null && spinner != null) setting = spinner._settingContainer;
        var flag = controlGo.GetComponentInParent<LE.Settings.EnumToggleSettingUI>();
        if (setting == null && flag != null) setting = flag._settingContainer;
        var enumDropdown = controlGo.GetComponentInParent<LE.Settings.EnumSettingUI>();
        if (setting == null && enumDropdown != null) setting = enumDropdown._settingContainer;
        var stringDropdown = controlGo.GetComponentInParent<LE.Settings.StringDropdownSettingUI>();
        if (setting == null && stringDropdown != null) setting = stringDropdown._settingContainer;
        if (UiVisibility.IsVisible(setting) && controlGo.transform.IsChildOf(setting.transform)) return setting.transform;

        Transform result = null;
        for (var current = controlGo.transform; current != null; current = current.parent)
        {
            if (IsScrollContent(current)) break;
            var controls = current.GetComponentsInChildren<Selectable>(true);
            if (controls.Length > 1) break;
            if (controls.Length == 1) result = current;
        }
        return result;
    }

    /// <summary>
    /// Checks if a transform is a scroll content container.
    /// </summary>
    public static bool IsScrollContent(Transform t)
    {
        var name = t.name.ToLowerInvariant();

        if (name == "content" || name == "viewport" || name.Contains("scroll"))
            return true;

        if (t.parent != null && t.parent.name.ToLowerInvariant().Contains("viewport"))
            return true;

        if (t.childCount > 5)
        {
            int childrenWithControls = 0;
            for (int i = 0; i < Mathf.Min(t.childCount, 8); i++)
            {
                var child = t.GetChild(i);
                if (child.GetComponentInChildren<Toggle>() != null ||
                    child.GetComponentInChildren<Slider>() != null ||
                    child.GetComponentInChildren<TMP_Dropdown>() != null)
                    childrenWithControls++;
            }
            if (childrenWithControls >= 3)
                return true;
        }

        return false;
    }

    private static void FindTextsInSiblings(Transform parent, GameObject controlGo, string excludeValue,
        ref string label, ref string description, int level)
    {
        var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(excludeValue))
            seen.Add(excludeValue);

        var allTexts = new List<(string text, TextType type, int siblingIndex, Component label)>();

        for (int i = 0; i < parent.childCount; i++)
        {
            var sibling = parent.GetChild(i);

            if (controlGo.transform.IsChildOf(sibling) && sibling.gameObject != controlGo.transform.parent?.gameObject)
                continue;

            CollectTextsFromTransform(sibling, controlGo, seen, allTexts, i, 0);
        }
        allTexts = UiReadingOrder.Order(allTexts, entry => entry.label);

        int controlSiblingIndex = controlGo.transform.parent == parent
            ? controlGo.transform.GetSiblingIndex()
            : controlGo.transform.parent?.GetSiblingIndex() ?? 0;

        foreach (var (text, textType, siblingIndex, _) in allTexts)
        {
            if (label == null && textType == TextType.Label && siblingIndex <= controlSiblingIndex)
            {
                label = text;
                break;
            }
        }

        if (label == null)
        {
            foreach (var (text, textType, siblingIndex, _) in allTexts)
            {
                if (textType == TextType.Label)
                {
                    label = text;
                    break;
                }
            }
        }

        foreach (var (text, textType, siblingIndex, _) in allTexts)
        {
            if (description == null && textType == TextType.Description && siblingIndex >= controlSiblingIndex)
            {
                description = text;
                break;
            }
        }

        if (description == null)
        {
            foreach (var (text, textType, siblingIndex, _) in allTexts)
            {
                if (textType == TextType.Description)
                {
                    description = text;
                    break;
                }
            }
        }
    }

    private static void CollectTextsFromTransform(Transform t, GameObject controlGo, HashSet<string> seen,
        List<(string text, TextType type, int siblingIndex, Component label)> results, int siblingIndex, int depth)
    {
        if (depth > 2 || !UiVisibility.IsVisible(t.gameObject)) return;

        if (t.gameObject == controlGo) return;

        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (TextUtils.IsVisibleLabel(tmp) && !string.IsNullOrWhiteSpace(tmp.text))
        {
            var text = TextUtils.CleanText(tmp.text);
            if (!string.IsNullOrEmpty(text) && !seen.Contains(text))
            {
                seen.Add(text);
                var textType = TextClassifier.ClassifyText(text);
                results.Add((text, textType, siblingIndex, tmp));
            }
        }

        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            if (child.gameObject == controlGo || controlGo.transform.IsChildOf(child))
                continue;
            CollectTextsFromTransform(child, controlGo, seen, results, siblingIndex, depth + 1);
        }
    }
}
