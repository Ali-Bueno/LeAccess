using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Settings;
using LeAccess.Accessibility.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class VisibilityTests
{
    internal static int Run()
    {
        int checks = 0;
        void Check(bool condition, string reason)
        { checks++; if (!condition) throw new InvalidOperationException(reason); }
        GameObject RectChild(GameObject parent, Rect bounds)
        {
            var child = new GameObject(true) { Parent = parent };
            parent.Children.Add(child);
            ((RectTransform)child.transform).Bounds = bounds;
            return child;
        }
        var root = new GameObject();
        var canvas = root.Add(new Canvas());
        var textGo = RectChild(root, new(100, 100, 120, 30));
        var text = textGo.Add(new TextMeshProUGUI { text = "Visible" });
        var rect = (RectTransform)textGo.transform;
        Check(UiVisibility.IsVisible(root), "Non-rendering menu owners remain visible");
        Check(UiVisibility.IsVisibleGraphic(text), "Visible rendered labels are accepted");
        Check(UiScreenGeometry.TryGetScreenRect(rect, out var bounds) && bounds.xMin == 100 && bounds.yMax == 130,
            "Screen bounds are available to reading order without changing geometry");
        rect.Bounds = new(-20, 100, 30, 30);
        Check(UiVisibility.IsVisible(textGo), "Partially visible labels remain readable");
        rect.Bounds = new(-30, 100, 30, 30);
        Check(!UiVisibility.IsVisible(textGo), "Touching the screen boundary without visible area is hidden");
        rect.Bounds = new(1920, 100, 100, 30);
        Check(!UiVisibility.IsVisible(textGo), "Text beyond the right screen boundary is hidden");
        rect.Bounds = new(100, 1080, 100, 30);
        Check(!UiVisibility.IsVisible(textGo), "Text above the top screen boundary is hidden");
        rect.Bounds = new(100, -40, 100, 30);
        Check(!UiVisibility.IsVisible(textGo), "Text below the bottom screen boundary is hidden");
        rect.Bounds = new(100, 100, 0, 0);
        Check(UiVisibility.IsVisible(textGo) && !UiVisibility.IsVisibleGraphic(text),
            "Zero-sized layout anchors survive but cannot present rendered text");
        rect.Bounds = new(float.NaN, 100, 100, 30);
        Check(!UiVisibility.IsVisible(textGo), "Invalid coordinates cannot become visible labels");
        rect.Bounds = new(100, 100, 100, 30);
        rect.Offset = new(2000, 0, 0);
        Check(!UiVisibility.IsVisible(textGo), "Animated off-screen translations suppress stale controls");
        rect.Offset = default;
        canvas.Enabled = false;
        Check(!UiVisibility.IsVisible(textGo), "Disabled parent canvas suppresses content");
        canvas.Enabled = true;
        var group = root.Add(new CanvasGroup { alpha = float.NaN });
        Check(!UiVisibility.IsVisible(textGo), "Invalid canvas opacity is rejected");
        group.alpha = 0;
        Check(!UiVisibility.IsVisible(textGo), "Fully faded menu content is rejected");
        var localGroup = textGo.Add(new CanvasGroup { ignoreParentGroups = true });
        Check(UiVisibility.IsVisible(textGo), "An explicit local canvas-group boundary is respected");
        localGroup.Enabled = false; group.alpha = 1;
        text.canvasRenderer.Alpha = 0;
        Check(!TextUtils.IsVisibleLabel(text), "CanvasRenderer fades cannot leak labels");
        text.canvasRenderer.Alpha = float.NaN;
        Check(!TextUtils.IsVisibleLabel(text), "Invalid renderer alpha fails closed");
        text.canvasRenderer.Alpha = 1; text.canvasRenderer.cull = true;
        Check(!TextUtils.IsVisibleLabel(text), "Native culling suppresses labels");
        text.canvasRenderer.cull = false; text.alpha = 0;
        Check(!TextUtils.IsVisibleLabel(text), "Text color opacity suppresses labels");
        text.alpha = 1; text.Enabled = false;
        Check(!TextUtils.IsVisibleLabel(text), "Disabled graphic cannot be read");
        text.Enabled = true;
        var transparentParent = root.Add(new Graphic { color = new() { a = 0 } });
        Check(TextUtils.IsVisibleLabel(text), "A transparent background image does not hide its independent child text");

        var viewport = RectChild(root, new(0, 0, 200, 200));
        var clip = viewport.Add(new RectMask2D());
        var item = RectChild(viewport, new(100, 250, 50, 30));
        var itemLabel = item.Add(new TextMeshProUGUI { text = "Outside scroll view" });
        Check(!UiVisibility.IsVisible(item), "Scrolled-out controls are excluded before their renderer updates");
        clip.Enabled = false;
        Check(UiVisibility.IsVisible(item), "Disabled clipper does not suppress controls");
        clip.Enabled = true;
        itemLabel.maskable = false;
        Check(UiVisibility.IsVisibleGraphic(itemLabel), "Explicitly unmaskable graphics retain visibility");
        itemLabel.maskable = true;
        ((RectTransform)item.transform).Bounds = new(180, 100, 30, 30);
        Check(UiVisibility.IsVisible(item), "Partial scroll-viewport intersections remain visible");
        clip.padding = new() { z = 25 };
        Check(!UiVisibility.IsVisible(item), "RectMask2D padding clips at the authored inner edge");
        clip.padding = new() { x = 300 };
        Check(!UiVisibility.IsVisible(item), "Padding that consumes the entire viewport hides children");
        clip.padding = default;
        ((RectTransform)item.transform).Bounds = new(100, 250, 50, 30);
        var nestedCanvas = item.Add(new Canvas { Root = canvas, overrideSorting = true });
        Check(UiVisibility.IsVisible(item), "A dropdown override-sorting canvas escapes the parent scroll clip");
        var popupLabel = RectChild(item, new(100, 250, 50, 30));
        popupLabel.Add(new TextMeshProUGUI { text = "Popup row" });
        Check(UiVisibility.IsVisible(popupLabel), "Descendants of the dropdown canvas also escape ancestor clips");
        nestedCanvas.overrideSorting = false;
        Check(!UiVisibility.IsVisible(popupLabel), "Ordinary nested canvases still inherit clipping");
        nestedCanvas.Enabled = false;
        Check(!UiVisibility.IsVisible(popupLabel), "A disabled override canvas cannot expose its descendants");
        nestedCanvas.Enabled = true;
        var innerViewport = RectChild(viewport, new(250, 0, 200, 200));
        innerViewport.Add(new RectMask2D());
        var spanning = RectChild(innerViewport, new(0, 0, 500, 500));
        spanning.Add(new TextMeshProUGUI { text = "Clipped row" });
        Check(!UiVisibility.IsVisible(spanning), "Disjoint masks intersect cumulatively, even when the child spans each one");
        clip.Enabled = false;
        var stencilGraphic = viewport.Add(new Graphic());
        var stencil = viewport.Add(new Mask());
        Check(!UiVisibility.IsVisible(item), "Active stencil-mask bounds suppress outside controls");
        stencilGraphic.Enabled = false;
        Check(UiVisibility.IsVisible(item), "A mask with no active masking graphic does not clip");
        stencilGraphic.Enabled = true; stencil.Active = false;
        Check(UiVisibility.IsVisible(item), "Inactive stencil masks do not clip");

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        Check(UiVisibility.IsVisible(textGo), "Screen-space canvas without an assigned camera uses overlay projection");
        var camera = new GameObject().Add(new Camera { pixelRect = new(500, 0, 500, 500) });
        canvas.worldCamera = camera;
        Check(!UiVisibility.IsVisible(textGo), "Camera viewport, rather than full screen, bounds camera-space menus");
        camera.pixelRect = new(0, 0, 1920, 1080);
        Check(UiVisibility.IsVisible(textGo), "Valid camera-projected text remains readable");
        rect.Depth = -1;
        Check(!UiVisibility.IsVisible(textGo), "World/camera-space rectangles behind the camera are suppressed");
        rect.Depth = 1; camera.Enabled = false;
        Check(!UiVisibility.IsVisible(textGo), "Inactive render camera suppresses camera-space menus");
        camera.Enabled = true; canvas.renderMode = RenderMode.WorldSpace; canvas.worldCamera = null;
        Check(UiVisibility.IsVisibleGraphic(text), "A missing world-space event camera does not prove that rendered text is hidden");
        Check(!UiScreenGeometry.TryGetScreenRect(rect, out _), "Unknown world-space projection cannot invent screen coordinates for reading order");
        canvas.worldCamera = camera; camera.Enabled = false;
        Check(UiVisibility.IsVisibleGraphic(text), "A disabled world-space event camera does not disable unrelated rendering cameras");
        camera.Enabled = true; rect.Depth = -1;
        Check(UiVisibility.IsVisibleGraphic(text), "A world-space event camera's depth is not evidence about all rendering cameras");
        text.canvasRenderer.cull = true;
        Check(!UiVisibility.IsVisibleGraphic(text), "Native culling remains strict when world-space projection is unknown");
        text.canvasRenderer.cull = false; text.canvasRenderer.Alpha = 0;
        Check(!UiVisibility.IsVisibleGraphic(text), "Renderer opacity remains strict when world-space projection is unknown");
        text.canvasRenderer.Alpha = 1; rect.Depth = 1;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var outsideLayout = RectChild(root, new(2500, 0, 200, 200));
        var insideLayoutLabel = RectChild(outsideLayout, new(100, 100, 100, 30));
        var insideText = insideLayoutLabel.Add(new TextMeshProUGUI { text = "Visible child of offscreen layout" });
        Check(UiVisibility.IsVisible(outsideLayout) && UiVisibility.IsVisibleGraphic(insideText),
            "A layout-only owner outside the viewport does not clip its on-screen child label");
        Check(TextUtils.GetTextFromHierarchy(outsideLayout) == "Visible child of offscreen layout",
            "Generic hierarchy reading reaches a visible label even when its layout owner is outside the viewport");
        ((RectTransform)insideLayoutLabel.transform).Bounds = new(2500, 0, 100, 30);
        Check(!UiVisibility.IsVisibleGraphic(insideText) && TextUtils.GetTextFromHierarchy(outsideLayout) == null,
            "The actual graphic remains screen-clipped even when its non-rendering owner is eligible");
        ((RectTransform)insideLayoutLabel.transform).Bounds = new(100, 100, 100, 30);
        outsideLayout.Add(new RectMask2D());
        Check(!UiVisibility.IsVisibleGraphic(insideText), "An explicit mask on an offscreen layout still clips the child graphic");

        var settingsRoot = new GameObject(); settingsRoot.Add(new Canvas());
        var row = settingsRoot.Child("Setting row");
        var lower = RectChild(row, new(10, 100, 100, 20));
        lower.Add(new TextMeshProUGUI { text = "Lower label" });
        var upper = RectChild(row, new(10, 200, 100, 20));
        upper.Add(new TextMeshProUGUI { text = "Upper label" });
        var control = RectChild(row, new(150, 200, 100, 20));
        control.Add(new Slider());
        control.Add(new LE.Settings.NumericSettingUI { _settingContainer = row });
        string orderedName = null, orderedDescription = null;
        TextDetector.FindLabelInImmediateContainer(control, ref orderedName, ref orderedDescription, null);
        Check(orderedName == "Upper label", "Generic row labels follow screen placement instead of prefab sibling order");
        orderedName = orderedDescription = null;
        TextDetector.FindLabelAndDescriptionAggressive(control, null, ref orderedName, ref orderedDescription);
        Check(orderedName == "Upper label", "Aggressive fallback keeps the same spatial order within its owned row");
        Check(TextDetector.FindAnyTextInParents(control, null) == "Upper label", "Last label fallback also uses visible spatial order");

        var owner = root.Add(new Navigable());
        var first = textGo.Add(new Button());
        var secondGo = root.Child("Second control"); secondGo.Add(new Button());
        NavigationManager.currentlySelected = owner;
        owner.ActiveSelectable = first;
        SettingsFocusReader.Current = null;
        ScreenContentAnnouncer.HasActiveContent = ScreenContentAnnouncer.HasPendingContent = false;
        LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = false;
        LE.UI.LoadingScreen.IsVisible = false; Application.isFocused = true;
        Check(UiFocusOwnership.Allows(textGo), "The native selected control owns generic focus");
        text.canvasRenderer.Alpha = 0;
        Check(!UiFocusOwnership.Allows(textGo) && !UiFocusOwnership.TryGetSelected(out _),
            "A retained control whose entire presentation has faded cannot speak or latch polling focus");
        textGo.Add(new InfoForTooltip { tooltipInfo = new TooltipInfo { nameText = "Invisible tooltip title", mainText = "Invisible details." } });
        ElementAnnouncer.AnnounceElement(textGo);
        Check(ScreenReader.Messages.Count == 0 && !ElementAnnouncer.TryReadDetails(textGo, out _),
            "Invisible generic controls cannot escape through tooltip, object-name or manual-detail fallback");
        var visibleDecoration = RectChild(textGo, new(100, 100, 40, 20)).Add(new Graphic());
        Check(UiFocusOwnership.Allows(textGo), "One visible child graphic keeps an otherwise faded control available");
        visibleDecoration.Enabled = false;
        Check(!UiVisibility.HasVisiblePresentation(textGo), "Inactive child graphics cannot revive an invisible control");
        text.canvasRenderer.Alpha = 1;
        Check(UiFocusOwnership.Allows(textGo), "A control becomes readable again as its graphic fades back in");
        Check(UiVisibility.HasVisiblePresentation(secondGo), "Non-rendering native controls retain semantic focus");
        Check(!UiFocusOwnership.Allows(secondGo), "Another visible control in the same menu cannot announce stale focus");
        Check(UiFocusOwnership.Allows(textGo.Child("Selected decoration")), "Cursor decoration below the selected control retains focus");
        Check(!UiFocusOwnership.Allows(root), "A whole menu ancestor cannot masquerade as its selected control");
        Check(!UiFocusOwnership.Allows(new GameObject()), "Foreign visible menus cannot claim the current navigation");
        var custom = root.Add(new CustomNavigable());
        NavigationManager.currentlySelected = custom;
        Check(UiFocusOwnership.Allows(textGo), "Custom non-Navigable owners accept visible controls in their own hierarchy");
        custom.Enabled = false;
        Check(!UiFocusOwnership.Allows(textGo), "Disabled custom social/search owners cannot retain focus speech");
        NavigationManager.currentlySelected = owner;
        owner.Enabled = false;
        Check(!UiFocusOwnership.Allows(textGo), "A disabled retained navigation owner cannot speak");
        owner.Enabled = true; owner.ActiveSelectable = null;
        Check(UiFocusOwnership.Allows(secondGo), "Native navigables without ActiveSelectable accept only their own hierarchy");
        var vertical = root.Add(new VerticalNavigable { selectables = new(new[] { first, secondGo.GetComponent<Button>() }), currentlySelectedIdx = 1 });
        NavigationManager.currentlySelected = vertical;
        Check(UiFocusOwnership.TryGetSelected(out var selectedVertical) && selectedVertical == secondGo,
            "MonoBehaviour-based vertical lists expose their exact indexed button for generic polling");
        Check(UiFocusOwnership.Allows(secondGo) && !UiFocusOwnership.Allows(textGo),
            "Vertical list cursor speech rejects stale buttons in its own visible hierarchy");
        vertical.currentlySelectedIdx = -1;
        Check(!UiFocusOwnership.TryGetSelected(out _) && !UiFocusOwnership.Allows(secondGo),
            "Invalid vertical indices cannot fall back to a stale hierarchy selection");
        vertical.currentlySelectedIdx = 2;
        Check(!UiFocusOwnership.TryGetSelected(out _), "Vertical upper array bounds are validated");
        vertical.currentlySelectedIdx = 0; vertical.selectables.WasCollected = true;
        Check(!UiFocusOwnership.TryGetSelected(out _), "Collected native vertical arrays cannot be dereferenced");
        vertical.selectables.WasCollected = false; vertical.Enabled = false;
        Check(!UiFocusOwnership.TryGetSelected(out _), "Disabled vertical owners cannot expose retained buttons");
        var editor = root.Child("Crafting materials search").Add(new TMP_InputField());
        var search = root.Add(new CraftingMaterialsPanelSearchInputFieldNavigable { inputField = editor });
        NavigationManager.currentlySelected = search;
        Check(UiFocusOwnership.TryGetSelected(out var selectedSearch) && selectedSearch == editor.gameObject,
            "Crafting material search exposes its declared native text editor without needing a cursor callback");
        Check(!UiFocusOwnership.Allows(secondGo), "Crafting search focus cannot read neighboring material controls");
        editor.gameObject.Active = false;
        Check(!UiFocusOwnership.TryGetSelected(out _), "Hidden crafting search editors remain silent");
        NavigationManager.currentlySelected = owner;
        owner.ActiveSelectable = first;
        SettingsFocusReader.Current = new(10, 2, secondGo, "Settings", null);
        Check(UiFocusOwnership.Allows(secondGo) && !UiFocusOwnership.Allows(textGo), "Settings exact rectangles override stale generic cursor selection");
        SettingsFocusReader.Details = "Visible challenge details";
        ScreenContentAnnouncer.HasActiveContent = true;
        Check(!SettingsFocusAnnouncer.ReadCurrentDetails(), "Manual settings/challenge details yield to the active popup");
        ScreenContentAnnouncer.HasActiveContent = false; LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = true;
        Check(!SettingsFocusAnnouncer.ReadCurrentDetails(), "Manual settings details cannot interrupt foreground dialogue");
        LeAccess.Accessibility.Dialogue.DialogueAnnouncer.HasActiveContent = false; LE.UI.LoadingScreen.IsVisible = true;
        Check(!SettingsFocusAnnouncer.ReadCurrentDetails(), "Manual settings details remain silent while loading");
        LE.UI.LoadingScreen.IsVisible = false; Application.isFocused = false;
        Check(!SettingsFocusAnnouncer.ReadCurrentDetails(), "Manual settings details remain silent after losing application focus");
        Application.isFocused = true; SettingsFocusReader.Details = null;
        SettingsFocusReader.Current = null;
        ElementAnnouncer.ResetFocus(); ScreenReader.Messages.Clear(); Time.unscaledTime += 1;
        ElementAnnouncer.AnnounceElement(textGo);
        Check(ScreenReader.Messages.Count == 1, "Owned visible control announces once");
        owner.ActiveSelectable = secondGo.GetComponent<Button>();
        Check(!ElementAnnouncer.IsRelatedToFocusedElement(textGo), "Cached old focus cannot own a value after navigation changes");
        Check(!ElementAnnouncer.TryReadDetails(textGo, out _), "Manual details cannot read stale current controls");
        ElementAnnouncer.AnnounceElement(textGo);
        Check(ScreenReader.Messages.Count == 1, "Background callback remains silent after native focus changes");
        owner.ActiveSelectable = first;
        ElementAnnouncer.AnnounceElement(textGo);
        Check(ScreenReader.Messages.Count == 2, "Returning to the same native control renews its announcement after ownership loss");
        ScreenContentAnnouncer.Root = secondGo; ScreenContentAnnouncer.HasActiveContent = true;
        Check(!UiFocusOwnership.HasForegroundAccess(textGo), "An active modal excludes otherwise visible background menus");
        Check(UiFocusOwnership.HasForegroundAccess(secondGo), "The active modal can expose its own content");
        ElementAnnouncer.ResetFocusIfUnowned();
        ScreenContentAnnouncer.HasActiveContent = false;
        ElementAnnouncer.AnnounceElement(textGo);
        Check(ScreenReader.Messages.Count == 3, "Closing a modal restores speech even if the native underlying index did not change");
        Application.isFocused = false;
        Check(!UiFocusOwnership.HasForegroundAccess(textGo), "Lost application focus suppresses all menu readers");
        Application.isFocused = true; LE.UI.LoadingScreen.IsVisible = true;
        Check(!UiFocusOwnership.HasForegroundAccess(textGo), "Loading screens suspend current-menu speech");
        LE.UI.LoadingScreen.IsVisible = false; NavigationManager.currentlySelected = null;
        ElementAnnouncer.ResetFocus(); ScreenReader.Messages.Clear();
        return checks;
    }
}
