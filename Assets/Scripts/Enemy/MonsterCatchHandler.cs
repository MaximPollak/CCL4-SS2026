using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MonsterCatchHandler : MonoBehaviour
{
    [Header("Catch Rules")]
    [SerializeField] private int catchesBeforeDeath = 3;
    [SerializeField] private string deathSceneName = "DeathScene";

    [Header("Respawn Points")]
    [SerializeField] private Transform playerCrewCabinetRespawnPoint;
    [SerializeField] private Transform enemyLabRespawnPoint;
    [SerializeField] private string playerCrewCabinetRespawnName = "CrewCabinetRespawnPoint";
    [SerializeField] private string playerCrewCabinetFallbackRespawnName = "CrewBedroom_SpawnPoint1";
    [SerializeField] private string enemyLabRespawnName = "Laboratory_SpawnPoint1";

    public void HandlePlayerCaught(MonsterAI monsterAI)
    {
        Transform player = FindPlayer();
        Vector3 catchPosition = player != null ? player.position : transform.position;
        Quaternion catchRotation = player != null ? player.rotation : transform.rotation;

        DropHeldItemAtCatchLocation(player, catchPosition, catchRotation);

        int catchCount = GameState.Instance.RegisterPlayerCaught();

        if (catchCount >= catchesBeforeDeath)
        {
            TriggerDeath();
            return;
        }

        Transform playerRespawn = ResolveRespawnPoint(
            playerCrewCabinetRespawnPoint,
            playerCrewCabinetRespawnName,
            playerCrewCabinetFallbackRespawnName
        );
        Transform enemyRespawn = ResolveRespawnPoint(enemyLabRespawnPoint, enemyLabRespawnName, "");

        if (player != null && playerRespawn != null)
        {
            // Catch 1/2 now sends the player to the crew cabinet, not the old bedroom spawn.
            TeleportCharacter(player, playerRespawn);
        }

        if (monsterAI != null && enemyRespawn != null)
        {
            TeleportCharacter(monsterAI.transform, enemyRespawn);
            monsterAI.ResetAfterCatchRespawn();

            // The Lab reset is only saved after catch 1/2, never during normal scene changes.
            GameState.Instance.SaveEnemyState(
                monsterAI.gameObject.scene.name,
                monsterAI.transform.position,
                monsterAI.transform.rotation,
                monsterAI.currentState
            );
        }
    }

    private void TriggerDeath()
    {
        if (!string.IsNullOrWhiteSpace(deathSceneName))
        {
            SceneManager.LoadScene(deathSceneName);
        }
    }

    private void DropHeldItemAtCatchLocation(
        Transform player,
        Vector3 catchPosition,
        Quaternion catchRotation
    )
    {
        if (player == null)
        {
            return;
        }

        InventorySlot inventorySlot = player.GetComponent<InventorySlot>();

        if (inventorySlot == null || inventorySlot.IsEmpty())
        {
            return;
        }

        // The held item is persisted at the catch position before any respawn/death transition.
        inventorySlot.DropHeldItemAt(catchPosition, catchRotation);
    }

    private Transform FindPlayer()
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

    private Transform ResolveRespawnPoint(
        Transform assignedPoint,
        string fallbackName,
        string secondaryFallbackName
    )
    {
        if (assignedPoint != null)
        {
            return assignedPoint;
        }

        Transform fallbackPoint = FindRespawnPointByName(fallbackName);
        return fallbackPoint != null
            ? fallbackPoint
            : FindRespawnPointByName(secondaryFallbackName);
    }

    private Transform FindRespawnPointByName(string respawnPointName)
    {
        if (string.IsNullOrWhiteSpace(respawnPointName))
        {
            return null;
        }

        GameObject fallbackObject = GameObject.Find(respawnPointName);
        return fallbackObject != null ? fallbackObject.transform : null;
    }

    private void TeleportCharacter(Transform character, Transform respawnPoint)
    {
        if (character == null || respawnPoint == null)
        {
            return;
        }

        CharacterController characterController = character.GetComponent<CharacterController>();

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        character.SetPositionAndRotation(respawnPoint.position, respawnPoint.rotation);
        Physics.SyncTransforms();

        FirstPersonLook firstPersonLook = character.GetComponent<FirstPersonLook>();

        if (firstPersonLook != null)
        {
            firstPersonLook.SnapYawTo(respawnPoint.rotation);
        }

        FirstPersonMovement firstPersonMovement = character.GetComponent<FirstPersonMovement>();

        if (firstPersonMovement != null)
        {
            firstPersonMovement.ResetVerticalVelocity();
        }

        if (characterController != null)
        {
            characterController.enabled = true;
        }
    }
}
