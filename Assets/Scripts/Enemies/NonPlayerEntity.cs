using Mirror;
using UnityEngine;


namespace Enemies
{

    [RequireComponent(typeof(CharacterController))]
    public class NonPlayerEntity : NetworkBehaviour
    {

        [Header("Enemy")]
        public float CurrHealth = 100f;
        public float MaxHealth = 100f;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        void ApplyDamage(float damage)
        {
            Debug.Log($"Enemy took {damage} damage.");
            CurrHealth -= damage;
            if (CurrHealth <= 0f)
            {
                Die();
            }
        }

        void Die()
        {
            Destroy(gameObject);
        }
    }
}
