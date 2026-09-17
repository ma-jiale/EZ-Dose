using DA_Assets.FCU.Drawers.CanvasDrawers;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Threading;
using DA_Assets.Logging;
using System.Linq;
using DA_Assets.DAI;


#if TextMeshPro
using TMPro;
#endif

#pragma warning disable IDE0003

namespace DA_Assets.FCU.Drawers
{
    [Serializable]
    public class CanvasDrawer : FcuBase
    {
        public override void Init(FigmaConverterUnity monoBeh)
        {
            base.Init(monoBeh);

            ImageDrawer.Init(monoBeh);
            TextDrawer.Init(monoBeh);
            AutoLayoutDrawer.Init(monoBeh);
            ContentSizeFitterDrawer.Init(monoBeh);
            AspectRatioFitterDrawer.Init(monoBeh);
            MaskDrawer.Init(monoBeh);
            ToggleDrawer.Init(monoBeh);
            ButtonDrawer.Init(monoBeh);
            ScriptGenerator.Init(monoBeh);
            InputFieldDrawer.Init(monoBeh);
            ScrollViewDrawer.Init(monoBeh);
            LocalizationDrawer.Init(monoBeh);
            ShadowDrawer.Init(monoBeh);
            CanvasGroupDrawer.Init(monoBeh);
            GameObjectDrawer.Init(monoBeh);
        }

        public async Task DrawToCanvas(List<FObject> fobjects, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            monoBeh.AssetTools.SelectFcu();

            this.TextDrawer.ClearTexts();
            this.ButtonDrawer.ClearButtons();
            this.ToggleDrawer.ClearToggles();
            this.InputFieldDrawer.ClearInputFields();
            this.ScrollViewDrawer.ClearScrollViews();
            this.LocalizationDrawer.ClearLocalization();

            int totalComponents = monoBeh.TagSetter.TagsCounter.Values.Sum();
            totalObjectsCount = 0;

            monoBeh.EditorDelegateHolder.StartProgress?.Invoke(monoBeh, ProgressBarCategory.DrawingComponents, totalComponents, false);
            await DrawComponents(fobjects, DrawByTag, token);
            monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(monoBeh, ProgressBarCategory.DrawingComponents);

            await this.ButtonDrawer.SetTargetGraphics(token);
            await this.ToggleDrawer.SetTargetGraphics(token);
            await this.InputFieldDrawer.SetTargetGraphics(token);
            await this.ScrollViewDrawer.SetTargetGraphics(token);
            this.LocalizationDrawer.SaveAndConnectTable(token);

            await FixAutolayoutMargins(fobjects, token);

            if (monoBeh.IsUGUI())
            {
                if (monoBeh.UsingSpriteRenderer())
                {
                    await monoBeh.CanvasDrawer.FixSpriteRenderers(fobjects, token);
                }
                else if (monoBeh.UsingJoshPui())
                {
                    await monoBeh.CanvasDrawer.FixJoshPui(token);
                }
                else if (monoBeh.UsingDttPui())
                {
                    await monoBeh.CanvasDrawer.FixDttImages(fobjects, token);
                }
            }
        }

