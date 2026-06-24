using UnityEngine;

public class RuntimeSpawnedItem : MonoBehaviour
{
    public string SceneName { get; private set; }
    public int SpawnPointIndex { get; private set; } = -1;

    public void Initialize(string sceneName, int spawnPointIndex)
    {
        SceneName = sceneName;
        SpawnPointIndex = spawnPointIndex;
    }

    public void MarkPickedUp()
    {
        if (string.IsNullOrWhiteSpace(SceneName) || SpawnPointIndex < 0)
        {
            return;
        }

        GameState.Instance.MarkRandomSpawnItemPickedUp(SceneName, SpawnPointIndex);
    }
}
