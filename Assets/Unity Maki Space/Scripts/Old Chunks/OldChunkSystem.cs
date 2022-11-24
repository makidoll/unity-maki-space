using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

public class OldChunkSystem : MonoBehaviour
{
    private Dictionary<Vector3Int, OldChunk> chunks = new();
    public List<OldChunk> chunksWithGameObjects = new();

    public PlayerController playerController;

    public OldChunk GetChunk(Vector3Int position)
    {
        if (chunks.ContainsKey(position)) return chunks[position];
        var chunk = new OldChunk(this, position);
        chunks[position] = chunk;
        return chunk;
    }

    [CanBeNull]
    private List<(float, OldChunk)> GetClosestChunksNearPlayer()
    {
        var playerChunkPosition = playerController.GetChunkPosition();
        
        const int viewDistance = 8;

        List<(float, OldChunk)> closestChunks = new();
            
        for (var deltaZ = -viewDistance; deltaZ <= viewDistance; deltaZ++)
        {
            for (var deltaX = -viewDistance; deltaX <= viewDistance; deltaX++)
            {
                var chunkPosition = playerChunkPosition + new Vector3Int(deltaX, 0, deltaZ);
                var chunk = GetChunk(chunkPosition);
                
                var distance = Vector3Int.Distance(playerChunkPosition, chunkPosition);
                if (distance > viewDistance) continue;
                
                closestChunks.Add((distance, chunk));
            }
        }

        return closestChunks;
    }

    private void FilterChunksWithGameObjects(ICollection<OldChunk> chunksToKeep)
    {
        foreach (var chunk in chunksWithGameObjects)
        {
            if (chunksToKeep.Contains(chunk)) continue;
            chunk.RemoveGameObject();
        }
    }

    private void LateUpdate()
    {
        var closestChunks = GetClosestChunksNearPlayer();
        if (closestChunks == null) return;
        
        // delete chunks that arent close by
        FilterChunksWithGameObjects(closestChunks.Select(c => c.Item2).ToArray());

        // sort with closest first
        closestChunks.Sort((a, b) => a.Item1.CompareTo(b.Item1));        
        foreach (var (_, chunk) in closestChunks)
        {
            if (!chunk.needMeshGen) continue;
            chunk.UpdateMeshGen();
            break; // wait till next frame to do the next chunk
        }
    }

    private static int GlslMod(int x, int m)
    {
        return (x % m + m) % m;
    }

    private (OldChunk, Vector3Int)? WorldSpaceToChunkPosition(Vector3Int worldPosition)
    {
        var chunkPosition = new Vector3Int(Mathf.FloorToInt((float) worldPosition.x / OldChunk.ChunkSize), 0,
            Mathf.FloorToInt((float) worldPosition.z / OldChunk.ChunkSize));

        var chunk = chunks[chunkPosition];
        if (chunk == null) return null;

        var blockPosition = new Vector3Int(GlslMod(worldPosition.x, OldChunk.ChunkSize),
            GlslMod(worldPosition.y, OldChunk.ChunkHeight),
            GlslMod(worldPosition.z, OldChunk.ChunkSize));

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
        
        // lets immediately update the mesh so it doesnt flicker
        chunk.UpdateMeshGen(); 
        
        // we need to regenerate the mesh of a nearby chunk if we're on the edge
        
        const int edge = OldChunk.ChunkSize - 1;
        if (positionInChunk.x == 0) NeedsMeshGen(worldPosition + new Vector3Int(-1, 0, 0));
        if (positionInChunk.z == 0) NeedsMeshGen(worldPosition + new Vector3Int(0, 0, -1));
        if (positionInChunk.x == edge) NeedsMeshGen(worldPosition + new Vector3Int(1, 0, 0));
        if (positionInChunk.z == edge) NeedsMeshGen(worldPosition + new Vector3Int(0, 0, 1));
    }
};