using System.Collections.Generic;
using UnityEngine;
using Mirror;
using ProceduralGeneration;


/*
	Documentation: https://mirror-networking.gitbook.io/docs/guides/networkbehaviour
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

namespace ProceduralGeneration
{
    /// <summary>
    /// ItemSpawner is responsible for generating items the first time a biome is generated.
    /// Based on https://brendanhu.atlassian.net/wiki/spaces/~7120208c3305a0c7bc41a9ad43a6fbba092622/whiteboard/8388610
    /// </summary>
    public class ItemSpawner : NetworkBehaviour
    {
        #region Unity Callbacks

        /// <summary>
        /// Add your validation code here after the base.OnValidate(); call.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
        }

        // NOTE: Do not put objects in DontDestroyOnLoad (DDOL) in Awake.  You can do that in Start instead.
        void Awake()
        {
        }

        void Start()
        {
        }

        #endregion

        public Rect defaultRect = new Rect(0, 0, 0, 0);

        private int spawnAttempts = 3;  // max spawn attempts per item before giving up

        /// <summary>
        /// 
        /// </summary>
        /// <param name="seed">designates which part of the perlin noise to use (x=0, y=1000*seed)</param>
        /// <param name="spawnData">a 2d array of spawn data. the parent array holds each group of items</param>
        /// <param name="area">the game world area to try to spawn items in. Defaults to looking for a terrain object but you should probably explicitly set this.</param>
        /// <param name="perlinArea">higher values mean its more "zoomed out" in the perlin noise, creating smaller groups</param>
        /// <param name="scanInterval">distance between points to check</param>
        /// <param name="jitterRealDistance">vary where we scan randomly</param>
        /// <param name="jitterPerlin">vary the perlin noise value randomly to create more variation in group sizes</param>
        [Server]
        public void FindCandidateSpawns(
            int seed,
            ItemSpawnData[][] spawnData,
            Rect area = default,
            float perlinArea = 100f,
            float scanInterval = 1f,
            float jitterRealDistance = 0.3f,
            float jitterPerlin = 0.04f
            )
        {
            // initialize PRNG carefully to prevent correlated randomness (doubt it matters that much in this case but whatever)
            // note: itemSpawner uses the perlin noise in a line near x = 0 and positive y, please avoid this when generating terrain!
            Random.InitState(seed);

            if (area == default)
            {
                // if the area is default, look for a terrain object and use its bounds as the area to spawn in
                Terrain worldTerrain = FindFirstObjectByType<Terrain>();

                if (worldTerrain != null)
                {
                    // don't spawn right at the edges
                    area = new Rect(
                        worldTerrain.transform.position.x + 4.0f,
                        worldTerrain.transform.position.z + 4.0f,
                        worldTerrain.terrainData.size.x - 8.0f,
                        worldTerrain.terrainData.size.z - 8.0f
                    );
                }

                TerrainGenerator terrainGenerator = FindFirstObjectByType<TerrainGenerator>();
                Mesh generatedMesh = null;

                if (terrainGenerator != null && terrainGenerator.transform.parent != null)
                {
                    MeshCollider meshCollider = terrainGenerator.transform.parent.GetComponentInChildren<MeshCollider>();

                    if (meshCollider != null)
                    {
                        generatedMesh = meshCollider.sharedMesh;

                        Bounds meshBounds = meshCollider.bounds;
                        area = new Rect(
                            meshBounds.min.x + 4.0f,
                            meshBounds.min.z + 4.0f,
                            meshBounds.size.x - 8.0f,
                            meshBounds.size.z - 8.0f
                        );
                    }
                }

            }

            int numGroups = spawnData.Length;
            Dictionary<int, List<Vector2>> candidateSpawnPoints = new Dictionary<int, List<Vector2>>();
            Dictionary<int, List<(Vector2 point, float value)>> candidateSpawnPointsWithValues = new Dictionary<int, List<(Vector2 point, float value)>>();

            for (int groupIndex = 0; groupIndex < numGroups; groupIndex++)
            {
                candidateSpawnPoints[groupIndex] = new List<Vector2>();
                candidateSpawnPointsWithValues[groupIndex] = new List<(Vector2 point, float value)>();
            }

            // scan through the area in a grid pattern, with some random jitter, and get the perlin noise at each point to determine the group.
            for (float x = area.xMin; x <= area.xMax; x += scanInterval)
            {
                for (float y = area.yMin; y <= area.yMax; y += scanInterval)
                {
                    float perlinScalingX = perlinArea / area.width;
                    float perlinScalingY = perlinArea / area.height;

                    float perlinX = x * perlinScalingX;
                    float perlinY = (y * perlinScalingY) + (seed * 1000f);
                    float perlinValue = Mathf.PerlinNoise(perlinX, perlinY) + ((Random.value - 0.5f) * jitterPerlin);
                    int groupIndex = Mathf.Min((int)(perlinValue * numGroups), numGroups - 1);

                    float jitterX = (Random.value * 2.0f - 1.0f) * jitterRealDistance;
                    float jitterY = (Random.value * 2.0f - 1.0f) * jitterRealDistance;
                    Vector2 point = new Vector2(x + jitterX, y + jitterY);
                    candidateSpawnPointsWithValues[groupIndex].Add((point, perlinValue));
                }
            }

            // sort the points in each group in descending order
            for (int groupIndex = 0; groupIndex < numGroups; groupIndex++)
            {
                List<(Vector2 point, float value)> groupPoints = candidateSpawnPointsWithValues[groupIndex];
                groupPoints.Sort((a, b) => b.value.CompareTo(a.value));

                List<Vector2> sortedPoints = candidateSpawnPoints[groupIndex];
                sortedPoints.Clear();

                for (int i = 0; i < groupPoints.Count; i++)
                {
                    sortedPoints.Add(groupPoints[i].point);
                }
            }

            for (int groupIndex = 0; groupIndex < numGroups; groupIndex++)
            {
                ItemSpawnData[] itemsInGroup = spawnData[groupIndex];
                List<Vector2> groupPoints = candidateSpawnPoints[groupIndex];

                List<GameObject> itemsToSpawn = new List<GameObject>();

                // combine all the items in this group into one list, with each item repeated according to how many times it should spawn
                foreach (ItemSpawnData itemData in itemsInGroup)
                {
                    int numToSpawn = Random.Range(itemData.minRate, itemData.maxRate + 1);

                    for (int i = 0; i < numToSpawn; i++)
                    {
                        itemsToSpawn.Add(itemData.item);
                    }
                }
                SeededShuffle(itemsToSpawn, Random.Range(0, int.MaxValue));

                int numActuallySpawned = 0;
                int nextSpawnIndex = 0;
                
                foreach (GameObject item in itemsToSpawn)
                {
                    for (int attempts = 0; attempts < spawnAttempts; attempts++)
                    {
                        Vector2 spawnPoint = groupPoints[nextSpawnIndex];
                        if (AttemptSpawnOnTerrain(spawnPoint, item))
                        {
                            numActuallySpawned++;
                            nextSpawnIndex++;
                            break;
                        }
                        Debug.LogWarning($"Group {groupIndex}: Failed to spawn {item.name} at {spawnPoint}");
                        nextSpawnIndex++;
                    }
                }

                Debug.Log($"Group {groupIndex}: successfully spawned {numActuallySpawned} of {itemsToSpawn.Count} items.");
            }
        }

        public static void SeededShuffle<T>(IList<T> list, int seed)
        {
            var rng = new System.Random(seed);
            int n = list.Count;

            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }


        /// <summary>
        /// AttemptSpawnOnTerrain is used to spawn new items on the terrain.
        /// </summary>
        /// <param name="xz">the xy coordinate to spawn (height is determined by terrain</param>
        /// <param name="item">the item to spawn</param>
        /// <returns>True if the item was successfully spawned, false otherwise</returns>
        [Server]
        private bool AttemptSpawnOnTerrain(Vector2 xz, GameObject item)
        {
            // raycast down from the sky to find the terrain height at this location, then spawn the item there
            // raycast only hits terrain and not other objects, so we don't have to worry about hitting other items or players
            RaycastHit hit;
            Physics.Raycast(new Vector3(xz.x, 1000f, xz.y), Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain"));

            if (hit.collider != null)
            {
                // spawn the item at the hit point, calculate the height of the item so its not stuck in the ground
                GameObject spawnedItem = Instantiate(item, hit.point, Quaternion.identity);

                Collider itemCollider = spawnedItem.GetComponentInChildren<Collider>();
                Renderer itemRenderer = spawnedItem.GetComponentInChildren<Renderer>();

                if (itemCollider != null)
                {
                    Bounds itemBounds = itemCollider.bounds;
                    float offset = hit.point.y - itemBounds.min.y;
                    spawnedItem.transform.position += new Vector3(0f, offset, 0f);
                }
                else if (itemRenderer != null)
                {
                    Bounds itemBounds = itemRenderer.bounds;
                    float offset = hit.point.y - itemBounds.min.y;
                    spawnedItem.transform.position += new Vector3(0f, offset, 0f);
                }

                Debug.DrawLine(
                    new Vector3(
                        spawnedItem.transform.position.x,
                        spawnedItem.transform.position.y,
                        spawnedItem.transform.position.z
                    ),
                    new Vector3(
                        spawnedItem.transform.position.x,
                        spawnedItem.transform.position.y + 60f,
                        spawnedItem.transform.position.z
                    ),
                    Color.greenYellow,
                    120f
                );

                NetworkServer.Spawn(spawnedItem);

                return true;
            }

            return false;
        }

        #region Start & Stop Callbacks

        /// <summary>
        /// This is invoked for NetworkBehaviour objects when they become active on the server.
        /// <para>This could be triggered by NetworkServer.Listen() for objects in the scene, or by NetworkServer.Spawn() for objects that are dynamically created.</para>
        /// <para>This will be called for objects on a "host" as well as for object on a dedicated server.</para>
        /// </summary>
        public override void OnStartServer() { }

        /// <summary>
        /// Invoked on the server when the object is unspawned
        /// <para>Useful for saving object data in persistent storage</para>
        /// </summary>
        public override void OnStopServer() { }

        /// <summary>
        /// Called on every NetworkBehaviour when it is activated on a client.
        /// <para>Objects on the host have this function called, as there is a local client on the host. The values of SyncVars on object are guaranteed to be initialized correctly with the latest state from the server when this function is called on the client.</para>
        /// </summary>
        public override void OnStartClient() { }

        /// <summary>
        /// This is invoked on clients when the server has caused this object to be destroyed.
        /// <para>This can be used as a hook to invoke effects or do client specific cleanup.</para>
        /// </summary>
        public override void OnStopClient() { }

        /// <summary>
        /// Called when the local player object has been set up.
        /// <para>This happens after OnStartClient(), as it is triggered by an ownership message from the server. This is an appropriate place to activate components or functionality that should only be active for the local player, such as cameras and input.</para>
        /// </summary>
        public override void OnStartLocalPlayer() { }

        /// <summary>
        /// Called when the local player object is being stopped.
        /// <para>This happens before OnStopClient(), as it may be triggered by an ownership message from the server, or because the player object is being destroyed. This is an appropriate place to deactivate components or functionality that should only be active for the local player, such as cameras and input.</para>
        /// </summary>
        public override void OnStopLocalPlayer() { }

        /// <summary>
        /// This is invoked on behaviours that have authority, based on context and <see cref="NetworkIdentity.hasAuthority">NetworkIdentity.hasAuthority</see>.
        /// <para>This is called after <see cref="OnStartServer">OnStartServer</see> and before <see cref="OnStartClient">OnStartClient.</see></para>
        /// <para>When <see cref="NetworkIdentity.AssignClientAuthority">AssignClientAuthority</see> is called on the server, this will be called on the client that owns the object. When an object is spawned with <see cref="NetworkServer.Spawn">NetworkServer.Spawn</see> with a NetworkConnectionToClient parameter included, this will be called on the client that owns the object.</para>
        /// </summary>
        public override void OnStartAuthority() { }

        /// <summary>
        /// This is invoked on behaviours when authority is removed.
        /// <para>When NetworkIdentity.RemoveClientAuthority is called on the server, this will be called on the client that owns the object.</para>
        /// </summary>
        public override void OnStopAuthority() { }

        #endregion
    }
}

