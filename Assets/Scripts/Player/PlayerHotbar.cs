using UnityEngine;
using Mirror;

namespace Player
{
    public class PlayerHotbar : NetworkBehaviour
    {
        public static PlayerHotbar LocalInstance;

        public int hotbarSize = 5;

        [SyncVar]
        private int selectedIndex = 0;

        public int[] hotbarItems;

        private void Awake()
        {
            hotbarItems = new int[hotbarSize];
        }

        public override void OnStartLocalPlayer()
        {
            LocalInstance = this;
        }

        private void OnDestroy()
        {
            if (LocalInstance == this)
                LocalInstance = null;
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
            if (TryPlaceItem(selectedIndex, itemID))
                return true;

            for (int i = 0; i < hotbarSize; i++)
            {
                if (TryPlaceItem(i, itemID))
                    return true;
            }

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

        private void SelectSlot(int index)
        {
            if (index < 0 || index >= hotbarSize) return;

            selectedIndex = index;
            Debug.Log("Selected slot: " + selectedIndex);
        }

        public int GetSelectedIndex()
        {
            return selectedIndex;
        }
    }
}