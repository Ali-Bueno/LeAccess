using System;
using System.Collections.Generic;
using LeAccess.I18n;

namespace LeAccess.Accessibility.ModMenu;

internal enum MenuCommand { None, Previous, Next, First, Last, Decrease, Increase, Activate, Back, Details, Preview, Reset, Close }

/// <summary>What the menu says after a command, and whether it closes or waits for a key.</summary>
internal readonly record struct MenuReply(string Speech, bool Close = false, bool CaptureKey = false);

/// <summary>One entry of a mod menu page.</summary>
internal abstract class MenuItem
{
    internal abstract string Label { get; }
    /// <summary>Spoken after the label; null for entries without a value.</summary>
    internal virtual string Value => null;
    internal virtual string Details => null;
    /// <summary>The page Cross opens, if any.</summary>
    internal virtual MenuPage Page => null;
    internal virtual MenuReply Activate() => default;
    /// <summary>Steps the value by one; false at a bound or without a value.</summary>
    internal virtual bool Adjust(int direction) => false;
    internal virtual bool Reset() => false;
    /// <summary>Plays the entry's sound; false when it has none.</summary>
    internal virtual bool Preview() => false;
}

internal sealed class MenuPage
{
    internal MenuPage(string title, IReadOnlyList<MenuItem> items) { Title = title; Items = items; }
    internal string Title { get; }
    internal IReadOnlyList<MenuItem> Items { get; }
}

/// <summary>Opens another page.</summary>
internal sealed class PageItem : MenuItem
{
    private readonly MenuPage _page;
    internal PageItem(MenuPage page) => _page = page;
    internal override string Label => _page.Title;
    internal override MenuPage Page => _page;
}

/// <summary>An entry whose only job is to run something, such as closing the menu.</summary>
internal sealed class ActionItem : MenuItem
{
    private readonly string _label;
    private readonly Func<MenuReply> _run;
    internal ActionItem(string label, Func<MenuReply> run) { _label = label; _run = run; }
    internal override string Label => _label;
    internal override MenuReply Activate() => _run();
}

/// <summary>The cursor through nested pages; no wrapping, Back returns to the entry that opened a page.</summary>
internal sealed class MenuNavigator
{
    private readonly Stack<(MenuPage Page, int Index)> _parents = new();

    internal MenuNavigator(MenuPage root) => Page = root;

    internal MenuPage Page { get; private set; }
    internal int Index { get; private set; }
    internal int Depth => _parents.Count;
    internal MenuItem Current => Page.Items.Count == 0 ? null : Page.Items[Index];

    /// <summary>The entry index on each page from the first page down to the current one.</summary>
    internal IReadOnlyList<int> Path
    {
        get
        {
            var path = new List<int>();
            foreach (var parent in _parents) path.Insert(0, parent.Index);
            path.Add(Index);
            return path;
        }
    }

    /// <summary>Walks a Path again on rebuilt pages, stopping where they no longer match.</summary>
    internal void Follow(IReadOnlyList<int> path)
    {
        for (int step = 0; step < path.Count && Page.Items.Count > 0; step++)
        {
            Index = Math.Clamp(path[step], 0, Page.Items.Count - 1);
            if (step == path.Count - 1 || Current.Page is not { } page) return;
            _parents.Push((Page, Index));
            Page = page;
            Index = 0;
        }
    }

    /// <summary>The page title followed by the focused entry.</summary>
    internal string Opening() => Sentence(Page.Title, Focus());

    internal string Focus()
    {
        var item = Current;
        if (item is null) return null;
        string value = item.Value;
        string text = item.Page != null ? ModText.Format("menu.submenu", item.Label)
            : value is null ? item.Label : ModText.Format("control.field_value", item.Label, value);
        string position = ListPosition.Text(Index + 1, Page.Items.Count);
        return position is null ? text : text + ", " + position;
    }

    internal MenuReply Handle(MenuCommand command)
    {
        var item = Current;
        switch (command)
        {
            case MenuCommand.Previous: return Move(Index - 1);
            case MenuCommand.Next: return Move(Index + 1);
            case MenuCommand.First: return Move(0);
            case MenuCommand.Last: return Move(Page.Items.Count - 1);
            case MenuCommand.Decrease:
            case MenuCommand.Increase:
                if (item is null || item.Value is null) return default;
                item.Adjust(command == MenuCommand.Increase ? 1 : -1);
                return new MenuReply(item.Value);
            case MenuCommand.Activate:
                if (item?.Page is { } page)
                {
                    _parents.Push((Page, Index));
                    Page = page;
                    Index = 0;
                    return new MenuReply(Opening());
                }
                return item?.Activate() ?? default;
            case MenuCommand.Back:
                if (_parents.Count == 0) return new MenuReply(null, Close: true);
                (Page, Index) = _parents.Pop();
                return new MenuReply(Focus());
            case MenuCommand.Details: return new MenuReply(item?.Details);
            case MenuCommand.Preview: item?.Preview(); return default;
            case MenuCommand.Reset:
                return item is not null && item.Reset() ? new MenuReply(item.Value) : default;
            case MenuCommand.Close: return new MenuReply(null, Close: true);
            default: return default;
        }
    }

    private MenuReply Move(int target)
    {
        if (Page.Items.Count == 0) return default;
        Index = Math.Clamp(target, 0, Page.Items.Count - 1);
        return new MenuReply(Focus());
    }

    private static string Sentence(string first, string second) =>
        string.IsNullOrEmpty(second) ? first : string.IsNullOrEmpty(first) ? second : first + ". " + second;
}
