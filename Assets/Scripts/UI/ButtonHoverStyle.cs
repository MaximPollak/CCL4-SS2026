using TMPro;
using UnityEngine;

public class ButtonTextHoverStyle : MonoBehaviour
{
    [SerializeField] private TMP_Text buttonText;


    private FontStyles normalStyle;

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
    }

    public void MakeNormal()
    {
        if (buttonText != null)
        {
            buttonText.fontStyle = normalStyle;
        }
    }
}