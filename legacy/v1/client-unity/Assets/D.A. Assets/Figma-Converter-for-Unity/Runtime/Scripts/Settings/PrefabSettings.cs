using System;
using System.IO;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class PrefabSettings : FcuBase
    {
        [SerializeField] string prefabsPath = Path.Combine("Assets", "Prefabs");
        public string PrefabsPath { get => prefabsPath; set => prefabsPath = value; }

        [SerializeField] TextPrefabNameType textPrefabNameType = TextPrefabNameType.HumanizedColorString;
        public TextPrefabNameType TextPrefabNameType { get => textPrefabNameType; set => textPrefabNameType = value; }
    }
}