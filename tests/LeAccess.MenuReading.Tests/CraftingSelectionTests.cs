using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;

internal static class CraftingSelectionTests
{
    internal static int Run(Action reset)
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        MaterialContainerUI Material(GameObject root, int subtype, string name, string quantity)
        {
            var go = root.Child();
            go.Add(new UnityEngine.UI.Graphic());
            var material = go.Add(new MaterialContainerUI { identifier = subtype });
            material.rectTransform = go.Add(new RectTransform());
            material.container = new SingleSubTypeContainer { id = subtype, allowedSubID = subtype, allowedBaseType = new(new[] { 17 }) };
            material.externalItemNumber = go.Child().Add(new TextMeshProUGUI { text = quantity });
            Localization.Items.Names[(17, subtype)] = name;
            return material;
        }
        reset(); LeAccess.I18n.ModText.SetLanguage("en"); Localization.Items = new();
        var nav = new GameObject().Add(new CraftingMaterialSelectorPanel());
        var rune = Material(nav.gameObject, 3, "Rune of Shattering", "7");
        var glyph = Material(nav.gameObject, 4, "Glyph of Hope", "0");
        nav._selectables = new(rune, glyph); nav._selectableRectTs = new(rune.rectTransform, glyph.rectTransform);
        NavigationManager.currentlySelected = nav;
        Check(UiFocusOwnership.TryGetSelected(out var selected) && selected.Pointer == rune.gameObject.Pointer, "Flyout resolves the native selected rectangle without ActiveSelectable");
        ElementAnnouncer.AnnounceElement(selected);
        Check(ScreenReader.Messages.Single().Text == "Rune of Shattering. Quantity: 7", "Icon-only native rune control reads its authored localized subtype and visible quantity");
        var tooltip = rune.gameObject.Add(new InfoForTooltip { tooltipInfo = new TooltipInfo { nameText = "Rune", mainText = "Destroys an item to recover shards." } });
        reset(); NavigationManager.currentlySelected = nav; ElementAnnouncer.AnnounceElement(rune.gameObject);
        Check(tooltip.SetTextCalls == 0, "Brief material speech does not populate tooltip models");
        reset(); NavigationManager.currentlySelected = nav;
        Check(ElementAnnouncer.TryReadDetails(rune.gameObject, out var details) && details.Contains("Destroys an item to recover shards.") && tooltip.SetTextCalls == 1, "Manual material inspection retains the existing native tooltip details path");
        reset(); NavigationManager.currentlySelected = nav; nav._selectedButtonId = 1;
        Check(!UiFocusOwnership.Allows(rune.gameObject) && UiFocusOwnership.Allows(glyph.gameObject), "Same-frame flyout switches immediately reject the retained rune");
        ElementAnnouncer.AnnounceElement(glyph.gameObject);
        Check(ScreenReader.Messages.Single().Text == "Glyph of Hope. Quantity: 0", "Empty material stacks remain named without constructing an item");
        reset(); NavigationManager.currentlySelected = nav; glyph.externalItemNumber.text = "2";
        ElementAnnouncer.AnnounceElement(glyph.gameObject);
        Check(ScreenReader.Messages.Single().Text.EndsWith("Quantity: 2"), "Material quantities are read afresh");
        reset(); NavigationManager.currentlySelected = nav; glyph.interactable = false; ElementAnnouncer.AnnounceElement(glyph.gameObject);
        Check(ScreenReader.Messages.Single().Text == "Glyph of Hope. Quantity: 2 (Disabled)", "Native material interaction state is announced exactly once"); glyph.interactable = true;
        reset(); NavigationManager.currentlySelected = nav; LeAccess.I18n.ModText.SetLanguage("es"); Localization.Items.Names[(17, 4)] = "Glifo de esperanza";
        ElementAnnouncer.AnnounceElement(glyph.gameObject);
        Check(ScreenReader.Messages.Single().Text == "Glifo de esperanza. Cantidad: 2", "Native material names and mod quantity labels remain localized");
        reset(); LeAccess.I18n.ModText.SetLanguage("en"); NavigationManager.currentlySelected = nav;
        var nested = glyph.gameObject.Child().Add(new UnityEngine.UI.Button());
        Check(!CraftingMaterialReader.TryRead(nested.gameObject, out _, out _), "Nested side actions cannot borrow material identity");
        foreach (int index in new[] { -1, 2 })
        {
            reset(); NavigationManager.currentlySelected = nav; nav._selectedButtonId = index;
            Check(!UiFocusOwnership.TryGetSelected(out _) && !UiFocusOwnership.Allows(glyph.gameObject), "Invalid native flyout index cannot fall back to whole-panel focus");
        }
        nav._selectedButtonId = 1;
        reset(); NavigationManager.currentlySelected = nav; nav._selectableRectTs.Values[1] = rune.rectTransform;
        Check(!UiFocusOwnership.Allows(glyph.gameObject), "A mismatched native control/rectangle pair is rejected"); nav._selectableRectTs.Values[1] = glyph.rectTransform;
        reset(); NavigationManager.currentlySelected = nav; nav._selectableRectTs.Values[1] = new GameObject().Add(new RectTransform());
        Check(!UiFocusOwnership.TryGetSelected(out _), "Foreign focus rectangles are rejected"); nav._selectableRectTs.Values[1] = glyph.rectTransform;
        reset(); NavigationManager.currentlySelected = nav; nav._selectables.WasCollected = true;
        Check(!UiFocusOwnership.Allows(glyph.gameObject), "Collected control lists release focus"); nav._selectables.WasCollected = false;
        glyph.Enabled = false;
        Check(!UiFocusOwnership.Allows(glyph.gameObject), "Disabled native material behaviours release selection"); glyph.Enabled = true;
        var source = (SingleSubTypeContainer)glyph.container;
        reset(); NavigationManager.currentlySelected = nav; source.id = 999;
        Check(!CraftingMaterialReader.TryRead(glyph.gameObject, out _, out _), "Backing containers must match the material's native identifier"); source.id = glyph.identifier;
        reset(); NavigationManager.currentlySelected = nav; source.allowedBaseType = new(new[] { 17, 18 });
        Check(!CraftingMaterialReader.TryRead(glyph.gameObject, out _, out _), "Ambiguous native type constraints do not invent a material type"); source.allowedBaseType = new(new[] { 17 });
        reset(); NavigationManager.currentlySelected = nav; glyph.externalItemNumber.gameObject.Active = false;
        ElementAnnouncer.AnnounceElement(glyph.gameObject);
        Check(ScreenReader.Messages.Single().Text == "Glifo de esperanza", "Hidden material counts are excluded"); glyph.externalItemNumber.gameObject.Active = true;
        reset(); NavigationManager.currentlySelected = nav; glyph.externalItemNumber = new GameObject().Add(new TextMeshProUGUI { text = "999" });
        Check(CraftingMaterialReader.TryRead(glyph.gameObject, out var text, out _) && !text.Contains("999"), "Foreign serialized quantity labels are excluded");
        foreach (string gate in new[] { "closed", "disabled", "collected", "loading", "background" })
        {
            reset(); NavigationManager.currentlySelected = nav;
            nav.gameObject.Active = gate != "closed"; nav.Enabled = gate != "disabled"; nav.WasCollected = gate == "collected";
            LE.UI.LoadingScreen.IsVisible = gate == "loading"; Application.isFocused = gate != "background";
            ElementAnnouncer.AnnounceElement(glyph.gameObject); Check(ScreenReader.Messages.Count == 0, "Material speech rejects " + gate);
            nav.gameObject.Active = nav.Enabled = true; nav.WasCollected = false;
        }
        reset(); NavigationManager.currentlySelected = nav; ScreenContentAnnouncer.HasActiveContent = true; ScreenContentAnnouncer.Root = new GameObject();
        ElementAnnouncer.AnnounceElement(glyph.gameObject); Check(ScreenReader.Messages.Count == 0, "Foreground popup suppresses background material selection");
        reset(); var matrixNav = new GameObject().Add(new CraftingRunesAndGlyphsPanel());
        var top = Material(matrixNav.gameObject, 5, "Rune of Removal", "3"); var bottom = Material(matrixNav.gameObject, 6, "Glyph of Stability", "4");
        matrixNav._selectables = new Il2CppSystem.Array(new NativeObject[,] { { top, null }, { null, bottom } });
        NavigationManager.currentlySelected = matrixNav; matrixNav._selectedRowIndex = matrixNav._selectedColumnIndex = 1;
        Check(UiFocusOwnership.TryGetSelected(out selected) && selected.Pointer == bottom.gameObject.Pointer, "Materials matrix uses native row,column order");
        ElementAnnouncer.AnnounceElement(selected);
        Check(ScreenReader.Messages.Single().Text == "Glyph of Stability. Quantity: 4", "Materials matrix glyph selection shares the typed name reader");
        reset(); NavigationManager.currentlySelected = matrixNav; matrixNav._selectedRowIndex = 0;
        Check(!UiFocusOwnership.TryGetSelected(out _) && !UiFocusOwnership.Allows(bottom.gameObject), "Empty matrix cells cannot retain old focus");
        matrixNav._selectedColumnIndex = 0;
        foreach (int row in new[] { -1, 2 })
        { matrixNav._selectedRowIndex = row; Check(!UiFocusOwnership.TryGetSelected(out _), "Matrix bounds are checked before native access"); }
        matrixNav._selectedRowIndex = 0;
        var matrix = (Il2CppSystem.Array)matrixNav._selectables; matrix.RankValue = 1;
        Check(!UiFocusOwnership.TryGetSelected(out _), "Unexpected native matrix rank is rejected"); matrix.RankValue = 2;
        reset(); NavigationManager.currentlySelected = matrixNav; top.rectTransform = new GameObject().Add(new RectTransform());
        Check(!UiFocusOwnership.TryGetSelected(out _), "Material matrix rectangles must belong to the native cell");
        reset(); var forge = new GameObject().Add(new CraftingSlotManager());
        var forgeButton = forge.gameObject.Child().Add(new UnityEngine.UI.Button());
        forgeButton.gameObject.Add(new UnityEngine.UI.Graphic());
        var forgeRect = forgeButton.gameObject.Add(new RectTransform());
        forge.selectables = new Il2CppSystem.Array(new NativeObject[,] { { forgeRect } }); NavigationManager.currentlySelected = forge;
        Check(UiFocusOwnership.TryGetSelected(out selected) && selected.Pointer == forgeButton.gameObject.Pointer, "Forge focus uses the native x,y rectangle matrix");
        forge.currentlySelectedIdx = new(1, 0);
        Check(!UiFocusOwnership.Allows(forgeButton.gameObject), "Unready forge matrices cannot invent generic panel focus");
        reset(); return checks;
    }
}

