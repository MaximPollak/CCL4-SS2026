using UnityEngine;

[DisallowMultipleComponent]
public class AquariumPoisonInteraction : MonoBehaviour, IInteractable
{
    [Header("State")]
    [SerializeField] private string aquariumId = "Aquarium";
    [SerializeField] private string requiredPoisonItemId = "Poison";

    [Header("Aquarium Result")]
    [SerializeField] private Renderer waterRenderer;
    [SerializeField] private Color poisonedWaterColor = Color.green;
    [SerializeField] private GameObject fishObject;

    [Header("Coin Pickup")]
    [SerializeField] private GameObject coinObject;
    [SerializeField] private PickupItem coinPickup;
    [SerializeField] private Collider[] coinColliders;
    [SerializeField] private bool disableCoinUntilPoisoned = true;

    [Header("Debug")]
    [SerializeField] private bool printDebugLogs = true;

    private bool isPoisoned;
    private Material waterMaterialInstance;

    private void Reset()
    {
        AssignReferences();
    }

    private void Awake()
    {
        AssignReferences();

        isPoisoned = GameState.Instance.IsAquariumPoisoned(aquariumId);
        ApplyPoisonedState();
    }

    public void Interact(PlayerInteraction player)
    {
        Log("Player interacted with aquarium.");

        if (isPoisoned)
        {
            Log("Aquarium is already poisoned. Coin pickup is available.");
            return;
        }

        if (player == null || player.Inventory == null)
        {
            Debug.LogWarning("AquariumPoisonInteraction cannot check poison because player inventory is missing.", this);
            return;
        }

        bool playerHasPoison = player.Inventory.HasItem(requiredPoisonItemId);
        Log("Player has poison: " + playerHasPoison);

        if (!playerHasPoison)
        {
            Log("Aquarium needs item: " + requiredPoisonItemId);
            return;
        }

        GameState.Instance.MarkItemConsumed(requiredPoisonItemId);
        player.Inventory.ClearItem();
        Log("Poison was consumed: " + requiredPoisonItemId);

        isPoisoned = true;
        GameState.Instance.MarkAquariumPoisoned(aquariumId);
        ApplyPoisonedState();

        Log("Aquarium poisoned. Coin pickup became available.");
    }

    private void ApplyPoisonedState()
    {
        if (waterRenderer != null)
        {
            if (waterMaterialInstance == null)
            {
                waterMaterialInstance = waterRenderer.material;
            }

            waterMaterialInstance.color = isPoisoned ? poisonedWaterColor : waterMaterialInstance.color;
        }

        if (fishObject != null)
        {
            fishObject.SetActive(!isPoisoned);
        }

        bool coinCanBePickedUp = isPoisoned || !disableCoinUntilPoisoned;

        if (coinObject != null)
        {
            if (isPoisoned)
            {
                coinObject.SetActive(true);
            }
        }

        if (coinPickup != null)
        {
            coinPickup.enabled = coinCanBePickedUp;
        }

        if (coinColliders != null)
        {
            foreach (Collider coinCollider in coinColliders)
            {
                if (coinCollider != null)
                {
                    coinCollider.enabled = coinCanBePickedUp;
                }
            }
        }

        Log("Coin pickup enabled: " + coinCanBePickedUp);
    }

    private void AssignReferences()
    {
        if (coinObject == null && coinPickup != null)
        {
            coinObject = coinPickup.gameObject;
        }

        if (coinObject != null && coinPickup == null)
        {
            coinPickup = coinObject.GetComponentInChildren<PickupItem>(true);
        }

        if (coinObject != null && (coinColliders == null || coinColliders.Length == 0))
        {
            coinColliders = coinObject.GetComponentsInChildren<Collider>(true);
        }
    }

    private void Log(string message)
    {
        if (printDebugLogs)
        {
            Debug.Log("AquariumPoisonInteraction: " + message, this);
        }
    }
}
