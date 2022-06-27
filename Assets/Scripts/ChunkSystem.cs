using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class ChunkSystem : MonoBehaviour
{
    private MaterialLoader materialLoader;

    private Dictionary<Vector3Int, Chunk> chunks = new();

    public Chunk GetChunk(Vector3Int position)
    {
        if (chunks.ContainsKey(position)) return chunks[position];
        var chunk = new Chunk(this, materialLoader, position);
        chunks[position] = chunk;
        
        return chunk;
    } 
    
    private void Awake()
    {
        materialLoader = new MaterialLoader();
        for (var x = -3; x <= 3; x++)
        {
            for (var z = -3; z <= 3; z++)
            {
                var position = new Vector3Int(x, 0, z);
                var chunk = GetChunk(position);
                chunk.MakeChunkGameObject();
            }
        }
    }
};