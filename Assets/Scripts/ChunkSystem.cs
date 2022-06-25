using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkSystem : MonoBehaviour
{
    private MaterialLoader materialLoader;

    private void Awake()
    {
        materialLoader = new MaterialLoader();
        new Chunk(materialLoader, Vector3Int.zero).MakeChunkGameObject();
        for (var x = -1; x <= 1; x++)
        {
            for (var z = -1; z <= 1; z++)
            {
                new Chunk(
                    materialLoader, 
                    new Vector3Int(x, 0, z)
                ).MakeChunkGameObject();
            }
        }
    }
};