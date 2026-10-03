using LeAccess.Accessibility;
using LeAccess.Accessibility.Announcements;
using LeAccess.Accessibility.ScreenContent;
using TMPro;
using UnityEngine;

internal static class FilterRangeTests
{
    internal static int Run(Action reset)
    {
        int checks = 0;
        void Check(bool value, string message)
        { checks++; if (!value) throw new InvalidOperationException(message); }
        TextMeshProUGUI Label(GameObject parent, string value) => parent.Child().Add(new TextMeshProUGUI { text = value });
        (InputRange Range, TMP_InputField Min, TMP_InputField Max, TMP_Text Header) Row(GameObject root, string heading)
        {
            var row = root.Child(); var header = Label(row, heading);
            var range = row.Child().Add(new InputRange());
            var min = range.gameObject.Child().Add(new NumberInput()); var max = range.gameObject.Child().Add(new NumberInput());
            min._input = min.gameObject.Add(new TMP_InputField { text = "2" }); max._input = max.gameObject.Add(new TMP_InputField { text = "5" });
            min._input.placeholder = Label(min.gameObject, "Min"); max._input.placeholder = Label(max.gameObject, "Max");
            range._minInput = min; range._maxInput = max;
            Label(row, "Applies to specific items.");
            return (range, min._input, max._input, header);
        }
        reset(); LeAccess.I18n.ModText.SetLanguage("en");
        var root = new GameObject().Add(new PotentialConditionUI());
        var lp = Row(root.gameObject, "Legendary Potential"); root._legendaryPotentialRange = lp.Range;
        var fp = Row(root.gameObject, "Forging Potential"); root._forgingPotentialRange = fp.Range;
        var ww = Row(root.gameObject, "Weaver's Will"); root._weaversWillRange = ww.Range;
        var wt = Row(root.gameObject, "Weaver's Touch"); root._weaversTouchRange = wt.Range;
        foreach (var item in new[] { lp, fp, ww, wt })
        {
            reset(); ElementAnnouncer.AnnounceElement(item.Min.gameObject);
            Check(ScreenReader.Messages.Single().Text == item.Header.text + ". Min: 2", "Filter minimum retains its own visible condition heading");
            reset(); ElementAnnouncer.AnnounceElement(item.Max.gameObject);
            Check(ScreenReader.Messages.Single().Text == item.Header.text + ". Max: 5", "Filter maximum retains its own visible condition heading");
        }
        reset(); var count = new GameObject().Add(new AffixCountConditionUI());
        var prefixes = Row(count.gameObject, "Prefixes"); var suffixes = Row(count.gameObject, "Suffixes");
        count._prefixesRange = prefixes.Range; count._suffixesRange = suffixes.Range;
        foreach (var item in new[] { prefixes, suffixes })
        {
            reset(); ElementAnnouncer.AnnounceElement(item.Min.gameObject);
            Check(ScreenReader.Messages.Single().Text == item.Header.text + ". Min: 2", "Affix counts retain their own prefix or suffix heading");
        }
        reset(); lp.Min.text = ""; ElementAnnouncer.AnnounceElement(lp.Min.gameObject);
        Check(ScreenReader.Messages.Single().Text == "Legendary Potential. Min: empty", "Empty bounds are announced without inventing zero");
        reset(); lp.Min.text = "3"; lp.Min.Interactable = false; ElementAnnouncer.AnnounceElement(lp.Min.gameObject);
        Check(ScreenReader.Messages.Single().Text.EndsWith("(Disabled)"), "Unavailable filter inputs retain disabled state"); lp.Min.Interactable = true;
        reset(); lp.Header.text = "Potencial legendario"; lp.Min.placeholder.GetComponent<TMP_Text>().text = "Mínimo";
        ElementAnnouncer.AnnounceElement(lp.Min.gameObject);
        Check(ScreenReader.Messages.Single().Text == "Potencial legendario. Mínimo: 3", "Native heading and bound role remain localized");
        reset(); root._legendaryPotentialRange = null; ElementAnnouncer.AnnounceElement(lp.Min.gameObject);
        Check(!ScreenReader.Messages.Single().Text.StartsWith("Potencial legendario"), "Replaced condition range cannot borrow old context"); root._legendaryPotentialRange = lp.Range;
        reset(); lp.Header.gameObject.Active = false; ElementAnnouncer.AnnounceElement(lp.Min.gameObject);
        Check(!ScreenReader.Messages.Single().Text.Contains("Potencial legendario"), "Hidden headings are rejected"); lp.Header.gameObject.Active = true;
        reset(); ScreenContentAnnouncer.HasActiveContent = true; ScreenContentAnnouncer.Root = root.gameObject;
        ElementAnnouncer.AnnounceElement(lp.Min.gameObject);
        Check(ScreenReader.Messages.Single().Text == "Potencial legendario. Mínimo: 3", "Foreground filter modal retains associated field context");
        reset(); ScreenContentAnnouncer.HasActiveContent = true; ScreenContentAnnouncer.Root = new GameObject();
        ElementAnnouncer.AnnounceElement(lp.Min.gameObject); Check(ScreenReader.Messages.Count == 0, "Another modal suppresses retained filter fields");
        reset(); root.gameObject.Active = false; ElementAnnouncer.AnnounceElement(lp.Min.gameObject);
        Check(ScreenReader.Messages.Count == 0, "Closed filter suppresses retained fields"); root.gameObject.Active = true;
        foreach (string lifetime in new[] { "collected", "disabled" })
        {
            reset(); root.WasCollected = lifetime == "collected"; root.Enabled = lifetime != "disabled";
            Check(!FilterRangeControlReader.TryRead(lp.Min.gameObject, out _, out _), "Filter rejects " + lifetime + " condition owners");
            root.WasCollected = false; root.Enabled = true;
            reset(); lp.Range.WasCollected = lifetime == "collected"; lp.Range.Enabled = lifetime != "disabled";
            Check(!FilterRangeControlReader.TryRead(lp.Min.gameObject, out _, out _), "Filter rejects " + lifetime + " range wrappers");
            lp.Range.WasCollected = false; lp.Range.Enabled = true;
        }
        reset(); var number = lp.Range._minInput; number._input = fp.Min;
        Check(!FilterRangeControlReader.TryRead(lp.Min.gameObject, out _, out _), "Replaced numeric input pointers cannot borrow retained context"); number._input = lp.Min;
        reset(); lp.Range._minInput = fp.Range._minInput;
        Check(!FilterRangeControlReader.TryRead(lp.Min.gameObject, out _, out _), "Different min-input owners cannot borrow retained context"); lp.Range._minInput = number;
        reset(); lp.Min.placeholder = fp.Min.placeholder;
        Check(!FilterRangeControlReader.TryRead(lp.Min.gameObject, out _, out _), "Foreign placeholders cannot supply a bound role");
        lp.Min.placeholder = Label(lp.Min.gameObject, "Mínimo");
        reset(); lp.Header.text = "<b></b>";
        Check(!FilterRangeControlReader.TryRead(lp.Min.gameObject, out _, out _), "Markup-only headings cannot supply retained context"); lp.Header.text = "Potencial legendario";
        reset(); lp.Min.placeholder.gameObject.Active = false; ElementAnnouncer.AnnounceElement(lp.Min.gameObject);
        Check(ScreenReader.Messages.Single().Text == "Potencial legendario. Mínimo: 3", "Owned placeholders supply their role when populated fields hide them");
        lp.Min.placeholder.gameObject.Active = true;
        reset(); return checks;
    }
}

public class NumberInput : UnityEngine.Behaviour { public TMP_InputField _input; }
public class InputRange : UnityEngine.Behaviour { public NumberInput _minInput, _maxInput; }
public class PotentialConditionUI : UnityEngine.Behaviour
{ public InputRange _legendaryPotentialRange, _forgingPotentialRange, _weaversWillRange, _weaversTouchRange; }
public class AffixCountConditionUI : UnityEngine.Behaviour { public InputRange _prefixesRange, _suffixesRange; }
