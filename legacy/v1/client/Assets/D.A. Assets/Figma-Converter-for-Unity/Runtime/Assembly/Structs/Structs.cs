using System;
using System.Collections.Generic;
using UnityEngine;

namespace DA_Assets.FCU
{
    public delegate void Return<T1>(DAResult<T1> result);

    [Serializable]
    public struct WebError
    {
        public Exception exception { get; set; }
        public int status { get; set; }
        public string err { get; set; }

        public WebError(
            int status = 0,
            string message = null,
            Exception ex = null)
        {
            this.status = status;
            this.err = message;
            this.exception = ex;
        }
    }

    public struct RequestHeader
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    [Serializable]
    public struct FcuHierarchy
    {
        public int Index;
        public string Name;
        public string Guid;
    }

    public struct DAResult<T1>
    {
        public bool Success { get; set; }
        public T1 Object { get; set; }
        public WebError Error { get; set; }
    }

    public struct DARequest
    {
        public RequestName Name { get; set; }
        public string Query { get; set; }
        public RequestType RequestType { get; set; }
        public RequestHeader RequestHeader { get; set; }
        public WWWForm WWWForm { get; set; }
    }

    [Serializable]
    public struct VectorMaterials
    {
        public Material UnlitVector;
        public Material UnlitVectorGradient;
        public Material UnlitVectorGradientUI;
        public Material UnlitVectorUI;
    }

    [Serializable]
    public struct FRect
    {
        [SerializeField] public Vector2 position;
        [SerializeField] public Vector2 size;
        [SerializeField] public RectOffsetCustom padding;
        [SerializeField] public float angle;
        [SerializeField] public float absoluteAngle;
    }

    public struct EffectHashData
    {
        private string name;
        public string Name => name;

        private List<FieldHashData> data;
        public List<FieldHashData> Data => data;

        public EffectHashData(string name, List<FieldHashData> data)
        {
            this.name = name;
            this.data = data;
        }
    }

    public struct FieldHashData
    {
        private string name;
        public string Name => name;

        private object data;
        public object Data => data;

        public FieldHashData(string name, object data)
        {
            this.name = name;
            this.data = data;
        }
    }

    [Serializable]
    public struct FontMetadata
    {
        [SerializeField] public string Family;
        [SerializeField] public int Weight;
        [SerializeField] public FontStyle FontStyle;
    }
}