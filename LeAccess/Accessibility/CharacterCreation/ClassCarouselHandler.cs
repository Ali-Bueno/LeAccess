using UnityEngine;

namespace LeAccess.Accessibility.CharacterCreation;

/// <summary>
/// Handles class selection carousel announcements in character creation.
/// Uses ListNavigable polling since the carousel doesn't trigger ControllerFocusCursor.
/// </summary>
public static class ClassCarouselHandler
{
    private static string _lastDetectedCenterClass;
    private static string _lastAnnouncedFullText;
    private static float _lastClassCheckTime;

    /// <summary>
    /// Called from MenuAccessibility.Update to check for class changes in the carousel.
    /// Uses ListNavigable "Select A Class Navigable" with _lastSelectedIndex to track selection.
    /// </summary>
    public static void CheckPendingClassAnnouncement()
    {
        // Periodically check for class changes when in class selection
        if (Time.time - _lastClassCheckTime < 0.25f) return;
        _lastClassCheckTime = Time.time;

        try
        {
            // Check if we're in class selection mode
            var charSelect = CharacterSelect.instance;
            if (charSelect == null) return;
            if (charSelect.currentState != CharacterSelect.CharacterSelectState.ClassSelection)
            {
                // Reset when leaving class selection
                if (_lastDetectedCenterClass != null)
                {
                    _lastDetectedCenterClass = null;
                }
                return;
            }

            // Find the "Select A Class Navigable" ListNavigable
            var allListNavs = Object.FindObjectsOfType<ListNavigable>();
            string classToAnnounce = null;

            foreach (var nav in allListNavs)
            {
                if (nav == null) continue;
                var children = nav._selectableChildren;
                if (children == null || children.Count == 0) continue;

                // Check if this ListNavigable has CharacterCreationSelection children
                bool hasClassSelections = false;
                for (int i = 0; i < children.Count && i < 3; i++)
                {
                    var child = children[i];
                    if (child?.TryCast<CharacterCreationSelection>() != null)
                    {
                        hasClassSelections = true;
                        break;
                    }
                }

                if (hasClassSelections)
                {
                    var listNavIdx = nav._lastSelectedIndex;
                    if (listNavIdx >= 0 && listNavIdx < children.Count)
                    {
                        var selectedChild = children[listNavIdx];
                        var charCreation = selectedChild?.TryCast<CharacterCreationSelection>();
                        if (charCreation != null)
                        {
                            // Try to find localized text in the UI hierarchy
                            var textComponents = charCreation.GetComponentsInChildren<TMPro.TMP_Text>(true);
                            if (textComponents != null)
                            {
                                foreach (var txt in textComponents)
                                {
                                    if (txt == null) continue;
                                    var text = txt.text;
                                    if (!string.IsNullOrEmpty(text) && text.Length > 1 && text.Length < 30)
                                    {
                                        classToAnnounce = text;
                                        break;
                                    }
                                }
                            }

                            // Fallback to className if no localized text found
                            if (string.IsNullOrEmpty(classToAnnounce) && charCreation.characterClass != null)
                            {
                                classToAnnounce = charCreation.characterClass.className;
                            }
                        }
                    }
                    break;
                }
            }

            if (!string.IsNullOrEmpty(classToAnnounce) && classToAnnounce != _lastDetectedCenterClass)
            {
                _lastDetectedCenterClass = classToAnnounce;

                // Try to find class description
                var description = FindClassDescription();
                string fullText = classToAnnounce;
                if (!string.IsNullOrEmpty(description))
                {
                    fullText = $"{classToAnnounce}. {description}";
                }

                // Only announce if the full text changed (avoids re-announcing same class)
                if (fullText != _lastAnnouncedFullText)
                {
                    _lastAnnouncedFullText = fullText;
                    ScreenReader.Speak(fullText);
                }
            }
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Error checking class selection: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds the class description from the UI.
    /// The description is usually displayed in a panel when a class is selected.
    /// </summary>
    private static string FindClassDescription()
    {
        try
        {
            var charSelect = CharacterSelect.instance;
            if (charSelect == null) return null;

            // Look for text elements that contain the class description
            // The description is typically a longer text with sentences
            var allTexts = charSelect.GetComponentsInChildren<TMPro.TMP_Text>(true);
            if (allTexts == null) return null;

            string bestDescription = null;

            foreach (var txt in allTexts)
            {
                if (txt == null) continue;
                if (!txt.gameObject.activeInHierarchy) continue;

                var text = txt.text;
                if (string.IsNullOrWhiteSpace(text)) continue;

                // Skip short text (class names, buttons, etc.)
                if (text.Length < 40) continue;

                // Description should have sentence structure
                if (!text.Contains(".") && !text.Contains(",")) continue;

                // Skip text that looks like a class name (single word, no punctuation in middle)
                var words = text.Split(' ');
                if (words.Length < 5) continue;

                // Check parent hierarchy to avoid button text
                var parentName = txt.transform.parent?.name?.ToLowerInvariant() ?? "";
                var grandparentName = txt.transform.parent?.parent?.name?.ToLowerInvariant() ?? "";

                if (parentName.Contains("button") || grandparentName.Contains("button"))
                    continue;

                // Prefer longer descriptions
                if (bestDescription == null || text.Length > bestDescription.Length)
                {
                    bestDescription = text.Trim();
                }
            }

            if (!string.IsNullOrEmpty(bestDescription))
            {
                Plugin.Log.LogInfo($"[ClassCarousel] Found description: {bestDescription.Substring(0, System.Math.Min(50, bestDescription.Length))}...");
            }

            return bestDescription;
        }
        catch (System.Exception ex)
        {
            Plugin.Log.LogWarning($"Error finding class description: {ex.Message}");
            return null;
        }
    }
}
