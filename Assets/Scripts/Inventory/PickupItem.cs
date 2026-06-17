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
        transform.SetParent(null);

        transform.position = position;
        transform.rotation = Quaternion.identity;

        Collider[] itemColliders = GetComponentsInChildren<Collider>();

        foreach (Collider itemCollider in itemColliders)
        {
            itemCollider.enabled = true;
        }

        Rigidbody itemRigidbody = GetComponent<Rigidbody>();

        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = false;
            itemRigidbody.linearVelocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;

            itemRigidbody.AddForce(
                (forwardDirection + Vector3.up) * plopForce,
                ForceMode.Impulse
            );
        }
    }
}