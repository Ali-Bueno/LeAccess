using System;
using System.Collections.Generic;
using System.Globalization;
using LeAccess.I18n;

namespace LeAccess.Accessibility.ModMenu;

internal enum SettingUnit { None, Percent, Metres, Seconds, Milliseconds, Times, BodyWidths }

/// <summary>How the menu shows and steps one config option; Id names its text keys.</summary>
internal readonly record struct SettingSpec(string Key, string Id, SettingUnit Unit = SettingUnit.None, double Step = 0,
    double Minimum = double.NaN, double Maximum = double.NaN, string ZeroText = null, bool Restart = false,
    string[] Choices = null);

/// <summary>A stored option as the menu sees it, without the config library's types.</summary>
internal interface ISetting
{
    Type Type { get; }
    object Value { get; set; }
    object DefaultValue { get; }
    /// <summary>The option's own accepted range, when it declares one.</summary>
    (double Minimum, double Maximum)? Range { get; }
}

/// <summary>A boolean, number or string option: Cross toggles or cycles, left/right step, Triangle restores the default.</summary>
internal sealed class SettingItem : MenuItem
{
    private readonly ISetting _setting;
    private readonly SettingSpec _spec;
    private readonly string _label, _details;
    private readonly Func<string, string> _choiceText;

    internal SettingItem(ISetting setting, SettingSpec spec, string label, string details,
        Func<string, string> choiceText = null)
    {
        _setting = setting;
        _spec = spec;
        _label = label;
        _details = details;
        _choiceText = choiceText;
    }

    internal override string Label => _label;
    internal override string Value => Text(_setting.Value);

    internal override string Details
    {
        get
        {
            var parts = new List<string> { _details, ModText.Format("menu.default", Text(_setting.DefaultValue)) };
            if (IsNumber && Bounds() is var (minimum, maximum))
                parts.Add(ModText.Format("menu.range", Text(minimum), Text(maximum)));
            if (_spec.Restart) parts.Add(ModText.Get("menu.restart"));
            return string.Join(" ", parts.FindAll(part => !string.IsNullOrEmpty(part)));
        }
    }

    private bool IsNumber => _setting.Type == typeof(int) || _setting.Type == typeof(float) || _setting.Type == typeof(double);
    private string[] Choices => _spec.Choices;

    internal override MenuReply Activate()
    {
        if (_setting.Type == typeof(bool) || Choices != null) { Adjust(1); return new MenuReply(Value); }
        if (_setting.Type == typeof(string)) return new MenuReply(ModText.Get("menu.edit_in_file"));
        return default;
    }

    internal override bool Adjust(int direction)
    {
        object next = Next(_setting.Value, direction);
        if (next is null || Equals(next, _setting.Value)) return false;
        _setting.Value = next;
        return true;
    }

    internal override bool Reset()
    {
        if (Equals(_setting.Value, _setting.DefaultValue)) return true;
        _setting.Value = _setting.DefaultValue;
        return true;
    }

    /// <summary>The value one step away: booleans flip, choices move without wrapping, numbers step and clamp.</summary>
    internal object Next(object value, int direction)
    {
        if (_setting.Type == typeof(bool)) return !(bool)value;
        if (Choices != null)
        {
            int index = Array.IndexOf(Choices, value as string);
            int target = index < 0 ? 0 : Math.Clamp(index + Math.Sign(direction), 0, Choices.Length - 1);
            return Choices[target];
        }
        if (!IsNumber) return null;
        double step = _spec.Step > 0 ? _spec.Step : _setting.Type == typeof(int) ? 1 : 0.1;
        double current = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        double stepped = Math.Round((current + Math.Sign(direction) * step) / step) * step;
        if (Bounds() is var (minimum, maximum)) stepped = Math.Clamp(stepped, minimum, maximum);
        stepped = Math.Round(stepped, Decimals(step));
        return Convert.ChangeType(stepped, _setting.Type, CultureInfo.InvariantCulture);
    }

    private (double Minimum, double Maximum)? Bounds()
    {
        if (!double.IsNaN(_spec.Minimum) && !double.IsNaN(_spec.Maximum)) return (_spec.Minimum, _spec.Maximum);
        return _setting.Range;
    }

    private static int Decimals(double step)
    {
        int decimals = 0;
        while (decimals < 4 && Math.Abs(step * Math.Pow(10, decimals) - Math.Round(step * Math.Pow(10, decimals))) > 1e-9)
            decimals++;
        return decimals;
    }

    private string Text(object value)
    {
        if (value is bool flag) return ModText.Get(flag ? "state.on" : "state.off");
        if (Choices != null) return _choiceText?.Invoke(value as string) ?? value as string;
        if (value is string text) return string.IsNullOrEmpty(text) ? ModText.Get("menu.empty") : text;
        double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
        if (number == 0 && _spec.ZeroText != null) return ModText.Get(_spec.ZeroText);
        return UnitText(_spec.Unit, number);
    }

    /// <summary>A number with its unit in the speech language.</summary>
    internal static string UnitText(SettingUnit unit, double number)
    {
        number = Math.Round(number, 2);
        return unit switch
        {
            SettingUnit.Percent => ModText.Format("menu.unit.percent", number),
            SettingUnit.Metres => ModText.Format("menu.unit.metres", number),
            SettingUnit.Seconds => ModText.Format("menu.unit.seconds", number),
            SettingUnit.Milliseconds => ModText.Format("menu.unit.milliseconds", number),
            SettingUnit.Times => ModText.Format("menu.unit.times", number),
            SettingUnit.BodyWidths => ModText.Format("menu.unit.body_widths", number),
            _ => ModText.Format("menu.unit.none", number)
        };
    }
}
