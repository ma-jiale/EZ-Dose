using DA_Assets.Extensions;
using DA_Assets.Networking;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DA_Assets.FCU
{
    public static class OtherExtensions
    {
        public static async Task WriteLog(this DARequest request, UnityHttpClient webRequest)
        {
            FileInfo[] fileInfos = new DirectoryInfo(FcuConfig.LogPath).GetFiles($"*.*");

            if (fileInfos.Length >= FcuConfig.LogFilesLimit)
            {
                foreach (FileInfo file in fileInfos)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {

                    }
                }
            }

            string logFileName = $"{DateTime.Now.ToString(FcuConfig.DateTimeFormat1)}_{FcuConfig.WebLogFileName}";
            string logFilePath = Path.Combine(FcuConfig.LogPath, logFileName);

            string result;

            string text = webRequest.downloadHandler.text;

            JFResult jfr = DAFormatter.Format<string>(text);

            if (jfr.IsValid)
            {
                result = jfr.Json;
            }
            else
            {
                result = text;
            }

            result = $"{request.Query}\n{webRequest.error}\n{result}";

            File.WriteAllText(logFilePath, result);

            await Task.Yield();
        }

        public static bool IsProjectEmpty(this SelectableFObject sf)
        {
            if (sf == null)
                return true;

            if (sf.Id.IsEmpty())
                return true;

            return false;
        }

        public static bool IsScrollContent(this string objectName)
        {
            if (objectName.IsEmpty())
                return false;

            objectName = objectName.ToLower();
            objectName = Regex.Replace(objectName, "[^a-z]", "");
            return objectName == "content";
        }

        public static bool IsScrollViewport(this string objectName)
        {
            if (objectName.IsEmpty())
                return false;

            objectName = objectName.ToLower();
            objectName = Regex.Replace(objectName, "[^a-z]", "");
            return objectName == "viewport";
        }

        public static bool IsInputTextArea(this string objectName)
        {
            if (objectName.IsEmpty())
                return false;

            objectName = objectName.ToLower();
            objectName = Regex.Replace(objectName, "[^a-z]", "");
            return objectName == "textarea";
        }

        public static bool IsCheckmark(this string objectName)
        {
            if (objectName.IsEmpty())
                return false;

            objectName = objectName.ToLower();
            objectName = Regex.Replace(objectName, "[^a-z]", "");
            return objectName == "checkmark";
        }
    }
}