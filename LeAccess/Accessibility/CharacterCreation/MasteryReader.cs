using UnityEngine;

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

        // Check for MasteryButton component
        var masteryButton = go.GetComponent<MasteryButton>()
            ?? go.GetComponentInParent<MasteryButton>()
            ?? go.GetComponentInChildren<MasteryButton>();

        if (masteryButton != null)
        {
            // Get name from MasteryButtonTooltip if available
            var tooltip = go.GetComponent<MasteryButtonTooltip>()
                ?? go.GetComponentInParent<MasteryButtonTooltip>()
                ?? go.GetComponentInChildren<MasteryButtonTooltip>();

            if (tooltip != null && !string.IsNullOrEmpty(tooltip.masteryName))
            {
                name = tooltip.masteryName;
            }

            // Fallback: try to find name from UI text
            if (string.IsNullOrEmpty(name))
            {
                name = FindMasteryNameFromUI(masteryButton.gameObject);
            }

            // Get description from MasteryButton
            if (!string.IsNullOrEmpty(masteryButton.classDescription))
            {
                description = masteryButton.classDescription;
            }

            // If we have at least a name, return success
            if (!string.IsNullOrEmpty(name))
            {
                Plugin.Log.LogInfo($"[Mastery] Found: name={name}, hasDesc={!string.IsNullOrEmpty(description)}, isMastery={masteryButton.isMasteryClass}");
                return true;
            }
        }

        // Also check if we're in a MasteryPanel context
        var masteryPanel = go.GetComponentInParent<MasteryPanel>();
        if (masteryPanel != null)
        {
            // Try to find mastery info from the focused button
            var button = go.GetComponent<UnityEngine.UI.Button>()
                ?? go.GetComponentInParent<UnityEngine.UI.Button>();

            if (button != null)
            {
                // Find which mastery button this corresponds to
                var buttons = masteryPanel.masteryButtons;
                var tmps = masteryPanel.masteryButtonTMPs;

                if (buttons != null && tmps != null)
                {
                    for (int i = 0; i < buttons.Length && i < tmps.Length; i++)
                    {
                        if (buttons[i] != null && buttons[i].gameObject == button.gameObject)
                        {
                            if (tmps[i] != null && !string.IsNullOrEmpty(tmps[i].text))
                            {
                                name = tmps[i].text;
                                Plugin.Log.LogInfo($"[Mastery] Found from panel TMPs: name={name}");
                                return true;
                            }
                        }
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Finds mastery name from UI text elements.
    /// </summary>
    private static string FindMasteryNameFromUI(GameObject go)
    {
        // Look for TMP_Text in the mastery button hierarchy
        var texts = go.GetComponentsInChildren<TMPro.TMP_Text>(true);
        if (texts != null)
        {
            foreach (var txt in texts)
            {
                if (txt == null) continue;
                var text = txt.text;

                // Skip empty, numbers, and very short text
                if (string.IsNullOrWhiteSpace(text)) continue;
                if (text.Length < 3 || text.Length > 30) continue;

                // Skip level indicators like "0" or "25"
                if (int.TryParse(text.Trim(), out _)) continue;

                // Skip "MASTERED" text
                if (text.ToUpperInvariant() == "MASTERED") continue;

                return text.Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Tries to get base class description from the UI.
    /// </summary>
    public static string TryGetClassDescriptionFromUI(GameObject charCreationGo)
    {
        if (charCreationGo == null) return null;

        // Look for description text in the character selection UI
        // The description is usually in a separate panel that shows when a class is selected

        // First, try to find the description panel in the CharacterSelect
        var charSelect = CharacterSelect.instance;
        if (charSelect == null) return null;

        // Try to find text elements that might contain the description
        // Usually the description is a longer text (30+ chars) with punctuation
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
                // Skip if it's a known non-description element
                var parentName = txt.transform.parent?.name?.ToLowerInvariant() ?? "";
                if (parentName.Contains("button") || parentName.Contains("header") || parentName.Contains("title"))
                    continue;

                // Prefer longer descriptions
                if (bestDescription == null || text.Length > bestDescription.Length)
                {
                    bestDescription = text.Trim();
                }
            }
        }

        return bestDescription;
    }
}
