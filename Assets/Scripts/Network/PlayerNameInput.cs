using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PlayerNameInput : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_InputField nameInputField = null;
    [SerializeField] private Button joinButton = null;
    [SerializeField] private Button hostButton = null;

    public static string DisplayName { get; private set; } = "Player";

    private const string PlayerPrefsNameKey = "PlayerName";
    private const int MinNameLength = 2;
    private const int MaxNameLength = 20;

    private void Start()
    {
        SetUpInputField();
    }

    private void SetUpInputField()
    {
        if (PlayerPrefs.HasKey(PlayerPrefsNameKey))
        {
            string savedName = PlayerPrefs.GetString(PlayerPrefsNameKey);
            nameInputField.text = savedName;
            DisplayName = savedName;
        }

        // Set character limit
        nameInputField.characterLimit = MaxNameLength;

        // Update button states based on initial value
        UpdateButtonStates(nameInputField.text);

        // Subscribe to input changes
        nameInputField.onValueChanged.AddListener(UpdateButtonStates);
    }

    private void UpdateButtonStates(string name)
    {
        bool isValidName = name.Length >= MinNameLength;

        if (joinButton != null)
            joinButton.interactable = isValidName;

        if (hostButton != null)
            hostButton.interactable = isValidName;
    }

    public void SetPlayerName(string name)
    {
        if (name.Length < MinNameLength) { return; }

        DisplayName = name;
        PlayerPrefs.SetString(PlayerPrefsNameKey, name);
    }

    // Call this from the input field's OnEndEdit event
    public void SavePlayerName()
    {
        SetPlayerName(nameInputField.text);
    }
}
