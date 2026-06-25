using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class MonsterCatchHandler : MonoBehaviour
{
    [Header("Catch Rules")]
    [SerializeField] private int catchesBeforeDeath = 3;
    [SerializeField] private string deathSceneName = "DeathScene";

    [Header("Respawn Points")]
    [SerializeField] private Transform playerBedroomRespawnPoint;
    [SerializeField] private Transform enemyLabRespawnPoint;
    [SerializeField] private string playerBedroomRespawnName = "Bedroom_SpawnPoint1";
    [SerializeField] private string enemyLabRespawnName = "Laboratory_SpawnPoint1";

    public void HandlePlayerCaught(MonsterAI monsterAI)
    {
        int catchCount = GameState.Instance.RegisterPlayerCaught();

        if (catchCount >= catchesBeforeDeath)
        {
            TriggerDeath();
            return;
        }

        Transform player = FindPlayer();
        Transform playerRespawn = ResolveRespawnPoint(playerBedroomRespawnPoint, playerBedroomRespawnName);
        Transform enemyRespawn = ResolveRespawnPoint(enemyLabRespawnPoint, enemyLabRespawnName);

        if (player != null && playerRespawn != null)
        {
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
        InventorySlot inventorySlot = FindFirstObjectByType<InventorySlot>();

        if (inventorySlot != null)
        {
            inventorySlot.ClearHeldItemForDeath();
        }

        if (!string.IsNullOrWhiteSpace(deathSceneName))
        {
            SceneManager.LoadScene(deathSceneName);
        }
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

    private Transform ResolveRespawnPoint(Transform assignedPoint, string fallbackName)
    {
        if (assignedPoint != null)
        {
            return assignedPoint;
        }

        if (string.IsNullOrWhiteSpace(fallbackName))
        {
            return null;
        }

        GameObject fallbackObject = GameObject.Find(fallbackName);
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
