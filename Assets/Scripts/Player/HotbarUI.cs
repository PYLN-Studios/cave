using UnityEngine;
using Player;

namespace UI
{
    public class HotbarUI : MonoBehaviour
    {
        [SerializeField] private GameObject[] highlights;

        private PlayerHotbar hotbar;

        public void SetHotbar(PlayerHotbar playerHotbar)
        {
            hotbar = playerHotbar;
            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (hotbar == null || highlights == null) return;

            int selected = hotbar.GetSelectedIndex();

            for (int i = 0; i < highlights.Length; i++)
            {
                if (highlights[i] != null)
                {
                    highlights[i].SetActive(i == selected);
                }
            }
        }
    }
}