using System;
using UnityEngine;

#if VECTOR_GRAPHICS_EXISTS
using Unity.VectorGraphics;
#endif

#if VECTOR_GRAPHICS_EXISTS && UNITY_EDITOR
using Unity.VectorGraphics.Editor;
#endif

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class SVGImporterSettings : FcuBase
    {
#if VECTOR_GRAPHICS_EXISTS && UNITY_EDITOR
        [SerializeField] SVGType svgType = SVGType.VectorSprite;
        public SVGType SvgType { get => svgType; set => svgType = value; }
#endif
        [SerializeField] int gradientResolution = 64;
        public int GradientResolution { get => gradientResolution; set => gradientResolution = value; }

        [SerializeField] Vector2 customPivot = new Vector2(0.5f, 0.5f);
        public Vector2 CustomPivot { get => customPivot; set => customPivot = value; }

        [SerializeField] bool generatePhysicsShape = false;
        public bool GeneratePhysicsShape { get => generatePhysicsShape; set => generatePhysicsShape = value; }

#if UNITY_EDITOR && VECTOR_GRAPHICS_EXISTS
        [SerializeField] Unity.VectorGraphics.ViewportOptions viewportOptions = Unity.VectorGraphics.ViewportOptions.DontPreserve;
        public ViewportOptions ViewportOptions { get => viewportOptions; set => viewportOptions = value; }
#endif

        [SerializeField] float stepDistance = 1f;
        public float StepDistance { get => stepDistance; set => stepDistance = value; }

        [SerializeField] float samplingSteps = 3;
        public float SamplingSteps { get => samplingSteps; set => samplingSteps = value; }

        [SerializeField] bool advancedMode = true;
        public bool AdvancedMode { get => advancedMode; set => advancedMode = value; }

        [SerializeField] bool maxCordDeviationEnabled = false;
        public bool MaxCordDeviationEnabled { get => maxCordDeviationEnabled; set => maxCordDeviationEnabled = value; }

        [SerializeField] float maxCordDeviation = 1;
        public float MaxCordDeviation { get => maxCordDeviation; set => maxCordDeviation = value; }

        [SerializeField] bool maxTangentAngleEnabled = false;
        public bool MaxTangentAngleEnabled { get => maxTangentAngleEnabled; set => maxTangentAngleEnabled = value; }

        [SerializeField] float maxTangentAngle = 5;
        public float MaxTangentAngle { get => maxTangentAngle; set => maxTangentAngle = value; }
    }
}

