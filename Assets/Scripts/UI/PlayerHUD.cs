using Mirror;
using Player;
using UnityEngine;

namespace UI
{
    public class PlayerHUD : NetworkBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject hudPrefab;

        private PlayerVitals vitals;
        private GameObject hudInstance;
        private RectTransform hpFillRect;
        private RectTransform hungerFillRect;
        private RectTransform staminaFillRect;
        private float hpMaxWidth;
        private float hungerMaxWidth;
        private float staminaMaxWidth;

        public override void OnStartLocalPlayer()
        {
            vitals = GetComponent<PlayerVitals>();
            TryCreateHud();
            UpdateBars();
        }

        public override void OnStopClient()
        {
            if (isLocalPlayer && hudInstance != null)
            {
                Destroy(hudInstance);
            }
        }

        private void LateUpdate()
        {
            if (!isLocalPlayer || hudInstance == null)
            {
                return;
            }

            if (vitals == null)
            {
                vitals = GetComponent<PlayerVitals>();
                if (vitals == null)
                {
                    return;
                }
            }

            UpdateBars();
        }

        private void TryCreateHud()
        {
            if (hudInstance != null)
            {
                return;
            }

            if (hudPrefab == null)
            {
                Debug.LogError("PlayerHUD needs a HUD prefab assigned.");
                return;
            }

            hudInstance = Instantiate(hudPrefab);

            PlayerHotbar playerHotbar = GetComponent<PlayerHotbar>();
            HotbarUI hotbarUI = hudInstance.GetComponentInChildren<HotbarUI>(true);

            if (hotbarUI == null)
            {
                Debug.LogError("HUD prefab is missing HotbarUI.");
            }
            else if (playerHotbar == null)
            {
                Debug.LogError("Player is missing PlayerHotbar.");
            }
            else
            {
                hotbarUI.SetHotbar(playerHotbar);
            }

            PlayerHUDRefs refs = hudInstance.GetComponent<PlayerHUDRefs>();

            if (refs == null)
            {
                Debug.LogError("HUD prefab is missing PlayerHUDRefs.");
                Destroy(hudInstance);
                hudInstance = null;
                return;
            }

            hpFillRect = refs.hpFillRect;
            hungerFillRect = refs.hungerFillRect;
            staminaFillRect = refs.staminaFillRect;

            if (hpFillRect == null || hungerFillRect == null || staminaFillRect == null)
            {
                Debug.LogError("PlayerHUDRefs is missing one or more fill rect assignments.");
                Destroy(hudInstance);
                hudInstance = null;
                return;
            }

            hpMaxWidth = GetInitialWidth(hpFillRect);
            hungerMaxWidth = GetInitialWidth(hungerFillRect);
            staminaMaxWidth = GetInitialWidth(staminaFillRect);
        }

        private float GetInitialWidth(RectTransform rect)
        {
            return Mathf.Max(1f, rect.rect.width, rect.sizeDelta.x);
        }

        private void UpdateBars()
        {
            if (vitals == null || hudInstance == null)
            {
                return;
            }

            SetBarWidth(hpFillRect, hpMaxWidth, vitals.HealthNormalized);
            SetBarWidth(hungerFillRect, hungerMaxWidth, vitals.HungerNormalized);
            SetBarWidth(staminaFillRect, staminaMaxWidth, vitals.StaminaNormalized);
        }

        private void SetBarWidth(RectTransform fillRect, float maxWidth, float normalized)
        {
            if (fillRect == null)
            {
                return;
            }

            normalized = Mathf.Clamp01(normalized);
            fillRect.sizeDelta = new Vector2(maxWidth * normalized, fillRect.sizeDelta.y);
        }
    }
}
