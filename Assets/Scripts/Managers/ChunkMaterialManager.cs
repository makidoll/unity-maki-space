using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class ChunkMaterialManager
{
    private readonly int textureSize;

    private const int AtlasWidth = 8;
    private const int AtlasHeight = 8;
    private readonly Texture2D atlasTexture;
    private readonly Material atlasMaterial;
    private readonly Dictionary<string, Vector2Int> atlasTexturePositions = new();

    private readonly Dictionary<DataTypes.Block, Material> breakParticleMaterials = new();

    public ChunkMaterialManager()
    {
        // get all required textures

        var requiredTexturePaths = new List<string>();
        foreach (var (_, blockInfo) in DataTypes.AllBlockInfo)
        {
            if (blockInfo.Textures == null) continue; // probably air
            foreach (var (_, texture) in blockInfo.Textures)
            {
                if (!requiredTexturePaths.Contains(texture.path))
                {
                    requiredTexturePaths.Add(texture.path);
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

        atlasTexture = new Texture2D(AtlasWidth * textureSize, AtlasHeight * textureSize, TextureFormat.RGB24, false)
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

        atlasMaterial = new Material(Shader.Find("Maki/Block"))
        {
            mainTexture = atlasTexture
        };
    }

    public Material GetAtlasMaterial()
    {
        return atlasMaterial;
    }

    private static Vector2 Rotate(Vector2 point, Vector2 pivot, float deg)
    {
        var theta = Mathf.Deg2Rad * deg;
        return new Vector2
        {
            x = Mathf.Cos(theta) * (point.x - pivot.x) - Mathf.Sin(theta) * (point.y - pivot.y) + pivot.x,
            y = Mathf.Sin(theta) * (point.x - pivot.x) + Mathf.Cos(theta) * (point.y - pivot.y) + pivot.y
        };
    }

    private static Vector2[] RotateUvCoords(Vector2[] uvCoords, Vector2 pivot, float deg)
    {
        return uvCoords.Select(point => Rotate(point, pivot, deg)).ToArray();
    }

    public Vector2[] GetBlockSideUv(DataTypes.Block block, DataTypes.BlockSide blockSide, Vector3Int blockPosition)
    {
        if (!DataTypes.AllBlockInfo.ContainsKey(block))
        {
            return new[] {Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero};
        }

        var texture = DataTypes.AllBlockInfo[block].Textures[blockSide];
        var coords = atlasTexturePositions[texture.path];

        var position = coords / new Vector2(AtlasWidth, AtlasHeight);
        var width = (float) textureSize / atlasTexture.width;
        var height = (float) textureSize / atlasTexture.height;

        // 0 --- 1
        // |     |
        // 3 --- 2
        // coords start at bottom left

        var uvCoords = new[]
        {
            position + new Vector2(0, height),
            position + new Vector2(width, height),
            position + new Vector2(width, 0),
            position,
        };

        if (texture.rotate)
        {
            var fixedRandom = Mathf.PerlinNoise(blockPosition.x * 123.456789f, blockPosition.z * 123.456789f);
            uvCoords = RotateUvCoords(
                uvCoords, position + new Vector2(width, height) * 0.5f,
                Mathf.FloorToInt(fixedRandom * 4) * 90
            );
        }

        return uvCoords;
    }

    public Material GetBreakParticleTexture(DataTypes.Block block)
    {
        if (breakParticleMaterials.ContainsKey(block)) return breakParticleMaterials[block];

        var blockTextures = DataTypes.AllBlockInfo[block].Textures;
        if (blockTextures == null) return null;

        var material = new Material(Shader.Find("Maki/Break Particle"))
        {
            mainTexture =
                DependencyManager.Instance.TextureManager.GetTexture(blockTextures[DataTypes.BlockSide.Top].path)
        };

        breakParticleMaterials[block] = material;

        return material;
    }
}