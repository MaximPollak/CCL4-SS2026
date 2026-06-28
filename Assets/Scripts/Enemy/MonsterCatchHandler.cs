using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Catch Overlay")]
    [SerializeField] private float catchOverlayDuration = 2f;
    [SerializeField] private string catchOverlayTextFormat = "Day {0}/{1}";
    [SerializeField] private Color catchOverlayBackgroundColor = Color.black;
    [SerializeField] private Color catchOverlayTextColor = Color.white;
    [SerializeField] private int catchOverlayFontSize = 42;

    private bool isHandlingCatch;

    public void HandlePlayerCaught(MonsterAI monsterAI)
    {
        if (isHandlingCatch)
        {
            return;
        }

        StartCoroutine(HandlePlayerCaughtRoutine(monsterAI));
    }

    private IEnumerator HandlePlayerCaughtRoutine(MonsterAI monsterAI)
    {
        Transform player = FindPlayer();
        Vector3 catchPosition = player != null ? player.position : transform.position;
        Quaternion catchRotation = player != null ? player.rotation : transform.rotation;

        DropHeldItemAtCatchLocation(player, catchPosition, catchRotation);

        int catchCount = GameState.Instance.RegisterPlayerCaught();

        bool isFinalCatch = catchCount >= catchesBeforeDeath;

        if (monsterAI != null)
        {
            monsterAI.PlayCatchSound(isFinalCatch);
        }

        isHandlingCatch = true;

        // Catch feedback briefly hides the scene and shows remaining chances before respawn/death.
        yield return ShowCatchOverlay(catchCount);

        if (isFinalCatch)
        {
            isHandlingCatch = false;
            TriggerDeath();
            yield break;
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

        isHandlingCatch = false;
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

    private IEnumerator ShowCatchOverlay(int catchCount)
    {
        GameObject overlayRoot = new GameObject("CatchOverlay", typeof(RectTransform));

        Canvas canvas = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 2;

        overlayRoot.AddComponent<CanvasScaler>();
        overlayRoot.AddComponent<GraphicRaycaster>();

        Image background = overlayRoot.AddComponent<Image>();
        background.color = catchOverlayBackgroundColor;

        RectTransform rootRect = overlayRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject textObject = new GameObject("CatchOverlayText", typeof(RectTransform));
        textObject.transform.SetParent(overlayRoot.transform, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = string.Format(
            catchOverlayTextFormat,
            Mathf.Clamp(catchCount, 1, catchesBeforeDeath),
            catchesBeforeDeath
        );
        text.color = catchOverlayTextColor;
        text.fontSize = catchOverlayFontSize;
        text.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        yield return new WaitForSecondsRealtime(Mathf.Max(0f, catchOverlayDuration));

        Destroy(overlayRoot);
    }
}
