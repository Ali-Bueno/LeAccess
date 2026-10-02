using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.Loader;
using LeAccess.InteropRepair;

int checks = 0;
void Check(bool condition, string message)
{
    checks++;
    if (!condition) throw new InvalidOperationException(message);
}

foreach (bool largeHeap in new[] { false, true })
{
    byte[] original = MetadataFixture.Create(largeStringHeap: largeHeap);
    byte[] repaired = (byte[])original.Clone();
    using var beforeImage = new PEReader(new MemoryStream(original));
    var before = beforeImage.GetMetadataReader();
    int width = before.GetHeapSize(HeapIndex.String) > ushort.MaxValue ? 4 : 2;
    Check(width == (largeHeap ? 4 : 2), "Fixture exercises the intended string-index width.");
    Check(OrphanTypeRepair.Repair(repaired) == 2, "Both invalid orphan rows are repaired.");
    using var afterImage = new PEReader(new MemoryStream(repaired));
    var after = afterImage.GetMetadataReader();
    var permittedBytes = new HashSet<int>();
    var topLevelNames = new HashSet<string>();
    int rowSize = before.GetTableRowSize(TableIndex.TypeDef);
    int tableOffset = beforeImage.PEHeaders.MetadataStartOffset + before.GetTableMetadataOffset(TableIndex.TypeDef);
    foreach (var handle in before.TypeDefinitions)
    {
        var oldType = before.GetTypeDefinition(handle);
        var newType = after.GetTypeDefinition(handle);
        var oldVisibility = oldType.Attributes & TypeAttributes.VisibilityMask;
        bool orphan = oldType.GetDeclaringType().IsNil && oldVisibility != TypeAttributes.Public && oldVisibility != TypeAttributes.NotPublic;
        Check(before.GetString(oldType.Name) == after.GetString(newType.Name), "Type names and their row identities remain unchanged.");
        Check(oldType.GetDeclaringType() == newType.GetDeclaringType(), "The NestedClass relation is preserved.");
        Check(oldType.GetMethods().SequenceEqual(newType.GetMethods()) && oldType.GetFields().SequenceEqual(newType.GetFields()),
            "Method/field ownership tokens are preserved.");
        if (orphan)
        {
            Check((newType.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic,
                "Orphans become legal nonpublic top-level types.");
            Check((oldType.Attributes & ~TypeAttributes.VisibilityMask) == newType.Attributes,
                "All nonvisibility TypeDef flags are preserved.");
            Check(after.GetString(newType.Namespace) != "Occupied", "Existing full-name collisions are avoided.");
            int start = tableOffset + (MetadataTokens.GetRowNumber(handle) - 1) * rowSize;
            for (int index = 0; index < 4; index++) permittedBytes.Add(start + index);
            for (int index = 0; index < width; index++) permittedBytes.Add(start + 4 + width + index);
        }
        else Check(oldType.Attributes == newType.Attributes && oldType.Namespace == newType.Namespace,
            "Valid top-level and nested rows are unchanged.");
        if (newType.GetDeclaringType().IsNil)
            Check(topLevelNames.Add(after.GetString(newType.Namespace) + "." + after.GetString(newType.Name)),
                "Repaired orphan names are distinct from each other and all existing top-level names.");
    }
    Check(original.Length == repaired.Length, "Repair does not resize the image.");
    Check(original.Where((value, index) => value != repaired[index] && !permittedBytes.Contains(index)).Any() == false,
        "Only orphan TypeDef visibility and namespace-index bytes may change.");
    for (int table = 0; table <= (int)TableIndex.GenericParamConstraint; table++)
        Check(before.GetTableRowCount((TableIndex)table) == after.GetTableRowCount((TableIndex)table),
            "All metadata table row counts remain stable.");
    Check(before.MethodDefinitions.SequenceEqual(after.MethodDefinitions) && before.FieldDefinitions.SequenceEqual(after.FieldDefinitions),
        "All method/field tokens remain stable for the native method map.");
    var method = after.GetMethodDefinition(after.MethodDefinitions.Single());
    Check(afterImage.GetMethodBody(method.RelativeVirtualAddress).GetILBytes()
        .SequenceEqual(beforeImage.GetMethodBody(before.GetMethodDefinition(before.MethodDefinitions.Single()).RelativeVirtualAddress).GetILBytes()),
        "Method body bytes remain unchanged.");
    byte[] once = (byte[])repaired.Clone();
    Check(OrphanTypeRepair.Repair(repaired) == 0 && repaired.SequenceEqual(once), "Repair is byte-for-byte idempotent.");
    var loadContext = new AssemblyLoadContext("repaired-fixture", isCollectible: true);
    try
    {
        using var imageStream = new MemoryStream(repaired);
        var assembly = loadContext.LoadFromStream(imageStream);
        Check(assembly.GetTypes().Length == after.TypeDefinitions.Count - 1, "CLR can load every repaired type without duplicate-name failures.");
    }
    finally { loadContext.Unload(); }
}

foreach (var fixture in new[]
{
    MetadataFixture.Create(orphans: 0),
    MetadataFixture.Create(assemblyName: "GameAssembly.Fixture")
})
{
    byte[] original = (byte[])fixture.Clone();
    Check(OrphanTypeRepair.Repair(fixture) == 0 && fixture.SequenceEqual(original),
        "Valid Unity metadata and out-of-scope assemblies stay untouched.");
}
var impossible = MetadataFixture.Create(exhaustNamespaces: true);
byte[] impossibleOriginal = (byte[])impossible.Clone();
try { OrphanTypeRepair.Repair(impossible); throw new Exception("Expected namespace collision refusal."); }
catch (InvalidDataException) { Check(impossible.SequenceEqual(impossibleOriginal), "Unsatisfiable collisions fail before changing any bytes."); }
byte[] partiallyPlanned = MetadataFixture.Create(oneFreeNamespace: true);
byte[] beforePlanning = (byte[])partiallyPlanned.Clone();
try { OrphanTypeRepair.Repair(partiallyPlanned); throw new Exception("Expected later namespace collision refusal."); }
catch (InvalidDataException) { Check(partiallyPlanned.SequenceEqual(beforePlanning), "A collision in a later orphan cannot apply earlier planned changes."); }

string path = Path.Combine(Path.GetTempPath(), "LeAccess.InteropRepair.Tests." + Guid.NewGuid().ToString("N") + ".dll");
string temporary = path + ".leaccess-repair.tmp";
try
{
    byte[] corrupt = MetadataFixture.Create();
    File.WriteAllBytes(path, corrupt);
    Check(OrphanTypeRepair.RepairFile(path) == 2 && !File.Exists(temporary), "File repair replaces the image and removes its staging file.");
    byte[] repaired = File.ReadAllBytes(path);
    DateTime written = File.GetLastWriteTimeUtc(path);
    Check(OrphanTypeRepair.RepairFile(path) == 0 && File.GetLastWriteTimeUtc(path) == written && File.ReadAllBytes(path).SequenceEqual(repaired),
        "Already repaired files are not rewritten.");
    File.WriteAllBytes(path, impossibleOriginal);
    try { OrphanTypeRepair.RepairFile(path); throw new Exception("Expected invalid file repair refusal."); }
    catch (InvalidDataException) { Check(File.ReadAllBytes(path).SequenceEqual(impossibleOriginal) && !File.Exists(temporary), "Failed planning preserves the original file and leaves no staging file."); }
    if (OperatingSystem.IsWindows())
    {
        File.WriteAllBytes(path, corrupt);
        using (var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            try { OrphanTypeRepair.RepairFile(path); throw new Exception("Expected Windows reader sharing violation."); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { Check(File.ReadAllBytes(path).SequenceEqual(corrupt) && !File.Exists(temporary), "A Cecil-style read lock cannot partially replace the original image."); }
        }
        Check(OrphanTypeRepair.RepairFile(path) == 2, "Repair succeeds once the external reader closes.");
    }
}
finally
{
    if (File.Exists(temporary)) File.Delete(temporary);
    if (File.Exists(path)) File.Delete(path);
}
Console.WriteLine($"Interop repair tests passed: {checks} checks; generated fixtures only, no game files changed.");
