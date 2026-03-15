using UnityEngine;
using Mirror;

namespace Player
{
    public class PlayerHotbar : NetworkBehaviour
    {
        public int hotbarSize = 5;

        [SyncVar]
        private int selectedIndex = 0;

        public int[] hotbarItems;

        private void Awake()
        {
            hotbarItems = new int[hotbarSize];
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

        public bool AddItem(int itemID)
        {
            // Try selected slot first
            if (TryPlaceItem(selectedIndex, itemID))
                return true;

            // Else try lowest empty slot
            for (int i = 0; i < hotbarSize; i++)
                if (TryPlaceItem(i, itemID))
                    return true;

            Debug.Log("Hotbar full!");
            return false;
        }

        private bool TryPlaceItem(int slot, int itemID)
        {
            if (hotbarItems[slot] == 0)
            {
                hotbarItems[slot] = itemID;
                return true;
            }
            return false;
        }

        public int GetSelectedItem()
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
        public int GetSelectedIndex()
        {
            return selectedIndex;
        }
    }
}