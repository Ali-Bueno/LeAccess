using GraphicsBackend.Options;
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
    /// Tries to get label from GraphicsOption components (ToggleOption, SliderOption).
    /// </summary>
    public static void TryGetFromGraphicsOption(GameObject go, ref string label)
    {
        try
        {
            var toggleOption = go.GetComponent<ToggleOption>() ?? go.GetComponentInParent<ToggleOption>();
            if (toggleOption != null && !string.IsNullOrWhiteSpace(toggleOption.displayName))
            {
                label = TextUtils.CleanText(toggleOption.displayName);
                return;
            }

            var sliderOption = go.GetComponent<SliderOption>() ?? go.GetComponentInParent<SliderOption>();
            if (sliderOption != null && !string.IsNullOrWhiteSpace(sliderOption.displayName))
            {
                label = TextUtils.CleanText(sliderOption.displayName);
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
        var parent = controlGo.transform.parent;

        for (int level = 0; level < 3 && parent != null; level++)
        {
            if (IsScrollContent(parent))
                break;

            FindTextsInSiblings(parent, controlGo, excludeValue, ref label, ref description, level);

            if (label != null)
                break;

            parent = parent.parent;
        }
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

        var allTexts = optionContainer.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (var tmp in allTexts)
        {
            if (tmp == null || string.IsNullOrWhiteSpace(tmp.text)) continue;

            var text = TextUtils.CleanText(tmp.text);
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

        var current = go.transform.parent;

        for (int level = 0; level < 5 && current != null; level++)
        {
            for (int i = 0; i < current.childCount; i++)
            {
                var child = current.GetChild(i);

                var tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                {
                    var text = TextUtils.CleanText(tmp.text);
                    if (!string.IsNullOrEmpty(text) && !seen.Contains(text))
                    {
                        var textType = TextClassifier.ClassifyText(text);
                        if (textType == TextType.Label || textType == TextType.Unknown)
                            return text;
                    }
                }

                for (int j = 0; j < child.childCount; j++)
                {
                    var grandchild = child.GetChild(j);
                    tmp = grandchild.GetComponent<TextMeshProUGUI>();
                    if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
                    {
                        var text = TextUtils.CleanText(tmp.text);
                        if (!string.IsNullOrEmpty(text) && !seen.Contains(text))
                        {
                            var textType = TextClassifier.ClassifyText(text);
                            if (textType == TextType.Label || textType == TextType.Unknown)
                                return text;
                        }
                    }
                }
            }

            if (IsScrollContent(current))
                break;

            current = current.parent;
        }

        return null;
    }

    /// <summary>
    /// Finds a container that has exactly one control (toggle/slider/dropdown).
    /// </summary>
    public static Transform FindSingleOptionContainer(GameObject controlGo)
    {
        var current = controlGo.transform.parent;

        for (int level = 0; level < 5 && current != null; level++)
        {
            int controlCount = 0;
            var toggles = current.GetComponentsInChildren<Toggle>(true);
            var sliders = current.GetComponentsInChildren<Slider>(true);
            var dropdowns = current.GetComponentsInChildren<TMP_Dropdown>(true);

            controlCount = toggles.Length + sliders.Length + dropdowns.Length;

            if (controlCount == 1)
                return current;

            if (controlCount == 0)
            {
                current = current.parent;
                continue;
            }

            if (level == 0)
                return current;

            return null;
        }

        return null;
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

        var allTexts = new List<(string text, TextType type, int siblingIndex)>();

        for (int i = 0; i < parent.childCount; i++)
        {
            var sibling = parent.GetChild(i);

            if (controlGo.transform.IsChildOf(sibling) && sibling.gameObject != controlGo.transform.parent?.gameObject)
                continue;

            CollectTextsFromTransform(sibling, controlGo, seen, allTexts, i, 0);
        }

        int controlSiblingIndex = controlGo.transform.parent == parent
            ? controlGo.transform.GetSiblingIndex()
            : controlGo.transform.parent?.GetSiblingIndex() ?? 0;

        foreach (var (text, textType, siblingIndex) in allTexts)
        {
            if (label == null && textType == TextType.Label && siblingIndex <= controlSiblingIndex)
            {
                label = text;
                break;
            }
        }

        if (label == null)
        {
            foreach (var (text, textType, siblingIndex) in allTexts)
            {
                if (textType == TextType.Label)
                {
                    label = text;
                    break;
                }
            }
        }

        foreach (var (text, textType, siblingIndex) in allTexts)
        {
            if (description == null && textType == TextType.Description && siblingIndex >= controlSiblingIndex)
            {
                description = text;
                break;
            }
        }

        if (description == null)
        {
            foreach (var (text, textType, siblingIndex) in allTexts)
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
        List<(string text, TextType type, int siblingIndex)> results, int siblingIndex, int depth)
    {
        if (depth > 2) return;

        if (t.gameObject == controlGo) return;

        var tmp = t.GetComponent<TextMeshProUGUI>();
        if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
        {
            var text = TextUtils.CleanText(tmp.text);
            if (!string.IsNullOrEmpty(text) && !seen.Contains(text))
            {
                seen.Add(text);
                var textType = TextClassifier.ClassifyText(text);
                results.Add((text, textType, siblingIndex));
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
