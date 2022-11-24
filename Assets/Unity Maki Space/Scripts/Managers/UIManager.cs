using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Unity_Maki_Space.Scripts.Managers
{
    public class UIManager : Manager
    {
        private readonly Canvas _canvas;

        private const int UiScale = 3;

        private UnityMakiSpaceInputActions _inputActions;

        private int _inventorySelectionPosition;
        private RectTransform _inventorySelectionRectTransform;

        public UIManager(Canvas canvas)
        {
            _canvas = canvas;
            _canvas.gameObject.SetActive(true);
        }
        
        public override Task Init()
        {
            AddCrosshair();
        
            var inventoryBar = AddInventoryBar();
            AddInventorySelection(inventoryBar);

            _inputActions = new UnityMakiSpaceInputActions();
            _inputActions.UI.Enable();

            _inputActions.UI.InventoryScroll.performed += OnInventoryScroll;
            _inputActions.UI.InventoryScroll.Enable();
        
            _inputActions.UI.InventoryKey.performed += OnInventoryKey;
            _inputActions.UI.InventoryKey.Enable();
            
            return Task.CompletedTask;
        }

        private GameObject MakeImageGameObject(string name, Transform parent)
        {
            var gameObject = new GameObject(name)
            {
                transform =
                {
                    parent = parent
                }
            };
            gameObject.AddComponent<RectTransform>();
            gameObject.AddComponent<CanvasRenderer>();
            gameObject.AddComponent<Image>();
            return gameObject;
        }
        
        private GameObject AddCrosshair()
        {
            var gameObject = MakeImageGameObject("Crosshair", _canvas.transform);

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
            var gameObject = MakeImageGameObject("Inventory Bar", _canvas.transform);

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

            _inventorySelectionRectTransform = rectTransform;

            return gameObject;
        }
        
        private void UpdateInventorySelection()
        {
            _inventorySelectionRectTransform.anchoredPosition = new Vector2(20 * _inventorySelectionPosition * UiScale, 0);
        }
        
        private void OnInventoryScroll(InputAction.CallbackContext context)
        {
            if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked) return;

            var scrollUp = context.ReadValue<float>() > 0;
            _inventorySelectionPosition = scrollUp ? _inventorySelectionPosition - 1 : _inventorySelectionPosition + 1;
            if (_inventorySelectionPosition > 8) _inventorySelectionPosition = 0;
            if (_inventorySelectionPosition < 0) _inventorySelectionPosition = 8;
            UpdateInventorySelection();
        }
    
        private void OnInventoryKey(InputAction.CallbackContext context)
        {
            if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked) return;

            _inventorySelectionPosition = (int) Mathf.Floor(context.ReadValue<float>()) - 1;
            if (_inventorySelectionPosition > 8) _inventorySelectionPosition = 8;
            if (_inventorySelectionPosition < 0) _inventorySelectionPosition = 0;
            UpdateInventorySelection();

        }
    }
}