using System.Collections.Generic;

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

    public enum Block
    {
        Grass,
        Dirt,
    }

    public static readonly Dictionary<Block, Dictionary<BlockSide, string>> BlockSideTextureNames = new()
    {
        {
            Block.Grass, new Dictionary<BlockSide, string>
            {
                {BlockSide.Top, "assets/minecraft/textures/block/grass_block_top.png"},
                {BlockSide.Left, "assets/minecraft/textures/block/grass_block_side.png"},
                {BlockSide.Right, "assets/minecraft/textures/block/grass_block_side.png"},
                {BlockSide.Front, "assets/minecraft/textures/block/grass_block_side.png"},
                {BlockSide.Back, "assets/minecraft/textures/block/grass_block_side.png"},
                {BlockSide.Bottom, "assets/minecraft/textures/block/dirt.png"},
            }
        },
        {
            Block.Dirt, new Dictionary<BlockSide, string>
            {
                {BlockSide.Top, "assets/minecraft/textures/block/dirt.png"},
                {BlockSide.Left, "assets/minecraft/textures/block/dirt.png"},
                {BlockSide.Right, "assets/minecraft/textures/block/dirt.png"},
                {BlockSide.Front, "assets/minecraft/textures/block/dirt.png"},
                {BlockSide.Back, "assets/minecraft/textures/block/dirt.png"},
                {BlockSide.Bottom, "assets/minecraft/textures/block/dirt.png"},
            }
        }
    };
}