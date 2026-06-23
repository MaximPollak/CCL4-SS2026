using UnityEngine;

public class SubmarineCodeBox : MonoBehaviour, IInteractable
{
    [Header("Repair")]
    [SerializeField] private SubmarineRepairManager repairManager;
    [SerializeField] private SubmarineRepairTask repairTask = SubmarineRepairTask.CodeBox;

    [Header("Code")]
    [SerializeField] private string correctCode = "2479";
    [SerializeField] private bool useKeyboardNumberInput = true;

    [Header("Visual Result")]
    [SerializeField] private GameObject objectToDisableAfterUnlock;
    [SerializeField] private GameObject objectToEnableAfterUnlock;

    [Header("Messages")]
    [SerializeField] private string promptMessage = "Enter code with number keys.";
    [SerializeField] private string wrongCodeMessage = "Wrong code.";
    [SerializeField] private string completedMessage = "Code accepted.";

    private string enteredCode = "";
    private bool isEnteringCode;

    private void Update()
    {
        if (!isEnteringCode || !useKeyboardNumberInput)
        {
            return;
        }

        for (int digit = 0; digit <= 9; digit++)
        {
            KeyCode alphaKey = (KeyCode)((int)KeyCode.Alpha0 + digit);
            KeyCode keypadKey = (KeyCode)((int)KeyCode.Keypad0 + digit);

            if (Input.GetKeyDown(alphaKey) || Input.GetKeyDown(keypadKey))
            {
                AddDigit(digit.ToString());
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace) && enteredCode.Length > 0)
        {
            enteredCode = enteredCode.Substring(0, enteredCode.Length - 1);
            Debug.Log("Code: " + enteredCode);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isEnteringCode = false;
            enteredCode = "";
            Debug.Log("Code entry cancelled.");
        }
    }

    public void Interact(PlayerInteraction player)
    {
        if (repairManager != null && repairManager.IsTaskComplete(repairTask))
        {
            Debug.Log("Already completed: " + repairTask);
            return;
        }

        isEnteringCode = true;
        enteredCode = "";
        Debug.Log(promptMessage);
    }

    private void AddDigit(string digit)
    {
        if (enteredCode.Length >= correctCode.Length)
        {
            return;
        }

        enteredCode += digit;
        Debug.Log("Code: " + enteredCode);

        if (enteredCode.Length == correctCode.Length)
        {
            CheckCode();
        }
    }

    private void CheckCode()
    {
        if (enteredCode != correctCode)
        {
            Debug.Log(wrongCodeMessage);
            enteredCode = "";
            return;
        }

        isEnteringCode = false;

        if (objectToDisableAfterUnlock != null)
        {
            objectToDisableAfterUnlock.SetActive(false);
        }

        if (objectToEnableAfterUnlock != null)
        {
            objectToEnableAfterUnlock.SetActive(true);
        }

        if (repairManager != null)
        {
            repairManager.CompleteTask(repairTask);
        }

        Debug.Log(completedMessage);
    }
}
