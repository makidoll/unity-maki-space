using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEngine;

public class MaterialLoader
{
    private readonly ZipArchive archive;
    private readonly Dictionary<string, Texture2D> loadedTextures = new();

    private readonly int textureSize;

    private const int AtlasWidth = 8;
    private const int AtlasHeight = 8;
    private readonly Texture2D atlasTexture;
    private readonly Material atlasMaterial;
    private readonly Dictionary<string, Vector2Int> atlasTexturePositions = new();

    private Texture2D GetTexture(string texturePath)
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

    public MaterialLoader()
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

        // get all required textures

        var requiredTexturePaths = new List<string>();
        foreach (var (_, blockSideTextureNames) in DataTypes.BlockSideTextureNames)
        {
            foreach (var (_, textureName) in blockSideTextureNames)
            {
                if (!requiredTexturePaths.Contains(textureName))
                {
                    requiredTexturePaths.Add(textureName);
                }
            }
        }

        if (requiredTexturePaths.Count > AtlasWidth * AtlasHeight)
        {
            throw new Exception("Material atlas size too small for required textures");
        }

        // make atlas

        var sampleDirtTexture = GetTexture("assets/minecraft/textures/block/dirt.png");

        // should be 16 but you never know right
        textureSize = sampleDirtTexture.width;

        atlasTexture = new Texture2D(AtlasWidth * textureSize, AtlasHeight * textureSize)
        {
            filterMode = FilterMode.Point
        };

        for (var i = 0; i < requiredTexturePaths.Count; i++)
        {
            var texturePath = requiredTexturePaths[i];
            var texture = GetTexture(texturePath);
            var x = i % AtlasWidth;
            var y = (int) Mathf.Floor((float) i / AtlasWidth);
            atlasTexturePositions[texturePath] = new Vector2Int(x, y);
            atlasTexture.SetPixels(x * textureSize, y * textureSize, textureSize, textureSize, texture.GetPixels());
        }

        atlasTexture.Apply();

        // make material

        atlasMaterial = new Material(Shader.Find("Maki/Minecraft"))
        {
            mainTexture = atlasTexture
        };
    }

    public Material GetAtlasMaterial()
    {
        return atlasMaterial;
    }

    public Rect GetBlockSideUv(DataTypes.Block block, DataTypes.BlockSide blockSide)
    {
        if (!DataTypes.BlockSideTextureNames.ContainsKey(block)) return Rect.zero;
        
        var texturePath = DataTypes.BlockSideTextureNames[block][blockSide];
        var coords = atlasTexturePositions[texturePath];

        return new Rect(
            (float) coords.x / AtlasWidth,
            (float) coords.y / AtlasHeight,
            (float) textureSize / atlasTexture.width,
            (float) textureSize / atlasTexture.height
        );
    }
}