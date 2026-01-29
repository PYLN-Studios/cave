using Enemies;
using Mirror;
using UnityEngine;
using Player;


namespace Projectiles
{
    public class BasicProjectile : NetworkBehaviour
    {
        // position and velocity
        private Vector3 velocity;
        private Quaternion angle = Quaternion.identity;
        private float weight;  // how much the projectile is affected by gravity
        private float drag;    // how much the projectile is slowed by air resistance
        private float maxTurnSpeed = 60f; // degrees per second, how fast it turns while flying ballistically

        // combat params
        [SerializeField] private float damage;
        [SerializeField] private float playerDamageMultiplier;

        // lifetime
        [SerializeField] private float duration;
        [SerializeField] private float lingerDuration; // set to 0 to destroy on impact

        // keep track of projectile lifetime and state
        [SerializeField] private float aliveTime = 0f;
        [SerializeField] private bool isAlive = true;
        [SerializeField] private float lingerTime = 0f;

        // projectile should not shoot self during the first few seconds
        public GameObject creator;

        // Initialize the projectile with parameters
        public void Initialize(
            Vector3 position,
            float speed,
            Quaternion angle,
            float lifetime,
            float damage = 0f,
            float weight = 1f,
            float drag = 0f,
            float playerDamageMultiplier = 1f,
            float lingerDuration = 0f
            )
        {
            transform.position = position;
            this.velocity = angle * Vector3.forward * speed;
            this.angle = angle;
            this.duration = lifetime;
            this.damage = damage;
            this.weight = weight;
            this.drag = drag;
            this.playerDamageMultiplier = playerDamageMultiplier;
            this.lingerDuration = lingerDuration;

            //Debug.Log($"projectile initialized with position {transform.position} and velocity {this.velocity}");
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            Debug.Log("projectile started!");
        }

        // Update is called once per frame
        [ServerCallback]
        void Update()
        {
            if (!isServer)
            {
                return;
            }

            if (!isAlive)
            {
                this.lingerTime += Time.deltaTime;
                if (this.lingerTime >= this.lingerDuration)
                {
                    NetworkServer.Destroy(gameObject);
                }
            } 
            else
            {
                aliveTime += Time.deltaTime;
                if (aliveTime >= duration)
                {
                    Destroy(gameObject);
                }
                // apply gravity first
                velocity += Physics.gravity * weight * Time.deltaTime;

                // clamp drag factor to [0,1]
                float dragFactor = Mathf.Clamp01(1f - drag * Time.deltaTime);
                velocity *= dragFactor;
                if (velocity.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(velocity);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        maxTurnSpeed * Time.deltaTime);
                }

                // hopefully this syncs
                transform.position += velocity * Time.deltaTime;
            }
        }

        // Check for collision
        [ServerCallback]
        void OnTriggerEnter(Collider other)
        {
            if (!isServer || !isAlive)
            {
                return;
            }
            if (other.gameObject.CompareTag("Projectile")) return; // ignore other projectiles

            Debug.Log($"Projectile hit {other.gameObject.name}");

            // check if it hit a non-player entity
            if (other.gameObject.GetComponent<NonPlayerEntity>())
            {
                NonPlayerEntity entity = other.gameObject.GetComponent<NonPlayerEntity>();
                entity.ApplyDamage(damage);
            }
            // check if it hits another player
            else if (other.gameObject.CompareTag("Player"))
            {
                if (other.gameObject == creator
                    && aliveTime < 1f) // ignore self-hit for first second
                {
                    return;
                }

                PlayerCombat player = other.gameObject.GetComponent<PlayerCombat>();
                player.ApplyDamage(damage * playerDamageMultiplier);
            }
            // check if it hit the ground
            else if (other.gameObject.CompareTag("Ground"))
            {
                // do nothing special for now
                // TODO probably have it ragdoll
            }

            isAlive = false;
            if (lingerDuration > 0f)
            {
                this.lingerTime = 0f;
                // attach itself to whatever it hit, if it moves
                Collider m_Collider = GetComponent<Collider>();
                transform.SetParent(other.transform);
            }
            else
            {
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}

