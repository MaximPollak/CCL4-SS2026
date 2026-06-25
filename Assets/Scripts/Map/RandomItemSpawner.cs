using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RandomItemSpawner : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform spawnPointParent;
    [SerializeField] private Transform[] spawnPoints;

    [Header("Items")]
    [SerializeField] private GameObject[] itemPrefabs;
    [SerializeField] private int amountToSpawn = 3;

    [Header("Settings")]
    [SerializeField] private bool spawnAllConfiguredItems = true;
    [SerializeField] private bool useEachItemOnlyOnce = true;
    [SerializeField] private bool matchSpawnPointRotation = false;
    [SerializeField] private bool parentItemsToSpawner = true;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Ground Fix")]
    [SerializeField] private bool autoFixGroundHeight = true;
    [SerializeField] private float groundOffset = 0.02f;
    [SerializeField] private float extraRaiseOffset = 0f;

    [Header("Debug")]
    [SerializeField] private bool printSpawnDebugLogs = false;
    [SerializeField] private bool ignoreRuntimeUnavailableFilter = false;

    private readonly List<GameObject> spawnedItems = new List<GameObject>();

    private void Awake()
    {
        RegisterItemPrefabs();
    }

    private void Start()
    {
        if (spawnOnStart)
        {
            SpawnItems();
        }
    }

    private void OnDisable()
    {
        SaveCurrentSpawnedItemStates();
    }

    public void SpawnItems()
    {
        ClearSpawnedItems();

        List<Transform> availableSpawnPoints = GetSpawnPoints();

        if (RestoreSavedSpawnPlan(availableSpawnPoints))
        {
            return;
        }

        List<GameObject> availableItems = new List<GameObject>(itemPrefabs);
        int itemCountBeforeFilter = availableItems.Count;

        if (ignoreRuntimeUnavailableFilter)
        {
            RemoveNullItems(availableItems);

            if (printSpawnDebugLogs)
            {
                Debug.Log("RandomItemSpawner: Runtime unavailable filter ignored for debugging.");
            }
        }
        else
        {
            RemoveUnavailableItems(availableItems);
        }

        if (availableSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points assigned.");
            return;
        }

        if (availableItems.Count == 0)
        {
            Debug.LogWarning("No item prefabs assigned.");
            return;
        }

        int requestedSpawnCount = spawnAllConfiguredItems
            ? availableItems.Count
            : amountToSpawn;

        int spawnCount = Mathf.Min(requestedSpawnCount, availableSpawnPoints.Count);

        if (useEachItemOnlyOnce)
        {
            spawnCount = Mathf.Min(spawnCount, availableItems.Count);
        }

        if (printSpawnDebugLogs)
        {
            Debug.Log(
                "RandomItemSpawner: points=" + availableSpawnPoints.Count +
                " | item prefabs before filter=" + itemCountBeforeFilter +
                " | after filter=" + availableItems.Count +
                " | amountToSpawn=" + amountToSpawn +
                " | spawnAllConfiguredItems=" + spawnAllConfiguredItems +
                " | final spawn count=" + spawnCount +
                " | unique items=" + useEachItemOnlyOnce
            );
        }

        List<Transform> unusedSpawnPoints = new List<Transform>(availableSpawnPoints);

        Shuffle(availableItems);

        List<GameState.RandomSpawnState> generatedSpawnStates = new List<GameState.RandomSpawnState>();
        int spawnedCount = 0;
        int spawnAttempts = 0;
        int maxSpawnAttempts = useEachItemOnlyOnce
            ? availableItems.Count
            : spawnCount * Mathf.Max(availableItems.Count, 1);

        while (spawnedCount < spawnCount && spawnAttempts < maxSpawnAttempts)
        {
            GameObject itemPrefab;

            if (useEachItemOnlyOnce)
            {
                itemPrefab = availableItems[spawnAttempts];
            }
            else
            {
                int randomItemIndex = Random.Range(0, availableItems.Count);
                itemPrefab = availableItems[randomItemIndex];
            }

            spawnAttempts++;

            PickupItem pickupItem = itemPrefab.GetComponentInChildren<PickupItem>(true);

            if (pickupItem == null)
            {
                Debug.LogWarning("RandomItemSpawner: " + itemPrefab.name + " has no PickupItem.");
                continue;
            }

            Transform spawnPoint = PickCompatibleSpawnPoint(
                pickupItem,
                unusedSpawnPoints
            );

            if (spawnPoint == null)
            {
                Debug.LogWarning(
                    "RandomItemSpawner: No compatible spawn point for " +
                    GetItemDebugName(itemPrefab) +
                    " with size " +
                    pickupItem.Size +
                    ". Item was not spawned."
                );
                continue;
            }

            int spawnPointIndex = GetSpawnPointIndex(spawnPoint, availableSpawnPoints);

            GameObject spawnedItem = SpawnItemPrefab(
                itemPrefab,
                spawnPoint,
                SceneManager.GetActiveScene().name,
                spawnPointIndex
            );

            unusedSpawnPoints.Remove(spawnPoint);
            spawnedCount++;

            if (pickupItem != null)
            {
                generatedSpawnStates.Add(new GameState.RandomSpawnState
                {
                    itemId = pickupItem.ItemId,
                    spawnPointIndex = spawnPointIndex,
                    isAvailable = true,
                    hasSavedTransform = true,
                    position = spawnedItem.transform.position,
                    rotation = spawnedItem.transform.rotation,
                    scale = spawnedItem.transform.localScale
                });
            }
        }

        GameState.Instance.SaveRandomSpawnStates(
            SceneManager.GetActiveScene().name,
            generatedSpawnStates
        );
    }

    [ContextMenu("Reset Runtime Game State And Respawn Items")]
    public void ResetRuntimeGameStateAndRespawnItems()
    {
        GameState.ResetRun();
        SpawnItems();
    }

    [ContextMenu("Respawn Items")]
    public void RespawnItems()
    {
        SpawnItems();
    }

    public bool SpawnSpecificItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        if (!GameState.Instance.TryGetItemPrefab(itemId, out GameObject itemPrefab))
        {
            Debug.LogWarning("No item prefab registered for item id: " + itemId);
            return false;
        }

        List<Transform> availableSpawnPoints = GetSpawnPoints();

        if (availableSpawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points assigned.");
            return false;
        }

        Transform spawnPoint = availableSpawnPoints[Random.Range(0, availableSpawnPoints.Count)];
        SpawnItemPrefab(itemPrefab, spawnPoint);
        return true;
    }

    public void ClearSpawnedItems()
    {
        foreach (GameObject item in spawnedItems)
        {
            if (item != null)
            {
                Destroy(item);
            }
        }

        spawnedItems.Clear();
    }

    private List<Transform> GetSpawnPoints()
    {
        List<Transform> points = new List<Transform>();

        if (spawnPointParent != null)
        {
            foreach (Transform child in spawnPointParent)
            {
                points.Add(child);
            }
        }

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            points.AddRange(spawnPoints);
        }

        return points;
    }

    private void RegisterItemPrefabs()
    {
        foreach (GameObject itemPrefab in itemPrefabs)
        {
            GameState.Instance.RegisterItemPrefab(itemPrefab);
        }
    }

    private bool RestoreSavedSpawnPlan(List<Transform> availableSpawnPoints)
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (!GameState.Instance.TryGetRandomSpawnStates(
            sceneName,
            out List<GameState.RandomSpawnState> spawnStates
        ))
        {
            return false;
        }

        foreach (GameState.RandomSpawnState spawnState in spawnStates)
        {
            if (
                spawnState == null
                || !spawnState.isAvailable
                || spawnState.spawnPointIndex < 0
                || spawnState.spawnPointIndex >= availableSpawnPoints.Count
                || !GameState.Instance.TryGetItemPrefab(spawnState.itemId, out GameObject itemPrefab)
            )
            {
                continue;
            }

            if (GameState.Instance.GetUnavailableItemCount(spawnState.itemId) > 0)
            {
                continue;
            }

            if (spawnState.hasSavedTransform)
            {
                SpawnItemPrefabAtState(itemPrefab, spawnState, sceneName);
            }
            else
            {
                SpawnItemPrefab(
                    itemPrefab,
                    availableSpawnPoints[spawnState.spawnPointIndex],
                    sceneName,
                    spawnState.spawnPointIndex
                );
            }
        }

        if (printSpawnDebugLogs)
        {
            Debug.Log(
                "RandomItemSpawner: Restored saved spawn plan for " +
                sceneName +
                " with " +
                spawnStates.Count +
                " saved slots."
            );
        }

        return true;
    }

    private void RemoveUnavailableItems(List<GameObject> availableItems)
    {
        Dictionary<string, int> removedCounts = new Dictionary<string, int>();

        for (int i = availableItems.Count - 1; i >= 0; i--)
        {
            GameObject itemPrefab = availableItems[i];

            if (itemPrefab == null)
            {
                if (printSpawnDebugLogs)
                {
                    Debug.LogWarning("RandomItemSpawner: Skipping null item prefab.");
                }

                availableItems.RemoveAt(i);
                continue;
            }

            PickupItem pickupItem = itemPrefab.GetComponentInChildren<PickupItem>(true);

            if (pickupItem == null)
            {
                if (printSpawnDebugLogs)
                {
                    Debug.LogWarning("RandomItemSpawner: " + itemPrefab.name + " has no PickupItem.");
                }

                continue;
            }

            int unavailableCount = GameState.Instance.GetUnavailableItemCount(pickupItem.ItemId);
            removedCounts.TryGetValue(pickupItem.ItemId, out int removedCount);

            if (removedCount >= unavailableCount)
            {
                continue;
            }

            if (printSpawnDebugLogs)
            {
                Debug.Log(
                    "RandomItemSpawner: Filtering unavailable item " +
                    pickupItem.ItemId +
                    " (" + itemPrefab.name + "), unavailable count=" +
                    unavailableCount
                );
            }

            availableItems.RemoveAt(i);
            removedCounts[pickupItem.ItemId] = removedCount + 1;
        }
    }

    private void RemoveNullItems(List<GameObject> availableItems)
    {
        for (int i = availableItems.Count - 1; i >= 0; i--)
        {
            if (availableItems[i] != null)
            {
                continue;
            }

            if (printSpawnDebugLogs)
            {
                Debug.LogWarning("RandomItemSpawner: Skipping null item prefab.");
            }

            availableItems.RemoveAt(i);
        }
    }

    private void SaveCurrentSpawnedItemStates()
    {
        if (!GameState.HasInstance)
        {
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;

        foreach (GameObject spawnedItem in spawnedItems)
        {
            if (spawnedItem == null)
            {
                continue;
            }

            RuntimeSpawnedItem runtimeSpawnedItem = spawnedItem.GetComponent<RuntimeSpawnedItem>();

            if (
                runtimeSpawnedItem == null
                || runtimeSpawnedItem.SpawnPointIndex < 0
                || runtimeSpawnedItem.SceneName != sceneName
            )
            {
                continue;
            }

            // Scene switches destroy map objects, so cache the latest live transform first.
            GameState.Instance.UpdateRandomSpawnItemState(
                sceneName,
                runtimeSpawnedItem.SpawnPointIndex,
                spawnedItem.transform.position,
                spawnedItem.transform.rotation,
                spawnedItem.transform.localScale
            );
        }
    }

    private Transform PickCompatibleSpawnPoint(
        PickupItem pickupItem,
        List<Transform> unusedSpawnPoints
    )
    {
        if (pickupItem == null || unusedSpawnPoints == null || unusedSpawnPoints.Count == 0)
        {
            return null;
        }

        List<Transform> compatibleSpawnPoints = new List<Transform>();

        foreach (Transform spawnPoint in unusedSpawnPoints)
        {
            if (spawnPoint == null)
            {
                continue;
            }

            if (AllowsItemSize(spawnPoint, pickupItem.Size))
            {
                compatibleSpawnPoints.Add(spawnPoint);
            }
        }

        if (compatibleSpawnPoints.Count == 0)
        {
            return null;
        }

        return compatibleSpawnPoints[Random.Range(0, compatibleSpawnPoints.Count)];
    }

    private bool AllowsItemSize(Transform spawnPoint, ItemSize itemSize)
    {
        ItemSpawnPoint itemSpawnPoint = spawnPoint.GetComponent<ItemSpawnPoint>();

        if (itemSpawnPoint == null)
        {
            // Existing scenes without ItemSpawnPoint components keep their previous all-sizes behavior.
            return true;
        }

        // Size compatibility is only used while generating the initial random spawn plan.
        return itemSpawnPoint.Allows(itemSize);
    }

    private GameObject SpawnItemPrefab(
        GameObject itemPrefab,
        Transform spawnPoint,
        string originSceneName = "",
        int originSpawnPointIndex = -1
    )
    {
        Quaternion rotation = matchSpawnPointRotation
            ? spawnPoint.rotation
            : itemPrefab.transform.rotation;

        Transform parent = parentItemsToSpawner ? transform : null;

        GameObject spawnedItem = Instantiate(
            itemPrefab,
            spawnPoint.position,
            rotation,
            parent
        );

        PickupItem spawnedPickupItem = spawnedItem.GetComponentInChildren<PickupItem>(true);

        if (spawnedPickupItem != null)
        {
            spawnedPickupItem.PrepareForWorldSpawn(printSpawnDebugLogs);
        }

        if (!string.IsNullOrWhiteSpace(originSceneName) && originSpawnPointIndex >= 0)
        {
            RuntimeSpawnedItem runtimeSpawnedItem = spawnedItem.GetComponent<RuntimeSpawnedItem>();

            if (runtimeSpawnedItem == null)
            {
                runtimeSpawnedItem = spawnedItem.AddComponent<RuntimeSpawnedItem>();
            }

            runtimeSpawnedItem.Initialize(originSceneName, originSpawnPointIndex);
        }

        if (autoFixGroundHeight)
        {
            FixGroundHeight(spawnedItem, spawnPoint.position.y);
        }

        if (extraRaiseOffset != 0f)
        {
            spawnedItem.transform.position += Vector3.up * extraRaiseOffset;
        }

        if (printSpawnDebugLogs)
        {
            Debug.Log(
                "RandomItemSpawner: Spawned " +
                GetItemDebugName(itemPrefab) +
                " at " +
                spawnPoint.name +
                " | position=" +
                spawnedItem.transform.position
            );
        }

        spawnedItems.Add(spawnedItem);
        return spawnedItem;
    }

    private GameObject SpawnItemPrefabAtState(
        GameObject itemPrefab,
        GameState.RandomSpawnState spawnState,
        string sceneName
    )
    {
        Transform parent = parentItemsToSpawner ? transform : null;

        GameObject spawnedItem = Instantiate(
            itemPrefab,
            spawnState.position,
            spawnState.rotation,
            parent
        );

        spawnedItem.transform.localScale = spawnState.scale;

        PickupItem spawnedPickupItem = spawnedItem.GetComponentInChildren<PickupItem>(true);

        if (spawnedPickupItem != null)
        {
            spawnedPickupItem.PrepareForWorldSpawn(printSpawnDebugLogs);
        }

        RuntimeSpawnedItem runtimeSpawnedItem = spawnedItem.GetComponent<RuntimeSpawnedItem>();

        if (runtimeSpawnedItem == null)
        {
            runtimeSpawnedItem = spawnedItem.AddComponent<RuntimeSpawnedItem>();
        }

        runtimeSpawnedItem.Initialize(sceneName, spawnState.spawnPointIndex);

        spawnedItems.Add(spawnedItem);
        return spawnedItem;
    }

    private int GetSpawnPointIndex(Transform spawnPoint, List<Transform> spawnPointList)
    {
        for (int i = 0; i < spawnPointList.Count; i++)
        {
            if (spawnPointList[i] == spawnPoint)
            {
                return i;
            }
        }

        return -1;
    }

    private string GetItemDebugName(GameObject itemPrefab)
    {
        if (itemPrefab == null)
        {
            return "null";
        }

        PickupItem pickupItem = itemPrefab.GetComponentInChildren<PickupItem>(true);

        if (pickupItem == null)
        {
            return itemPrefab.name;
        }

        return pickupItem.ItemId + " (" + itemPrefab.name + ")";
    }

    private void FixGroundHeight(GameObject item, float groundY)
    {
        if (!TryGetObjectBounds(item, out Bounds bounds))
        {
            Debug.LogWarning("No Renderer found on spawned item: " + item.name);
            return;
        }

        float objectBottomY = bounds.min.y;
        float wantedBottomY = groundY + groundOffset;

        float difference = wantedBottomY - objectBottomY;

        item.transform.position += Vector3.up * difference;
    }

    private bool TryGetObjectBounds(GameObject item, out Bounds bounds)
    {
        Renderer[] renderers = item.GetComponentsInChildren<Renderer>(true);

        if (renderers.Length == 0)
        {
            bounds = new Bounds(item.transform.position, Vector3.zero);
            return false;
        }

        bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = Random.Range(i, list.Count);

            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
