using UnityEngine;
using Mirror;

namespace Player
{
    public class PlayerHotbar : NetworkBehaviour
    {
        public int hotbarSize = 5;

        [SyncVar]
        private int selectedIndex = 0;

        public string[] hotbarItems;

        private void Awake()
        {
            hotbarItems = new string[hotbarSize];
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            if (Input.GetKeyDown(KeyCode.Alpha1)) SelectSlot(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SelectSlot(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SelectSlot(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SelectSlot(3);
            if (Input.GetKeyDown(KeyCode.Alpha5)) SelectSlot(4);
        }

        public bool AddItem(string itemID)
        {
            for (int i = 0; i < hotbarSize; i++)
            {
                if (string.IsNullOrEmpty(hotbarItems[i]))
                {
                    hotbarItems[i] = itemID;
                    Debug.Log("Added item " + itemID + " to hotbar slot " + i);
                    return true;
                }
            }

            Debug.Log("Hotbar full!");
            return false;
        }

        public string GetSelectedItem()
        {
            return hotbarItems[selectedIndex];
        }

        public void SelectSlot(int slot)
        {
            if (slot >= 0 && slot < hotbarSize)
            {
                selectedIndex = slot;
                Debug.Log("Selected hotbar slot: " + slot);
            }
        }
    }
}