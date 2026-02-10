using Mirror;
using UnityEngine;

namespace Enemies
{
    public class MammothEnemy : NonPlayerEntity
    {
        private enum MammothState
        {
            Normal,
            Alert,
            Charging,
            Recovery
        }

        [Header("Mammoth Stats")]
        public float walkSpeed = 3f;

        [Header("Detection")]
        public float alertRange = 30f;     // Normal -> Alert distance is 30 units
        public float disengageRange = 80f; // Alert -> Normal distance is 80 units

        [Header("Charge")]
        public float chargeSpeed = 8f;
        public float chargeDistance = 56f;     // Cannot change direction while charging, so charge ends after traveling this far
        public float chargeStartRange = 35f;   // only attempt charge if target is within this range
        public float chargeCooldown = 6f;

        // [Header("Optional Timing")]
        // public float alertThinkInterval = 0.5f; // how often to re-evaluate target in alert
        // private float alertThinkTimer = 0f;

        private MammothState state = MammothState.Normal;

        private Vector3 chargeDir = Vector3.zero;
        private float chargeTraveled = 0f;
        private float cooldownTimer = 0f;

        private GameObject target;

        protected override void Awake()
        {
            base.Awake();

            entityName = "Mammoth";
            maxHealth = 300f;
            currHealth = maxHealth;

            moveSpeed = walkSpeed;

            // Normal state uses random movement
            useRandomMove = true;
        }

        [ServerCallback]
        protected override void Update()
        {
            switch (state)
            {
                case MammothState.Normal:
                    TickNormal();
                    break;

                case MammothState.Alert:
                    TickAlert();
                    break;

                case MammothState.Charging:
                    TickCharging();
                    break;

                case MammothState.Recovery:
                    TickRecovery();
                    break;
            }
        }

        // Normal state: random wandering, look for players to enter alert
        [Server]
        private void TickNormal()
        {
            target = FindClosestPlayer();

            if (target != null)
            {
                float d = DistanceToTarget(target);
                if (d <= alertRange)
                {
                    EnterAlert(target);
                    TickAlert();
                    return;
                }
            }

            // Parent class already has random movement logic, so just call that
            DefaultMove();
        }

        // Alert state: look for charge opportunity or return to normal if player is far
        [Server]
        private void TickAlert()
        {
            // Disengage condition: if target is too far or doesn't exist, back to Normal
            if (target == null)
            {
                target = FindClosestPlayer();
                if (target == null)
                {
                    EnterNormal();
                    TickNormal();
                    return;
                }
            }

            float d = DistanceToTarget(target);
            if (d > disengageRange)
            {
                EnterNormal();
                TickNormal();
                return;
            }

            // // Optional: reselect target occasionally (helps if multiple players)
            // alertThinkTimer -= Time.deltaTime;
            // if (alertThinkTimer <= 0f)
            // {
            //     target = FindClosestPlayer();
            //     alertThinkTimer = alertThinkInterval;
            // }

            // Decide whether to charge
            if (cooldownTimer <= 0f && target != null && d <= chargeStartRange)
            {
                StartCharge(target);
                state = MammothState.Charging;
                TickCharging();
                return;
            }

            // Otherwise: wander slowly while alert (or you can chase here if you want)
            DefaultMove();
        }

        //Charging state: move fast in a straight line for a certain distance, then enter recovery
        [Server]
        private void TickCharging()
        {
            Vector3 before = transform.position;

            // Locked direction, fast speed
            Vector3 step = chargeDir * chargeSpeed * Time.deltaTime;
            controller.Move(step);

            Vector3 after = transform.position;
            chargeTraveled += (after - before).magnitude;

            // Keep moveVelocity updated (useful for debug/anim)
            moveVelocity = chargeDir * chargeSpeed;

            // End charge only after actually traveling the required distance
            if (chargeTraveled >= chargeDistance)
            {
                state = MammothState.Recovery;
                cooldownTimer = chargeCooldown;
                moveVelocity = Vector3.zero;
            }
        }

        // Recovery state: after charging, you can't do anything for a few seconds. If player is far, drop back to normal immediately
        [Server]
        private void TickRecovery()
        {
            cooldownTimer -= Time.deltaTime;

            // If target is far, drop back to normal even during recovery
            if (target == null)
                target = FindClosestPlayer();

            if (target == null)
            {
                EnterNormal();
                return;
            }

            float d = DistanceToTarget(target);
            if (d > disengageRange)
            {
                EnterNormal();
                return;
            }

            // During recovery, you can idle or wander; here we idle
            moveVelocity = Vector3.zero;

            if (cooldownTimer <= 0f)
            {
                // Return to Alert (still in range)
                state = MammothState.Alert;
            }
        }

        // -------------------------
        // Transitions / Helpers
        // -------------------------
        [Server]
        private void EnterNormal()
        {
            state = MammothState.Normal;
            useRandomMove = true;
            target = null;
        }

        [Server]
        private void EnterAlert(GameObject t)
        {
            state = MammothState.Alert;
            target = t;
            useRandomMove = true; // alert still wanders unless you change it to chase
            // alertThinkTimer = 0f;
        }

        [Server]
        private void StartCharge(GameObject t)
        {
            Vector3 dir = (t.transform.position - transform.position);
            dir.y = 0f;

            chargeDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
            chargeTraveled = 0f;
        }

        [Server]
        private float DistanceToTarget(GameObject t)
        {
            Vector3 a = transform.position;
            Vector3 b = t.transform.position;
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        [Server]
        private GameObject FindClosestPlayer()
        {
            // Requires player objects to be tagged "Player"
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            if (players == null || players.Length == 0) return null;

            GameObject best = null;
            float bestD = float.MaxValue;

            Vector3 pos = transform.position;
            pos.y = 0f;

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null) continue;

                Vector3 p = players[i].transform.position;
                p.y = 0f;

                float d = Vector3.Distance(pos, p);
                if (d < bestD)
                {
                    bestD = d;
                    best = players[i];
                }
            }

            return best;
        }
    }
}