        private async Task DrawByTag(FObject fobject, FcuTag tag, Action onDraw)
        {
            try
            {
                if (fobject.Data.GameObject == null)
                {
                    return;
                }

                switch (tag)
                {
                    case FcuTag.Shadow:
                        this.ShadowDrawer.Draw(fobject);
                        break;
                    case FcuTag.AutoLayoutGroup:
                        this.AutoLayoutDrawer.Draw(fobject);
                        break;
                    case FcuTag.ContentSizeFitter:
                        this.ContentSizeFitterDrawer.Draw(fobject);
                        break;
                    case FcuTag.AspectRatioFitter:
                        this.AspectRatioFitterDrawer.Draw(fobject);
                        break;
                    case FcuTag.InputField:
                    case FcuTag.PasswordField:
                        this.InputFieldDrawer.Draw(fobject);
                        break;
                    case FcuTag.ScrollView:
                        this.ScrollViewDrawer.Draw(fobject);
                        break;
                    case FcuTag.Toggle:
                    case FcuTag.ToggleGroup:
                        this.ToggleDrawer.Draw(fobject);
                        break;
                    case FcuTag.Button:
                        this.ButtonDrawer.Draw(fobject);
                        break;
                    case FcuTag.Mask:
                        this.MaskDrawer.Draw(fobject);
                        break;
                    case FcuTag.CanvasGroup:
                        this.CanvasGroupDrawer.Draw(fobject);
                        break;
                    case FcuTag.Placeholder:
                    case FcuTag.Text:
                        this.TextDrawer.Draw(fobject);
                        this.LocalizationDrawer.Draw(fobject);
                        break;
                    case FcuTag.Image:
                        this.ImageDrawer.Draw(fobject);
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(FcuLocKey.log_cant_draw_object.Localize(fobject.Data.NameHierarchy));
                Debug.LogException(ex);
            }

            onDraw.Invoke();
            await Task.Yield();
        }

        public async Task FixSpriteRenderers(List<FObject> fobjects, CancellationToken token)
        {
            List<Transform> frames = monoBeh.transform.GetTopLevelChilds();

            int maxOrder = 32767;

            foreach (Transform frame in frames)
            {
                token.ThrowIfCancellationRequested();
                int initialOrder = 0;
                SetOrderInLayerRecursively(frame, ref initialOrder, token);
            }

            void SetOrderInLayerRecursively(Transform trans, ref int order, CancellationToken token)
            {
                SpriteRenderer spriteRenderer = trans.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    spriteRenderer.sortingOrder = order;
                    order += monoBeh.Settings.SpriteRendererSettings.NextOrderStep;
                    if (order > maxOrder)
                    {
                        order = maxOrder;
                    }
                }

                for (int i = 0; i < trans.childCount; i++)
                {
                    token.ThrowIfCancellationRequested();
                    SetOrderInLayerRecursively(trans.GetChild(i), ref order, token);
                }
            }

            foreach (FObject fobject in fobjects)
            {
                token.ThrowIfCancellationRequested();

                if (fobject.Data.GameObject == null)
                    continue;

                if (!fobject.ContainsTag(FcuTag.Image))
                    continue;

                if (!fobject.Data.GameObject.TryGetComponentSafe(out SpriteRenderer _))
                    continue;

                fobject.Data.GameObject.SetActive(false);
                await Task.Delay(10, token);
                fobject.Data.GameObject.SetActive(true);
            }
        }

        public async Task FixDttImages(List<FObject> fobjects, CancellationToken token)
        {
            foreach (FObject fobject in fobjects)
            {
                token.ThrowIfCancellationRequested();

                if (fobject.Data.GameObject == null)
                    continue;

                if (!fobject.ContainsTag(FcuTag.Image))
                    continue;

#if PROCEDURAL_UI_ASSET_STORE_RELEASE
                if (!fobject.Data.GameObject.TryGetComponentSafe(out DTT.UI.ProceduralUI.GradientEffect _))
                    continue;

#if UNITY_EDITOR
                UnityEditor.Selection.activeGameObject = fobject.Data.GameObject;
#endif
                await Task.Delay(100, token);
#endif
            }

            monoBeh.AssetTools.SelectFcu();
            Scene activeScene = SceneManager.GetActiveScene();
            activeScene.SetExpanded(false);
            await Task.Yield();
        }

        private async Task FixAutolayoutMargins(List<FObject> fobjects, CancellationToken token)
        {
            foreach (FObject fobject in fobjects)
            {
                token.ThrowIfCancellationRequested();

                if (fobject.Data.GameObject == null)
                    continue;

                HorizontalOrVerticalLayoutGroup layoutGroup;

                if (monoBeh.CurrentProject.TryGetByIndex(fobject.Data.ParentIndex, out FObject parent))
                {
                    if (!parent.IsInsideAutoLayout(out layoutGroup))
                    {
                        continue;
                    }
                }
                else
                {
                    continue;
                }

                int leftp = layoutGroup.padding.left;
                int rightp = layoutGroup.padding.right;

                float newX = fobject.Size.x;
                float newY = fobject.Size.y;

                float parentSize = parent.Data.FRect.size.x;

                if (leftp + rightp + newX > parentSize)
                {
                    float excess = (leftp + rightp + newX) - parentSize;
                    float totalPadding = leftp + rightp;

                    float leftFactor = leftp / totalPadding;
                    float rightFactor = rightp / totalPadding;

                    int newLeft = leftp - (int)Math.Floor(excess * leftFactor);
                    int newRight = rightp - (int)Math.Floor(excess * rightFactor);

                    if (newLeft > 0 && newRight > 0)
                    {
                        layoutGroup.padding.left = newLeft;
                        layoutGroup.padding.right = newRight;
                    }
                }
            }

            await Task.Yield();
        }

