using UnityEngine;
using Player;

public class HotbarUI : MonoBehaviour
{
    public GameObject[] highlights;

    void Start()
    {
        for (int i = 0; i < highlights.Length; i++)
        {
            if (highlights[i] != null)
            {
                highlights[i].SetActive(false);
            }
        }
    }

    void Update()
    {
        if (PlayerHotbar.LocalInstance == null) return;

        int selected = PlayerHotbar.LocalInstance.GetSelectedIndex();

        Debug.Log("UI selected slot: " + selected);
        Debug.Log("LocalInstance: " + PlayerHotbar.LocalInstance);

        for (int i = 0; i < highlights.Length; i++)
        {
            if (highlights[i] != null)
            {
                highlights[i].SetActive(i == selected);
            }
        }
    }
}