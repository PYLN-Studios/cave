namespace Projectiles
{
    [System.Serializable]
    public struct SpearData
    {
        public float speed;
        public float lifetime;
        public float damage;
        public float weight;
        public float drag;
        public float playerDamageMultiplier;
        public float lingerDuration;

        public static SpearData Default => new SpearData
        {
            speed = 25f,
            lifetime = 20f,
            damage = 50f,
            weight = 1.2f,
            drag = 0f,
            playerDamageMultiplier = 2.1f, // set very high for testing, todo revert to 0.7
            lingerDuration = 60f
        };
    }
}