using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonTextHoverStyle : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private string hoverSoundEvent = "Play_button_click";


    private FontStyles normalStyle;
    private int lastHoverSoundFrame = -1;

    private void Awake()
    {
        if (buttonText != null)
        {
            normalStyle = buttonText.fontStyle;
        }
    }

    public void MakeBold()
    {
        if (buttonText != null)
        {
            buttonText.fontStyle = normalStyle | FontStyles.Bold;
        }

        PlayHoverSound();
    }

    public void MakeNormal()
    {
        if (buttonText != null)
        {
            buttonText.fontStyle = normalStyle;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayHoverSound();
    }

    private void PlayHoverSound()
    {
        if (lastHoverSoundFrame == Time.frameCount)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(hoverSoundEvent))
        {
            return;
        }

        lastHoverSoundFrame = Time.frameCount;
        uint eventId = AkUnitySoundEngine.PostEvent(hoverSoundEvent, gameObject);

        if (eventId == 0)
        {
            Debug.LogError("Button hover sound event not found or SoundBank not loaded: " + hoverSoundEvent);
        }
    }
}
