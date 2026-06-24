using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameState : MonoBehaviour
{
    public class WorldItemState
    {
        public string itemId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    public class RandomSpawnState
    {
        public string itemId;
        public int spawnPointIndex;
        public bool isAvailable = true;
    }

    private static GameState instance;

    private readonly Dictionary<string, GameObject> itemPrefabsById = new Dictionary<string, GameObject>();
    private readonly HashSet<SubmarineRepairTask> completedSubmarineTasks = new HashSet<SubmarineRepairTask>();
    private readonly Dictionary<SubmarineRepairTask, int> submarineTaskProgress =
        new Dictionary<SubmarineRepairTask, int>();
    private readonly Dictionary<string, int> consumedItemCounts = new Dictionary<string, int>();
    private readonly HashSet<string> consumedScenePickupKeys = new HashSet<string>();
    private readonly Dictionary<string, List<WorldItemState>> droppedItemsByScene =
        new Dictionary<string, List<WorldItemState>>();
    private readonly Dictionary<string, List<RandomSpawnState>> randomSpawnStatesByScene =
        new Dictionary<string, List<RandomSpawnState>>();

    public static GameState Instance
    {
        get
        {
            EnsureInstance();
            return instance;
        }
    }

    public string HeldItemId { get; private set; } = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null)
        {
            return;
        }

        GameObject gameStateObject = new GameObject("GameState");
        instance = gameStateObject.AddComponent<GameState>();
        DontDestroyOnLoad(gameStateObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    public static void ResetRun()
    {
        Instance.ClearRunState();
    }

    public static void EndRun()
    {
        if (instance == null)
        {
            return;
        }

        Destroy(instance.gameObject);
        instance = null;
    }

    public void ClearRunState()
    {
        HeldItemId = "";
        completedSubmarineTasks.Clear();
        submarineTaskProgress.Clear();
        consumedItemCounts.Clear();
        consumedScenePickupKeys.Clear();
        droppedItemsByScene.Clear();
        randomSpawnStatesByScene.Clear();
    }

    public void RegisterItemPrefab(GameObject itemPrefab)
    {
        if (itemPrefab == null)
        {
            return;
        }

        PickupItem pickupItem = itemPrefab.GetComponentInChildren<PickupItem>(true);

        if (pickupItem == null || string.IsNullOrWhiteSpace(pickupItem.ItemId))
        {
            return;
        }

        itemPrefabsById[pickupItem.ItemId] = itemPrefab;
    }

    public bool TryGetItemPrefab(string itemId, out GameObject itemPrefab)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            itemPrefab = null;
            return false;
        }

        return itemPrefabsById.TryGetValue(itemId, out itemPrefab) && itemPrefab != null;
    }

    public void SetHeldItem(string itemId)
    {
        HeldItemId = string.IsNullOrWhiteSpace(itemId) ? "" : itemId;
    }

    public void ClearHeldItem()
    {
        HeldItemId = "";
    }

    public string ConsumeHeldItemForDeath()
    {
        string lostItemId = HeldItemId;
        HeldItemId = "";
        return lostItemId;
    }

    public bool IsSubmarineTaskComplete(SubmarineRepairTask task)
    {
        return completedSubmarineTasks.Contains(task);
    }

    public void CompleteSubmarineTask(SubmarineRepairTask task)
    {
        completedSubmarineTasks.Add(task);
    }

    public void ClearSubmarineTask(SubmarineRepairTask task)
    {
        completedSubmarineTasks.Remove(task);
        submarineTaskProgress.Remove(task);
    }

    public int GetSubmarineTaskProgress(SubmarineRepairTask task)
    {
        return submarineTaskProgress.TryGetValue(task, out int progress) ? progress : 0;
    }

    public void SetSubmarineTaskProgress(SubmarineRepairTask task, int progress)
    {
        if (progress <= 0)
        {
            submarineTaskProgress.Remove(task);
            return;
        }

        submarineTaskProgress[task] = progress;
    }

    public void MarkItemConsumed(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return;
        }

        consumedItemCounts.TryGetValue(itemId, out int count);
        consumedItemCounts[itemId] = count + 1;
    }

    public void UnmarkItemConsumed(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return;
        }

        if (!consumedItemCounts.TryGetValue(itemId, out int count))
        {
            return;
        }

        if (count <= 1)
        {
            consumedItemCounts.Remove(itemId);
        }
        else
        {
            consumedItemCounts[itemId] = count - 1;
        }
    }

    public int GetUnavailableItemCount(string itemId)
    {
        int count = 0;

        if (consumedItemCounts.TryGetValue(itemId, out int consumedCount))
        {
            count += consumedCount;
        }

        if (!string.IsNullOrWhiteSpace(HeldItemId) && HeldItemId == itemId)
        {
            count++;
        }

        return count;
    }

    public bool IsScenePickupConsumed(string scenePickupKey)
    {
        return !string.IsNullOrWhiteSpace(scenePickupKey)
            && consumedScenePickupKeys.Contains(scenePickupKey);
    }

    public void MarkScenePickupConsumed(string scenePickupKey)
    {
        if (string.IsNullOrWhiteSpace(scenePickupKey))
        {
            return;
        }

        consumedScenePickupKeys.Add(scenePickupKey);
    }

    public void SaveRandomSpawnStates(string sceneName, List<RandomSpawnState> spawnStates)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || spawnStates == null)
        {
            return;
        }

        randomSpawnStatesByScene[sceneName] = new List<RandomSpawnState>(spawnStates);
    }

    public bool TryGetRandomSpawnStates(string sceneName, out List<RandomSpawnState> spawnStates)
    {
        if (
            !string.IsNullOrWhiteSpace(sceneName)
            && randomSpawnStatesByScene.TryGetValue(sceneName, out List<RandomSpawnState> storedStates)
        )
        {
            spawnStates = storedStates;
            return true;
        }

        spawnStates = null;
        return false;
    }

    public void MarkRandomSpawnItemPickedUp(string sceneName, int spawnPointIndex)
    {
        if (
            string.IsNullOrWhiteSpace(sceneName)
            || !randomSpawnStatesByScene.TryGetValue(sceneName, out List<RandomSpawnState> spawnStates)
        )
        {
            return;
        }

        foreach (RandomSpawnState spawnState in spawnStates)
        {
            if (spawnState.spawnPointIndex != spawnPointIndex)
            {
                continue;
            }

            spawnState.isAvailable = false;
            return;
        }
    }

    public void AddDroppedWorldItem(
        string sceneName,
        string itemId,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale
    )
    {
        if (string.IsNullOrWhiteSpace(sceneName) || string.IsNullOrWhiteSpace(itemId))
        {
            return;
        }

        if (!droppedItemsByScene.TryGetValue(sceneName, out List<WorldItemState> droppedItems))
        {
            droppedItems = new List<WorldItemState>();
            droppedItemsByScene[sceneName] = droppedItems;
        }

        droppedItems.Add(new WorldItemState
        {
            itemId = itemId,
            position = position,
            rotation = rotation,
            scale = scale
        });
    }

    public void RemoveDroppedWorldItem(string sceneName, string itemId, Vector3 pickupPosition)
    {
        if (
            string.IsNullOrWhiteSpace(sceneName)
            || string.IsNullOrWhiteSpace(itemId)
            || !droppedItemsByScene.TryGetValue(sceneName, out List<WorldItemState> droppedItems)
        )
        {
            return;
        }

        int bestIndex = -1;
        float bestDistance = Mathf.Infinity;

        for (int i = 0; i < droppedItems.Count; i++)
        {
            WorldItemState droppedItem = droppedItems[i];

            if (droppedItem.itemId != itemId)
            {
                continue;
            }

            float distance = Vector3.SqrMagnitude(droppedItem.position - pickupPosition);

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            droppedItems.RemoveAt(bestIndex);
        }
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        StartCoroutine(RestoreDroppedItemsAfterSceneLoad(scene.name));
    }

    private System.Collections.IEnumerator RestoreDroppedItemsAfterSceneLoad(string sceneName)
    {
        yield return null;

        if (
            string.IsNullOrWhiteSpace(sceneName)
            || !droppedItemsByScene.TryGetValue(sceneName, out List<WorldItemState> droppedItems)
        )
        {
            yield break;
        }

        foreach (WorldItemState droppedItem in droppedItems)
        {
            if (
                droppedItem == null
                || string.IsNullOrWhiteSpace(droppedItem.itemId)
                || !TryGetItemPrefab(droppedItem.itemId, out GameObject itemPrefab)
            )
            {
                continue;
            }

            GameObject restoredItem = Instantiate(
                itemPrefab,
                droppedItem.position,
                droppedItem.rotation
            );

            restoredItem.transform.localScale = droppedItem.scale;

            PickupItem pickupItem = restoredItem.GetComponentInChildren<PickupItem>(true);

            if (pickupItem != null)
            {
                pickupItem.PrepareForWorldSpawn();
            }
        }
    }
}
