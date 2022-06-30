using UnityEngine;

public class DependencyManager : MonoBehaviour
{
    public static DependencyManager Instance;

    public TextureManager TextureManager;
    public ChunkMaterialManager ChunkMaterialManager;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        
        TextureManager = new TextureManager();
        ChunkMaterialManager = new ChunkMaterialManager();
    }
}
