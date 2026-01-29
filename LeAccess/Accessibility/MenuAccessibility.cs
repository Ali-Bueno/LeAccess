using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LeAccess.Accessibility;

/// <summary>
/// Main component for menu accessibility.
/// Tracks UI selection changes and speaks element text.
/// </summary>
public class MenuAccessibility : MonoBehaviour
{
    private INavigable _lastNavigable;
    private int _lastSelectableInstanceId;

    private float _announceDelay = 0.05f;
    private float _timeSinceLastAnnounce;
    private string _pendingAnnouncement;

    void Update()
    {
        // Handle delayed announcements
        if (_pendingAnnouncement != null)
        {
            _timeSinceLastAnnounce += Time.deltaTime;
            if (_timeSinceLastAnnounce >= _announceDelay)
            {
                ScreenReader.Speak(_pendingAnnouncement);
                _pendingAnnouncement = null;
            }
        }

        // Check for selection changes
        CheckSelectionChange();
    }

    private void CheckSelectionChange()
    {
        try
        {
            var currentNavigable = NavigationManager.currentlySelected;
            if (currentNavigable == null) return;

            // Check if the navigable itself changed
            bool navigableChanged = false;
            if (_lastNavigable == null || currentNavigable.Pointer != _lastNavigable.Pointer)
            {
                navigableChanged = true;
                _lastNavigable = currentNavigable;
            }

            // Try to get the active selectable from PrioritizedNavigable
            Selectable activeSelectable = null;
            var prioritizedNav = currentNavigable.TryCast<PrioritizedNavigable>();
            if (prioritizedNav != null)
            {
                activeSelectable = prioritizedNav.ActiveSelectable;
            }

            // If not a PrioritizedNavigable, try VerticalNavigable
            if (activeSelectable == null)
            {
                var verticalNav = currentNavigable.TryCast<VerticalNavigable>();
                if (verticalNav != null && verticalNav.selectables != null)
                {
                    var idx = verticalNav.currentlySelectedIdx;
                    if (idx >= 0 && idx < verticalNav.selectables.Length)
                    {
                        activeSelectable = verticalNav.selectables[idx];
                    }
                }
            }

            // Check if the active selectable changed
            if (activeSelectable != null)
            {
                var instanceId = activeSelectable.GetInstanceID();
                if (instanceId != _lastSelectableInstanceId)
                {
                    _lastSelectableInstanceId = instanceId;
                    AnnounceSelectable(activeSelectable);
                    return;
                }
            }

            // If navigable changed but no specific selectable, announce the navigable itself
            if (navigableChanged)
            {
                var mono = currentNavigable.TryCast<MonoBehaviour>();
                if (mono != null)
                {
                    AnnounceElement(mono.gameObject);
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.LogDebug($"CheckSelectionChange error: {ex.Message}");
        }
    }

    /// <summary>
    /// Announce a Selectable UI element.
    /// </summary>
    public void AnnounceSelectable(Selectable selectable)
    {
        if (selectable == null) return;

        var go = selectable.gameObject;
        if (go == null) return;

        var text = GetElementText(go);
        if (!string.IsNullOrWhiteSpace(text))
        {
            QueueAnnouncement(text);
        }
    }

    /// <summary>
    /// Announce a UI element through the screen reader.
    /// </summary>
    public void AnnounceElement(GameObject element)
    {
        if (element == null) return;

        var text = GetElementText(element);
        if (!string.IsNullOrWhiteSpace(text))
        {
            QueueAnnouncement(text);
        }
    }

    private void QueueAnnouncement(string text)
    {
        _pendingAnnouncement = text;
        _timeSinceLastAnnounce = 0f;
    }

    /// <summary>
    /// Extract readable text from a UI element.
    /// </summary>
    private string GetElementText(GameObject obj)
    {
        var parts = new List<string>();

        // Get text content - search in self and children
        var textContent = GetTextContent(obj);
        if (!string.IsNullOrWhiteSpace(textContent))
        {
            parts.Add(textContent);
        }

        // Try to get the element type and state
        var elementType = GetElementType(obj);
        var elementState = GetElementState(obj);

        // Add element type if we found text
        if (parts.Count > 0 && !string.IsNullOrEmpty(elementType))
        {
            parts.Add(elementType);
        }

        // Add state info
        if (!string.IsNullOrEmpty(elementState))
        {
            parts.Add(elementState);
        }

        // If no text found, try object name as fallback
        if (parts.Count == 0)
        {
            var name = CleanObjectName(obj.name);
            if (!string.IsNullOrWhiteSpace(name))
            {
                parts.Add(name);
                if (!string.IsNullOrEmpty(elementType))
                {
                    parts.Add(elementType);
                }
            }
        }

        return string.Join(", ", parts);
    }

    private string GetTextContent(GameObject obj)
    {
        // Try TextMeshPro first (most common in modern Unity games)
        var tmp = obj.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
        {
            return CleanText(tmp.text);
        }

        // Try legacy Text component
        var legacyText = obj.GetComponentInChildren<Text>();
        if (legacyText != null && !string.IsNullOrWhiteSpace(legacyText.text))
        {
            return CleanText(legacyText.text);
        }

        return null;
    }

    private string GetElementType(GameObject obj)
    {
        // Check for specific UI components
        if (obj.GetComponent<Button>() != null)
            return "button";
        if (obj.GetComponent<Toggle>() != null)
            return "checkbox";
        if (obj.GetComponent<Slider>() != null)
            return "slider";
        if (obj.GetComponent<InputField>() != null || obj.GetComponent<TMP_InputField>() != null)
            return "text field";
        if (obj.GetComponent<Dropdown>() != null || obj.GetComponent<TMP_Dropdown>() != null)
            return "dropdown";
        if (obj.GetComponent<ScrollRect>() != null)
            return "scroll area";

        return null;
    }

    private string GetElementState(GameObject obj)
    {
        // Check toggle state
        var toggle = obj.GetComponent<Toggle>();
        if (toggle != null)
        {
            return toggle.isOn ? "checked" : "not checked";
        }

        // Check slider value
        var slider = obj.GetComponent<Slider>();
        if (slider != null)
        {
            var percent = Mathf.RoundToInt((slider.value - slider.minValue) / (slider.maxValue - slider.minValue) * 100);
            return $"{percent} percent";
        }

        // Check button interactability
        var button = obj.GetComponent<Button>();
        if (button != null && !button.interactable)
        {
            return "disabled";
        }

        return null;
    }

    private string CleanText(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        // Remove rich text tags
        text = System.Text.RegularExpressions.Regex.Replace(text, "<[^>]+>", "");
        // Trim whitespace
        text = text.Trim();
        // Replace multiple spaces with single space
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

        return text;
    }

    private string CleanObjectName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        // Remove common Unity suffixes
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\(\d+\)$", "");
        name = System.Text.RegularExpressions.Regex.Replace(name, @"\s*\(Clone\)$", "");

        // Convert PascalCase/camelCase to spaces
        name = System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        name = System.Text.RegularExpressions.Regex.Replace(name, "([A-Z]+)([A-Z][a-z])", "$1 $2");

        // Remove underscores
        name = name.Replace("_", " ");

        return name.Trim();
    }

    /// <summary>
    /// Force announce a specific text.
    /// </summary>
    public static void Announce(string text, bool interrupt = true)
    {
        ScreenReader.Speak(text, interrupt);
    }
}
