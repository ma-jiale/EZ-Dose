using DA_Assets.DAI;
using System;
using System.IO;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class ScriptGeneratorSettings : FcuBase
    {
        [SerializeField] FieldSerializationMode serializationMode = FieldSerializationMode.SyncHelpers;
        public FieldSerializationMode SerializationMode { get => serializationMode; set => serializationMode = value; }

        [SerializeField] string @namespace = "MyNamespace";
        public string Namespace { get => @namespace; set => @namespace = value; }

        [SerializeField] string baseClass = nameof(MonoBehaviour);
        public string BaseClass { get => baseClass; set => baseClass = value; }

        [SerializeField] string outputPath = Path.Combine("Assets", "GeneratedScripts");
        public string OutputPath { get => outputPath; set => outputPath = value; }

        [SerializeField] int fieldNameMaxLenght = 16;
        public int FieldNameMaxLenght { get => fieldNameMaxLenght; set => fieldNameMaxLenght = value; }

        [SerializeField] int methodNameMaxLenght = 16;
        public int MethodNameMaxLenght { get => methodNameMaxLenght; set => methodNameMaxLenght = value; }

        [SerializeField] int classNameMaxLenght = 16;
        public int ClassNameMaxLenght { get => classNameMaxLenght; set => classNameMaxLenght = value; }
    }
}