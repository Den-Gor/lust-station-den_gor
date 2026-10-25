using System.IO;
using Content.Shared._Lust.Canvas;

#pragma warning disable IDE0130 // Файл расположен в папке Lust, но расширяет долгоживущий клиентский NetTexturesManager.
namespace Content.Client._Sunrise;

public sealed partial class NetTexturesManager
{
    /// <summary>Читает байты PNG из локального кеша NetTextures, не отправляя сетевой запрос.</summary>
    public bool TryGetCachedCanvasImage(string resourcePath, out byte[] data)
    {
        data = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(resourcePath))
            return false;

        var resourceKey = ToResPath(resourcePath).ToString();
        if (!_loadedTextures.ContainsKey(resourceKey))
            return false;

        var uploadedPath = GetUploadedPath(resourcePath);
        if (!_resourceManager.TryContentFileRead(uploadedPath, out var stream))
            return false;

        using (stream)
        {
            if (stream.Length is <= 0 or > CanvasSaveMessage.MaxDataLength)
                return false;

            data = new byte[(int) stream.Length];
            var offset = 0;
            while (offset < data.Length)
            {
                var read = stream.Read(data, offset, data.Length - offset);
                if (read == 0)
                {
                    data = Array.Empty<byte>();
                    return false;
                }

                offset += read;
            }
        }

        return true;
    }
}
