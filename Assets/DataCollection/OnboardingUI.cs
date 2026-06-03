using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OnboardingUI : MonoBehaviour
{
    [SerializeField] TMP_InputField ageInput;
    [SerializeField] TMP_Dropdown genderDropdown;
    [SerializeField] Button submitButton;
    [SerializeField] TMP_Text errorText;

    void Start()
    {
        submitButton.onClick.AddListener(OnSubmit);
        errorText.gameObject.SetActive(false);
    }

    void OnSubmit()
    {
        if (!int.TryParse(ageInput.text, out int age) || age < 1 || age > 120)
        {
            ShowError("Please enter a valid age (1–120).");
            return;
        }

        if (genderDropdown.value == 0)
        {
            ShowError("Please select a gender option.");
            return;
        }

        string gender = genderDropdown.options[genderDropdown.value].text;
        DataCollectionManager.Instance.SetParticipant(age, gender);
        gameObject.SetActive(false);
    }

    void ShowError(string message)
    {
        errorText.text = message;
        errorText.gameObject.SetActive(true);
    }
}
