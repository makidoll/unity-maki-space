using System.Threading.Tasks;
using UnityEngine;

namespace Unity_Maki_Space.Scripts.Managers
{
    public class ChunkDataManager : Manager
    {
        public override Task Init()
        {
            return Task.CompletedTask;
        }

        public DataTypes.Block GetWorldBlock(Vector3Int worldPos) 
        {
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
                else
                {
                    return worldPos.y == height - 1 ? DataTypes.Block.Grass : DataTypes.Block.Dirt;
                }
            }
        
            return DataTypes.Block.Air;
        }
    }
}
