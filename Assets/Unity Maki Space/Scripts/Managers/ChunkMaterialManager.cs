using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity_Maki_Space.Scripts.Managers
{
    public class ChunkMaterialManager : Manager
    {
        private int _textureSize;

        private const int AtlasWidth = 8;
        private const int AtlasHeight = 8;
        private Texture2D _atlasTexture;
        private Material _atlasMaterial;
        private readonly Dictionary<string, Vector2Int> _atlasTexturePositions = new();

        private readonly Dictionary<DataTypes.Block, Material> _breakParticleMaterials = new();

        public override Task Init()
        {
            // get all required textures

            var requiredTexturePaths = new List<string>();
            foreach (var (_, blockInfo) in DataTypes.AllBlockInfo)
            {
                if (blockInfo.Texture != null && !requiredTexturePaths.Contains(blockInfo.Texture.path))
                {
                    requiredTexturePaths.Add(blockInfo.Texture.path);
                }

                if (blockInfo.SideTexture != null && !requiredTexturePaths.Contains(blockInfo.SideTexture.path))
                {
                    requiredTexturePaths.Add(blockInfo.SideTexture.path);
                }

                if (blockInfo.BottomTexture != null && !requiredTexturePaths.Contains(blockInfo.BottomTexture.path))
                {
                    requiredTexturePaths.Add(blockInfo.BottomTexture.path);
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
            _textureSize = sampleDirtTexture.width;

            _atlasTexture = new Texture2D(AtlasWidth * _textureSize, AtlasHeight * _textureSize, TextureFormat.RGB24,
                false)
            {
                filterMode = FilterMode.Point
            };

            for (var i = 0; i < requiredTexturePaths.Count; i++)
            {
                var texturePath = requiredTexturePaths[i];
                var texture = textureManger.GetTexture(texturePath);
                var x = i % AtlasWidth;
                var y = Mathf.FloorToInt((float)i / AtlasWidth);
                _atlasTexturePositions[texturePath] = new Vector2Int(x, y);
                _atlasTexture.SetPixels(x * _textureSize, y * _textureSize, _textureSize, _textureSize,
                    texture.GetPixels());
            }

            _atlasTexture.Apply();

            // make material

            _atlasMaterial = new Material(Shader.Find("Maki/Block"))
            {
                mainTexture = _atlasTexture
            };

            return Task.CompletedTask;
        }

        public Material GetAtlasMaterial()
        {
            return _atlasMaterial;
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
                return new[] { Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero };
            }

            var blockInfo = DataTypes.AllBlockInfo[block];
            var isTopOrBottom = blockSide is DataTypes.BlockSide.Top or DataTypes.BlockSide.Bottom;

            var texture = blockInfo.Texture;
            if (!isTopOrBottom && blockInfo.SideTexture != null)
            {
                texture = blockInfo.SideTexture;
            }

            if (blockSide == DataTypes.BlockSide.Bottom && blockInfo.BottomTexture != null)
            {
                texture = blockInfo.BottomTexture;
            }

            var coords = _atlasTexturePositions[texture.path];
            var position = coords / new Vector2(AtlasWidth, AtlasHeight);
            // TODO: optimize this?
            var width = (float)_textureSize / (AtlasWidth * _textureSize);
            var height = (float)_textureSize / (AtlasHeight * _textureSize);

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

            if (isTopOrBottom)
            {
                var fixedRandom = Mathf.PerlinNoise(blockPosition.x * 123.456789f, blockPosition.z * 123.456789f);
                uvCoords = RotateUvCoords(
                    uvCoords, position + new Vector2(width, height) * 0.5f,
                    Mathf.FloorToInt(fixedRandom * 4) * 90
                );
            }

            return uvCoords;
        }

        public Material GetBreakParticleMaterial(DataTypes.Block block)
        {
            if (_breakParticleMaterials.ContainsKey(block)) return _breakParticleMaterials[block];

            var blockInfo = DataTypes.AllBlockInfo[block];
            var topTexture = blockInfo.Texture;

            if (topTexture == null) return null;

            var material = new Material(Shader.Find("Maki/Break Particle"))
            {
                mainTexture =
                    DependencyManager.Instance.TextureManager.GetTexture(topTexture.path)
            };

            material.SetFloat(Shader.PropertyToID("_IsGrass"), block == DataTypes.Block.Grass ? 1 : 0);

            _breakParticleMaterials[block] = material;

            return material;
        }
    }
}