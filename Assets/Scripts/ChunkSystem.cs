using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ChunkSystem : MonoBehaviour
{
    private MaterialLoader materialLoader;

    private int chunkSize = 16;
    private int chunkHeight = 32;

    // 0 --- 1
    // |     |
    // 3 --- 2

    private static readonly Dictionary<DataTypes.BlockSide, Vector3[]> SideVerts = new()
    {
        {DataTypes.BlockSide.Front, new Vector3[] {new(-1, 1, -1), new(1, 1, -1), new(1, -1, -1), new(-1, -1, -1)}},
        {DataTypes.BlockSide.Back, new Vector3[] {new(1, 1, 1), new(-1, 1, 1), new(-1, -1, 1), new(1, -1, 1)}},
        {DataTypes.BlockSide.Left, new Vector3[] {new(-1, 1, 1), new(-1, 1, -1), new(-1, -1, -1), new(-1, -1, 1)}},
        {DataTypes.BlockSide.Right, new Vector3[] {new(1, 1, -1), new(1, 1, 1), new(1, -1, 1), new(1, -1, -1)}},
        {DataTypes.BlockSide.Top, new Vector3[] {new(-1, 1, 1), new(1, 1, 1), new(1, 1, -1), new(-1, 1, -1)}},
        {DataTypes.BlockSide.Bottom, new Vector3[] {new(-1, -1, -1), new(1, -1, -1), new(1, -1, 1), new(-1, -1, 1)}},
    };

    // private static readonly Vector2[] SideUvs =
    // {
    //     new(0, 1),
    //     new(1, 1),
    //     new(1, 0),
    //     new(0, 1),
    //     new(1, 0),
    //     new(0, 0),
    // };

    private void AddSquareToCubeMesh(ref Mesh mesh, DataTypes.Block block, DataTypes.BlockSide blockSide)
    {
        var vertices = mesh.vertices.ToList();
        var triangles = mesh.triangles.ToList();
        var uv = mesh.uv.ToList();
        var colors = mesh.colors.ToList();

        var sideVertices = SideVerts[blockSide];
        foreach (var index in new[] {0, 1, 2, 0, 2, 3})
        {
            vertices.Add(sideVertices[index]);
            triangles.Add(vertices.Count - 1);
        }

        var blockSideUv = materialLoader.GetBlockSideUv(block, blockSide);
        uv.Add(new Vector2(blockSideUv.xMin, blockSideUv.yMax));
        uv.Add(new Vector2(blockSideUv.xMax, blockSideUv.yMax));
        uv.Add(new Vector2(blockSideUv.xMax, blockSideUv.yMin));
        uv.Add(new Vector2(blockSideUv.xMin, blockSideUv.yMax));
        uv.Add(new Vector2(blockSideUv.xMax, blockSideUv.yMin));
        uv.Add(new Vector2(blockSideUv.xMin, blockSideUv.yMin));

        // grass has vertex color 0,1,0 so that the shader can do grass things
        if (block == DataTypes.Block.Grass && blockSide != DataTypes.BlockSide.Bottom)
        {
            colors.AddRange(new[] {Color.green, Color.green, Color.green, Color.green, Color.green, Color.green});
        }
        else
        {
            colors.AddRange(new[] {Color.black, Color.black, Color.black, Color.black, Color.black, Color.black});
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uv.ToArray();
        mesh.colors = colors.ToArray();
    }

    private void AddChunkGameObject(Vector2 chunkPosition)
    {
        var chunkGameObject = new GameObject($"Chunk{chunkPosition.x},{chunkPosition.y}");
        var meshRenderer = chunkGameObject.AddComponent<MeshRenderer>();
        var meshFilter = chunkGameObject.AddComponent<MeshFilter>();

        var mesh = meshFilter.mesh;

        AddSquareToCubeMesh(ref mesh, DataTypes.Block.Grass, DataTypes.BlockSide.Front);
        AddSquareToCubeMesh(ref mesh, DataTypes.Block.Grass, DataTypes.BlockSide.Back);
        AddSquareToCubeMesh(ref mesh, DataTypes.Block.Grass, DataTypes.BlockSide.Left);
        AddSquareToCubeMesh(ref mesh, DataTypes.Block.Grass, DataTypes.BlockSide.Right);
        AddSquareToCubeMesh(ref mesh, DataTypes.Block.Grass, DataTypes.BlockSide.Top);
        AddSquareToCubeMesh(ref mesh, DataTypes.Block.Grass, DataTypes.BlockSide.Bottom);

        var atlasMaterial = materialLoader.GetAtlasMaterial();
        meshRenderer.material = atlasMaterial;

        // for (var y = 0; y < chunkHeight; y++)
        // {
        //     for (var z = 0; z < chunkSize; z++)
        //     {
        //         for (var x = 0; x < chunkSize; x++)
        //         {
        //             Vector3[] cubeVerts =
        //             {
        //                 new Vector3(-1, -1, -1),
        //             };
        //
        //             meshFilter.mesh.vertices
        //         }
        //     }
        // }
    }

    private void Awake()
    {
        materialLoader = new MaterialLoader();
        AddChunkGameObject(Vector2.zero);
    }
};