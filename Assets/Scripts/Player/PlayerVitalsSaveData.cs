using System;

namespace Player
{
    [Serializable]
    public class PlayerVitalsSaveData
    {
        public string playerId;
        public float health;
        public float hunger;
        public float stamina;
        public string updatedUtc;
    }
}
