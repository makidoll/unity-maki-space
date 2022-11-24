using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity_Maki_Space.Scripts.Chunks
{
    // [ExecuteInEditMode]
    public class ChunkSystem : MonoBehaviour
    {
        [Header("General"), Range(1, 32f)] public int viewDistance = 4;
        public Transform playerCharacterTransform;

        public const int ChunkSize = 16;
        public const int ChunkHeight = 256;

        private readonly Dictionary<Vector2Int, Chunk> _chunks = new();
        private readonly object _chunksLock = new();

        private Vector2Int _lastPlayerPosition = new(999999, 999999);

        private Vector2Int[] _currentSortedChunkPositions = { };
        private readonly object _currentSortedChunkPositionsLock = new();

        private Thread[] _meshGenThreads;
        private bool _externalThreadRunning = true;

        private void OnEnable()
        {
            _externalThreadRunning = true;

            _meshGenThreads = new[]
            {
                new Thread(ExternalThread),
                new Thread(ExternalThread),
                new Thread(ExternalThread),
                new Thread(ExternalThread)
            };

            foreach (var thread in _meshGenThreads)
            {
                thread.Start();
            }

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update += UpdateFn;
            }
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                EditorApplication.update -= UpdateFn;
            }
#endif

            _externalThreadRunning = false;

            foreach (var thread in _meshGenThreads)
            {
                if (thread.IsAlive)
                {
                    thread.Join();
                }
            }

            DeleteAllChunks();
        }

        private void DestroyChunk(Chunk chunk)
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
                Destroy(chunk.gameObject);
            }
            else
            {
                DestroyImmediate(chunk.gameObject);
            }
#else
            Destroy(chunk.gameObject)
#endif
        }

        private void DeleteAllChunks()
        {
            lock (_chunksLock)
            {
                foreach (var chunk in _chunks.Values)
                {
                    DestroyChunk(chunk);
                }

                _chunks.Clear();
            }
        }

        public void ReloadAllChunks()
        {
            lock (_chunksLock)
            {
                foreach (var chunk in _chunks.Values)
                {
                    chunk.ReloadThreadSafe();
                }
            }
        }

        private void OnValidate()
        {
            // when parameters change
            _lastPlayerPosition = new Vector2Int(999, 999);

            // DeleteAllChunks();
            ReloadAllChunks();
        }

        private void SetThreadSafeChunk(Vector2Int position, Chunk chunk)
        {
            lock (_chunksLock)
            {
                _chunks[position] = chunk;
            }
        }

        public Chunk GetThreadSafeChunk(Vector2Int position)
        {
            lock (_chunksLock)
            {
                return _chunks.TryGetValue(position, out var chunk) ? chunk : null;
            }
        }


        private Vector2Int GetPlayerChunkPosition()
        {
            var playerPos = playerCharacterTransform.position;
            const float halfAChunk = ChunkSize * 0.5f;
            return new Vector2Int(
                Mathf.FloorToInt(playerPos.x + halfAChunk) / ChunkSize,
                Mathf.FloorToInt(playerPos.z + halfAChunk) / ChunkSize
            );
        }

        private Vector2Int[] GetSpiralChunkPositionsAroundPlayer(Vector2Int playerChunkPosition)
        {
            // could be optimized but works pretty well for now

            var chunkPositions = new List<(Vector2Int, float)>();

            for (var deltaZ = -viewDistance; deltaZ < viewDistance; deltaZ++)
            {
                for (var deltaX = -viewDistance; deltaX < viewDistance; deltaX++)
                {
                    var chunkPosition = playerChunkPosition + new Vector2Int(deltaX, deltaZ);
                    var chunkDistance = Vector2.Distance(playerChunkPosition, chunkPosition);
                    if (chunkDistance < viewDistance)
                    {
                        // in view distance radius
                        chunkPositions.Add((chunkPosition, chunkDistance));
                    }
                }
            }

            return chunkPositions
                .OrderBy(p => p.Item2)
                .Select(tuple => tuple.Item1)
                .ToArray();
        }

        private Chunk CreateChunkGameObject(Vector2Int chunkPosition)
        {
            var position = new Vector3(
                chunkPosition.x * ChunkSize,
                0,
                chunkPosition.y * ChunkSize
            );

            var chunk = new GameObject
            {
                name = $"Chunk {chunkPosition.x},{chunkPosition.y}",
                isStatic = true,
                transform =
                {
                    position = position,
                    parent = transform
                },
                hideFlags = HideFlags.DontSave
                // hideFlags = HideFlags.HideAndDontSave
            };

            var chunkComponent = chunk.AddComponent<Chunk>();
            chunkComponent.chunkSystem = this;
            chunkComponent.chunkPosition = chunkPosition;
            chunkComponent.GenerateChunkDataThreadSafe();

            return chunkComponent;
        }

        private async void ExternalThread()
        {
            while (_externalThreadRunning)
            {
                // find closest chunk that needs work

                Vector2Int[] currentSortedChunkPositions;
                lock (_currentSortedChunkPositionsLock)
                {
                    currentSortedChunkPositions = _currentSortedChunkPositions;
                }

                Chunk chunk = null;

                foreach (var position in currentSortedChunkPositions)
                {
                    var queryChunk = GetThreadSafeChunk(position);
                    if (
                        queryChunk == null ||
                        queryChunk.doingExternalThreadWork ||
                        queryChunk.status is not (ChunkStatus.NeedMeshGen or ChunkStatus.NeedPhysicsBake)
                    ) continue;
                    chunk = queryChunk;
                    break;
                }

                if (chunk == null) continue;

                await chunk.DoExternalThreadWork();
            }
        }

        public void UpdateFn()
        {
            var playerChunkPosition = GetPlayerChunkPosition();
            var playedMovedChunk = false;

            if (playerChunkPosition != _lastPlayerPosition)
            {
                var currentSortedChunkPositions = GetSpiralChunkPositionsAroundPlayer(playerChunkPosition);
                lock (_currentSortedChunkPositionsLock)
                {
                    _currentSortedChunkPositions = currentSortedChunkPositions;
                }

                _lastPlayerPosition = playerChunkPosition;
                playedMovedChunk = true;
            }

            // search around player with view distance

            var canUpdateOneChunk = true;

            foreach (var chunkPosition in _currentSortedChunkPositions)
            {
                var chunk = GetThreadSafeChunk(chunkPosition);
                if (chunk != null)
                {
                    // if thread generated mesh data but it's not been applied yet (has to be done in main thread)
                    if (canUpdateOneChunk && chunk.status is ChunkStatus.GotMeshGen or ChunkStatus.GotPhysicsBake)
                    {
                        chunk.DoMainThreadWork();
                        canUpdateOneChunk = false;
                    }
                }
                else
                {
                    // make a chunk
                    chunk = CreateChunkGameObject(chunkPosition);
                    SetThreadSafeChunk(chunkPosition, chunk);
                }
            }

            // remove chunks not required

            if (playedMovedChunk)
            {
                Chunk[] chunks;
                lock (_chunksLock)
                {
                    chunks = _chunks.Values.ToArray();
                }

                foreach (var chunk in chunks)
                {
                    if (_currentSortedChunkPositions.Contains(chunk.chunkPosition)) continue;
                    DestroyChunk(chunk);
                    lock (_chunksLock)
                    {
                        _chunks.Remove(chunk.chunkPosition);
                    }
                }
            }
        }

        public void Update()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) return;
#endif
            UpdateFn();
        }
    }
}