using DA_Assets.Extensions;
using System;
using System.IO;
using System.Linq;
using UnityEngine;
using DA_Assets.Shared.Extensions;
using UnityEngine.SceneManagement;

namespace DA_Assets.Tools
{
    public class SceneBackuper
    {
        public static bool TryBackupActiveScene()
        {
            try
            {
                Scene activeScene = SceneManager.GetActiveScene();
                bool sceneFileExists = File.Exists(activeScene.path);

                string sceneName = activeScene.name;

                if (sceneName.IsEmpty())
                {
                    sceneName = "Untitled";
                }

                int backupNumber = 0;
                string nameWithoutNumber = sceneName;

                if (sceneName.Contains("-"))
                {
                    string[] parts = sceneName.Split('-');

                    if (parts.Length > 1)
                    {
                        if (int.TryParse(parts.Last(), out int _backupNumber))
                        {
                            backupNumber = _backupNumber;
                            nameWithoutNumber = string.Join("-", parts.Take(parts.Length - 1));
                        }
                    }
                }

                string backupsPath = GetBackupsPath();
                backupsPath.CreateFolderIfNotExists();

                string[] backupFiles = Directory.GetFiles(backupsPath, $"{nameWithoutNumber}-*.unity");
                if (backupFiles.Length > 0)
                {
                    foreach (string file in backupFiles)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(file);

                        if (fileName.Contains("-"))
                        {
                            string[] parts = fileName.Split('-');

                            if (parts.Length > 1)
                            {
                                if (int.TryParse(parts.Last(), out int existingBackupNumber))
                                {
                                    if (existingBackupNumber > backupNumber)
                                    {
                                        backupNumber = existingBackupNumber;
                                    }
                                }
                            }
                        }           
                    }
                }

                backupNumber++;

                string newName = $"{nameWithoutNumber}-{backupNumber}.unity";
                string filePath = Path.Combine(backupsPath, newName);

                if (sceneFileExists)
                {
                    File.Copy(activeScene.path, filePath);
                }
                else
                {
#if UNITY_EDITOR
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene, filePath);
#endif
                }

                Debug.Log(SharedLocKey.log_scene_backup_created.Localize(filePath));
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(SharedLocKey.log_scene_backup_failed.Localize(ex.Message));
                return false;
            }
        }

        public static string GetBackupsPath()
        {
            string path = Path.Combine("Library", "Backup", "Scenes");
            return path;
        }

        public static void MakeActiveSceneDirty()
        {
            if (!Application.isPlaying)
            {
#if UNITY_EDITOR
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
#endif
            }
        }
    }
}
