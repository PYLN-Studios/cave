using Mirror;
using StarterAssets;
using UnityEngine;
using UnityEngine.Windows;
using Projectiles;
using UnityEngine.Splines;



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
        [SyncVar] public float currHealth = 100f;
        public float maxHealth = 100f;

        [Header("Player Offensive Stats")]
        public string weaponName = "Spear";
        public float rateOfFire = 1f;
        public float currCooldown = 0f;

        [Header("Projectile Settings")]
        [Tooltip("Spear prefab to spawn")]
        public GameObject spearPrefab;

        [Header("Player State")]
        [SyncVar] public bool isAlive = true;

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

        private uint projectileGuid;

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
            //System.Guid projectileAssetGuid = System.Guid.NewGuid();
            //projectileGuid = NetworkIdentity.AssetGuidToUint(projectileAssetGuid);

            //Debug.Log("registering spear as asset ID: " + projectileGuid);
            //NetworkClient.RegisterPrefab(spearPrefab, projectileGuid, SpawnProjectile, UnSpawnProjectile);
        }

        [Client]
        private void Update()
        {
            if (!isLocalPlayer) return;
            if (_input == null) return;
            
            if (!isAlive)
            {
                Respawn();
                isAlive = true;
            }
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
                    Vector3 spawnPosition = _mainCamera.transform.position + _mainCamera.transform.forward;
                    Quaternion spawnRotation = _mainCamera.transform.rotation;
                    // TODO add player velocity to projectile speed

                    CmdSpawnProjectile(spawnPosition, spawnRotation);
                    currCooldown = 1f / rateOfFire;
                }

                // input buffering
                if (currCooldown >= attackInputBufferTime)
                {
                    _input.attack = false;
                }
            }
        }

        // [Command] functions are called on the client but executed on the server
        [Command]
        void CmdSpawnProjectile(Vector3 spawnPosition, Quaternion spawnRotation)
        {
            Debug.Log("CmdSpawnProjectile called on server");

            // Instantiate the spear
            GameObject newProjectile = Instantiate(spearPrefab, spawnPosition, spawnRotation);

            // Get SpearData defaults
            SpearData data = SpearData.Default;

            // Initialize the spear with data
            BasicProjectile basicProjectile = newProjectile.GetComponent<BasicProjectile>();
            if (basicProjectile != null)
            {
                basicProjectile.Initialize(
                    spawnPosition,
                    data.speed,
                    spawnRotation,
                    data.lifetime,
                    damage: data.damage,
                    weight: data.weight,
                    drag: data.drag,
                    playerDamageMultiplier: data.playerDamageMultiplier,
                    lingerDuration: data.lingerDuration
                );
                basicProjectile.creator = this.gameObject;
            }
            else
            {
                Debug.LogError("Spear prefab does not have BasicProjectile component!");
            }

            // Spawn the projectile on the network
            NetworkServer.Spawn(newProjectile);
        }

        [Server]
        public void ApplyDamage(float damage)
        {
            Debug.Log($"Player {name} took {damage} damage.");
            currHealth -= damage;
            if (currHealth <= 0f)
            {
                Die();
            }
        }

        [Server]
        void Die()
        {
            // Eventually do something here, for now just set health back
            //Destroy(gameObject);

            Debug.Log($"Player died, resetting HP");
            isAlive = false;
            currHealth = maxHealth;
        }

        void Respawn()
        {
            transform.position += new Vector3(0f, 5f, 0f);
        }
    }
}
