using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;

namespace Klrohias.NFast.Resource
{
    public static class Extensions
    {
        public static async Task<AudioClip> GetAudioResource(this IResourceProvider provider, string id)
        {
            var path = await provider.GetResourcePath(id);
            using var request =
                UnityWebRequestMultimedia.GetAudioClip($"file://{path}", AudioType.MPEG);
            await request.SendWebRequest();
            if (!string.IsNullOrEmpty(request.error))
            {
                throw new FileLoadException(request.error);
            }
            return DownloadHandlerAudioClip.GetContent(request);
        }

        public static async Task<Texture2D> GetTextureResource(this IResourceProvider provider, string id)
        {
            var path = await provider.GetResourcePath(id);
            using var request =
                UnityWebRequestTexture.GetTexture($"file://{path}");
            await request.SendWebRequest();
            if (!string.IsNullOrEmpty(request.error))
            {
                throw new FileLoadException(request.error);
            }
            return DownloadHandlerTexture.GetContent(request);
        }
    }
}