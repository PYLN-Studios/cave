using Mirror;
using UnityEngine;


namespace Enemies
{

    [RequireComponent(typeof(CharacterController))]
    public class NonPlayerEntity : NetworkBehaviour
    {

        [Header("Enemy")]
        // TODO eventually we probably want to make players and enemies inherit from the same class
        public string entityName = "Enemy";
        public float currHealth = 100f;
        public float maxHealth = 100f;

        [Header("MovementAI")]
        public bool testRandomMove = true; // TODO remove later
        private float randomMoveTimer = 0f;
        private Vector2 randomMoveInterval = new Vector2(3f, 5f);
        private float moveSpeed = 1f;
        private Vector3 moveVelocity = Vector3.zero;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            randomMoveTimer -= Time.deltaTime;
            if (testRandomMove && randomMoveTimer <= 0f)
            {
                Vector3 moveVelocity = new Vector3(
                    Random.Range(-1f, 1f),
                    0f,
                    Random.Range(-1f, 1f)
                ).normalized * moveSpeed;
                randomMoveTimer = Random.Range(randomMoveInterval.x, randomMoveInterval.y);
            }
            transform.position += moveVelocity * Time.deltaTime;
        }

        public void ApplyDamage(float damage)
        {
            Debug.Log($"Enemy took {damage} damage. Health: {currHealth}/{maxHealth}");
            currHealth -= damage;
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
