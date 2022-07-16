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
        public bool rotate;
    }

    public record BlockInfo
    {
        public string Name;
        [CanBeNull] public BlockTexture Texture;
        [CanBeNull] public Dictionary<BlockSide, BlockTexture> Textures;
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
                Textures = new Dictionary<BlockSide, BlockTexture>
                {
                    {BlockSide.Top, new() {path = TexturesBlockPath + "grass_block_top.png", rotate = true}},
                    {BlockSide.Left, new() {path = TexturesBlockPath + "grass_block_side.png", rotate = false}},
                    {BlockSide.Right, new() {path = TexturesBlockPath + "grass_block_side.png", rotate = false}},
                    {BlockSide.Front, new() {path = TexturesBlockPath + "grass_block_side.png", rotate = false}},
                    {BlockSide.Back, new() {path = TexturesBlockPath + "grass_block_side.png", rotate = false}},
                    {BlockSide.Bottom, new() {path = TexturesBlockPath + "dirt.png", rotate = true}},
                }
            }
        },
        {
            Block.Dirt, new BlockInfo
            {
                Name = "Dirt",
                Texture = new BlockTexture {path = TexturesBlockPath + "dirt.png", rotate = true}
            }
        },
        {
            Block.Sand, new BlockInfo
            {
                Name = "Sand",
                Texture = new BlockTexture {path = TexturesBlockPath + "sand.png", rotate = true}
            }
        }
    };
}