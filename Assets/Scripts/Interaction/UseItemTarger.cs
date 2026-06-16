using UnityEngine;

public class UseItemTarget : MonoBehaviour, IInteractable
{
    [Header("Required Item")]
    [SerializeField] private string requiredItemId = "Screwdriver";
    [SerializeField] private bool consumeItemOnUse = true;

    [Header("Result")]
    [SerializeField] private GameObject objectToDisableAfterUse;
    [SerializeField] private GameObject objectToEnableAfterUse;

    [Header("Debug")]
    [SerializeField] private string successMessage = "Item used successfully.";
    [SerializeField] private string missingItemMessage = "You need another item.";

    public void Interact(PlayerInteraction player)
    {
        if (player.Inventory == null)
        {
            Debug.LogWarning("Player has no inventory.");
            return;
        }

        if (!player.Inventory.HasItem(requiredItemId))
        {
            Debug.Log(missingItemMessage + " Required item: " + requiredItemId);
            return;
        }

        Debug.Log(successMessage + " Used: " + requiredItemId);

        if (consumeItemOnUse)
        {
            player.Inventory.ClearItem();
        }

        if (objectToDisableAfterUse != null)
        {
            objectToDisableAfterUse.SetActive(false);
        }

        if (objectToEnableAfterUse != null)
        {
            objectToEnableAfterUse.SetActive(true);
        }
    }
}