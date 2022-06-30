using System;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMaterialManager
{
    private readonly int textureSize;

    private const int AtlasWidth = 8;
    private const int AtlasHeight = 8;
    private readonly Texture2D atlasTexture;
    private readonly Material atlasMaterial;
    private readonly Dictionary<string, Vector2Int> atlasTexturePositions = new();
    
    public ChunkMaterialManager()
    {
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

        var textureManger = DependencyManager.Instance.TextureManager;
        
        // make atlas

        var sampleDirtTexture = textureManger.GetTexture("assets/minecraft/textures/block/dirt.png");

        // should be 16 but you never know right
        textureSize = sampleDirtTexture.width;

        atlasTexture = new Texture2D(AtlasWidth * textureSize, AtlasHeight * textureSize)
        {
            filterMode = FilterMode.Point
        };

        for (var i = 0; i < requiredTexturePaths.Count; i++)
        {
            var texturePath = requiredTexturePaths[i];
            var texture = textureManger.GetTexture(texturePath);
            var x = i % AtlasWidth;
            var y = Mathf.FloorToInt((float) i / AtlasWidth);
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