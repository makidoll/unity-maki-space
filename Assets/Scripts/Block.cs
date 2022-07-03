using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Block
{
    public readonly DataTypes.Block block;

    // 0, 1, 2, 3
    public readonly Dictionary<DataTypes.BlockSide, int> blockSideRotations;

    public Block(DataTypes.Block block)
    {
        this.block = block;

        var textures = DataTypes.AllBlockInfo[block].Textures;
        if (textures == null) return;

        // there are no rotations so dont worry about it
        if (!textures.Any(t => t.Value.rotate)) return;

        blockSideRotations = new Dictionary<DataTypes.BlockSide, int>();
        foreach (var (blockSide, blockTexture) in textures)
        {
            blockSideRotations[blockSide] = blockTexture.rotate ? Random.Range(0, 3) : 0;
        }
    }
}