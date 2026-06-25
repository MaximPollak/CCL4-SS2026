using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryHudBootstrap : MonoBehaviour
{
    private const string HudCanvasName = "HUDCanvas";
    private const string InventoryTextName = "InventoryText";
    private const string CrosshairName = "Crosshair";

    [Header("References")]
    [SerializeField] private InventorySlot inventorySlot;

    [Header("Text Settings")]
    [SerializeField] private Vector2 anchoredPosition = new Vector2(150f, 20f);
    [SerializeField] private Vector2 size = new Vector2(250f, 50f);
    [SerializeField] private float fontSize = 24f;

    [Header("Crosshair Settings")]
    [SerializeField] private bool createCrosshair = true;
    [SerializeField] private string crosshairText = "+";
    [SerializeField] private float crosshairFontSize = 36f;
    [SerializeField] private Vector2 crosshairSize = new Vector2(50f, 50f);

    private void Awake()
    {
        if (inventorySlot == null)
        {
            inventorySlot = GetComponent<InventorySlot>();
        }

        Canvas canvas = FindHudCanvas();

        if (canvas == null)
        {
            canvas = CreateHudCanvas();
        }

        if (FindFirstObjectByType<InventoryTextUI>() == null)
        {
            CreateInventoryText(canvas.transform);
        }

        if (createCrosshair && GameObject.Find(CrosshairName) == null)
        {
            CreateCrosshair(canvas.transform);
        }
    }

    private Canvas FindHudCanvas()
    {
        GameObject hudCanvasObject = GameObject.Find(HudCanvasName);
        return hudCanvasObject != null ? hudCanvasObject.GetComponent<Canvas>() : null;
    }

    private Canvas CreateHudCanvas()
    {
        GameObject canvasObject = new GameObject(HudCanvasName);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        return canvas;
    }

    private void CreateInventoryText(Transform canvasTransform)
    {
        GameObject textObject = new GameObject(InventoryTextName);
        textObject.transform.SetParent(canvasTransform, false);

        TextMeshProUGUI inventoryText = textObject.AddComponent<TextMeshProUGUI>();
        inventoryText.fontSize = fontSize;
        inventoryText.color = Color.white;
        inventoryText.alignment = TextAlignmentOptions.Left;

        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.zero;
        textTransform.pivot = new Vector2(0.5f, 0.5f);
        textTransform.anchoredPosition = anchoredPosition;
        textTransform.sizeDelta = size;

        textObject.AddComponent<InventoryTextUI>();

        if (inventorySlot != null)
        {
            inventoryText.text = string.IsNullOrEmpty(inventorySlot.CurrentItemId)
                ? "Holding: Empty"
                : "Holding: " + inventorySlot.CurrentItemId;
        }
        else
        {
            inventoryText.text = "Holding: Empty";
        }
    }

    private void CreateCrosshair(Transform canvasTransform)
    {
        GameObject crosshairObject = new GameObject(CrosshairName);
        crosshairObject.transform.SetParent(canvasTransform, false);

        TextMeshProUGUI crosshair = crosshairObject.AddComponent<TextMeshProUGUI>();
        crosshair.text = crosshairText;
        crosshair.fontSize = crosshairFontSize;
        crosshair.color = Color.white;
        crosshair.alignment = TextAlignmentOptions.Center;
        crosshair.raycastTarget = false;

        RectTransform crosshairTransform = crosshairObject.GetComponent<RectTransform>();
        crosshairTransform.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairTransform.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairTransform.pivot = new Vector2(0.5f, 0.5f);
        crosshairTransform.anchoredPosition = Vector2.zero;
        crosshairTransform.sizeDelta = crosshairSize;
    }
}
