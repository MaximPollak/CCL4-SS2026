using UnityEngine;

[RequireComponent(typeof(AkGameObj))]
public class SceneWwiseEventPlayer : MonoBehaviour
{
    [Header("Wwise Events")]
    [SerializeField] private string playEvent = "Play_story";
    [SerializeField] private string stopEvent = "";

    [Header("Playback")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private bool stopOnDisable;

    private bool isPlaying;

    private void OnEnable()
    {
        if (playOnEnable)
        {
            Play();
        }
    }

    private void OnDisable()
    {
        if (stopOnDisable)
        {
            Stop();
        }
    }

    public void Play()
    {
        if (isPlaying || string.IsNullOrWhiteSpace(playEvent))
        {
            return;
        }

        uint eventId = AkUnitySoundEngine.PostEvent(playEvent, gameObject);

        if (eventId == 0)
        {
            Debug.LogError("Scene Wwise event not found or SoundBank not loaded: " + playEvent);
            return;
        }

        isPlaying = true;
    }

    public void Stop()
    {
        if (!isPlaying || string.IsNullOrWhiteSpace(stopEvent))
        {
            return;
        }

        AkUnitySoundEngine.PostEvent(stopEvent, gameObject);
        isPlaying = false;
    }
}
