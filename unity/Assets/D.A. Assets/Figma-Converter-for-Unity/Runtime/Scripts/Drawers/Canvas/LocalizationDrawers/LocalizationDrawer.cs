using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Threading;


#if DALOC_EXISTS
using DA_Assets.MiniExcelLibs;
using DA_Assets.MiniExcelLibs.Csv;
#endif

#pragma warning disable CS0649
#pragma warning disable IDE0003

namespace DA_Assets.FCU.Drawers.CanvasDrawers
{
    [Serializable]
    public class LocalizationDrawer : FcuBase
    {
        private Dictionary<string, string> _localizationDictionary = new Dictionary<string, string>();
        public Dictionary<string, string> LocalizationDictionary => _localizationDictionary;

        public override void Init(FigmaConverterUnity monoBeh)
        {
            base.Init(monoBeh);
            this.DALocalizatorDrawer.Init(monoBeh);
#if I2LOC_EXISTS && UNITY_EDITOR
            this.I2LocalizationDrawer.Init(monoBeh);
#endif
        }

        public void ClearLocalization()
        {
            _localizationDictionary.Clear();
            
            switch (monoBeh.Settings.LocalizationSettings.LocalizationComponent)
            {
                case LocalizationComponent.DALocalizator:
                    break;
                case LocalizationComponent.I2Localization:
#if I2LOC_EXISTS && UNITY_EDITOR
                    this.I2LocalizationDrawer.Init();
#endif
                    break;
            }
        }

        public void Draw(FObject fobject)
        {

            string locKey = fobject.Data.Names.LocKey;

            if (locKey.IsEmpty())
                return;

            string text = fobject.GetText();

            if (text.IsEmpty())
                return;

            _localizationDictionary.TryAddValue(locKey, text);

            switch (monoBeh.Settings.LocalizationSettings.LocalizationComponent)
            {
                case LocalizationComponent.DALocalizator:
                    this.DALocalizatorDrawer.Draw(locKey, fobject);
                    break;
                case LocalizationComponent.I2Localization:
#if I2LOC_EXISTS && UNITY_EDITOR
                    this.I2LocalizationDrawer.Draw(locKey, fobject);
#endif
                    break;
            }
        }

        public string SaveTable(CancellationToken token)
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("Key", typeof(string));
            dataTable.Columns.Add(monoBeh.Settings.LocalizationSettings.CurrentFigmaLayoutCulture, typeof(string));

            foreach (var kvp in _localizationDictionary)
            {
                token.ThrowIfCancellationRequested();
                dataTable.Rows.Add(kvp.Key, kvp.Value);
            }

            string folderPath = monoBeh.Settings.LocalizationSettings.LocFolderPath;
            string fileNameNoExt = Path.GetFileNameWithoutExtension(monoBeh.Settings.LocalizationSettings.LocFileName);
            string fileExt = Path.GetExtension(monoBeh.Settings.LocalizationSettings.LocFileName);

            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string[] files = Directory.GetFiles(folderPath, $"{fileNameNoExt}*{fileExt}");
            int fileCount = files.Length;
            string newFileName = $"{fileNameNoExt}-{fileCount + 1}{fileExt}";
            string filePath = Path.Combine(folderPath, newFileName);

#if DALOC_EXISTS
            CsvConfiguration config = new CsvConfiguration()
            {
                Seperator = (char)monoBeh.Settings.LocalizationSettings.CsvSeparator
            };

            token.ThrowIfCancellationRequested();
            MiniExcel.SaveAs(filePath, dataTable, configuration: config);
#endif
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif

            return filePath.RemovePathExtension();
        }

        static string RemoveAssetsAndResourcesFolders(string path)
        {
            string normalizedPath = path.Replace('\\', Path.DirectorySeparatorChar)
                                        .Replace('/', Path.DirectorySeparatorChar);

            string assetsResourcesPrefix = $"Assets{Path.DirectorySeparatorChar}Resources{Path.DirectorySeparatorChar}";
            string assetsPrefix = $"Assets{Path.DirectorySeparatorChar}";

            if (normalizedPath.StartsWith(assetsResourcesPrefix))
            {
                return normalizedPath.Substring(assetsResourcesPrefix.Length);
            }
            else if (normalizedPath.StartsWith(assetsPrefix))
            {
                return normalizedPath.Substring(assetsPrefix.Length);
            }
            else
            {
                return normalizedPath;
            }
        }

        public void SaveAndConnectTable(CancellationToken token)
        {
            string filePath = SaveTable(token);
            Debug.Log(FcuLocKey.log_localization_file_saved.Localize(filePath));  
            ConnectTable(filePath, token);
        }

        internal void ConnectTable(string filePath, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            switch (monoBeh.Settings.LocalizationSettings.LocalizationComponent)
            {
                case LocalizationComponent.DALocalizator:
                    {
                        this.DALocalizatorDrawer.ConnectTable(filePath);
                    }
                    break;
                case LocalizationComponent.I2Localization:
                    {
#if I2LOC_EXISTS && UNITY_EDITOR
                        this.I2LocalizationDrawer.ConnectTable(filePath);
#endif
                    }
                    break;
            }
        }

#if I2LOC_EXISTS && UNITY_EDITOR
        [SerializeField] public I2LocalizationDrawer I2LocalizationDrawer = new I2LocalizationDrawer();
#endif
        [SerializeField] public DALocalizatorDrawer DALocalizatorDrawer = new DALocalizatorDrawer();
    }
}
