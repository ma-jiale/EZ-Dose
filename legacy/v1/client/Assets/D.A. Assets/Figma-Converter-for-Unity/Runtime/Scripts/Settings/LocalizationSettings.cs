using DA_Assets.DAI;
using DA_Assets.Tools;
using System;
using System.IO;
using UnityEngine;

#pragma warning disable CS0649

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class LocalizationSettings : FcuBase
    {
        [SerializeField] LocalizationComponent locComponent = LocalizationComponent.None;
        public LocalizationComponent LocalizationComponent
        {
            get => locComponent;
            set => locComponent = value;
        }

        [SerializeField] public LocalizationKeyCaseType LocKeyCaseType = LocalizationKeyCaseType.snake_case;

        [SerializeField] string currentFigmaLayoutCulture = FcuConfig.DefaultLocalizationCulture;
        public string CurrentFigmaLayoutCulture { get => currentFigmaLayoutCulture; set => currentFigmaLayoutCulture = value; }

        [SerializeField] int locKeyMaxLenght = 24;
        public int LocKeyMaxLenght { get => locKeyMaxLenght; set => locKeyMaxLenght = value; }

        [SerializeField] ScriptableObject localizator;
        public ScriptableObject Localizator { get => localizator; set => localizator = value; }

        [SerializeField] string locFolderPath = Path.Combine("Assets", "Resources", "Localizations");
        public string LocFolderPath { get => locFolderPath; set => locFolderPath = value; }

        [SerializeField] string locFileName = "Localization.csv";
        public string LocFileName { get => locFileName; set => locFileName = value; }

        [SerializeField] CsvSeparator csvSeparator = CsvSeparator.Semicolon;
        public CsvSeparator CsvSeparator { get => csvSeparator; set => csvSeparator = value; }
    }
}
