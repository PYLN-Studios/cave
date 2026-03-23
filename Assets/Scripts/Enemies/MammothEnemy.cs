using Mirror;
using Combat;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SoundManager;

namespace Enemies
{
    /// <summary>
    /// MammothEnemy: A Large slow enemy that charges at players when they get too close. 
    /// Has four states: 
    /// Normal (wandering in random directions, not aware of player), 
    /// Alert (aware of player, can still wander, but will charge at player if in chargeStartRange and charge is off cooldown),
    /// Charging (moving fast in a straight line, can damage player on hit), 
    /// Recovery (after charging, can't do anything for a few seconds).
    /// Mammoth will transition back to Normal from Alert or Recovery if player is further than disengageRange, 
    /// and will transition back to Alert from Recovery after cooldown if player is still within disengageRange.
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
        public float walkSpeed = 1f;

        [Header("Detection")]
        public float alertRange = 30f;     // Normal -> Alert distance is 30 units
        public float disengageRange = 60f; // Alert -> Normal distance is 60 units

        [Header("Charge")]
        public float chargeSpeed = 9f;
        public float chargeDistance = 45f;     // Cannot change direction while charging, so charge ends after traveling this far
        public float chargeStartRange = 27f;   // only attempt charge if target is within this range
        public float chargeCooldown = 5f;

        [Header("Charge Damage")]
        public float chargeDamage = 60f;
        public float chargeHitCooldown = 0.5f;   // prevents getting hit multiple times in a frame
        public LayerMask damageLayers;            // set to Player layer in inspector

        // Tracks recently hit targets so we don't spam damage
        private readonly Dictionary<uint, double> lastHitTimeByNetId = new();

        [SerializeField] private float maxMammothHealth = 300f;

        // Current mammoth state
        private MammothState state = MammothState.Normal;

        // Charge state variables
        private Vector3 chargeDir = Vector3.zero;
        private float chargeTraveled = 0f;
        private float cooldownTimer = 0f;
        private float chargeTimeElapsed = 0f;
        private float maxChargeDuration = 10f;

        private GameObject target;


        // Audio metadata
        [Header("Audio")]
        public SoundType mammothFootstepSound = SoundType.MAMMOTHFOOTSTEP;

        [Range(0f, 1f)]
        public float footstepVolume = 0.8f;

        public float footstepMinDistance = 5f;
        public float footstepMaxDistance = 60f;

        // time between steps when walking (slower), alert and recovery use this interval
        public float walkStepInterval = 1.5f;

        // time between steps when charging (faster)
        [Tooltip("Seconds between steps when charging (faster).")]
        public float chargeStepInterval = 0.35f;

        //must move minimum horizontal speed to trigger footstep sounds, prevents audio spam when barely moving or stuck on geometry
        [Tooltip("Minimum horizontal speed required to trigger footsteps.")]
        public float footstepMoveThreshold = 0.50f;

        private float footstepTimer = 0f;


        [Header("Rotation")]
        public float rotationSpeed = 7f;


        protected override void Awake()
        {
            base.Awake();

            entityName = "Mammoth";
            currHealth = maxMammothHealth;

            moveSpeed = walkSpeed;

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
            // Rotate when not charging
            if (state != MammothState.Charging)
            {
                Vector3 horizontalVel = controller.velocity;
                horizontalVel.y = 0f;
                RotateTowardsDir(horizontalVel);
            }

            // footstep 
            TickFootsteps();
        }

