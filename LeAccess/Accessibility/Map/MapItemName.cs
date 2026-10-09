using LeAccess.Accessibility.Forge;

namespace LeAccess.Accessibility.Map;

/// <summary>The name of the item a one-slot map container holds, such as an island's blessing.</summary>
internal static class MapItemName
{
    internal static string Of(ItemContainerUI slot)
    {
        var container = slot.container;
        var content = container is null || container.WasCollected ? null : container.GetContent();
        if (content is null || content.WasCollected || content.Count == 0) return null;
        return ForgeCellReader.NameOf(content[0]?.data);
    }
}
