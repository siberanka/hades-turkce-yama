using System;
using System.Drawing.Imaging;
using System.IO;
using HadesTurkce;

static class ArtworkGenerator
{
    static void Main(string[] args)
    {
        int[] sizes = { 16, 32, 48, 256 };
        var frames = new byte[sizes.Length][];
        for (int i = 0; i < sizes.Length; i++)
            using (var image = Artwork.Emblem(sizes[i]))
            using (var stream = new MemoryStream()) { image.Save(stream, ImageFormat.Png); frames[i] = stream.ToArray(); }
        using (var writer = new BinaryWriter(File.Create(args[0])))
        {
            writer.Write((ushort)0); writer.Write((ushort)1); writer.Write((ushort)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i])); writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                writer.Write((byte)0); writer.Write((byte)0); writer.Write((ushort)1); writer.Write((ushort)32);
                writer.Write(frames[i].Length); writer.Write(offset); offset += frames[i].Length;
            }
            foreach (var frame in frames) writer.Write(frame);
        }
    }
}
