using UnityEngine;
using UnityEngine.UI;

namespace Player
{
    public class HotbarUI : MonoBehaviour
    {
        public Image[] slotImages;     // slot backgrounds
        public Color normalColor = new Color(1,1,1,0.4f);
        public Color selectedColor = Color.white;

        private PlayerHotbar playerHotbar;

        void Start()
        {
            playerHotbar = FindObjectOfType<PlayerHotbar>();
        }

        void Update()
        {
            if (playerHotbar == null) return;

            UpdateSlots(playerHotbar.GetSelectedIndex());
        }

        void UpdateSlots(int selected)
        {
            for (int i = 0; i < slotImages.Length; i++)
            {
                if (i == selected)
                    slotImages[i].color = selectedColor;
                else
                    slotImages[i].color = normalColor;
            }
        }
    }
}