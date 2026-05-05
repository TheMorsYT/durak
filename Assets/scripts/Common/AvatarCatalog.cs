using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

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

        string[] folders =
        {
            Path.Combine(Application.dataPath, "images", "Avatars"),
            @"E:\Kursova\Durak\Assets\images\Avatars"
        };

        string avatarFolder = folders.FirstOrDefault(Directory.Exists);
        if (string.IsNullOrEmpty(avatarFolder))
        {
            Debug.LogWarning("[AvatarCatalog] Avatar folder not found.");
            isLoaded = false;
            return;
        }

        string[] files = Directory.GetFiles(avatarFolder)
            .Where(filePath =>
            {
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                return extension == ".png" || extension == ".jpg" || extension == ".jpeg";
            })
            .OrderBy(GetOrder)
            .ThenBy(Path.GetFileNameWithoutExtension)
            .ToArray();

        foreach (string filePath in files)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);

                if (!texture.LoadImage(bytes))
                {
                    Object.Destroy(texture);
                    continue;
                }

                texture.name = Path.GetFileNameWithoutExtension(filePath);
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;

                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);

                sprite.name = texture.name;
                CachedAvatars.Add(sprite);
            }
            catch (IOException ioError)
            {
                Debug.LogError($"[AvatarCatalog] Failed to read avatar: {filePath}. {ioError.Message}");
            }
        }

        isLoaded = true;
    }

    private static int GetOrder(string path)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        return int.TryParse(name, out int numericOrder) ? numericOrder : int.MaxValue;
    }
}
