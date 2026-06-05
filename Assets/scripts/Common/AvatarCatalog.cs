using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class AvatarCatalog
{
    private static readonly List<Sprite> CachedAvatars = new List<Sprite>();
    private static bool isLoaded;

    public static int Count
    {
        get
        {
            EnsureLoaded();
            return CachedAvatars.Count;
        }
    }

    public static IReadOnlyList<Sprite> GetAll()
    {
        EnsureLoaded();
        return CachedAvatars;
    }

    public static void ForceReload()
    {
        isLoaded = false;
        EnsureLoaded();
    }

    public static Sprite GetAt(int index)
    {
        EnsureLoaded();

        if (CachedAvatars.Count == 0)
        {
            return null;
        }

        int wrappedIndex = ((index % CachedAvatars.Count) + CachedAvatars.Count) % CachedAvatars.Count;
        return CachedAvatars[wrappedIndex];
    }

    public static Sprite GetByName(string avatarName)
    {
        EnsureLoaded();
        if (string.IsNullOrWhiteSpace(avatarName))
        {
            return null;
        }

        string normalized = avatarName.Trim();
        return CachedAvatars.FirstOrDefault(sprite =>
            sprite != null &&
            string.Equals(sprite.name, normalized, System.StringComparison.OrdinalIgnoreCase));
    }

    public static bool TryGetIndexByName(string avatarName, out int index)
    {
        EnsureLoaded();
        index = -1;

        if (string.IsNullOrWhiteSpace(avatarName))
        {
            return false;
        }

        string normalized = avatarName.Trim();
        for (int i = 0; i < CachedAvatars.Count; i++)
        {
            Sprite sprite = CachedAvatars[i];
            if (sprite == null)
            {
                continue;
            }

            if (string.Equals(sprite.name, normalized, System.StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                return true;
            }
        }

        return false;
    }

    private static void EnsureLoaded()
    {
        bool cacheIsInvalid = CachedAvatars.Count == 0 || CachedAvatars.Any(sprite => sprite == null);
        if (isLoaded && !cacheIsInvalid)
        {
            return;
        }

        CachedAvatars.Clear();

#if UNITY_EDITOR
        LoadFromImagesFolder();
#else
        LoadFromResources();
#endif

        isLoaded = true;
    }

#if UNITY_EDITOR
    private static void LoadFromImagesFolder()
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/images/Avatars" });
        
        if (guids.Length == 0)
        {
            LoadFromResources();
            return;
        }

        List<Sprite> loadedSprites = new List<Sprite>();
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            
            if (sprite != null && !sprite.name.Equals("bot", System.StringComparison.OrdinalIgnoreCase))
            {
                loadedSprites.Add(sprite);
            }
        }

        CachedAvatars.AddRange(loadedSprites
            .OrderBy(sprite => int.TryParse(sprite.name, out int order) ? order : int.MaxValue)
            .ThenBy(sprite => sprite.name));
    }
#endif

    private static void LoadFromResources()
    {
        Sprite[] resourceAvatars = Resources.LoadAll<Sprite>("Avatars");
        
        if (resourceAvatars != null && resourceAvatars.Length > 0)
        {
            CachedAvatars.AddRange(resourceAvatars
                .Where(sprite => sprite != null && !sprite.name.StartsWith("bot", System.StringComparison.OrdinalIgnoreCase))
                .OrderBy(sprite => {
                    string name = sprite.name;
                    int underscoreIndex = name.IndexOf('_');
                    if (underscoreIndex > 0)
                    {
                        name = name.Substring(0, underscoreIndex);
                    }
                    return int.TryParse(name, out int order) ? order : int.MaxValue;
                })
                .ThenBy(sprite => sprite.name));
        }
    }
}
