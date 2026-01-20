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
    public class PlayerCombat : NetworkBehaviour
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

        private MainGameWorldNetworkManager world;
        private MainGameWorldNetworkManager World
        {
            get
            {
                if (world != null) { return world; }
                return world = MainGameWorldNetworkManager.singleton as MainGameWorldNetworkManager;
            }
        }

        private const float _threshold = 0.01f;

        // Quality of life: don't clear the attack action if its done when almost ready
        private float attackInputBufferTime = 0.2f;

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
            //NetworkClient.RegisterPrefab(spearPrefab, SpawnProjectile, UnSpawnProjectile);
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
                    CmdSpawnProjectile();
                    currCooldown = 1f / rateOfFire;
                }

                // input buffering
                if (currCooldown >= attackInputBufferTime)
                {
                    _input.attack = false;
                }
            }
        }

        //// Used by NetworkClient.RegisterPrefab
        //GameObject SpawnProjectile(Vector3 position, uint assetId)
        //{
        //    Debug.Log("Spawning projectile with assetId: " + assetId);

        //    return newProjectile;
        //}

        //// Used by NetworkClient.RegisterPrefab
        //void UnSpawnProjectile(GameObject spawned)
        //{
        //    Debug.Log("Unspawning projectile" + spawned.name);
        //    Destroy(spawned);
        //}

        // [Command] functions are called on the client but executed on the server
        [Command]
        void CmdSpawnProjectile()
        {
            //Debug.Log("CmdSpawnProjectile called on server");

            // Get spawn position and rotation
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position + transform.forward;
            Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            // Instantiate the spear
            GameObject newProjectile = Instantiate(spearPrefab) as GameObject;

            // Get SpearData defaults
            SpearData data = SpearData.Default;

            // Initialize the spear with data
            BasicProjectile basicProjectile = newProjectile.GetComponent<BasicProjectile>();
            if (basicProjectile != null)
            {
                Debug.Log($"projectile initialized with damage {data.damage}");
                basicProjectile.Initialize(
                    spawnPosition,
                    data.speed,
                    spawnRotation,
                    data.lifetime,
                    damage: data.damage,
                    weight: 0.1f,
                    drag: data.drag,
                    playerDamageMultiplier: data.playerDamageMultiplier,
                    lingerDuration: data.lingerDuration
                );
                basicProjectile.creator = this.gameObject;
                Debug.Log($"projectile initialized with damage {data.damage}");
            }
            else
            {
                Debug.LogError("Spear prefab does not have BasicProjectile component!");
            }

            NetworkServer.Spawn(newProjectile);
        }

        public void ApplyDamage(float damage)
        {
            Debug.Log($"Player {name} took {damage} damage.");
            currHealth -= damage;
            if (currHealth <= 0f)
            {
                Die();
            }
        }

        void Die()
        {
            // Eventually do something here, for now just set health back
            //Destroy(gameObject);

            Debug.Log($"Player died, resetting HP");
            transform.position += new Vector3(0f, 5f, 0f);
            currHealth = maxHealth;
        }

        void OnDestroy()
        {
            NetworkClient.UnregisterPrefab(spearPrefab);
        }
    }
}
