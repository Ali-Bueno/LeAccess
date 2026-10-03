using System;
using System.IO;
using System.Text;

namespace LeAccess.Accessibility.Audio;

internal static class PcmWaveReader
{
    // Bundled cues are at most one second; reject damaged assets before allocating.
    private const int MaximumFrames = NaturalCueBank.SampleRate;
    private const int PcmFormat = 1;
    private const int MonoChannels = 1;
    private const int BitsPerSample = 16;
    private const int BytesPerSample = BitsPerSample / 8;

    internal static float[] ReadMono48(Stream stream, int maximumFrames = MaximumFrames)
    {
        if (stream == null || !stream.CanRead || !stream.CanSeek)
            throw new InvalidDataException("Cue resource must be a readable, seekable WAV.");
        using var reader = new BinaryReader(stream, Encoding.ASCII, leaveOpen: true);
        if (stream.Length < 12 || ReadTag(reader) != "RIFF")
            throw new InvalidDataException("Cue resource has no RIFF header.");
        long end = reader.ReadUInt32() + 8L;
        if (end > stream.Length || end < 12 || ReadTag(reader) != "WAVE")
            throw new InvalidDataException("Cue resource has an invalid WAV size.");
        bool hasFormat = false;
        long dataOffset = 0;
        uint dataBytes = 0;
        while (stream.Position < end)
        {
            if (end - stream.Position < 8)
                throw new InvalidDataException("Incomplete WAV chunk header.");
            string tag = ReadTag(reader);
            uint bytes = reader.ReadUInt32();
            long next = stream.Position + bytes + (bytes & 1);
            if (next > end) throw new InvalidDataException("WAV chunk exceeds its resource.");
            if (tag == "fmt ")
            {
                if (hasFormat || bytes < 16 || reader.ReadUInt16() != PcmFormat ||
                    reader.ReadUInt16() != MonoChannels || reader.ReadUInt32() != NaturalCueBank.SampleRate ||
                    reader.ReadUInt32() != NaturalCueBank.SampleRate * BytesPerSample ||
                    reader.ReadUInt16() != BytesPerSample || reader.ReadUInt16() != BitsPerSample)
                    throw new InvalidDataException("Cue requires mono PCM16 at 48 kHz.");
                hasFormat = true;
            }
            else if (tag == "data")
            {
                if (dataOffset != 0 || bytes == 0 || bytes % BytesPerSample != 0 ||
                    bytes / BytesPerSample > maximumFrames)
                    throw new InvalidDataException("Cue data is empty, duplicated, incomplete or too long.");
                dataOffset = stream.Position;
                dataBytes = bytes;
            }
            stream.Position = next;
        }
        if (!hasFormat || dataOffset == 0)
            throw new InvalidDataException("Cue requires format and sample data chunks.");
        stream.Position = dataOffset;
        var samples = new float[dataBytes / BytesPerSample];
        for (int frame = 0; frame < samples.Length; frame++)
            samples[frame] = reader.ReadInt16() / 32768f;
        return samples;
    }

    private static string ReadTag(BinaryReader reader) => Encoding.ASCII.GetString(reader.ReadBytes(4));
}
