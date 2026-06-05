using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;





public class AvatarBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("[AvatarBuildPreprocessor] Starting avatar synchronization...");

        string sourceFolder = Path.Combine(Application.dataPath, "images", "Avatars");
        string targetFolder = Path.Combine(Application.dataPath, "Resources", "Avatars");

        if (!Directory.Exists(sourceFolder))
        {
            Debug.LogWarning($"[AvatarBuildPreprocessor] Source folder not found: {sourceFolder}");
            return;
        }


        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
            Debug.Log($"[AvatarBuildPreprocessor] Created folder: {targetFolder}");
        }


        string[] pngFiles = Directory.GetFiles(sourceFolder, "*.png", SearchOption.TopDirectoryOnly);
        int copiedCount = 0;

        foreach (string sourceFile in pngFiles)
        {
            string fileName = Path.GetFileName(sourceFile);
            

            if (fileName.Equals("bot.png", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string targetFile = Path.Combine(targetFolder, fileName);


            if (!File.Exists(targetFile) || File.GetLastWriteTime(sourceFile) > File.GetLastWriteTime(targetFile))
            {
                File.Copy(sourceFile, targetFile, true);
                copiedCount++;
                Debug.Log($"[AvatarBuildPreprocessor] Copied: {fileName}");
            }
        }


        AssetDatabase.Refresh();

        Debug.Log($"[AvatarBuildPreprocessor] Synchronization complete. Copied {copiedCount} avatars to Resources/Avatars/");
    }
}
