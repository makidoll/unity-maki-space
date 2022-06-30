using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

public class TextureManager
{
    private readonly ZipArchive archive;
    private readonly Dictionary<string, Texture2D> loadedTextures = new();
    
    public Texture2D GetTexture(string texturePath)
    {
        if (loadedTextures.ContainsKey(texturePath)) return loadedTextures[texturePath];

        var entry = archive.Entries.First(entry => entry.FullName == texturePath);
        var stream = entry.Open();
        var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        stream.Close();

        var texture = new Texture2D(1, 1);
        texture.LoadImage(memoryStream.ToArray());
        texture.filterMode = FilterMode.Point;

        loadedTextures[texturePath] = texture;

        return texture;
    }

    public TextureManager()
    {
        // var resourcePackGuids = AssetDatabase.FindAssets("", new[] {"Assets/Resources/Resource Packs"});
        // if (resourcePackGuids.Length == 0) throw new Exception("No resource packs found");
        //
        // var resourcePackPath = AssetDatabase.GUIDToAssetPath(resourcePackGuids[0])
        //     .Replace("Assets/Resources/", "")
        //     .Replace(".bytes", "");

        var resourcePackBytes = Resources.Load<TextAsset>("Resource Packs/Dandelion+X+1.19b").bytes;
        var resourcePackStream = new MemoryStream(resourcePackBytes);

        archive = new ZipArchive(resourcePackStream, ZipArchiveMode.Read);
    }
}
