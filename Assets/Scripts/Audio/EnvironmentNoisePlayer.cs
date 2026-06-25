using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AkGameObj))]
public class EnvironmentNoisePlayer : MonoBehaviour
{
    [Header("Wwise Events")]
    [SerializeField] private string playEvent = "Play_enviorment_noise";
    [SerializeField] private string stopEvent = "Stop_enviorment_noise";

    [Header("White Noise Loop")]
    [SerializeField] private bool playWhiteNoise = true;
    [SerializeField] private string playWhiteNoiseEvent = "Play_white_noise";
    [SerializeField] private string stopWhiteNoiseEvent = "Stop_white_noise";

    [Header("Timing")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool playImmediately = true;
    [SerializeField] private float startupDelaySeconds = 0.25f;
    [SerializeField] private float initialDelaySeconds = 60f;
    [SerializeField] private float minimumCooldownSeconds = 90f;
    [SerializeField] private float maximumCooldownSeconds = 120f;

    private Coroutine noiseRoutine;
    private bool hasPostedPlayEvent;
    private bool isWhiteNoisePlaying;

    private void OnValidate()
    {
        startupDelaySeconds = Mathf.Max(0f, startupDelaySeconds);
        initialDelaySeconds = Mathf.Max(0f, initialDelaySeconds);
        minimumCooldownSeconds = Mathf.Max(0f, minimumCooldownSeconds);
        maximumCooldownSeconds = Mathf.Max(minimumCooldownSeconds, maximumCooldownSeconds);
    }

    private void OnEnable()
    {
        if (playOnEnable)
        {
            StartNoise();
        }
    }

    private void OnDisable()
    {
        StopNoise();
    }

    public void StartNoise()
    {
        if (noiseRoutine != null)
        {
            return;
        }

        noiseRoutine = StartCoroutine(PlayNoiseRoutine());
    }

    public void StopNoise()
    {
        if (noiseRoutine != null)
        {
            StopCoroutine(noiseRoutine);
            noiseRoutine = null;
        }

        StopWhiteNoise();

        if (!hasPostedPlayEvent)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent(stopEvent, gameObject);
        hasPostedPlayEvent = false;
    }

    private void StartWhiteNoise()
    {
        if (!playWhiteNoise || isWhiteNoisePlaying)
        {
            return;
        }

        uint eventId = AkUnitySoundEngine.PostEvent(playWhiteNoiseEvent, gameObject);
        isWhiteNoisePlaying = true;

        if (eventId == 0)
        {
            Debug.LogError("White noise event not found or SoundBank not loaded: " + playWhiteNoiseEvent);
        }
    }

    private void StopWhiteNoise()
    {
        if (!isWhiteNoisePlaying)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent(stopWhiteNoiseEvent, gameObject);
        isWhiteNoisePlaying = false;
    }

    private IEnumerator PlayNoiseRoutine()
    {
        if (startupDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(startupDelaySeconds);
        }

        StartWhiteNoise();

        if (initialDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(initialDelaySeconds);
        }

        if (!playImmediately)
        {
            yield return new WaitForSeconds(GetRandomCooldown());
        }

        while (true)
        {
            uint eventId = AkUnitySoundEngine.PostEvent(playEvent, gameObject);
            hasPostedPlayEvent = true;

            if (eventId == 0)
            {
                Debug.LogError("Environment noise event not found or SoundBank not loaded: " + playEvent);
            }

            yield return new WaitForSeconds(GetRandomCooldown());
        }
    }

    private float GetRandomCooldown()
    {
        if (Mathf.Approximately(minimumCooldownSeconds, maximumCooldownSeconds))
        {
            return minimumCooldownSeconds;
        }

        return Random.Range(minimumCooldownSeconds, maximumCooldownSeconds);
    }
}
