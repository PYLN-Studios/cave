using Combat;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace Enemies
{
    public class MammothChargeHitbox : MonoBehaviour
    {
        private MammothEnemy mammoth;

        private void Awake()
        {
            mammoth = GetComponentInParent<MammothEnemy>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!NetworkServer.active) return; // server-only
            if (mammoth == null) return;

            mammoth.TryHitWithCharge(other);
        }
    }
}