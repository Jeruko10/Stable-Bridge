using TMPro;
using UnityEngine;

public class OnboardingUI : MonoBehaviour
{
    [SerializeField] TMP_InputField ageInput;
    [SerializeField] TMP_Dropdown genderDropdown;
    [SerializeField] TMP_Text errorText;

    void Start()
    {
        errorText.gameObject.SetActive(false);
    }

    public void SubmitData()
    {
        if (!int.TryParse(ageInput.text, out int age) || age < 1 || age > 120)
        {
            ShowError("Please enter a valid age (1–120).");
            return;
        }

        string gender = genderDropdown.options[genderDropdown.value].text;
        DataCollectionManager.Instance.SetParticipant(age, gender);
    }

    void ShowError(string message)
    {
        Debug.LogWarning(message);
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }
}
