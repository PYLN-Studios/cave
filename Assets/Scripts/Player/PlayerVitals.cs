using Mirror;
using System;
using UnityEngine;
using Combat;

namespace Player
{
    public class PlayerVitals : NetworkBehaviour, IDamageable
    {
        [Header("Identity")]
        [SyncVar] private string persistenceKey = string.Empty;

        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SyncVar(hook = nameof(OnHealthChanged))] private float currentHealth = 100f;

        [Header("Hunger")]
        [SerializeField] private float maxHunger = 100f;
        [SerializeField] private float hungerDrainPerMinute = 10f;
        [SerializeField] private float hungerTickInterval = 1f;
        [SyncVar(hook = nameof(OnHungerChanged))] private float currentHunger = 100f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 5f;
        [SerializeField] private float staminaRegenDelay = 1f;
        [SerializeField] private float staminaRegenPerSecond = 2f;
        [SyncVar(hook = nameof(OnStaminaChanged))] private float currentStamina = 5f;

        [Header("State")]
        [SyncVar] private bool isAlive = true;

        private float hungerTickTimer;
        private float staminaRegenDelayTimerServer;
        private bool sprintIntentServer;
        private bool moveIntentServer;
        // Set when save data is applied before OnStartServer defaults run
        private bool loadedFromSaveBeforeSpawn;

        private bool lastSprintIntentSent;
        private bool lastMoveIntentSent;

        public string PersistenceKey => persistenceKey;
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float MaxHunger => maxHunger;
        public float CurrentHunger => currentHunger;
        public float MaxStamina => maxStamina;
        public float CurrentStamina => currentStamina;
        public bool IsAlive => isAlive;

        public float HealthNormalized => maxHealth <= 0f ? 0f : currentHealth / maxHealth;
        public float HungerNormalized => maxHunger <= 0f ? 0f : currentHunger / maxHunger;
        public float StaminaNormalized => maxStamina <= 0f ? 0f : currentStamina / maxStamina;

        // Initialize default vitals unless save is loaded
        public override void OnStartServer()
        {
            if (loadedFromSaveBeforeSpawn)
            {
                loadedFromSaveBeforeSpawn = false;
                return;
            }

            currentHealth = maxHealth;
            currentHunger = maxHunger;
            currentStamina = maxStamina;
            isAlive = true;
            sprintIntentServer = false;
            moveIntentServer = false;
            staminaRegenDelayTimerServer = 0f;
        }

        [ServerCallback]
        private void Update()
        {
            TickHunger();
            TickStamina();
        }

        [Server]
        public void SetPersistenceKey(string playerId)
        {
            persistenceKey = playerId ?? string.Empty;
        }

        // Apply vitals loaded for this player id
        [Server]
        public void ApplySavedData(PlayerVitalsSaveData data)
        {
            if (data == null)
            {
                return;
            }

            currentHealth = Mathf.Clamp(data.health, 0f, maxHealth);
            currentHunger = Mathf.Clamp(data.hunger, 0f, maxHunger);
            currentStamina = Mathf.Clamp(data.stamina, 0f, maxStamina);
            isAlive = currentHealth > 0f;
            sprintIntentServer = false;
            moveIntentServer = false;
            staminaRegenDelayTimerServer = 0f;
            loadedFromSaveBeforeSpawn = true;
        }

        [Server]
        public PlayerVitalsSaveData CreateSaveData()
        {
            return new PlayerVitalsSaveData
            {
                playerId = persistenceKey,
                health = currentHealth,
                hunger = currentHunger,
                stamina = currentStamina,
                updatedUtc = DateTime.UtcNow.ToString("o")
            };
        }

        [Server]
        public void ApplyDamage(float damage)
        {
            if (!isAlive || damage <= 0f)
            {
                return;
            }

            IncreaseHealth(-damage);
        }

