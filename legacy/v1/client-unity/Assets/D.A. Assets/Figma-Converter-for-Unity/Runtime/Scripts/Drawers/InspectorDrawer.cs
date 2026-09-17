using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DA_Assets.FCU.Drawers
{
    [Serializable]
    public class InspectorDrawer : FcuBase
    {
        public Action OnFramesChanged { get; set; }
        public Action OnScrollContentUpdated { get; set; }

        //maxDepth = 2, because [Project (0) > Page (1) > Frame (2)]
        public SelectableFObject FillSelectableFramesArray(FObject document, int maxDepth = 2)
        {
            SelectableFObject doc = new SelectableFObject();

            FillNewSelectableItemRecursively(doc, document, 0, maxDepth);

            bool same = CompareIdsRecursively(_document, doc);

            if (!same)
            {
                _document = doc;
                _document.SetAllSelected(true);
            }

            this.OnScrollContentUpdated?.Invoke();
            this.OnFramesChanged?.Invoke();

            return _document;
        }


        public void FillNewSelectableItemRecursively(SelectableFObject parentItem, FObject parent, int currentDepth, int maxDepth)
        {
            parentItem.Id = parent.Id;
            parentItem.Type = parent.Type;
            parentItem.Name = parent.Name;

            if (currentDepth > maxDepth)
                return;

            if (parent.Children.IsEmpty())
                return;

            foreach (FObject child in parent.Children)
            {
                bool isAllowed = IsAllowed(child, parent);

                if (!isAllowed)
                    continue;

                SelectableFObject childItem = new SelectableFObject();
                FillNewSelectableItemRecursively(childItem, child, currentDepth + 1, maxDepth);
                parentItem.Childs.Add(childItem);
            }
        }

        private bool IsAllowed(FObject fobject, FObject parent)
        {
            monoBeh.TagSetter.TryGetManualTags(fobject, out List<FcuTag> manualTags);

            if (manualTags.Contains(FcuTag.Ignore))
            {
                return false;
            }

            if (!fobject.IsVisible())
            {
                return false;
            }

            if (fobject.Type == NodeType.CANVAS)
            {
                return true;
            }

            if (parent.Type == NodeType.CANVAS)
            {
                return true;
            }

            return false;
        }

        private bool CompareIdsRecursively(SelectableFObject item1, SelectableFObject item2)
        {
            if (item1.Id != item2.Id)
                return false;

            if (item1.Childs.Count != item2.Childs.Count)
                return false;

            for (int i = 0; i < item1.Childs.Count; i++)
            {
                if (!CompareIdsRecursively(item1.Childs[i], item2.Childs[i]))
                    return false;
            }

            return true;
        }

        [SerializeField] SelectableFObject _document = new SelectableFObject();
        public SelectableFObject SelectableDocument => _document;
    }
}