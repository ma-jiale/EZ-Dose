using System;
using System.Reflection;

namespace DA_Assets.Shared
{
    public class PathsRef
    {
        private static Func<string, string> _makeValidFileName;

        public static string MakeValidFileName(string unsafeFileName)
        {
#if UNITY_EDITOR
            if (_makeValidFileName == null)
            {
                var assembly = typeof(UnityEditor.Editor).Assembly;
                var type = assembly.GetType("UnityEditor.Utils.Paths");
                var methodInfo = type.GetMethod("MakeValidFileName",
                    BindingFlags.Static |
                    BindingFlags.Public |
                    BindingFlags.NonPublic);

                _makeValidFileName = (Func<string, string>)Delegate.CreateDelegate(typeof(Func<string, string>), methodInfo);
            }
#endif
            return _makeValidFileName(unsafeFileName);
        }
    }
}