        [Server]
        public void ReviveToFull()
        {
            isAlive = true;
            currentHealth = maxHealth;
        }

        // Submit local sprint intent while reading stamina
        [Client]
        public bool ResolveSprint(float _, bool sprintPressed, bool hasMoveInput)
        {
            if (!isLocalPlayer)
            {
                return false;
            }

            SubmitSprintIntent(sprintPressed, hasMoveInput);
            return sprintPressed && hasMoveInput && currentStamina > 0f;
        }

        // Only send intent changes to reduce command traffic
        [Client]
        private void SubmitSprintIntent(bool sprintPressed, bool hasMoveInput)
        {
            if (!isLocalPlayer)
            {
                return;
            }

            if (sprintPressed == lastSprintIntentSent && hasMoveInput == lastMoveIntentSent)
            {
                return;
            }

            lastSprintIntentSent = sprintPressed;
            lastMoveIntentSent = hasMoveInput;
            CmdSetSprintIntent(sprintPressed, hasMoveInput);
        }

        // Server receives local movement intent and runs stamina simulation
        [Command]
        private void CmdSetSprintIntent(bool sprintPressed, bool hasMoveInput)
        {
            sprintIntentServer = sprintPressed;
            moveIntentServer = hasMoveInput;
        }

        [Server]
        public void SetCurrentHunger(float hunger)
        {
            currentHunger = Mathf.Clamp(hunger, 0f, maxHunger);
        }
        [Server]
        public void IncreaseHunger(float hunger)
        {
            SetCurrentHunger(currentHunger + hunger);
        }

        [Server]
        public void SetCurrentHealth(float health)
        {
            currentHealth = Mathf.Clamp(health, 0f, maxHealth);
            if (currentHealth > 0f)
            {
                isAlive = true;
            }
            else if (currentHealth <= 0f)
            {
                isAlive = false;
                sprintIntentServer = false;
                moveIntentServer = false;
            }
        }
        [Server]
        public void IncreaseHealth(float health)
        {
            SetCurrentHealth(currentHealth + health);
        }

        [Server]
        public void SetCurrentStamina(float stamina)
        {
            currentStamina = Mathf.Clamp(stamina, 0f, maxStamina);
        }
        [Server]
        public void IncreaseStamina(float stamina)
        {
            SetCurrentStamina(currentStamina + stamina);
        }


        [Server]
        private void TickHunger()
        {
            if (currentHunger <= 0f)
            {
                return;
            }

            hungerTickTimer += Time.deltaTime;
            if (hungerTickTimer < hungerTickInterval)
            {
                return;
            }

            float elapsed = hungerTickTimer;
            hungerTickTimer = 0f;

            float hungerDrainPerSecond = hungerDrainPerMinute / 60f;
            currentHunger = Mathf.Clamp(currentHunger - hungerDrainPerSecond * elapsed, 0f, maxHunger);
        }

        // Stamina drain/regen with regen delay.
        [Server]
        private void TickStamina()
        {
            if (!isAlive)
            {
                return;
            }

            if (sprintIntentServer && moveIntentServer && currentStamina > 0f)
            {
                currentStamina = Mathf.Clamp(currentStamina - Time.deltaTime, 0f, maxStamina);
                staminaRegenDelayTimerServer = staminaRegenDelay;
                return;
            }

            if (staminaRegenDelayTimerServer > 0f)
            {
                staminaRegenDelayTimerServer -= Time.deltaTime;
                return;
            }

            if (sprintIntentServer && moveIntentServer)
            {
                return;
            }

            if (currentStamina < maxStamina)
            {
                currentStamina = Mathf.Clamp(currentStamina + staminaRegenPerSecond * Time.deltaTime, 0f, maxStamina);
            }
        }

        private void OnHealthChanged(float oldValue, float newValue)
        {
        }

        private void OnHungerChanged(float _, float __) { }

        private void OnStaminaChanged(float _, float __) { }
    }
}
