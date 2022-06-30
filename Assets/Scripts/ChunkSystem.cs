using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class ChunkSystem : MonoBehaviour
{
    private Dictionary<Vector3Int, Chunk> chunks = new();

    public Chunk GetChunk(Vector3Int position)
    {
        if (chunks.ContainsKey(position)) return chunks[position];
        var chunk = new Chunk(this, position);
        chunks[position] = chunk;
        return chunk;
    }

    private void Awake()
    {
        for (var x = -2; x <= 2; x++)
        {
            for (var z = -2; z <= 2; z++)
            {
                var position = new Vector3Int(x, 0, z);
                var chunk = GetChunk(position);
                chunk.MakeChunkGameObject();
                chunk.GenerateMesh();
            }
        }
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

    private void GenerateMesh(Vector3Int worldPosition)
    {
        var chunkAndPosition = WorldSpaceToChunkPosition(worldPosition);
        if (chunkAndPosition == null) return;
        chunkAndPosition.Value.Item1.GenerateMesh();
    }
    
    public void SetBlock(Vector3Int worldPosition, DataTypes.Block block)
    {
        var chunkAndPosition = WorldSpaceToChunkPosition(worldPosition);
        if (chunkAndPosition == null) return;

        var chunk = chunkAndPosition.Value.Item1;
        var positionInChunk = chunkAndPosition.Value.Item2;

        chunk.SetBlock(positionInChunk, block);
        chunk.GenerateMesh();
        
        // we need to regenerate the mesh of a nearby chunk if we're on the edge
        
        const int edge = Chunk.ChunkSize - 1;
        if (positionInChunk.x == 0) GenerateMesh(worldPosition + new Vector3Int(-1, 0, 0));
        if (positionInChunk.z == 0) GenerateMesh(worldPosition + new Vector3Int(0, 0, -1));
        if (positionInChunk.x == edge) GenerateMesh(worldPosition + new Vector3Int(1, 0, 0));
        if (positionInChunk.z == edge) GenerateMesh(worldPosition + new Vector3Int(0, 0, 1));
    }
};