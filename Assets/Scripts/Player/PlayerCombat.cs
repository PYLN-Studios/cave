using Mirror;
using StarterAssets;
using UnityEngine;
using Projectiles;

namespace Player
{
    [RequireComponent(typeof(PlayerVitals))]
    public class PlayerCombat : NetworkBehaviour
    {
        [Header("Player Offensive Stats")]
        public string weaponName = "Spear";
        public float rateOfFire = 1f;
        public float currCooldown = 0f;

        [Header("Projectile Settings")]
        [Tooltip("Spear prefab to spawn")]
        public GameObject spearPrefab;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;
        private PlayerVitals _vitals;
        private bool _respawnRequestPending;

        // Quality of life: don't clear the attack action if its done when almost ready
        private float attackInputBufferTime = 0.2f;


        public override void OnStartLocalPlayer()
        {
            // Re-grab references just in case
            _input = GetComponent<StarterAssetsInputs>();

            // spawn projectiles based on first person view camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _input = GetComponent<StarterAssetsInputs>();
            _vitals = GetComponent<PlayerVitals>();

            if (_vitals == null)
            {
                Debug.LogError("PlayerVitals component is required on the player.");
            }

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
            if (_vitals == null) _vitals = GetComponent<PlayerVitals>();

            if (_vitals != null && !_vitals.IsAlive)
            {
                if (!_respawnRequestPending)
                {
                    _respawnRequestPending = true;
                    CmdRequestRespawn();
                }
                return;
            }

            _respawnRequestPending = false;
            CheckAttackInput();
        }

        void CheckAttackInput()
        {
            currCooldown -= Time.deltaTime;
            if (_input.attack)
            {
                if (currCooldown <= 0f)
                {
                    if (_mainCamera == null)
                    {
                        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
                    }
                    if (_mainCamera == null)
                    {
                        Debug.LogWarning("Attack ignored because MainCamera was not found.");
                        return;
                    }

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
            if (_vitals == null)
            {
                _vitals = GetComponent<PlayerVitals>();
            }

            if (_vitals != null)
            {
                _vitals.ApplyDamage(damage);
            }
        }

        [Command]
        private void CmdRequestRespawn()
        {
            if (_vitals == null)
            {
                _vitals = GetComponent<PlayerVitals>();
            }

            if (_vitals == null || _vitals.IsAlive)
            {
                return;
            }

            _vitals.ReviveToFull();
            Vector3 respawnPosition = transform.position + new Vector3(0f, 5f, 0f);
            transform.position = respawnPosition;
            TargetCompleteRespawn(connectionToClient, respawnPosition);
        }

        [TargetRpc]
        private void TargetCompleteRespawn(NetworkConnectionToClient target, Vector3 respawnPosition)
        {
            transform.position = respawnPosition;
            _respawnRequestPending = false;
        }
    }
}
