using Combat;
using Mirror;
using UnityEngine;


namespace Enemies
{

    [RequireComponent(typeof(CharacterController))]
    public class NonPlayerEntity : NetworkBehaviour, IDamageable
    {

        [Header("Enemy")]
        // TODO eventually we probably want to make players and enemies inherit from the same class
        public string entityName = "Enemy";
        public float currHealth = 100f;
        public float maxHealth = 100f;
        protected float moveSpeed = 1f;

        [Header("Attacking")]
        public float attackDamage = 10f;
        public float attackRange = 2f;
        public float attackCooldown = 1f;
        protected float attackTimer;

        [Header("MovementAI")]
        public bool useRandomMove = false; // TODO remove later
        private float randomMoveTimer;
        private Vector2 randomMoveInterval = new Vector2(3f, 5f);
        protected Vector3 moveVelocity = Vector3.zero;

        protected CharacterController controller;


        protected virtual void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        [Server]
        protected virtual void Update()
        {
            attackTimer -= Time.deltaTime;

            DefaultMove();
            
        }

        [Server]
        public virtual bool CanAttack()
        {
            return attackTimer <= 0f;
        }

        protected virtual void DefaultMove()
        {
            randomMoveTimer -= Time.deltaTime;
            if (randomMoveTimer <= 0f)
            {
                Vector3 moveVelocity = new Vector3(
                    Random.Range(-1f, 1f),
                    0f,
                    Random.Range(-1f, 1f)
                ).normalized * moveSpeed;
                randomMoveTimer = Random.Range(randomMoveInterval.x, randomMoveInterval.y);
            }
            // transform.position += moveVelocity * Time.deltaTime; 
            controller.Move(moveVelocity * Time.deltaTime);
        }


        [Server]
        public virtual void Attack(GameObject target)
        {
            if (!CanAttack() || target == null) return;

            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist > attackRange) return;

            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.ApplyDamage(attackDamage);
                attackTimer = attackCooldown;
            }
        }


        [Server]
        public void ApplyDamage(float damage)
        {
            currHealth -= damage;
            Debug.Log($"Enemy took {damage} damage. Health: {currHealth}/{maxHealth}");
            if (currHealth <= 0f)
            {
                Die();
            }
        }

        void Die()
        {
            Debug.Log($"Enemy {entityName} died");
            Destroy(gameObject);
        }
    }
}