        public async Task DrawComponents(List<FObject> fobjects, DrawByTag drawByTag, CancellationToken token)
        {
            Array fcuTags = Enum.GetValues(typeof(FcuTag));

            foreach (FcuTag tag in fcuTags)
            {
                token.ThrowIfCancellationRequested();

                if (tag.GetTagConfig().HasComponent == false)
                    continue;

                int drawnObjectsCount = 0;
                int objectsToDrawCount = monoBeh.TagSetter.TagsCounter[tag];

                if (objectsToDrawCount < 1)
                    continue;

                for (int i = 0; i < fobjects.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    if (i % 150 == 0)
                    {
                        await Task.Delay(10, token);
                    }

                    var fobject = fobjects[i];

                    if (fobject.ContainsTag(tag) == false)
                    {
                        continue;
                    }

                    Action onDraw = () =>
                    {
                        drawnObjectsCount++;
                        totalObjectsCount++;
                        monoBeh.EditorDelegateHolder.UpdateProgress?.Invoke(monoBeh, ProgressBarCategory.DrawingComponents, totalObjectsCount);
                        monoBeh.Events.OnAddComponent?.Invoke(monoBeh, fobject, tag);
                    };

                    _ = drawByTag(fobject, tag, onDraw);
                }

                int tempCount = -1;
                while (FcuLogger.WriteLogBeforeEqual(
                    ref drawnObjectsCount,
                    ref objectsToDrawCount,
                    FcuLocKey.log_drawn_count.Localize($"{tag}", drawnObjectsCount, objectsToDrawCount),
                    ref tempCount))
                {
                    await Task.Delay(1000, token);
                }
            }
        }

        // TODO: add referenceResolution
        public void AddCanvasComponent()
        {
            monoBeh.gameObject.TryAddComponent(out Canvas c);
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            if (monoBeh.gameObject.TryGetComponentSafe(out CanvasScaler cs))
                cs.enabled = false;

            monoBeh.gameObject.TryAddComponent(out GraphicRaycaster gr);

            if (MonoBehExtensions.IsExistsOnScene<EventSystem>() == false)
            {
                GameObject go = MonoBehExtensions.CreateEmptyGameObject();
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
                go.name = nameof(EventSystem);
            }
        }

        public async Task FixJoshPui(CancellationToken token)
        {
            List<Transform> frames = monoBeh.transform.GetTopLevelChilds();

            foreach (Transform frame in frames)
            {
                token.ThrowIfCancellationRequested();

                frame.gameObject.SetActive(false);
                await Task.Delay(100, token);
                frame.gameObject.SetActive(true);
            }
        }

        private async Task FixTextSizes(List<FObject> fobjects)
        {
            foreach (FObject fobject in fobjects)
            {
                if (!fobject.ContainsTag(FcuTag.Text))
                    continue;

                if (fobject.Data.GameObject == null)
                    continue;

                RectTransform rt = fobject.Data.GameObject.GetComponent<RectTransform>();

                Vector2 rectSize = new Vector2(rt.rect.width, rt.rect.height);
                Vector2 m = new Vector2((fobject.Size.x - rectSize.x) / 2f, (fobject.Size.y - rectSize.y) / 2f);

                Vector4 marginV4 = new Vector4();

                if (fobject.Size.y > rectSize.y && fobject.Size.x > rectSize.x)
                {
                    marginV4 = new Vector4(m.x, m.y, m.x, m.y);
                }
                else if (fobject.Size.y > rectSize.y)
                {
                    marginV4 = new Vector4(0, m.y, 0, m.y);
                }
                else if (fobject.Size.x > rectSize.x)
                {
                    marginV4 = new Vector4(m.x, 0, m.x, 0);
                }

#if TextMeshPro
                if (fobject.Data.GameObject.TryGetComponentSafe(out TMP_Text text))
                    text.margin = marginV4;
#endif
            }

            await Task.Yield();
        }

        public ImageDrawer ImageDrawer = new ImageDrawer();
        public TextDrawer TextDrawer = new TextDrawer();
        public AutoLayoutDrawer AutoLayoutDrawer = new AutoLayoutDrawer();
        public ContentSizeFitterDrawer ContentSizeFitterDrawer = new ContentSizeFitterDrawer();
        public AspectRatioFitterDrawer AspectRatioFitterDrawer = new AspectRatioFitterDrawer();
        public MaskDrawer MaskDrawer = new MaskDrawer();
        public ToggleDrawer ToggleDrawer = new ToggleDrawer();
        public ButtonDrawer ButtonDrawer = new ButtonDrawer();
        public ScriptGenerator ScriptGenerator = new ScriptGenerator();
        public InputFieldDrawer InputFieldDrawer = new InputFieldDrawer();
        public ScrollViewDrawer ScrollViewDrawer = new ScrollViewDrawer();
        public LocalizationDrawer LocalizationDrawer = new LocalizationDrawer();
        public ShadowDrawer ShadowDrawer = new ShadowDrawer();
        public CanvasGroupDrawer CanvasGroupDrawer = new CanvasGroupDrawer();
        public GameObjectDrawer GameObjectDrawer = new GameObjectDrawer();
        private int totalObjectsCount;
    }
}
