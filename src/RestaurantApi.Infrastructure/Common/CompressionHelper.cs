using System.IO.Compression;
using System.Text;

namespace RestaurantApi.Infrastructure.Common;

public static class CompressionHelper
{
    public static byte[] Compress(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);

        using var output = new MemoryStream();

        using (var gzip = new GZipStream(output, CompressionLevel.Optimal))
        {
            gzip.Write(bytes, 0, bytes.Length);
        }

        return output.ToArray();
    }

    public static string Decompress(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        return reader.ReadToEnd();
    }
}