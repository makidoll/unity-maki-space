using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Unity_Maki_Space.Scripts.Managers
{
    public class ChunkDataManager : Manager
    {
        // TODO: split this up into chunks maybe. locking makes this stupidly slow too
        
        private readonly Dictionary<Vector3Int, DataTypes.Block> _userPlacedBlocks = new();
        private readonly object _userPlacedBlocksLock = new();

        public Action<Vector3Int, DataTypes.Block> blockChanged = (_, _) => { };

        public override Task Init()
        {
            return Task.CompletedTask;
        }

        public DataTypes.Block GetWorldBlock(Vector3Int worldPos)
        {
            lock (_userPlacedBlocksLock)
            {
                if (_userPlacedBlocks.TryGetValue(worldPos, out var userPlacedBlock))
                {
                    return userPlacedBlock;
                }
            }

            const int tallestHeight = 128;

            const float grassNoiseScale = 1 / 20f;
            const float grassNoiseHeight = 4f;

            const float biomeNoiseScale = 1 / 80f;

            var biomeNoise = Mathf.PerlinNoise(
                worldPos.x * biomeNoiseScale,
                worldPos.z * biomeNoiseScale
            );

            var height = tallestHeight - Mathf.FloorToInt(
                Mathf.PerlinNoise(
                    worldPos.x * grassNoiseScale,
                    worldPos.z * grassNoiseScale
                ) * (grassNoiseHeight + 1f)
            );

            if (worldPos.y < height)
            {
                if (biomeNoise < 0.4)
                {
                    // return = biomeNoise < 0.4 ? DataTypes.Block.Water : DataTypes.Block.Sand;
                    return DataTypes.Block.Sand;
                }

                return worldPos.y == height - 1 ? DataTypes.Block.Grass : DataTypes.Block.Dirt;
            }

            return DataTypes.Block.Air;
        }

        public void SetWorldBlock(Vector3Int worldPos, DataTypes.Block block)
        {
            _userPlacedBlocks[worldPos] = block;
            blockChanged.Invoke(worldPos, block);
        }
    }
}