using Unity_Maki_Space.Scripts.Chunks;
using UnityEngine;

namespace Unity_Maki_Space.Scripts
{
    public static class Utils
    {
        public static int GlslMod(int x, int m)
        {
            return (x % m + m) % m;
        }

        public static Vector2Int WorldPosToChunkPos(Vector3Int worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt((float)worldPos.x / ChunkSystem.ChunkSize),
                Mathf.FloorToInt((float)worldPos.z / ChunkSystem.ChunkSize)
            );
        }

        public static Vector3Int WorldPosToPosInChunk(Vector3Int worldPos)
        {
            return new Vector3Int(
                GlslMod(worldPos.x, ChunkSystem.ChunkSize),
                worldPos.y,
                GlslMod(worldPos.z, ChunkSystem.ChunkSize)
            );
        }
    }
}