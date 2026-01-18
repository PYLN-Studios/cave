using Enemies;
using Mirror;
using UnityEngine;


namespace Projectiles
{
    public class BasicProjectile : NetworkBehaviour
    {
        // position and velocity
        private Vector3 velocity;
        private Quaternion angle = Quaternion.identity;
        private float weight;  // how much the projectile is affected by gravity
        private float drag;    // how much the projectile is slowed by air resistance

        // combat params
        private float damage;
        private float playerDamageMultiplier;

        // lifetime
        private float duration;
        private float lingerDuration; // set to 0 to destroy on impact

        // keep track of projectile lifetime and state
        private float aliveTime;
        private bool isAlive;
        private float lingerTime;

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
            float lingerTime = 0f
            )
        {
            transform.position = position;
            this.velocity = angle * Vector3.forward * speed;
            this.angle = angle;
            this.duration = lifetime;
            this.damage = damage = 0f;
            this.weight = weight;
            this.drag = drag;
            this.playerDamageMultiplier = playerDamageMultiplier;
            this.lingerTime = lingerTime;
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (!isAlive)
            {
                this.lingerTime += Time.deltaTime;
                if (this.lingerTime >= this.lingerDuration)
                {
                    Destroy(gameObject);
                }
            }

            // Move the projectile forward, then apply gravity and angle it
            transform.position += velocity * Time.deltaTime;
            Vector3 gravity = Physics.gravity * (1f - weight) * Time.deltaTime;
            this.velocity.y += gravity.y;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, angle, weight * 90f * Time.deltaTime);
        }

        // Check for collision
        void OnTriggerEnter(Collider other)
        {
            if (!isAlive) return;
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
                // TODO apply damage to player
                // PlayerController player = other.gameObject.GetComponent<PlayerController>();
                // player.ApplyDamage(damage * playerDamageMultiplier);
            }
            // check if it hit the ground
            else if (other.gameObject.CompareTag("Ground"))
            {
                // do nothing special for now
                // TODO probably have it ragdoll
            }

            if (lingerDuration > 0f)
            {
                isAlive = false;
                this.lingerTime = 0f;
                // attach itself to whatever it hit, if it moves
                // move forward by a small amount to make it look like its stuck
                Collider m_Collider = GetComponent<Collider>();
                transform.position += velocity.normalized * m_Collider.bounds.size.x;
                transform.SetParent(other.transform);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}

