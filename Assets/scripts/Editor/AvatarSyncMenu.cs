using UnityEditor;
using UnityEngine;
using System.IO;





public static class AvatarSyncMenu
{
    [MenuItem("Tools/Sync Avatars to Resources")]
    public static void SyncAvatars()
    {
        Debug.Log("[AvatarSync] Starting manual avatar synchronization...");

        string sourceFolder = Path.Combine(Application.dataPath, "images", "Avatars");
        string targetFolder = Path.Combine(Application.dataPath, "Resources", "Avatars");

        if (!Directory.Exists(sourceFolder))
        {
            EditorUtility.DisplayDialog(
                "Sync Avatars",
                $"Source folder not found:\n{sourceFolder}\n\nPlease ensure avatars are in Assets/images/Avatars/",
                "OK"
            );
            return;
        }


        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
            Debug.Log($"[AvatarSync] Created folder: {targetFolder}");
        }


        string[] pngFiles = Directory.GetFiles(sourceFolder, "*.png", SearchOption.TopDirectoryOnly);
        int copiedCount = 0;
        int skippedCount = 0;

        foreach (string sourceFile in pngFiles)
        {
            string fileName = Path.GetFileName(sourceFile);
            

            if (fileName.Equals("bot.png", System.StringComparison.OrdinalIgnoreCase))
            {
                skippedCount++;
                continue;
            }

            string targetFile = Path.Combine(targetFolder, fileName);


            File.Copy(sourceFile, targetFile, true);
            copiedCount++;
        }


        AssetDatabase.Refresh();

        string message = $"Synchronization complete!\n\n" +
                        $"Copied: {copiedCount} avatars\n" +
                        $"Skipped: {skippedCount} files (bot.png)\n" +
                        $"Target: Assets/Resources/Avatars/";

        EditorUtility.DisplayDialog("Sync Avatars", message, "OK");
        Debug.Log($"[AvatarSync] {message.Replace("\n", " ")}");
    }

    [MenuItem("Tools/Open Avatars Folder")]
    public static void OpenAvatarsFolder()
    {
        string folder = Path.Combine(Application.dataPath, "images", "Avatars");
        
        if (Directory.Exists(folder))
        {
            EditorUtility.RevealInFinder(folder);
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Open Avatars Folder",
                $"Folder not found:\n{folder}",
                "OK"
            );
        }
    }

    [MenuItem("Tools/Validate Avatar Setup")]
    public static void ValidateAvatarSetup()
    {
        string imagesFolder = Path.Combine(Application.dataPath, "images", "Avatars");
        string resourcesFolder = Path.Combine(Application.dataPath, "Resources", "Avatars");
        string botFile = Path.Combine(Application.dataPath, "Resources", "bot.png");

        bool imagesExists = Directory.Exists(imagesFolder);
        bool resourcesExists = Directory.Exists(resourcesFolder);
        bool botExists = File.Exists(botFile);

        int imagesCount = imagesExists ? Directory.GetFiles(imagesFolder, "*.png").Length : 0;
        int resourcesCount = resourcesExists ? Directory.GetFiles(resourcesFolder, "*.png").Length : 0;

        string report = "Avatar Setup Validation:\n\n";
        report += $"✓ Assets/images/Avatars/: {(imagesExists ? $"Found ({imagesCount} files)" : "NOT FOUND")}\n";
        report += $"✓ Assets/Resources/Avatars/: {(resourcesExists ? $"Found ({resourcesCount} files)" : "NOT FOUND")}\n";
        report += $"✓ Assets/Resources/bot.png: {(botExists ? "Found" : "NOT FOUND")}\n\n";

        if (!imagesExists)
        {
            report += "⚠ Warning: Source folder Assets/images/Avatars/ not found!\n";
        }

        if (!resourcesExists || resourcesCount == 0)
        {
            report += "⚠ Warning: No avatars in Resources/Avatars/!\n";
            report += "   Run 'Tools → Sync Avatars to Resources' to fix.\n";
        }

        if (!botExists)
        {
            report += "⚠ Warning: bot.png not found in Resources/!\n";
        }

        if (imagesExists && resourcesExists && imagesCount != resourcesCount)
        {
            report += $"\n⚠ Note: Different file counts ({imagesCount} vs {resourcesCount}).\n";
            report += "   Consider running 'Tools → Sync Avatars to Resources'.\n";
        }

        Debug.Log($"[AvatarValidation] {report.Replace("\n", " ")}");
        EditorUtility.DisplayDialog("Avatar Setup Validation", report, "OK");
    }
}
