using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Chunk
{
    private readonly MaterialLoader materialLoader;

    private const int ChunkSize = 16;
    private const int ChunkHeight = 16;

    private readonly DataTypes.Block[,,] chunkData = new DataTypes.Block[ChunkSize, ChunkHeight, ChunkSize];
    private readonly Vector3Int chunkPosition;
    
    public Chunk(MaterialLoader materialLoader, Vector3Int chunkPosition)
    {
        this.materialLoader = materialLoader;
        this.chunkPosition = chunkPosition;
        this.chunkPosition.y = 0;

        for (var y = 0; y < ChunkHeight; y++)
        {
            if (y >= 8) continue; // keep air
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var z = 0; z < ChunkSize; z++)
                {
                    chunkData[x, y, z] = DataTypes.Block.Grass;
                }
            }
        }

        chunkData[1, 8, 1] = DataTypes.Block.Grass;
        chunkData[1, 12, 1] = DataTypes.Block.Grass;
        chunkData[3, 15, 3] = DataTypes.Block.Grass;
    }

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

    private void AddSquareToCubeMesh(ref Mesh mesh, Vector3Int blockPosition, DataTypes.Block block,
        DataTypes.BlockSide blockSide)
    {
        if (block == DataTypes.Block.Air) return;

        var vertices = mesh.vertices.ToList();
        var triangles = mesh.triangles.ToList();
        var uv = mesh.uv.ToList();
        var colors = mesh.colors.ToList();

        var sideVertices = SideVerts[blockSide];
        foreach (var index in new[] {0, 1, 2, 0, 2, 3})
        {
            vertices.Add(sideVertices[index] * 0.5f + blockPosition);
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

    private bool IsAirAroundBlock(Vector3Int position, DataTypes.BlockSide blockSide)
    {
        var queryOffset = blockSide switch
        {
            DataTypes.BlockSide.Front => new Vector3Int(0, 0, -1),
            DataTypes.BlockSide.Back => new Vector3Int(0, 0, 1),
            DataTypes.BlockSide.Left => new Vector3Int(-1, 0, 0),
            DataTypes.BlockSide.Right => new Vector3Int(1, 0, 0),
            DataTypes.BlockSide.Top => new Vector3Int(0, 1, 0),
            DataTypes.BlockSide.Bottom => new Vector3Int(0, -1, 0),
            _ => Vector3Int.zero
        };

        var queryPosition = queryOffset + position;
        if (queryPosition.x is < 0 or > ChunkSize - 1) return true;
        if (queryPosition.y is < 0 or > ChunkHeight - 1) return true;
        if (queryPosition.z is < 0 or > ChunkSize - 1) return true;

        var queryBlock = chunkData[queryPosition.x, queryPosition.y, queryPosition.z];
        return queryBlock == DataTypes.Block.Air;
    }

    public void MakeChunkGameObject()
    {
        var chunkGameObject = new GameObject($"Chunk{chunkPosition.x},{chunkPosition.y}")
        {
            transform =
            {
                position = chunkPosition * ChunkSize
            }
        };

        var mesh = new Mesh();

        for (var x = 0; x < ChunkSize; x++)
        {
            for (var y = 0; y < ChunkHeight; y++)
            {
                for (var z = 0; z < ChunkSize; z++)
                {
                    var position = new Vector3Int(x, y, z);
                    var block = chunkData[x, y, z];

                    if (IsAirAroundBlock(position, DataTypes.BlockSide.Front))
                        AddSquareToCubeMesh(ref mesh, position, block, DataTypes.BlockSide.Front);
                    if (IsAirAroundBlock(position, DataTypes.BlockSide.Back))
                        AddSquareToCubeMesh(ref mesh, position, block, DataTypes.BlockSide.Back);
                    if (IsAirAroundBlock(position, DataTypes.BlockSide.Left))
                        AddSquareToCubeMesh(ref mesh, position, block, DataTypes.BlockSide.Left);
                    if (IsAirAroundBlock(position, DataTypes.BlockSide.Right))
                        AddSquareToCubeMesh(ref mesh, position, block, DataTypes.BlockSide.Right);
                    if (IsAirAroundBlock(position, DataTypes.BlockSide.Top))
                        AddSquareToCubeMesh(ref mesh, position, block, DataTypes.BlockSide.Top);
                    if (IsAirAroundBlock(position, DataTypes.BlockSide.Bottom))
                        AddSquareToCubeMesh(ref mesh, position, block, DataTypes.BlockSide.Bottom);
                }
            }
        }
        
        mesh.Optimize();

        var meshRenderer = chunkGameObject.AddComponent<MeshRenderer>();
        
        var atlasMaterial = materialLoader.GetAtlasMaterial();
        meshRenderer.material = atlasMaterial;
        
        var meshFilter = chunkGameObject.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

    }
}