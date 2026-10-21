using LeAccess.Accessibility.ScreenContent;
using LeAccess.Accessibility.Utils;

namespace LeAccess.Accessibility.Items;

/// <summary>Speaks an item as the inventory does: its full name, then its base type.</summary>
internal static class ItemIdentityReader
{
    /// <summary>Null when the item is gone or does not unpack to itself.</summary>
    internal static string Read(ItemData data)
    {
        if (data is null || data.WasCollected) return null;
        var unpacked = data.getAsUnpacked();
        if (unpacked is null || unpacked.WasCollected ||
            unpacked.Pointer != data.Pointer && !unpacked.IdEquals(data)) return null;
        string identity = ScreenContentTracker.Combine(new[]
        {
            TextUtils.CleanText(unpacked.FullName), TextUtils.CleanText(unpacked.LocalisedBaseTypeName)
        });
        return string.IsNullOrWhiteSpace(identity) ? null : identity;
    }
}
