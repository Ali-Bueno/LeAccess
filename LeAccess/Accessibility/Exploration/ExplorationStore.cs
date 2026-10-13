using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace LeAccess.Accessibility.Exploration;

// What the characters have explored on fixed-layout scenes; the binary file format is in reference/exploration-mode.md.
internal sealed class ExplorationSnapshot
{
    internal Dictionary<string, List<Vector3>> Explored { get; } = new(StringComparer.Ordinal);
    internal Dictionary<string, List<string>> Seen { get; } = new(StringComparer.Ordinal);
}

internal static class ExplorationStore
{
    internal const int Version = 1;
    internal const int MaxScopes = 4096;
    internal const int MaxPointsPerScope = 100_000;
    internal const int MaxKeysPerScope = 50_000;
    internal const int MaxStringBytes = 1024;
    private const int MaxFileBytes = 64 * 1024 * 1024;
    private static readonly byte[] Magic = { (byte)'L', (byte)'E', (byte)'X', (byte)'P' };
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static void Write(Stream stream, ExplorationSnapshot snapshot)
    {
        using var writer = new BinaryWriter(stream, Utf8, leaveOpen: true);
        writer.Write(Magic);
        writer.Write(Version);
        writer.Write(snapshot.Explored.Count);
        foreach (var scope in snapshot.Explored)
        {
            WriteString(writer, scope.Key);
            writer.Write(scope.Value.Count);
            foreach (var point in scope.Value) { writer.Write(point.X); writer.Write(point.Y); writer.Write(point.Z); }
        }
        writer.Write(snapshot.Seen.Count);
        foreach (var scope in snapshot.Seen)
        {
            WriteString(writer, scope.Key);
            writer.Write(scope.Value.Count);
            foreach (var key in scope.Value) WriteString(writer, key);
        }
    }

    // Never throws: any problem rejects the whole file with a reason.
    internal static bool TryRead(Stream stream, out ExplorationSnapshot snapshot, out string reason)
    {
        snapshot = null;
        try
        {
            var bytes = ReadAll(stream);
            if (bytes is null) { reason = "file too large"; return false; }
            using var reader = new BinaryReader(new MemoryStream(bytes, writable: false), Utf8);
            var result = new ExplorationSnapshot();
            reason = ReadSnapshot(reader, bytes.Length, result);
            if (reason != null) return false;
            snapshot = result;
            return true;
        }
        catch (Exception ex)
        {
            reason = $"unreadable ({ex.GetType().Name})";
            return false;
        }
    }

    private static string ReadSnapshot(BinaryReader reader, int length, ExplorationSnapshot result)
    {
        if (length < Magic.Length + 4 || !reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic)) return "bad magic";
        if (reader.ReadInt32() != Version) return "unsupported version";
        int scopes = reader.ReadInt32();
        if (scopes < 0 || scopes > MaxScopes) return "bad explored scope count";
        for (int index = 0; index < scopes; index++)
        {
            string scope = ReadString(reader, length);
            int count = reader.ReadInt32();
            if (scope is null || count < 0 || count > MaxPointsPerScope || (long)count * 12 > Remaining(reader, length))
                return "bad explored scope";
            var points = new List<Vector3>(count);
            for (int p = 0; p < count; p++)
            {
                var point = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                if (!float.IsFinite(point.X) || !float.IsFinite(point.Y) || !float.IsFinite(point.Z)) return "non-finite point";
                points.Add(point);
            }
            if (!result.Explored.TryAdd(scope, points)) return "duplicate explored scope";
        }
        scopes = reader.ReadInt32();
        if (scopes < 0 || scopes > MaxScopes) return "bad seen scope count";
        for (int index = 0; index < scopes; index++)
        {
            string scope = ReadString(reader, length);
            int count = reader.ReadInt32();
            if (scope is null || count < 0 || count > MaxKeysPerScope || count > Remaining(reader, length))
                return "bad seen scope";
            var keys = new List<string>(count);
            for (int k = 0; k < count; k++)
            {
                string key = ReadString(reader, length);
                if (key is null) return "bad seen key";
                keys.Add(key);
            }
            if (!result.Seen.TryAdd(scope, keys)) return "duplicate seen scope";
        }
        return reader.BaseStream.Position == length ? null : "trailing data";
    }

    private static long Remaining(BinaryReader reader, int length) => length - reader.BaseStream.Position;

    private static void WriteString(BinaryWriter writer, string value)
    {
        var bytes = Utf8.GetBytes(value ?? "");
        if (bytes.Length > MaxStringBytes) throw new InvalidDataException("string too long");
        writer.Write((ushort)bytes.Length);
        writer.Write(bytes);
    }

    private static string ReadString(BinaryReader reader, int length)
    {
        int size = reader.ReadUInt16();
        if (size > MaxStringBytes || size > Remaining(reader, length)) return null;
        return Utf8.GetString(reader.ReadBytes(size));
    }

    private static byte[] ReadAll(Stream stream)
    {
        using var copy = new MemoryStream();
        var buffer = new byte[16 * 1024];
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (copy.Length + read > MaxFileBytes) return null;
            copy.Write(buffer, 0, read);
        }
        return copy.ToArray();
    }
}
