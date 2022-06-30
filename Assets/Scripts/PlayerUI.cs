using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    private GameObject MakeImageGameObject(string name)
    {
        var gameObject = new GameObject(name)
        {
            transform =
            {
                parent = transform // transform should be a canvas
            }
        };
        gameObject.AddComponent<RectTransform>();
        gameObject.AddComponent<CanvasRenderer>();
        gameObject.AddComponent<Image>();
        return gameObject;
    }

    private void AddCrosshair()
    {
        var gameObject = MakeImageGameObject("Crosshair");

        gameObject.GetComponent<Image>().sprite = DependencyManager.Instance.TextureManager.GetCroppedSprite(
            "assets/minecraft/textures/gui/widgets.png", 
            new Rect(240, 240, 16, 16)
        );
        
        var rectTransform = gameObject.GetComponent<RectTransform>();
        
        const int scale = 3;
        rectTransform.sizeDelta = new Vector2(16 * scale, 16 * scale);
        // cant start perfectly center if we're using a 16x16 texture
        rectTransform.anchoredPosition = new Vector2(0.5f * scale, -0.5f * scale);
    }
    
    private void Start()
    {
        AddCrosshair();
    }
}
