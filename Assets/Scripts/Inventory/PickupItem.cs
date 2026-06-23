using UnityEngine;

public class PickupItem : MonoBehaviour, IInteractable
{
    [Header("Item Settings")]
    [SerializeField] private string itemId = "Screwdriver";

    [Header("Held Visual Override")]
    [SerializeField] private bool overrideHeldPosition = false;
    [SerializeField] private bool overrideHeldRotation = false;
    [SerializeField] private Vector3 heldLocalPosition = new Vector3(0.45f, -0.35f, 0.75f);
    [SerializeField] private Vector3 heldLocalRotation = new Vector3(15f, -25f, 10f);
    [SerializeField] private float heldScaleMultiplier = 1f;

    [Header("Drop Physics")]
    [SerializeField] private bool addRigidbodyIfMissing = true;
    [SerializeField] private float upwardDropForce = 0.6f;
    [SerializeField] private float spinForce = 0f;

    public string ItemId => itemId;
    public bool OverrideHeldPosition => overrideHeldPosition;
    public bool OverrideHeldRotation => overrideHeldRotation;
    public Vector3 HeldLocalPosition => heldLocalPosition;
    public Vector3 HeldLocalRotation => heldLocalRotation;
    public float HeldScaleMultiplier => heldScaleMultiplier;

    public void Interact(PlayerInteraction player)
    {
        if (player.Inventory == null)
        {
            Debug.LogWarning("Player has no inventory.");
            return;
        }

        player.Inventory.PickUpItem(this, player.PlayerCamera.transform);
    }

    public void DropFromHand(Vector3 forwardDirection, Vector3 originalScale, float plopForce)
    {
        gameObject.SetActive(true);
        transform.SetParent(null, true);
        transform.localScale = originalScale;

        Collider[] itemColliders = GetComponentsInChildren<Collider>();

        foreach (Collider itemCollider in itemColliders)
        {
            itemCollider.enabled = true;
        }

        Rigidbody itemRigidbody = GetComponent<Rigidbody>();

        if (itemRigidbody == null && addRigidbodyIfMissing)
        {
            itemRigidbody = gameObject.AddComponent<Rigidbody>();
        }

        if (itemRigidbody != null)
        {
            itemRigidbody.isKinematic = false;
            itemRigidbody.linearVelocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
            itemRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            itemRigidbody.AddForce(
                (forwardDirection + Vector3.up * upwardDropForce).normalized * plopForce,
                ForceMode.Impulse
            );

            if (spinForce > 0f)
            {
                itemRigidbody.AddTorque(
                    Random.insideUnitSphere * spinForce,
                    ForceMode.Impulse
                );
            }
        }
    }
}
