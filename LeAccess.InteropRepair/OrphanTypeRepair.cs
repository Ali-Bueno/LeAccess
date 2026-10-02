using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace LeAccess.InteropRepair;

/// <summary>Repairs invalid generated nested types without rewriting metadata tokens.</summary>
internal static class OrphanTypeRepair
{
    internal static int RepairFile(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        int count = Repair(bytes);
        if (count == 0) return 0;
        string temporary = path + ".leaccess-repair.tmp";
        try
        {
            File.WriteAllBytes(temporary, bytes);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
        return count;
    }

    internal static int Repair(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        using var pe = new PEReader(stream);
        var metadata = pe.GetMetadataReader();
        string assembly = metadata.GetString(metadata.GetAssemblyDefinition().Name);
        if (!assembly.StartsWith("UnityEngine", StringComparison.Ordinal)) return 0;

        var orphans = new List<(int Row, TypeDefinition Type)>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var namespaces = new Dictionary<int, string>();
        foreach (var handle in metadata.TypeDefinitions)
        {
            var type = metadata.GetTypeDefinition(handle);
            bool topLevel = type.GetDeclaringType().IsNil;
            var visibility = type.Attributes & TypeAttributes.VisibilityMask;
            if (topLevel && visibility != TypeAttributes.NotPublic && visibility != TypeAttributes.Public)
                orphans.Add((MetadataTokens.GetRowNumber(handle), type));
            if (topLevel)
                names.Add(metadata.GetString(type.Namespace) + "." + metadata.GetString(type.Name));
            if (!type.Namespace.IsNil)
                namespaces.TryAdd(MetadataTokens.GetHeapOffset(type.Namespace), metadata.GetString(type.Namespace));
        }
        if (orphans.Count == 0) return 0;

        int stringIndexSize = metadata.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
        int tableOffset = pe.PEHeaders.MetadataStartOffset + metadata.GetTableMetadataOffset(TableIndex.TypeDef);
        int rowSize = metadata.GetTableRowSize(TableIndex.TypeDef);
        var changes = new List<(int Offset, uint Flags, int Namespace)>();
        foreach (var (row, type) in orphans)
        {
            string name = metadata.GetString(type.Name);
            var candidate = namespaces.FirstOrDefault(ns => !names.Contains(ns.Value + "." + name));
            if (candidate.Key == 0) throw new InvalidDataException("No unique existing namespace for generated orphan type.");
            names.Add(candidate.Value + "." + name);
            int offset = checked(tableOffset + (row - 1) * rowSize);
            uint flags = (uint)(type.Attributes & ~TypeAttributes.VisibilityMask);
            changes.Add((offset, flags, candidate.Key));
        }

        // ECMA-335 TypeDef row: Flags (UInt32), Name (string index), Namespace (string index).
        foreach (var change in changes)
        {
            BitConverter.GetBytes(change.Flags).CopyTo(bytes, change.Offset);
            int namespaceOffset = change.Offset + sizeof(uint) + stringIndexSize;
            byte[] index = stringIndexSize == 2 ? BitConverter.GetBytes(checked((ushort)change.Namespace))
                : BitConverter.GetBytes(change.Namespace);
            index.CopyTo(bytes, namespaceOffset);
        }
        return changes.Count;
    }
}
