using UnityEngine;

namespace ProceduralGeneration
{
    public struct ItemSpawnData
    {
        public GameObject item;
        
        // integer value. Items sharing a spawn group will tend to spawn together.
        // group numbers next to each other (e.g. 2 and 3) will also tend to be closer to each other.
        public int spawnGroup;
        
        // frequency. Min and Max number of times to attempt to spawn this item per biome generation
        // note that it may be less than the min if theres more items than spawn locations.
        public int minRate;
        public int maxRate;

    }
}