        // Normal state: random wandering, look for players to enter alert
        [Server]
        private void TickNormal()
        {
            target = FindClosestPlayer();
            //Debug.Log($"[Mammoth] FindClosestPlayer target={(target ? target.name : "null")} alertRange={alertRange}");

            if (target != null)
            {
                float d = DistanceToTarget(target);
                // Debug.Log($"[Mammoth] distToTarget={d}");
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


            // Decide whether to charge
            if (cooldownTimer <= 0f && target != null && d <= chargeStartRange)
            {
                StartCharge(target);
                state = MammothState.Charging;
                TickCharging();
                //Debug.Log($"[Mammoth] ENTER CHARGING d={d} cooldownTimer={cooldownTimer}");
                return;
            }

            // Otherwise: wander slowly while alert (or you can chase here if you want)
            DefaultMove();
        }
        // Charging state: move fast in a straight line for a certain distance, then enter recovery
        [Server]
        private void TickCharging()
        {
            
            RotateTowardsDir(chargeDir);

            // Counter for how long we've been charging
            chargeTimeElapsed += Time.deltaTime;

            // Abort charge if it lasts longer than maxChargeDuration to prevent infinite charges due to bugs or getting stuck on geometry
            if (chargeTimeElapsed >= maxChargeDuration)
            {
                state = MammothState.Recovery;
                chargeDir = Vector3.zero;
                moveVelocity = Vector3.zero;
                chargeTraveled = 0f;
                return;
            }

            Vector3 before = transform.position;

            float yVel = GetVerticalVelocity();

            Vector3 motion = chargeDir * chargeSpeed;
            motion.y = yVel;

            controller.Move(motion * Time.deltaTime);

            // If hit switched state to Recovery, stop immediately
            if (state != MammothState.Charging)
            {
                moveVelocity = Vector3.zero;
                chargeDir = Vector3.zero;
                return;
            }

            Vector3 after = transform.position;
            chargeTraveled += (after - before).magnitude;

            if (chargeTraveled >= chargeDistance)
            {
                state = MammothState.Recovery;
                footstepTimer = walkStepInterval; 
                cooldownTimer = chargeCooldown;
                moveVelocity = Vector3.zero;
                chargeDir = Vector3.zero;
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

            moveVelocity = Vector3.zero;

            if (cooldownTimer <= 0f)
            {
                // Return to Alert (still in range)
                state = MammothState.Alert;
            }
        }

        //Helper functions for state transitions and common calculations
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
            
            footstepTimer = 0f;                       
            RpcPlayMammothFootstepUnreliable(transform.position); 
            Vector3 dir = (t.transform.position - transform.position);
            dir.y = 0f;

            chargeDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : transform.forward;
            chargeTraveled = 0f;
            
            chargeTimeElapsed = 0f;
            lastHitTimeByNetId.Clear();
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
            if (players == null || players.Length == 0) 
            {
                return null;
            }
            
            GameObject best = null;
            float bestDist = float.MaxValue;

            Vector3 pos = transform.position;
            pos.y = 0f;

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null) continue;

                Vector3 p = players[i].transform.position;
                p.y = 0f;

                float dist = Vector3.Distance(pos, p);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = players[i];
                }
            }

            return best;
        }

        [ServerCallback]
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (state != MammothState.Charging) return;
            if (hit.collider == null) return;

            TryHitWithCharge(hit.collider);
        }

        [Server]
        public void TryHitWithCharge(Collider col)
        {
            if (state != MammothState.Charging) return;
            if (col == null) return;
            
            if (((1 << col.gameObject.layer) & damageLayers.value) == 0)
                return;

            if (col.transform.root == transform.root) return;

            var damageable = col.GetComponentInParent<Combat.IDamageable>();
            if (damageable == null) return;

            var ni = col.GetComponentInParent<NetworkIdentity>();
            if (ni != null)
            {
                double now = NetworkTime.time;
                if (lastHitTimeByNetId.TryGetValue(ni.netId, out double last) &&
                    now - last < chargeHitCooldown)
                    return;

                lastHitTimeByNetId[ni.netId] = now;
            }

            damageable.ApplyDamage(chargeDamage);

            state = MammothState.Recovery;
            cooldownTimer = chargeCooldown;
            moveVelocity = Vector3.zero;
            chargeDir = Vector3.zero;
            chargeTraveled = chargeDistance;
            lastHitTimeByNetId.Clear();
        }

        [Server]
        private void TickFootsteps()
        {
            if (state == MammothState.Recovery){
                return;
            }
            // Only step sounds if grounded + moving horizontally
            if (controller == null)
                return;

            if (!controller.isGrounded)
                return;

            Vector3 v = controller.velocity;
            v.y = 0f;

            if (v.magnitude < footstepMoveThreshold)
                return;

            float interval;

            // Faster footsteps when charging, slower when normal or alert/recovery
            if (state == MammothState.Charging)
            {
                interval = chargeStepInterval;
            }
            else
            {
                interval = walkStepInterval;
            }

            footstepTimer -= Time.deltaTime;

            if (footstepTimer > 0f)
                return;

            RpcPlayMammothFootstepUnreliable(transform.position);
            footstepTimer = interval;
        }

        [ClientRpc(channel = Channels.Unreliable)] //Unreliable since footsteps are frequent and not critical
        private void RpcPlayMammothFootstepUnreliable(Vector3 worldPos)
        {
            SoundManager.Play3D(
                SoundType.MAMMOTHFOOTSTEP,
                worldPos,
                footstepVolume,
                footstepMinDistance,
                footstepMaxDistance
            );
        }

        [ClientRpc] //Reliable to ensure the audio is played
        private void RpcPlayMammothFootstepReliable(Vector3 worldPos)
        {
            SoundManager.Play3D(
                SoundType.MAMMOTHFOOTSTEP,
                worldPos,
                footstepVolume,
                footstepMinDistance,
                footstepMaxDistance
            );
        }

        [Server]
        private void RotateTowardsDir(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }
    }
}
