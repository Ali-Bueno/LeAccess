using UnityEngine;
using UnityEngine.UI;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>
/// Reads mastery information from MasteryButton and related components.
/// </summary>
public static class MasteryReader
{
    /// <summary>
    /// Tries to get mastery name and description from a GameObject.
    /// Returns true if mastery info was found.
    /// </summary>
    public static bool TryGetMasteryInfo(GameObject go, out string name, out string description)
    {
        name = null;
        description = null;

        if (go == null) return false;

        // Check if we're in a MasteryPanel context
        var masteryPanel = go.GetComponentInParent<MasteryPanel>();

        // If not in parent, check if there's an active MasteryPanel and we might be its child
        if (masteryPanel == null)
        {
            var panels = Object.FindObjectsOfType<MasteryPanel>();
            foreach (var panel in panels)
            {
                if (panel != null && panel.gameObject.activeInHierarchy)
                {
                    // Check if the current go is within this panel's hierarchy
                    if (go.transform.IsChildOf(panel.transform))
                    {
                        masteryPanel = panel;
                        break;
                    }
                }
            }
        }

        if (masteryPanel != null && masteryPanel.gameObject.activeInHierarchy)
        {
            Plugin.Log.LogInfo($"[MasteryReader] Found MasteryPanel, checking buttons...");

            // Find the Button or Selectable component we're on
            var button = go.GetComponent<Button>() ?? go.GetComponentInParent<Button>();
            var selectable = go.GetComponent<Selectable>() ?? go.GetComponentInParent<Selectable>();

            // Get the arrays from MasteryPanel
            var buttons = masteryPanel.masteryButtons;
            var tmps = masteryPanel.masteryButtonTMPs;

            if (buttons != null)
            {
                Plugin.Log.LogInfo($"[MasteryReader] Panel has {buttons.Length} buttons");

                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] == null) continue;

                    // Check if this is the button we're on
                    bool isMatch = false;

                    if (button != null && buttons[i].Pointer == button.Pointer)
                    {
                        isMatch = true;
                    }
                    else if (selectable != null && buttons[i].Pointer == selectable.Pointer)
                    {
                        isMatch = true;
                    }
                    else
                    {
                        // Also check if go is a child of the button
                        if (go.transform.IsChildOf(buttons[i].transform))
                        {
                            isMatch = true;
                        }
                    }

                    if (isMatch)
                    {
                        Plugin.Log.LogInfo($"[MasteryReader] Matched button at index {i}");

                        // Get name from TMP at same index
                        if (tmps != null && i < tmps.Length && tmps[i] != null)
                        {
                            var tmpText = tmps[i].text;
                            if (!string.IsNullOrWhiteSpace(tmpText))
                            {
                                name = tmpText.Trim();
                                Plugin.Log.LogInfo($"[MasteryReader] Got name from TMP: {name}");
                            }
                        }

                        // Get MasteryButton component for description
                        var masteryButton = buttons[i].GetComponent<MasteryButton>();
                        if (masteryButton != null)
                        {
                            if (!string.IsNullOrEmpty(masteryButton.classDescription))
                            {
                                description = masteryButton.classDescription;
                                Plugin.Log.LogInfo($"[MasteryReader] Got description from MasteryButton");
                            }

                            // If no name from TMP, try MasteryButtonTooltip
                            if (string.IsNullOrEmpty(name))
                            {
                                var tooltip = masteryButton.GetComponent<MasteryButtonTooltip>()
                                    ?? masteryButton.GetComponentInChildren<MasteryButtonTooltip>();
                                if (tooltip != null && !string.IsNullOrEmpty(tooltip.masteryName))
                                {
                                    name = tooltip.masteryName;
                                    Plugin.Log.LogInfo($"[MasteryReader] Got name from tooltip: {name}");
                                }
                            }

                            // Try to get name from CharacterClass.masteries array
                            if (string.IsNullOrEmpty(name))
                            {
                                var charClass = masteryPanel.characterClass;
                                if (charClass != null)
                                {
                                    // Index 0 is usually base class, others are masteries
                                    // The button index may correspond to mastery index
                                    if (masteryButton.isMasteryClass)
                                    {
                                        // This is a mastery, not base class
                                        var masteries = charClass.masteries;
                                        if (masteries != null)
                                        {
                                            // Mastery buttons are typically indices 1-3 (after base class at 0)
                                            int masteryIdx = i - 1;
                                            if (masteryIdx >= 0 && masteryIdx < masteries.Length)
                                            {
                                                var mastery = masteries[masteryIdx];
                                                if (mastery != null)
                                                {
                                                    // Try localized name first, then fallback to internal name
                                                    try
                                                    {
                                                        var localizedName = mastery.LocalizedName;
                                                        if (!string.IsNullOrEmpty(localizedName))
                                                        {
                                                            name = localizedName;
                                                            Plugin.Log.LogInfo($"[MasteryReader] Got localized name from masteries[{masteryIdx}]: {name}");
                                                        }
                                                    }
                                                    catch
                                                    {
                                                        // LocalizedName property might fail
                                                    }

                                                    if (string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(mastery.name))
                                                    {
                                                        name = mastery.name;
                                                        Plugin.Log.LogInfo($"[MasteryReader] Got internal name from masteries[{masteryIdx}]: {name}");
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        // This is the base class
                                        if (!string.IsNullOrEmpty(charClass.className))
                                        {
                                            name = charClass.className;
                                            Plugin.Log.LogInfo($"[MasteryReader] Got base class name: {name}");
                                        }
                                    }
                                }
                            }

                            // Final fallback: find any text in button
                            if (string.IsNullOrEmpty(name))
                            {
                                name = FindMasteryNameFromUI(masteryButton.gameObject);
                                if (!string.IsNullOrEmpty(name))
                                {
                                    Plugin.Log.LogInfo($"[MasteryReader] Got name from UI search: {name}");
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(name))
                        {
                            return true;
                        }
                    }
                }
            }
        }

        // Direct MasteryButton check (for cases outside MasteryPanel context)
        var directMasteryButton = go.GetComponent<MasteryButton>()
            ?? go.GetComponentInParent<MasteryButton>()
            ?? go.GetComponentInChildren<MasteryButton>();

        if (directMasteryButton != null)
        {
            Plugin.Log.LogInfo($"[MasteryReader] Found direct MasteryButton");

            // Try MasteryButtonTooltip first
            var tooltip = directMasteryButton.GetComponent<MasteryButtonTooltip>()
                ?? directMasteryButton.GetComponentInChildren<MasteryButtonTooltip>();

            if (tooltip != null && !string.IsNullOrEmpty(tooltip.masteryName))
            {
                name = tooltip.masteryName;
            }

            // Fallback: find name from UI text within the button
            if (string.IsNullOrEmpty(name))
            {
                name = FindMasteryNameFromUI(directMasteryButton.gameObject);
            }

            // Get description
            if (!string.IsNullOrEmpty(directMasteryButton.classDescription))
            {
                description = directMasteryButton.classDescription;
            }

            if (!string.IsNullOrEmpty(name))
            {
                Plugin.Log.LogInfo($"[MasteryReader] Direct MasteryButton result: name={name}, hasDesc={!string.IsNullOrEmpty(description)}");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Finds mastery name from UI text elements within a GameObject hierarchy.
    /// </summary>
    private static string FindMasteryNameFromUI(GameObject go)
    {
        var texts = go.GetComponentsInChildren<TMPro.TMP_Text>(true);
        if (texts == null) return null;

        foreach (var txt in texts)
        {
            if (txt == null) continue;
            var text = txt.text;

            if (string.IsNullOrWhiteSpace(text)) continue;
            if (text.Length < 3 || text.Length > 30) continue;

            // Skip level indicators like "0" or "25"
            if (int.TryParse(text.Trim(), out _)) continue;

            // Skip common non-name texts
            var upper = text.ToUpperInvariant().Trim();
            if (upper == "MASTERED" || upper == "LOCKED" || upper == "UNLOCKED")
                continue;

            return text.Trim();
        }

        return null;
    }

    /// <summary>
    /// Tries to get base class description from the UI.
    /// </summary>
    public static string TryGetClassDescriptionFromUI(GameObject charCreationGo)
    {
        if (charCreationGo == null) return null;

        var charSelect = CharacterSelect.instance;
        if (charSelect == null) return null;

        var allTexts = charSelect.GetComponentsInChildren<TMPro.TMP_Text>(true);
        if (allTexts == null) return null;

        string bestDescription = null;

        foreach (var txt in allTexts)
        {
            if (txt == null || !txt.gameObject.activeInHierarchy) continue;

            var text = txt.text;
            if (string.IsNullOrWhiteSpace(text)) continue;

            // Description criteria: long text with punctuation
            if (text.Length > 50 && (text.Contains(".") || text.Contains(",") || text.Contains("!")))
            {
                var parentName = txt.transform.parent?.name?.ToLowerInvariant() ?? "";
                if (parentName.Contains("button") || parentName.Contains("header") || parentName.Contains("title"))
                    continue;

                if (bestDescription == null || text.Length > bestDescription.Length)
                {
                    bestDescription = text.Trim();
                }
            }
        }

        return bestDescription;
    }
}
