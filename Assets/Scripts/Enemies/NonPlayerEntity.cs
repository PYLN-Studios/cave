using Combat;
using Mirror;
using UnityEngine;

namespace Enemies
{
    [RequireComponent(typeof(CharacterController))]
    public class NonPlayerEntity : NetworkBehaviour, IDamageable
    {
        [Header("Enemy")]
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
        public bool useRandomMove = false; 
        private float randomMoveTimer;
        private Vector2 randomMoveInterval = new Vector2(3f, 5f);
        protected Vector3 moveVelocity = Vector3.zero;

        [Header("Gravity / Grounding")]
        public float gravity = -30f;
        public float stickToGroundForce = -2f;
        protected float verticalVelocity = 0f;

        protected CharacterController controller;

        protected virtual void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        void Start()
        {
        }

        // Update is called once per frame
        [ServerCallback]
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

        // Default move is just random wandering, but can be overridden for more complex behavior
        // Uses helper function GetVerticalVelocity to handle gravity and grounding
        [Server]
        protected virtual void DefaultMove()
        {
            randomMoveTimer -= Time.deltaTime;

            if (useRandomMove && randomMoveTimer <= 0f)
            {
                moveVelocity = new Vector3(
                    Random.Range(-1f, 1f),
                    0f,
                    Random.Range(-1f, 1f)
                ).normalized * moveSpeed;

                randomMoveTimer = Random.Range(randomMoveInterval.x, randomMoveInterval.y);
            }

            float yVel = GetVerticalVelocity();

            Vector3 motion = moveVelocity;
            motion.y = yVel;

            controller.Move(motion * Time.deltaTime);
        }

        //Old default move but it has gravity function built in
        // [Server]
        // protected virtual void DefaultMove()
        // {
        //     // Default Move: Random wandering but does not consider gravity
        //     randomMoveTimer -= Time.deltaTime;

        //     if (useRandomMove && randomMoveTimer <= 0f)
        //     {
        //         moveVelocity = new Vector3(
        //             Random.Range(-1f, 1f),
        //             0f,
        //             Random.Range(-1f, 1f)
        //         ).normalized * moveSpeed;

        //         randomMoveTimer = Random.Range(randomMoveInterval.x, randomMoveInterval.y);
        //     }

        //     // New Default Move: Random wandering but considers gravity and grounding
        //     if (controller.isGrounded)
        //     {
        //         if (verticalVelocity < 0f)
        //             verticalVelocity = stickToGroundForce;
        //     }
        //     else
        //     {
        //         verticalVelocity += gravity * Time.deltaTime;
        //     }

        //     Vector3 motion = moveVelocity;
        //     motion.y = verticalVelocity;

        //     controller.Move(motion * Time.deltaTime);

        //     // Old Default Move: Random wandering but does not consider gravity

        //     // // transform.position += moveVelocity * Time.deltaTime; 
        //     // controller.Move(moveVelocity * Time.deltaTime);
        // }

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

        [Server]
        protected virtual void Die()
        {
            Debug.Log($"Enemy {entityName} died");
            NetworkServer.Destroy(gameObject);
        }
        [Server]
        protected float GetVerticalVelocity()
        {
            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f)
                    verticalVelocity = stickToGroundForce;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }

            return verticalVelocity;
        }

    }
}
