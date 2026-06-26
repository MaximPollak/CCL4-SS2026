using UnityEngine;

[RequireComponent(typeof(AkGameObj))]
public class MenuMusicPlayer : MonoBehaviour
{
    [Header("Wwise Events")]
    [SerializeField] private string playMenuMusicEvent = "Play_menu_music";
    [SerializeField] private string stopMenuMusicEvent = "Stop_menu_music";

    private bool isPlaying;

    private void OnEnable()
    {
        PlayMenuMusic();
    }

    private void OnDisable()
    {
        StopMenuMusic();
    }

    private void PlayMenuMusic()
    {
        if (isPlaying)
        {
            return;
        }

        uint eventId = AkUnitySoundEngine.PostEvent(playMenuMusicEvent, gameObject);

        if (eventId == 0)
        {
            Debug.LogError("Menu music event not found or SoundBank not loaded: " + playMenuMusicEvent);
            return;
        }

        isPlaying = true;
    }

    private void StopMenuMusic()
    {
        if (!isPlaying)
        {
            return;
        }

        AkUnitySoundEngine.PostEvent(stopMenuMusicEvent, gameObject);
        isPlaying = false;
    }
}