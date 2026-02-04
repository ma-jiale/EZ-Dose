using DA_Assets.FCU.Model;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;


#if DABUTTON_EXISTS
using DA_Assets.DAB;
#endif

namespace DA_Assets.FCU.Drawers.CanvasDrawers
{
    [Serializable]
    public class ButtonDrawer : FcuBase
    {
        [SerializeField] List<FObject> buttons = new List<FObject>();
        public List<FObject> Buttons => buttons;

        public override void Init(FigmaConverterUnity monoBeh)
        {
            base.Init(monoBeh);

#if DABUTTON_EXISTS
            DAButtonDrawer.Init(monoBeh);
#endif
            UnityButtonDrawer.Init(monoBeh);
        }

        public void ClearButtons()
        {
            buttons.Clear();
        }

        public void Draw(FObject fobject)
        {
            fobject.Data.ButtonComponent = monoBeh.Settings.ButtonSettings.ButtonComponent;

            switch (monoBeh.Settings.ButtonSettings.ButtonComponent)
            {
#if DABUTTON_EXISTS
                case ButtonComponent.DAButton:
                    {
                        fobject.Data.GameObject.TryAddComponent(out DAButton _);
                    }
                    break;
#endif
                default:
                    {
                        fobject.Data.GameObject.TryAddComponent(out UnityEngine.UI.Button _);
                    }
                    break;
            }

            buttons.Add(fobject);
        }

        public async Task SetTargetGraphics(CancellationToken token)
        {
            foreach (FObject fobject in buttons)
            {
                token.ThrowIfCancellationRequested();

                switch (fobject.Data.ButtonComponent)
                {
#if DABUTTON_EXISTS
                    case ButtonComponent.DAButton:
                        {
                            this.DAButtonDrawer.SetupDAButton(fobject.Data);
                        }
                        break;
#endif
                    default:
                        {
                            this.UnityButtonDrawer.SetupUnityButton(fobject.Data);
                        }
                        break;
                }

                await Task.Yield();
            }
        }

#if DABUTTON_EXISTS
        [SerializeField] public DAButtonDrawer DAButtonDrawer = new DAButtonDrawer();
#endif
        [SerializeField] public UnityButtonDrawer UnityButtonDrawer = new UnityButtonDrawer();
    }
}