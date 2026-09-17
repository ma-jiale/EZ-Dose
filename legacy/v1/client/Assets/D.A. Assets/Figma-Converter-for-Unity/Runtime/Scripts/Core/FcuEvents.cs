using DA_Assets.FCU.Model;
using System;
using UnityEngine;
using UnityEngine.Events;

#pragma warning disable CS0649

namespace DA_Assets.FCU
{
    [Serializable]
    public class FcuEvents : FcuBase
    {
        /// <summary>
        /// Called when the project's json downloading fails. Fires once per import.
        /// </summary>
        [SerializeField] public UnityEvent<FigmaConverterUnity> OnProjectDownloadFail;

        /// <summary>
        /// Called when the project's json downloading is started. Fires once per import.
        /// </summary>
        [SerializeField] public UnityEvent<FigmaConverterUnity> OnProjectDownloadStart;

        /// <summary>
        /// Called when the project's json file has downloaded. Fires once per import.
        /// </summary>
        [SerializeField] public UnityEvent<FigmaConverterUnity> OnProjectDownloaded;

        /// <summary>
        /// Called when import starts. Fires once per import.
        /// </summary>
        [SerializeField] public UnityEvent<FigmaConverterUnity> OnImportStart;

        /// <summary>
        /// Called after import is successfully complete. Fires once per import.
        /// </summary>
        [SerializeField] public UnityEvent<FigmaConverterUnity> OnImportComplete;

        /// <summary>
        /// Called when import stops due to an error. Fires once per import.
        /// </summary>
        [SerializeField] public UnityEvent<FigmaConverterUnity> OnImportFail;

        /// <summary>
        /// Called when a fobject's GameObject is created on the scene. Called once per GameObject.
        /// </summary>
        [SerializeField] public UnityEvent<FigmaConverterUnity, FObject> OnObjectInstantiate;

        /// <summary>
        /// Called when a component is added to a GameObject based on tag. Called multiple times per GameObject.
        /// </summary>
        [SerializeField] public UnityEvent<FigmaConverterUnity, FObject, FcuTag> OnAddComponent;
    }
}