public class CraftingMaterialSelectorPanel : CustomNavigable
{
    public Il2CppSystem.Collections.Generic.List<UnityEngine.Object> _selectables;
    public Il2CppSystem.Collections.Generic.List<RectTransform> _selectableRectTs;
    public int _selectedButtonId;
}
public class CraftingRunesAndGlyphsPanel : CustomNavigable
{ public Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase _selectables; public int _selectedRowIndex, _selectedColumnIndex; }
public class CraftingSlotManager : CustomNavigable
{ public Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase selectables; public Vector2Int currentlySelectedIdx; }
public class MaterialContainerUI : UnityEngine.Behaviour
{
    public IItemContainer container;
    public int identifier;
    public bool interactable = true;
    public RectTransform rectTransform;
    public TextMeshProUGUI externalItemNumber;
}
public class IItemContainer : NativeObject { }
public class SingleSubTypeContainer : IItemContainer
{
    public int id, allowedSubID;
    public NativeArray<int> allowedBaseType;
    public int GetContainerID() => id;
}
public static class Localization { public static ItemLocalization Items = new(); }
public class ItemLocalization : NativeObject
{
    public Dictionary<(int, int), string> Names = new();
    public string GetSubTypeName(int type, int subtype) => Names.GetValueOrDefault((type, subtype));
}
namespace Il2CppInterop.Runtime.InteropTypes { public class Il2CppObjectBase : NativeObject { } }
namespace UnityEngine
{
    public struct Vector2Int
    {
        public int x, y;
        public Vector2Int(int first, int second) { x = first; y = second; }
    }
}
namespace Il2CppSystem
{
    public class Array : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase
    {
        public NativeObject[,] Cells;
        public int RankValue = 2;
        public Array(NativeObject[,] cells) => Cells = cells;
        public int Rank => RankValue;
        public int GetLength(int dimension) => Cells.GetLength(dimension);
        public NativeObject GetValue(int first, int second) => Cells[first, second];
    }
}
