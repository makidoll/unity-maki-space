using System.Collections;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    private const int UiScale = 3;
    
    private InputActions inputActions;

    private int inventorySelectionPosition = 0;
    private RectTransform inventorySelectionRectTransform;

    private GameObject MakeImageGameObject(string name, Transform parent = null)
    {
        var gameObject = new GameObject(name)
        {
            transform =
            {
                parent = parent ? parent : transform
            }
        };
        gameObject.AddComponent<RectTransform>();
        gameObject.AddComponent<CanvasRenderer>();
        gameObject.AddComponent<Image>();
        return gameObject;
    }

    private GameObject AddCrosshair()
    {
        var gameObject = MakeImageGameObject("Crosshair");

        gameObject.GetComponent<Image>().sprite = DependencyManager.Instance.TextureManager.GetCroppedSprite(
            "assets/minecraft/textures/gui/widgets.png", 
            new Rect(240, 240, 16, 16)
        );
        
        var rectTransform = gameObject.GetComponent<RectTransform>();
        
        rectTransform.sizeDelta = new Vector2(16, 16) * UiScale;
        // cant start perfectly center if we're using a 16x16 texture
        rectTransform.anchoredPosition = new Vector2(0.5f, -0.5f) * UiScale;

        return gameObject;
    }
    
    private GameObject AddInventoryBar()
    {
        var gameObject = MakeImageGameObject("Inventory Bar");

        gameObject.GetComponent<Image>().sprite = DependencyManager.Instance.TextureManager.GetCroppedSprite(
            "assets/minecraft/textures/gui/widgets.png", 
            new Rect(0, 232, 182, 24)
        );
        
        var rectTransform = gameObject.GetComponent<RectTransform>();
        
        rectTransform.sizeDelta = new Vector2(182, 24) * UiScale;
        rectTransform.pivot = new Vector2(0.5f, 0);
        rectTransform.anchorMin = new Vector2(0.5f, 0);
        rectTransform.anchorMax = new Vector2(0.5f, 0);
        rectTransform.anchoredPosition = Vector2.zero;

        return gameObject;
    }

    private GameObject AddInventorySelection(GameObject inventoryBar)
    {
        var gameObject = MakeImageGameObject("Inventory Selection", inventoryBar.transform);

        gameObject.GetComponent<Image>().sprite = DependencyManager.Instance.TextureManager.GetCroppedSprite(
            "assets/minecraft/textures/gui/widgets.png", 
            new Rect(1, 210, 22, 22)
        );
        
        var rectTransform = gameObject.GetComponent<RectTransform>();
        
        rectTransform.sizeDelta = new Vector2(22, 22) * UiScale;
        rectTransform.pivot = new Vector2(0f, 0.5f);
        rectTransform.anchorMin = new Vector2(0, 0.5f);
        rectTransform.anchorMax = new Vector2(0, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        inventorySelectionRectTransform = rectTransform;

        return gameObject;
    }

    private void UpdateInventorySelection()
    {
        inventorySelectionRectTransform.anchoredPosition = new Vector2(20 * inventorySelectionPosition * UiScale, 0);
    }

    private void OnInventoryScroll(InputAction.CallbackContext context)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        var scrollUp = context.ReadValue<float>() > 0;
        inventorySelectionPosition = scrollUp ? inventorySelectionPosition - 1 : inventorySelectionPosition + 1;
        if (inventorySelectionPosition > 8) inventorySelectionPosition = 0;
        if (inventorySelectionPosition < 0) inventorySelectionPosition = 8;
        UpdateInventorySelection();
    }
    
    private void OnInventoryKey(InputAction.CallbackContext context)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        inventorySelectionPosition = (int) Mathf.Floor(context.ReadValue<float>()) - 1;
        if (inventorySelectionPosition > 8) inventorySelectionPosition = 8;
        if (inventorySelectionPosition < 0) inventorySelectionPosition = 0;
        UpdateInventorySelection();

    }
    
    private void Start()
    {
        AddCrosshair();
        
        var inventoryBar = AddInventoryBar();
        AddInventorySelection(inventoryBar);

        inputActions = new InputActions();
        inputActions.UI.Enable();

        inputActions.UI.InventoryScroll.performed += OnInventoryScroll;
        inputActions.UI.InventoryScroll.Enable();
        
        inputActions.UI.InventoryKey.performed += OnInventoryKey;
        inputActions.UI.InventoryKey.Enable();

    }
}
