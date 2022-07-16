using System.Collections.Generic;
using JetBrains.Annotations;

public static class DataTypes
{
    public enum BlockSide
    {
        Front,
        Back,
        Left,
        Right,
        Top,
        Bottom
    }

    public record BlockTexture
    {
        public string path;
        // public bool rotate;
    }

    public record BlockInfo
    {
        public string Name;
        public BlockTexture Texture;
        [CanBeNull] public BlockTexture SideTexture;
        [CanBeNull] public BlockTexture BottomTexture;
        // public bool TopBottomTextureRotates = false;
    }

    private const string TexturesBlockPath = "assets/minecraft/textures/block/";

    public enum Block
    {
        Air,
        Grass,
        Dirt,
        Sand
    }

    public static readonly Dictionary<Block, BlockInfo> AllBlockInfo = new()
    {
        {
            Block.Air, new BlockInfo
            {
                Name = "Air",
            }
        },
        {
            Block.Grass, new BlockInfo
            {
                Name = "Grass",
                Texture = new BlockTexture {path = TexturesBlockPath + "grass_block_top.png"},
                SideTexture = new BlockTexture {path = TexturesBlockPath + "grass_block_side.png"},
                BottomTexture = new BlockTexture {path = TexturesBlockPath + "dirt.png"},
            }
        },
        {
            Block.Dirt, new BlockInfo
            {
                Name = "Dirt",
                Texture = new BlockTexture {path = TexturesBlockPath + "dirt.png"}
            }
        },
        {
            Block.Sand, new BlockInfo
            {
                Name = "Sand",
                Texture = new BlockTexture {path = TexturesBlockPath + "sand.png"},
                SideTexture = new BlockTexture {path = TexturesBlockPath + "sand.png"}
            }
        }
    };
}