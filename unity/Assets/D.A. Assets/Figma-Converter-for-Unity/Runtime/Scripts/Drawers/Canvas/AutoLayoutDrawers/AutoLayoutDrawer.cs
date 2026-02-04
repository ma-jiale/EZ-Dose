using DA_Assets.Extensions;
using DA_Assets.FCU.Model;
using System;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable CS0649

namespace DA_Assets.FCU.Drawers.CanvasDrawers
{
    [Serializable]
    public class AutoLayoutDrawer : FcuBase
    {
        public override void Init(FigmaConverterUnity monoBeh)
        {
            base.Init(monoBeh);

            GridLayoutDrawer.Init(monoBeh);
            VertLayoutDrawer.Init(monoBeh);
            HorLayoutDrawer.Init(monoBeh);
            LayoutElementDrawer.Init(monoBeh);
        }

        public void Draw(FObject fobject)
        {
            if (fobject.Data.FRect.absoluteAngle != 0)
            {
                return;
            }

            foreach (int index in fobject.Data.ChildIndexes)
            {
                if (monoBeh.CurrentProject.TryGetByIndex(index, out FObject child))
                {
                    if (child.Data.FRect.absoluteAngle != 0)
                    {
                        return;
                    }

                    if (child.Data.GameObject != null)
                    {
                        this.LayoutElementDrawer.Draw(child, fobject);
                    }
                }
            }

            if (fobject.Data.GameObject.TryGetComponentSafe(out LayoutGroup oldLayoutGroup))
            {
                oldLayoutGroup.Destroy();
            }

            if (fobject.LayoutWrap == LayoutWrap.WRAP)
            {
                this.GridLayoutDrawer.Draw(fobject);
            }
            else if (fobject.LayoutMode == LayoutMode.HORIZONTAL)
            {
                this.HorLayoutDrawer.Draw(fobject);
            }
            else if (fobject.LayoutMode == LayoutMode.VERTICAL)
            {
                this.VertLayoutDrawer.Draw(fobject);
            }
        }

        [SerializeField] public GridLayoutDrawer GridLayoutDrawer = new GridLayoutDrawer();
        [SerializeField] public VertLayoutDrawer VertLayoutDrawer = new VertLayoutDrawer();
        [SerializeField] public HorLayoutDrawer HorLayoutDrawer = new HorLayoutDrawer();
        [SerializeField] public LayoutElementDrawer LayoutElementDrawer = new LayoutElementDrawer();
    }
}