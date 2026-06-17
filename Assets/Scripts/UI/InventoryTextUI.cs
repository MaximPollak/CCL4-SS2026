using TMPro;
using UnityEngine;

public class InventoryTextUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySlot inventorySlot;
    [SerializeField] private TextMeshProUGUI inventoryText;

    private string lastDisplayedItemId;

    private void Awake()
    {
        ResolveMissingReferences();
    }

    private void OnEnable()
    {
        ResolveMissingReferences();

        if (inventorySlot != null)
        {
            inventorySlot.OnInventoryChanged += UpdateInventoryText;
        }
    }

    private void OnDisable()
    {
        if (inventorySlot != null)
        {
            inventorySlot.OnInventoryChanged -= UpdateInventoryText;
        }
    }

    private void Start()
    {
        ResolveMissingReferences();

        if (inventorySlot != null)
        {
            UpdateInventoryText(inventorySlot.CurrentItemId);
        }
        else
        {
            UpdateInventoryText("");
        }
    }

    private void Update()
    {
        ResolveMissingReferences();

        if (inventorySlot == null)
        {
            UpdateInventoryText("");
            return;
        }

        if (inventorySlot.CurrentItemId != lastDisplayedItemId)
        {
            UpdateInventoryText(inventorySlot.CurrentItemId);
        }
    }

    private void ResolveMissingReferences()
    {
        if (inventoryText == null)
        {
            inventoryText = GetComponent<TextMeshProUGUI>();
        }

        if (inventoryText == null)
        {
            GameObject inventoryTextObject = GameObject.Find("InventoryText");

            if (inventoryTextObject != null)
            {
                inventoryText = inventoryTextObject.GetComponent<TextMeshProUGUI>();
            }
        }

        if (inventorySlot == null)
        {
            inventorySlot = FindFirstObjectByType<InventorySlot>();
        }
    }

    private void UpdateInventoryText(string itemId)
    {
        if (inventoryText == null)
        {
            return;
        }

        lastDisplayedItemId = itemId;

        if (string.IsNullOrEmpty(itemId))
        {
            inventoryText.text = "Holding: Empty";
        }
        else
        {
            inventoryText.text = "Holding: " + itemId;
        }
    }
}
