using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

namespace DA_Assets.FCU.Drawers.CanvasDrawers
{
    [Serializable]
    public class TextDrawer : FcuBase
    {
        [SerializeField] List<FObject> texts = new List<FObject>();
        public List<FObject> Texts => texts;

        public override void Init(FigmaConverterUnity monoBeh)
        {
            base.Init(monoBeh);

            UnityTextDrawer.Init(monoBeh);
            TextMeshDrawer.Init(monoBeh);
        }

        public void ClearTexts()
        {
            texts.Clear();
        }

        public void Draw(FObject fobject)
        {
            if (fobject.Data.GameObject.IsPartOfAnyPrefab() == false)
            {
                if (fobject.Data.GameObject.TryGetComponentSafe(out Graphic oldGraphic))
                {
                    Type curType = monoBeh.GetCurrentTextType();

                    if (oldGraphic.GetType().Equals(curType) == false)
                    {
                        //TODO
                        //oldGraphic.RemoveComponentsDependingOn();
                        oldGraphic.Destroy();
                    }
                }
            }

            if (monoBeh.IsNova())
            {
                this.TextMeshDrawer.DrawNovaTMP(fobject);
            }
            else
            {
                switch (monoBeh.Settings.TextFontsSettings.TextComponent)
                {
                    case TextComponent.TextMeshPro:
                        this.TextMeshDrawer.DrawTMP(fobject);
                        break;
                    case TextComponent.RTL_TextMeshPro:
                        this.TextMeshDrawer.DrawRTL(fobject);
                        break;
                    case TextComponent.UnityEngine_UI_Text:
                        this.UnityTextDrawer.Draw(fobject);
                        break;
                }
            }

            texts.Add(fobject);
        }

        [SerializeField] public UnityTextDrawer UnityTextDrawer = new UnityTextDrawer();
        [SerializeField] public TextMeshDrawer TextMeshDrawer = new TextMeshDrawer();
    }
}