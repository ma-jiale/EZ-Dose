using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Logging;
using DA_Assets.SVGMeshUnity;
using DA_Assets.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public class SpriteGenerator : FcuBase
    {
        [SerializeField] RectTransform rectTransform;
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] MeshFilter meshFilter;
        [SerializeField] Camera camera;

        private int meshUpscaleFactor = 16;
        private int renderAntialiasing = 8;
        private float blurCoof = 10f;

        private FilterMode filterMode = FilterMode.Bilinear;
        private TextureFormat textureFormat = TextureFormat.ARGB32;
        private RenderTextureFormat renderTextureFormat = RenderTextureFormat.ARGB32;

        public async Task GenerateSprites(List<FObject> fobjects, CancellationToken token)
        {
            List<FObject> generative = fobjects.Where(x => x.Data.NeedGenerate).ToList();

            if (generative.IsEmpty())
                return;

            if (camera == null)
            {
                GameObject cameraGo = MonoBehExtensions.CreateEmptyGameObject();
                cameraGo.name = "SpriteGeneratorCamera";
                cameraGo.TryAddComponent(out camera);
                camera.orthographic = true;
                camera.backgroundColor = new Color(0, 0, 0, 0);
                camera.clearFlags = CameraClearFlags.Color;
            }

            int generatedCount = 0;
            FObject fobject;

            for (int i = 0; i < generative.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                fobject = generative[i];

                _ = GenerateSprite(fobject, () =>
                {
                    generatedCount++;
                });

                await Task.Delay(250);
            }

            while (true)
            {
                Debug.Log(FcuLocKey.log_generating_sprites.Localize(generatedCount, generative.Count));

                if (generatedCount >= generative.Count)
                    break;

                await Task.Delay(1000, token);
            }

            camera.gameObject.Destroy();
        }

        private async Task GenerateSprite(FObject fobject, Action increase)
        {
            FcuLogger.Debug($"GenerateSprites | {fobject.Data.NameHierarchy} | {fobject.Data.NeedGenerate}", FcuDebugSettingsFlags.LogSpriteGenerator);

            try
            {
                Texture2D fillTexture = null;
                Texture2D strokeTexture = null;
                Texture2D finalTexture = null;

                FGraphic graphic = fobject.Data.Graphic;

                if (!graphic.HasFill && graphic.HasStroke && !fobject.ContainsRoundedCorners())
                {
                    IndividualStrokeWeights ind = fobject.IndividualStrokeWeights;

                    if (ind.IsDefault())
                    {
                        ind = new IndividualStrokeWeights
                        {
                            Left = fobject.StrokeWeight,
                            Right = fobject.StrokeWeight,
                            Top = fobject.StrokeWeight,
                            Bottom = fobject.StrokeWeight,
                        };
                    }

                    if (ind.Left != 0 && ind.Left < 1)
                    {
                        ind.Left = 1;
                    }

                    if (ind.Right != 0 && ind.Right < 1)
                    {
                        ind.Right = 1;
                    }

                    if (ind.Top != 0 && ind.Top < 1)
                    {
                        ind.Top = 1;
                    }

                    if (ind.Bottom != 0 && ind.Bottom < 1)
                    {
                        ind.Bottom = 1;
                    }

                    float coof = monoBeh.Settings.ImageSpritesSettings.ImageScale;

                    int texWidth = (int)(fobject.Size.x * coof);
                    int texHeight = (int)(fobject.Size.y * coof);

                    int leftWidth = (int)(ind.Left * coof);
                    int rightWidth = (int)(ind.Right * coof);
                    int topWidth = (int)(ind.Top * coof);
                    int bottomWidth = (int)(ind.Bottom * coof);

                    finalTexture = TextureBorderDrawer.CreateTextureWithBorder(
                        texWidth, texHeight,
                        leftWidth, rightWidth, topWidth, bottomWidth,
                        Color.white);
                }
                else
                {
                    Vector2Int finalSize = default;

                    if (graphic.HasFill)
                    {
                        Vector2 fillSize = fobject.Size;
                        fillSize -= new Vector2Int(1, 0);
                        fillSize.IsSupportedRenderSize(monoBeh.Settings.ImageSpritesSettings.ImageScale, out finalSize, out Vector2Int bakeFillSize);

                        string fillPath = fobject.FillGeometry[0].Path;

                        Color textureColor;

                        if (graphic.HasFill && graphic.HasStroke)
                        {
                            textureColor = graphic.Fill.SolidPaint.Color;
                        }
                        else
                        {
                            textureColor = Color.white;
                        }

                        fillTexture = GenerateTexture(fillPath, fillSize, bakeFillSize, textureColor);
                    }

                    if (graphic.HasStroke)
                    {
                        Vector2 strokeSize = fobject.Size;
                        strokeSize += new Vector2(fobject.StrokeWeight * 2, fobject.StrokeWeight * 2);
                        strokeSize.IsSupportedRenderSize(monoBeh.Settings.ImageSpritesSettings.ImageScale, out finalSize, out Vector2Int bakeStrokeSize);

                        string strokePath = fobject.StrokeGeometry[0].Path;

                        Color textureColor;

                        if (graphic.HasFill && graphic.HasStroke)
                        {
                            textureColor = graphic.Stroke.SolidPaint.Color;
                        }
                        else
                        {
                            textureColor = Color.white;
                        }

                        strokeTexture = GenerateTexture(strokePath, strokeSize, bakeStrokeSize, textureColor);
                    }

                    FcuLogger.Debug($"GenerateSprites | {fobject.Data.NameHierarchy} | hasFills: {graphic.HasFill} | hasStrokes: {graphic.HasStroke}", FcuDebugSettingsFlags.LogSpriteGenerator);

                    if (fillTexture != null && strokeTexture != null)
                    {
                        finalTexture = strokeTexture.Merge(fillTexture);
                    }
                    else if (strokeTexture != null)
                    {
                        finalTexture = strokeTexture;
                    }
                    else if (fillTexture != null)
                    {
                        finalTexture = fillTexture;
                    }

                    if (finalTexture == null)
                    {
                        throw new Exception("finalTexture is null");
                    }

                    finalTexture.Blur(monoBeh.Settings.ImageSpritesSettings.ImageScale / blurCoof);
                    finalTexture.Resize(finalSize, 0, filterMode, renderTextureFormat);
                }

                byte[] textureBytes = finalTexture.EncodeToPNG();
                finalTexture.Destroy();

                File.WriteAllBytes(fobject.Data.SpritePath, textureBytes);
            }
            catch (Exception ex)
            {
                FcuLogger.Debug($"Can't generate '{fobject.Data.NameHierarchy}'\n{ex}", FcuDebugSettingsFlags.LogError);
                fobject.Data.FcuImageType = FcuImageType.Drawable;
            }

            increase.Invoke();
            await Task.Yield();
        }

        public Texture2D GenerateTexture(string svgPath, Vector2 sourceSize, Vector2Int bakeResolution, Color color)
        {
            GameObject meshObject = MonoBehExtensions.CreateEmptyGameObject();
            meshObject.name = sourceSize.ToString();
            meshObject.transform.position = new Vector3(-20000, -20000, 0);

            try
            {
                GenerateMesh(meshObject, sourceSize, svgPath);
                Texture2D bakedTexture = BakeTexture(meshObject, bakeResolution, color);
                return bakedTexture;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                meshObject.Destroy();
            }
        }

        private void GenerateMesh(GameObject meshObject, Vector2 objectSize, string svgPath)
        {
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshRenderer = meshObject.AddComponent<MeshRenderer>();

#if UNITY_EDITOR
            Material spriteMaterial = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
            meshRenderer.material = spriteMaterial;
#endif

            SVGMesh svgMesh = new SVGMesh();
            svgMesh.Init(meshUpscaleFactor);

            SVGData svgData = new SVGData();
            svgData.Path(svgPath);
            svgMesh.Fill(svgData, meshFilter);
        }

        private Texture2D BakeTexture(GameObject meshObject, Vector2Int bakeResolution, Color color)
        {
            RenderTexture renderTexture = RenderTexture.GetTemporary(bakeResolution.x, bakeResolution.y, 8, renderTextureFormat);

            renderTexture.antiAliasing = renderAntialiasing;
            renderTexture.filterMode = filterMode;

            camera.targetTexture = renderTexture;
            camera.SetToObject(meshObject);
            camera.Render();

            Texture2D texture = new Texture2D(bakeResolution.x, bakeResolution.y, textureFormat, false);
            texture.filterMode = filterMode;

            RenderTexture.active = renderTexture;

            texture.ReadPixels(new Rect(0, 0, bakeResolution.x, bakeResolution.y), 0, 0);
            texture.Apply();

            texture.Colorize(color);

            RenderTexture.ReleaseTemporary(renderTexture);
            RenderTexture.active = null;
            camera.targetTexture = null;

            return texture;
        }
    }
}
