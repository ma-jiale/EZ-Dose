using UnityEngine;

namespace DA_Assets.DAI
{
    public abstract class LinkerBase<T3> where T3 : UnityEngine.Object
    {
        [SerializeField] protected T3 monoBeh;
        public T3 MonoBeh
        {
            get => monoBeh;
            set => monoBeh = value;
        }

        public virtual void OnLink() { }
    }
}