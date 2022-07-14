using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

public class ChunkSystem : MonoBehaviour
{
    private Dictionary<Vector3Int, Chunk> chunks = new();

    public PlayerController playerController;

    public Chunk GetChunk(Vector3Int position)
    {
        if (chunks.ContainsKey(position)) return chunks[position];
        var chunk = new Chunk(this, position);
        chunks[position] = chunk;
        return chunk;
    }

    [CanBeNull]
    private Chunk GetClosestChunkWithoutMeshNearPlayer()
    {
        var playerChunkPosition = playerController.GetChunkPosition();
        
        const int viewDistance = 2;

        Chunk currentClosestChunk = null;
        float currentClosestChunkDistance = 999;
            
        for (var deltaZ = -viewDistance; deltaZ <= viewDistance; deltaZ++)
        {
            for (var deltaX = -viewDistance; deltaX <= viewDistance; deltaX++)
            {
                var chunkPosition = playerChunkPosition + new Vector3Int(deltaX, 0, deltaZ);
                
                var chunk = GetChunk(chunkPosition);
                if (!chunk.needMeshGen) continue;
                
                var distance = Vector3Int.Distance(playerChunkPosition, chunkPosition);
                if (distance > viewDistance || distance > currentClosestChunkDistance) continue;
                
                currentClosestChunk = chunk;
                currentClosestChunkDistance = distance;
            }
        }

        return currentClosestChunk;
    }

    private void LateUpdate()
    {
        var closestChunk = GetClosestChunkWithoutMeshNearPlayer();
        closestChunk?.UpdateMeshGen();
        // will wait till next frame to do the next chunk
    }

    private static int GlslMod(int x, int m)
    {
        return (x % m + m) % m;
    }

    private (Chunk, Vector3Int)? WorldSpaceToChunkPosition(Vector3Int worldPosition)
    {
        var chunkPosition = new Vector3Int(Mathf.FloorToInt((float) worldPosition.x / Chunk.ChunkSize), 0,
            Mathf.FloorToInt((float) worldPosition.z / Chunk.ChunkSize));

        var chunk = chunks[chunkPosition];
        if (chunk == null) return null;

        var blockPosition = new Vector3Int(GlslMod(worldPosition.x, Chunk.ChunkSize),
            GlslMod(worldPosition.y, Chunk.ChunkHeight),
            GlslMod(worldPosition.z, Chunk.ChunkSize));

        return (chunk, blockPosition);
    }

    public DataTypes.Block GetBlock(Vector3Int worldPosition)
    {
        var chunkAndPosition = WorldSpaceToChunkPosition(worldPosition);
        if (chunkAndPosition == null) return DataTypes.Block.Air;

        var chunk = chunkAndPosition.Value.Item1;
        var position = chunkAndPosition.Value.Item2;
        return chunk.GetBlock(position);
    }

    private void NeedsMeshGen(Vector3Int worldPosition)
    {
        var chunkAndPosition = WorldSpaceToChunkPosition(worldPosition);
        if (chunkAndPosition == null) return;
        chunkAndPosition.Value.Item1.needMeshGen = true;
    }
    
    public void SetBlock(Vector3Int worldPosition, DataTypes.Block block)
    {
        var chunkAndPosition = WorldSpaceToChunkPosition(worldPosition);
        if (chunkAndPosition == null) return;

        var chunk = chunkAndPosition.Value.Item1;
        var positionInChunk = chunkAndPosition.Value.Item2;

        chunk.SetBlock(positionInChunk, block);
        chunk.needMeshGen = true;
        
        // we need to regenerate the mesh of a nearby chunk if we're on the edge
        
        const int edge = Chunk.ChunkSize - 1;
        if (positionInChunk.x == 0) NeedsMeshGen(worldPosition + new Vector3Int(-1, 0, 0));
        if (positionInChunk.z == 0) NeedsMeshGen(worldPosition + new Vector3Int(0, 0, -1));
        if (positionInChunk.x == edge) NeedsMeshGen(worldPosition + new Vector3Int(1, 0, 0));
        if (positionInChunk.z == edge) NeedsMeshGen(worldPosition + new Vector3Int(0, 0, 1));
    }
};