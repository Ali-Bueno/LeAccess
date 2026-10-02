using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

internal static class MetadataFixture
{
    internal static byte[] Create(string assemblyName = "UnityEngine.RepairFixture", int orphans = 2,
        bool largeStringHeap = false, bool exhaustNamespaces = false, bool oneFreeNamespace = false)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString(assemblyName + ".dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(metadata.GetOrAddString(assemblyName), new Version(1, 0), default, default, 0, AssemblyHashAlgorithm.None);
        var core = typeof(object).Assembly.GetName();
        var coreReference = metadata.AddAssemblyReference(metadata.GetOrAddString(core.Name), core.Version, default,
            metadata.GetOrAddBlob(core.GetPublicKeyToken()), 0, default);
        var objectReference = metadata.AddTypeReference(coreReference, metadata.GetOrAddString("System"), metadata.GetOrAddString("Object"));
        metadata.AddTypeDefinition(TypeAttributes.NotPublic, default, metadata.GetOrAddString("<Module>"), default,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        TypeDefinitionHandle Add(string space, string name, TypeAttributes visibility) => metadata.AddTypeDefinition(
            visibility | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            metadata.GetOrAddString(space), metadata.GetOrAddString(name), objectReference,
            MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));
        Add("Occupied", "<>O", TypeAttributes.Public);
        var host = Add(exhaustNamespaces ? "Occupied" : "First", "Host", TypeAttributes.Public);
        var nested = Add("", "NormalNested", TypeAttributes.NestedPrivate);
        metadata.AddNestedType(nested, host);
        if (!exhaustNamespaces && !oneFreeNamespace) Add("Second", "Other", TypeAttributes.NotPublic);
        if (largeStringHeap) Add(new string('N', ushort.MaxValue + 1), "HeapBoundary", TypeAttributes.NotPublic);
        for (int index = 0; index < orphans; index++)
            Add("", "<>O", index % 2 == 0 ? TypeAttributes.NestedPrivate : TypeAttributes.NestedPublic);
        Add(exhaustNamespaces ? "Occupied" : "First", "TokenOwner", TypeAttributes.Public);

        var fieldSignature = new BlobBuilder();
        new BlobEncoder(fieldSignature).FieldSignature().Int32();
        metadata.AddFieldDefinition(FieldAttributes.Public | FieldAttributes.Static,
            metadata.GetOrAddString("Value"), metadata.GetOrAddBlob(fieldSignature));
        var methodSignature = new BlobBuilder();
        new BlobEncoder(methodSignature).MethodSignature().Parameters(0, result => result.Type().Int32(), _ => { });
        var instructions = new BlobBuilder();
        var encoder = new InstructionEncoder(instructions);
        encoder.LoadConstantI4(7);
        encoder.OpCode(ILOpCode.Ret);
        var bodies = new BlobBuilder();
        int offset = new MethodBodyStreamEncoder(bodies).AddMethodBody(encoder);
        metadata.AddMethodDefinition(MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.HideBySig,
            MethodImplAttributes.IL, metadata.GetOrAddString("GetValue"), metadata.GetOrAddBlob(methodSignature), offset,
            MetadataTokens.ParameterHandle(1));

        var image = new ManagedPEBuilder(new PEHeaderBuilder(imageCharacteristics: Characteristics.ExecutableImage | Characteristics.Dll),
            new MetadataRootBuilder(metadata), bodies, flags: CorFlags.ILOnly);
        var result = new BlobBuilder();
        image.Serialize(result);
        return result.ToArray();
    }
}
