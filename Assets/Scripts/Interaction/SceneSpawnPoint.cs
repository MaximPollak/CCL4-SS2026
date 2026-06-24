using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawnPoint : MonoBehaviour
{
    [SerializeField] private string spawnPointId = "Default";
    [SerializeField] private bool snapPlayerToGround = true;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckHeight = 3f;
    [SerializeField] private float groundCheckDistance = 8f;
    [SerializeField] private float groundOffset = 0.05f;
    [SerializeField] private bool printDebugLogs = true;

    private static string pendingSpawnPointId;
    private static bool subscribedToSceneLoaded;

    public static void SetPendingSpawnPoint(string spawnPointId)
    {
        pendingSpawnPointId = spawnPointId;
        EnsureSceneLoadedSubscription();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureSceneLoadedSubscription()
    {
        if (subscribedToSceneLoaded)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        subscribedToSceneLoaded = true;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrWhiteSpace(pendingSpawnPointId))
        {
            return;
        }

        SceneSpawnPoint spawnPoint = FindSpawnPoint(pendingSpawnPointId);

        if (spawnPoint == null)
        {
            Debug.LogWarning("No SceneSpawnPoint found for id: " + pendingSpawnPointId);
            pendingSpawnPointId = "";
            return;
        }

        spawnPoint.StartCoroutine(spawnPoint.TeleportPlayerAfterSceneReady(pendingSpawnPointId));
        pendingSpawnPointId = "";
    }

    private IEnumerator TeleportPlayerAfterSceneReady(string requestedSpawnPointId)
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        Transform player = FindPlayerTransform();

        if (player == null)
        {
            Debug.LogWarning("No player found for scene spawn point: " + requestedSpawnPointId);
            yield break;
        }

        TeleportPlayer(player, this);
    }

    private static SceneSpawnPoint FindSpawnPoint(string spawnPointId)
    {
        SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (SceneSpawnPoint spawnPoint in spawnPoints)
        {
            if (spawnPoint.spawnPointId == spawnPointId)
            {
                return spawnPoint;
            }
        }

        return null;
    }

    private static Transform FindPlayerTransform()
    {
        PlayerInteraction playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        if (playerInteraction != null)
        {
            return playerInteraction.transform;
        }

        FirstPersonMovement firstPersonMovement = FindFirstObjectByType<FirstPersonMovement>();

        if (firstPersonMovement != null)
        {
            return firstPersonMovement.transform;
        }

        GameObject taggedPlayer = GameObject.FindGameObjectWithTag("Player");
        return taggedPlayer != null ? taggedPlayer.transform : null;
    }

    private static void TeleportPlayer(Transform player, SceneSpawnPoint spawnPoint)
    {
        CharacterController characterController = player.GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        Vector3 spawnPosition = spawnPoint.GetSpawnPosition(characterController);
        player.SetPositionAndRotation(spawnPosition, spawnPoint.transform.rotation);
        Physics.SyncTransforms();

        FirstPersonLook firstPersonLook = player.GetComponent<FirstPersonLook>();

        if (firstPersonLook != null)
        {
            firstPersonLook.SnapYawTo(spawnPoint.transform.rotation);
        }

        FirstPersonMovement firstPersonMovement = player.GetComponent<FirstPersonMovement>();

        if (firstPersonMovement != null)
        {
            firstPersonMovement.ResetVerticalVelocity();
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }

    private Vector3 GetSpawnPosition(CharacterController characterController)
    {
        Vector3 spawnPosition = transform.position;

        if (!snapPlayerToGround || characterController == null)
        {
            return spawnPosition;
        }

        Vector3 rayOrigin = transform.position + Vector3.up * groundCheckHeight;
        float rayDistance = groundCheckHeight + groundCheckDistance;

        if (!Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out RaycastHit hit,
            rayDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        ))
        {
            Debug.LogWarning("No ground found below SceneSpawnPoint: " + spawnPointId, this);
            return spawnPosition;
        }

        float controllerBottomOffset = characterController.center.y - characterController.height * 0.5f;
        spawnPosition.y = hit.point.y - controllerBottomOffset + groundOffset;

        if (printDebugLogs)
        {
            Debug.Log(
                "SceneSpawnPoint '" + spawnPointId + "' snapped player to " + hit.collider.name,
                hit.collider
            );
        }

        return spawnPosition;
    }
}
