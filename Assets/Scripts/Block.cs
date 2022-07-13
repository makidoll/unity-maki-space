using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct Block
{
    public readonly DataTypes.Block block;

    // int is either 0, 1, 2, 3
    public readonly Dictionary<DataTypes.BlockSide, int> blockSideRotations;

    public Block(DataTypes.Block blockToSet)
    {
        block = blockToSet;
        blockSideRotations = null;

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