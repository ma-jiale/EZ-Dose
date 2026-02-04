using System;
using System.Reflection;
using UnityEngine;
using DA_Assets.Shared.Extensions;

namespace DA_Assets.Tools
{
    public class URIHelpersRef
    {
        private static Type _uriHelpersType;
        private static MethodInfo _makeAssetUriMethod;

        /// <summary>
        /// https://github.com/Unity-Technologies/UnityCsReference/blob/b1c78d185a6f77aee1c1da32db971eaf1006f83e/Editor/Mono/UIElements/StyleSheets/URIHelpers.cs
        /// </summary>
        public static string MakeAssetUri(UnityEngine.Object asset)
        {
            if (asset == null) return null;
            if (_uriHelpersType == null)
            {
                _uriHelpersType = Type.GetType("UnityEditor.UIElements.StyleSheets.URIHelpers, UnityEditor");
            }

            if (_uriHelpersType == null)
            {
                Debug.LogError(SharedLocKey.log_urihelpers_type_not_found.Localize());
                return null;
            }

            if (_makeAssetUriMethod == null)
            {
                _makeAssetUriMethod = _uriHelpersType.GetMethod("MakeAssetUri", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(UnityEngine.Object) }, null);
            }

            if (_makeAssetUriMethod == null)
            {
                Debug.LogError(SharedLocKey.log_makeasseturi_method_missing.Localize());
                return null;
            }

            object result = _makeAssetUriMethod.Invoke(null, new object[] { asset });

            string input = (string)result;

            int index = input.IndexOf("?fileID=");
            if (index >= 0)
                input = input.Substring(0, index);

            return input;
        }
    }
}
