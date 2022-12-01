using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity_Maki_Space.Scripts.Managers;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace Unity_Maki_Space.Scripts.Chunks
{
    public enum ChunkStatus
    {
        NeedMeshGen,
        GotMeshGen,
        NeedPhysicsBake,
        GotPhysicsBake,
        Done
    }

    public class MeshAsLists
    {
        public List<Vector3> vertices;
        public List<Vector3> normals;
        public List<int> triangles;
        public List<Vector2> uv;
        public List<Color> colors;
    }

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        public ChunkSystem chunkSystem;
        public Vector2Int chunkPosition;

        private MeshAsLists _meshData;
        private Mesh _mesh;

        private int _meshInstanceId;

        public bool doingExternalThreadWork;
        public ChunkStatus status = ChunkStatus.NeedMeshGen;

        private readonly CancellationTokenSource _cts = new();

        // private Bounds _bounds;
        //
        // private void OnDrawGizmosSelected()
        // {
        //     GizmoUtils.DrawBounds(_bounds);
        // }

        private DataTypes.Block GetBlock(Vector3Int positionInChunk)
        {
            if (
                positionInChunk.x is < 0 or > ChunkSystem.ChunkSize - 1 ||
                positionInChunk.y is < 0 or > ChunkSystem.ChunkHeight - 1 ||
                positionInChunk.z is < 0 or > ChunkSystem.ChunkSize - 1
            )
            {
                return DataTypes.Block.Air;
            }

            var worldPos = positionInChunk + new Vector3Int(
                chunkPosition.x * ChunkSystem.ChunkSize, 0, chunkPosition.y * ChunkSystem.ChunkSize
            );

            return DependencyManager.Instance.ChunkDataManager.GetWorldBlock(worldPos);
        }

        // 0 --- 1
        // |     |
        // 3 --- 2

        private static readonly Dictionary<DataTypes.BlockSide, Vector3[]> SideVerts = new()
        {
            {
                DataTypes.BlockSide.Front,
                new Vector3[] { new(-1, 1, -1), new(1, 1, -1), new(1, -1, -1), new(-1, -1, -1) }
            },
            {
                DataTypes.BlockSide.Back,
                new Vector3[] { new(1, 1, 1), new(-1, 1, 1), new(-1, -1, 1), new(1, -1, 1) }
            },
            {
                DataTypes.BlockSide.Left,
                new Vector3[] { new(-1, 1, 1), new(-1, 1, -1), new(-1, -1, -1), new(-1, -1, 1) }
            },
            {
                DataTypes.BlockSide.Right,
                new Vector3[] { new(1, 1, -1), new(1, 1, 1), new(1, -1, 1), new(1, -1, -1) }
            },
            {
                DataTypes.BlockSide.Top,
                new Vector3[] { new(-1, 1, 1), new(1, 1, 1), new(1, 1, -1), new(-1, 1, -1) }
            },
            {
                DataTypes.BlockSide.Bottom,
                new Vector3[] { new(-1, -1, -1), new(1, -1, -1), new(1, -1, 1), new(-1, -1, 1) }
            },
        };

        private static readonly Dictionary<DataTypes.BlockSide, Vector3> SideNormals = new()
        {
            { DataTypes.BlockSide.Front, new Vector3(0, 0, -1) },
            { DataTypes.BlockSide.Back, new Vector3(0, 0, 1) },
            { DataTypes.BlockSide.Left, new Vector3(-1, 0, 0) },
            { DataTypes.BlockSide.Right, new Vector3(1, 0, 0) },
            { DataTypes.BlockSide.Top, new Vector3(0, 1, 0) },
            { DataTypes.BlockSide.Bottom, new Vector3(0, -1, 0) },
        };

        private static void AddSquareToCubeMesh(
            ref MeshAsLists mesh,
            Vector3Int blockPosition,
            DataTypes.Block block,
            DataTypes.BlockSide blockSide
        )
        {
            if (block == DataTypes.Block.Air) return;

            var sideVertices = SideVerts[blockSide];
            foreach (var index in new[] { 0, 1, 2, 0, 2, 3 })
            {
                mesh.vertices.Add(sideVertices[index] * 0.5f + blockPosition);
                mesh.triangles.Add(mesh.vertices.Count - 1);
                mesh.normals.Add(SideNormals[blockSide]);
            }

            // 4 clockwise coords 
            var blockSideUv =
                DependencyManager.Instance.ChunkMaterialManager.GetBlockSideUv(block, blockSide, blockPosition);

            mesh.uv.Add(blockSideUv[0]);
            mesh.uv.Add(blockSideUv[1]);
            mesh.uv.Add(blockSideUv[2]);
            mesh.uv.Add(blockSideUv[0]);
            mesh.uv.Add(blockSideUv[2]);
            mesh.uv.Add(blockSideUv[3]);

            // grass has vertex color 0,1,0 so that the shader can do grass things
            if (block == DataTypes.Block.Grass && blockSide != DataTypes.BlockSide.Bottom)
            {
                mesh.colors.AddRange(new[]
                {
                    Color.green, Color.green, Color.green, Color.green, Color.green, Color.green
                });
            }
            else
            {
                mesh.colors.AddRange(new[]
                {
                    Color.black, Color.black, Color.black, Color.black, Color.black, Color.black
                });
            }
        }

        private bool IsAirAroundBlock(Vector3Int blockPositionInChunk, DataTypes.BlockSide blockSide)
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

            var queryPositionInChunk = queryOffset + blockPositionInChunk;
            if (queryPositionInChunk.y is < 0 or > ChunkSystem.ChunkHeight - 1) return true; // top and bottom

            const int edge = ChunkSystem.ChunkSize - 1;

            if (queryPositionInChunk.x is >= 0 and <= edge && queryPositionInChunk.z is >= 0 and <= edge)
            {
                return GetBlock(queryPositionInChunk) == DataTypes.Block.Air;
            }

            // outside of chunk
            
            var queryChunkPosition = chunkPosition + new Vector2Int(queryOffset.x, queryOffset.z);
            var queryChunkWorldPosition = new Vector3Int(
                queryChunkPosition.x * ChunkSystem.ChunkSize, 0, queryChunkPosition.y * ChunkSystem.ChunkSize
            );
            
            switch (queryPositionInChunk.x)
            {
                case < 0:
                    return DependencyManager.Instance.ChunkDataManager.GetWorldBlock(
                        new Vector3Int(edge, blockPositionInChunk.y, blockPositionInChunk.z) + queryChunkWorldPosition
                    ) == DataTypes.Block.Air;
                case > edge:
                    return DependencyManager.Instance.ChunkDataManager.GetWorldBlock(
                        new Vector3Int(0, blockPositionInChunk.y, blockPositionInChunk.z) + queryChunkWorldPosition
                    ) == DataTypes.Block.Air;
            }

            switch (queryPositionInChunk.z)
            {
                case < 0:
                    return DependencyManager.Instance.ChunkDataManager.GetWorldBlock(
                        new Vector3Int(blockPositionInChunk.x, blockPositionInChunk.y, edge) + queryChunkWorldPosition
                    ) == DataTypes.Block.Air;
                case > edge:
                    return DependencyManager.Instance.ChunkDataManager.GetWorldBlock(
                        new Vector3Int(blockPositionInChunk.x, blockPositionInChunk.y, 0) + queryChunkWorldPosition
                    ) == DataTypes.Block.Air;
            }

            return GetBlock(queryPositionInChunk) == DataTypes.Block.Air;
        }

        private MeshAsLists GenerateMeshData()
        {
            Profiler.BeginSample("Chunk GenerateMeshData");

            var meshData = new MeshAsLists
            {
                vertices = new List<Vector3>(),
                normals = new List<Vector3>(),
                triangles = new List<int>(),
                uv = new List<Vector2>(),
                colors = new List<Color>(),
            };

            for (var x = 0; x < ChunkSystem.ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSystem.ChunkHeight; y++)
                {
                    for (var z = 0; z < ChunkSystem.ChunkSize; z++)
                    {
                        var position = new Vector3Int(x, y, z);
                        var block = GetBlock(position);

                        if (IsAirAroundBlock(position, DataTypes.BlockSide.Front))
                            AddSquareToCubeMesh(ref meshData, position, block, DataTypes.BlockSide.Front);

                        if (IsAirAroundBlock(position, DataTypes.BlockSide.Back))
                            AddSquareToCubeMesh(ref meshData, position, block, DataTypes.BlockSide.Back);

                        if (IsAirAroundBlock(position, DataTypes.BlockSide.Left))
                            AddSquareToCubeMesh(ref meshData, position, block, DataTypes.BlockSide.Left);

                        if (IsAirAroundBlock(position, DataTypes.BlockSide.Right))
                            AddSquareToCubeMesh(ref meshData, position, block, DataTypes.BlockSide.Right);

                        if (IsAirAroundBlock(position, DataTypes.BlockSide.Top))
                            AddSquareToCubeMesh(ref meshData, position, block, DataTypes.BlockSide.Top);

                        if (IsAirAroundBlock(position, DataTypes.BlockSide.Bottom))
                            AddSquareToCubeMesh(ref meshData, position, block, DataTypes.BlockSide.Bottom);
                    }
                }
            }

            Profiler.EndSample();

            return meshData;
        }

        private void GenerateAllMeshData()
        {
            Profiler.BeginSample("Chunk GenerateAllMeshData");

            _meshData = GenerateMeshData();

            Profiler.EndSample();
        }

        private void CreateMeshes()
        {
            Profiler.BeginSample("Chunk CreateMeshes");

            _mesh = new Mesh
            {
                name = gameObject.name,
                vertices = _meshData.vertices.ToArray(),
                normals = _meshData.normals.ToArray(),
                triangles = _meshData.triangles.ToArray(),
                uv = _meshData.uv.ToArray(),
                colors = _meshData.colors.ToArray(),
                indexFormat = IndexFormat.UInt16
            };
            // _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            // _mesh.Optimize();

            _meshInstanceId = _mesh.GetInstanceID();

            Profiler.EndSample();
        }

        private void PhysicsBake()
        {
            Profiler.BeginSample("Chunk PhysicsBake");

            Physics.BakeMesh(_meshInstanceId, false);

            Profiler.EndSample();
        }

        private void PushMeshes()
        {
            Profiler.BeginSample("Chunk PushMeshes");

            GetComponent<MeshRenderer>().material = DependencyManager.Instance.ChunkMaterialManager.GetAtlasMaterial();

            GetComponent<MeshFilter>().sharedMesh = _mesh;
            GetComponent<MeshCollider>().sharedMesh = _mesh;

            Profiler.EndSample();
        }

        public void ReloadThreadSafe()
        {
            _cts.Cancel();
            doingExternalThreadWork = false;
            status = ChunkStatus.NeedMeshGen;
        }

        public Task DoExternalThreadWork()
        {
            if (doingExternalThreadWork) return Task.CompletedTask;
            doingExternalThreadWork = true;

            return Task.Run(() =>
            {
                switch (status)
                {
                    case ChunkStatus.NeedMeshGen:
                        GenerateAllMeshData();
                        status = ChunkStatus.GotMeshGen;
                        break;
                    case ChunkStatus.NeedPhysicsBake:
                        PhysicsBake();
                        status = ChunkStatus.GotPhysicsBake;
                        break;
                }

                doingExternalThreadWork = false;
            });
        }

        public void DoMainThreadWork()
        {
            switch (status)
            {
                case ChunkStatus.GotMeshGen:
                    CreateMeshes();
                    status = ChunkStatus.NeedPhysicsBake;
                    break;
                case ChunkStatus.GotPhysicsBake:
                    PushMeshes();
                    status = ChunkStatus.Done;
                    break;
            }
        }
    }
}