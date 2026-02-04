using System;
using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public struct UnityFonts
    {
        [SerializeField] List<FontStruct> missing;
        [SerializeField] List<FontStruct> existing;

        public List<FontStruct> Existing { get => existing; set => existing = value; }
        public List<FontStruct> Missing { get => missing; set => missing = value; }
    }
}