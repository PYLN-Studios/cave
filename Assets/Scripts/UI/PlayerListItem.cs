using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour {
    [SerializeField] private TMP_Text playerNameText = null;
    [SerializeField] private TMP_Text readyStatusText = null;
    [SerializeField] private Image backgroundImage = null;

    [Header("Colors")]
    [SerializeField] private Color readyColor = new Color(0.2f, 0.8f, 0.2f, 0.3f);
    [SerializeField] private Color notReadyColor = new Color(0.8f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color localPlayerColor = new Color(0.2f, 0.5f, 0.8f, 0.3f);

    public void Setup(string playerName, bool isReady, bool isLocalPlayer) {
        Debug.Log($"name {playerName} status {isReady}");
        playerNameText.text = playerName;

        if (isReady) {
            readyStatusText.text = "Ready";
            readyStatusText.color = Color.green;
        } else {
            readyStatusText.text = "Not Ready";
            readyStatusText.color = Color.red;
        }

        // Optional: highlight local player or show ready state via background
        if (backgroundImage != null) {
            if (isLocalPlayer)
                backgroundImage.color = localPlayerColor;
            else if (isReady)
                backgroundImage.color = readyColor;
            else
                backgroundImage.color = notReadyColor;
        }
    }
}