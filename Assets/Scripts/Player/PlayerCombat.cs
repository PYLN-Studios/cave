using Mirror;
using StarterAssets;
using UnityEngine;
using UnityEngine.Windows;
using Projectiles;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Player
{
    public class CombatScript : NetworkBehaviour
    {
        // TODO eventually we probably want to make players and enemies inherit from the same class

        [Header("Player Defensive Stats")]
        [Tooltip("Health")]
        public float currHealth = 100f;
        public float maxHealth = 100f;

        [Header("Player Offensive Stats")]
        public string weaponName = "Spear";
        public float rateOfFire = 1f;
        public float currCooldown = 0f;

        [Header("Projectile Settings")]
        [Tooltip("Spear prefab to spawn")]
        public GameObject spearPrefab;
        [Tooltip("Where the spear spawns from")]
        public Transform spawnPoint;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;

        // Quality of life: don't clear the attack action if its done when almost ready
        private float attackInputBufferTime = 0.2f;

        public void ApplyDamage(float damage)
        {
            Debug.Log($"Player took {damage} damage.");
            currHealth -= damage;
            if (currHealth <= 0f)
            {
                Die();
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            _input = GetComponent<StarterAssetsInputs>();

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif

            if (!isLocalPlayer)
            {
                SetInputEnabled(false);
            }
        }


        private void SetInputEnabled(bool enabled)
        {
#if ENABLE_INPUT_SYSTEM
            if (_playerInput != null)
            {
                _playerInput.enabled = enabled;

                if (enabled)
                {
                    _playerInput.neverAutoSwitchControlSchemes = true;
                    _playerInput.ActivateInput();
                }
            }
#endif

            if (_input != null)
            {
                _input.enabled = enabled;
            }
        }


        public override void OnStartLocalPlayer()
        {
            // Re-grab references just in case
            _input = GetComponent<StarterAssetsInputs>();

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif
            SetInputEnabled(true);

            // spawn projectiles based on first person view camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
            // Set spawn point to camera if not set
            if (spawnPoint == null && _mainCamera != null)
            {
                spawnPoint = _mainCamera.transform;
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();

            // Load weapon prefab if not set
            // TODO this doesnt work, try using Addressable Asset System
            if (spearPrefab == null)
            {
                spearPrefab = Resources.Load<GameObject>("Prefabs/Projectiles/Spear");
                if (spearPrefab == null)
                {
                    Debug.LogError("Failed to load spear prefab from Resources. Make sure it's at Assets/Resources/Prefabs/Projectiles/Spear.prefab");
                }
            }
        }

        [Client]
        private void Update()
        {
            if (!isLocalPlayer) return;
            if (_input == null) return;
            CheckAttackInput();
        }

        void CheckAttackInput()
        {
            currCooldown -= Time.deltaTime;
            if (_input.attack)
            {
                if (currCooldown <= 0f)
                {
                    Debug.Log("Player attack input detected");
                    CmdThrowSpear();
                    currCooldown = 1f / rateOfFire;
                }

                // input buffering
                if (currCooldown >= attackInputBufferTime)
                {
                    _input.attack = false;
                }
            }
        }

        [Command]
        void CmdThrowSpear()
        {
            Debug.Log("is server? " + isServer);

            if (spearPrefab == null)
            {
                Debug.LogError("Spear prefab is not assigned!");
                return;
            }

            // Verify the prefab has NetworkIdentity
            if (spearPrefab.GetComponent<NetworkIdentity>() == null)
            {
                Debug.LogError("Spear prefab must have a NetworkIdentity component!");
                return;
            }

            // Get spawn position and rotation
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward;
            Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            // Instantiate the spear
            GameObject spear = Instantiate(spearPrefab) as GameObject;

            // Get SpearData defaults
            SpearData data = SpearData.Default;

            // Initialize the spear with data
            BasicProjectile projectile = spear.GetComponent<BasicProjectile>();
            if (projectile != null)
            {
                //projectile.Initialize(
                //    spawnPosition,
                //    data.speed,
                //    spawnRotation,
                //    data.lifetime,
                //    data.damage,
                //    data.weight,
                //    data.drag,
                //    data.playerDamageMultiplier,
                //    data.lingerTime
                //);
                Vector3 testSpawnPoint = Vector3.zero;
                testSpawnPoint.x = 5f;
                testSpawnPoint.y = 5f;
                testSpawnPoint.z = 5f;
                projectile.Initialize(
                    testSpawnPoint,
                    1f,
                    Quaternion.identity,
                    data.lifetime,
                    data.damage,
                    0.01f,
                    data.drag,
                    data.playerDamageMultiplier,
                    data.lingerTime
                );
            }
            else
            {
                Debug.LogError("Spear prefab does not have BasicProjectile component!");
            }

            if (spear)
            {
                Debug.Log("spear is spear");
            }

            // Spawn on network
            NetworkServer.Spawn(spear);
            Debug.Log("Spear spawned on network");
        }

        void Die()
        {
            // Eventually do something here, for now just set health back
            //Destroy(gameObject);

            Debug.Log($"Player died, resetting HP");
            transform.position += new Vector3(0f, 2f, 0f);
            currHealth = maxHealth;
        }
    }
}
