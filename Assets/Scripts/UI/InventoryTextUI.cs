using TMPro;
using UnityEngine;

public class InventoryTextUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InventorySlot inventorySlot;
    [SerializeField] private TextMeshProUGUI inventoryText;

    private void OnEnable()
    {
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
        if (inventorySlot != null)
        {
            UpdateInventoryText(inventorySlot.CurrentItemId);
        }
        else
        {
            UpdateInventoryText("");
        }
    }

    private void UpdateInventoryText(string itemId)
    {
        if (inventoryText == null)
        {
            return;
        }

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