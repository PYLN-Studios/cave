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
            speed = 21f,
            lifetime = 15f,
            damage = 50f,
            weight = 1.4f,
            drag = 0.02f,
            playerDamageMultiplier = 2.1f, // set very high for testing, todo revert to 0.7
            lingerDuration = 20f
        };
    }
}