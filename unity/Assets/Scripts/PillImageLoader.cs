using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace EZDose.UI
{
    /// <summary>
    /// Helper class for loading pill images from server.
    /// Used to display pill images during dispensing.
    /// </summary>
    public static class PillImageLoader
    {
        /// <summary>
        /// Load a pill image from the server by its resource ID.
        /// </summary>
        /// <param name="serverUrl">Base server URL (e.g., http://192.168.1.100:5000)</param>
        /// <param name="imageResourceId">Image filename returned from server (e.g., pill_1_1706345678.jpg)</param>
        /// <returns>Loaded Texture2D or null if failed</returns>
        public static async Task<Texture2D> LoadImageAsync(string serverUrl, string imageResourceId)
        {
            if (string.IsNullOrEmpty(serverUrl) || string.IsNullOrEmpty(imageResourceId))
            {
                return null;
            }

            var url = $"{serverUrl.TrimEnd('/')}/static/images/{imageResourceId}";
            
            try
            {
                using (var request = UnityWebRequestTexture.GetTexture(url))
                {
                    request.timeout = 10;
                    
                    var op = request.SendWebRequest();
                    while (!op.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogWarning($"[PillImageLoader] Failed to load image: {request.error}");
                        return null;
                    }

                    var texture = DownloadHandlerTexture.GetContent(request);
                    Debug.Log($"[PillImageLoader] Loaded image: {imageResourceId} ({texture.width}x{texture.height})");
                    return texture;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PillImageLoader] Exception loading image: {e.Message}");
                return null;
            }
        }
    }
}
