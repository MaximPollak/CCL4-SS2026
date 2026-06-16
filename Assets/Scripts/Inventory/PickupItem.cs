using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Item Settings")]
    [SerializeField] private string itemId = "Screwdriver";

    public string ItemId => itemId;

    public void Interact(PlayerInteraction player)
    {
        if (player.Inventory == null)
        {
            Debug.LogWarning("Player has no inventory.");
            return;
        }

        player.Inventory.PickUpItem(this, player.PlayerCamera.transform);
    }

    public void DropTo(Vector3 position, Vector3 forwardDirection, float plopForce)
    {
        gameObject.SetActive(true);

        transform.position = position;
        transform.rotation = Quaternion.identity;

        Rigidbody rigidbody = GetComponent<Rigidbody>();

        if (rigidbody != null)
        {
            rigidbody.AddForce((forwardDirection + Vector3.up) * plopForce, ForceMode.Impulse);
        }
    }
}