using Mirror;
using ProceduralGeneration;
using System;
using System.Collections.Generic;
using TreeEditor;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

/*
	Documentation: https://mirror-networking.gitbook.io/docs/guides/networkbehaviour
	API Reference: https://mirror-networking.com/docs/api/Mirror.NetworkBehaviour.html
*/

namespace ProceduralGeneration
{
public class TerrainGenerator : NetworkBehaviour
    {
        Mesh mesh;
        private int MESH_SCALE = 50;
        public GameObject[] objects;  // random objects to spawn
        [SerializeField] private AnimationCurve heightCurve;
        private Vector3[] vertices;
        private int[] triangles;

        private Color[] colors;
        [SerializeField] private Gradient gradient;

        private float minTerrainheight;
        private float maxTerrainheight;

        public int xSize;
        public int zSize;

        public int baseAmplitude;
        public float scale;
        public int octaves;
        public float lacunarity;

        public int seed;

        private float lastNoiseHeight;


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
            // Use this method if you havn't filled out the properties in the inspector
            // SetNullProperties(); 

            //mesh = new Mesh();
            //GetComponent<MeshFilter>().mesh = mesh;
            //CreateNewMap();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="heightConversionMap"></param>
        private void SetNullProperties()
        {
            if (xSize <= 0) xSize = 50;
            if (zSize <= 0) zSize = 50;
            if (octaves <= 0) octaves = 5;
            if (lacunarity <= 0) lacunarity = 2;
            if (scale <= 0) scale = 50;
        }

        [Server]
        public void CreateNewMap(int seed = 1)
        {
            this.seed = seed;
            mesh = new Mesh();
            GetComponent<MeshFilter>().mesh = mesh;

            CreateMeshShape();
            CreateTriangles();
            ColorMap();
            UpdateMesh();
        }

        [Server]
        private void CreateMeshShape()
        {
            // Creates seed
            Vector2[] octaveOffsets = GetOffsetSeed();

            if (scale <= 0) scale = 0.0001f;

            // Create vertices
            vertices = new Vector3[(xSize + 1) * (zSize + 1)];

            for (int i = 0, z = 0; z <= zSize; z++)
            {
                for (int x = 0; x <= xSize; x++)
                {
                    // Set height of vertices
                    float noiseHeight = GenerateNoiseHeight(z, x, octaveOffsets);
                    SetMinMaxHeights(noiseHeight);
                    vertices[i] = new Vector3(x, noiseHeight, z);
                    i++;
                }
            }
        }

        [Server]
        private Vector2[] GetOffsetSeed()
        {
            seed = UnityEngine.Random.Range(0, 1000);

            // changes area of map
            System.Random prng = new System.Random(seed);
            Vector2[] octaveOffsets = new Vector2[octaves];

            for (int o = 0; o < octaves; o++)
            {
                float offsetX = prng.Next(-100000, 100000);
                float offsetY = prng.Next(-100000, 100000);
                octaveOffsets[o] = new Vector2(offsetX, offsetY);
            }
            return octaveOffsets;
        }

        [Server]
        private float GenerateNoiseHeight(int z, int x, Vector2[] octaveOffsets)
        {
            float amplitude = baseAmplitude;
            float frequency = 1;
            float persistence = 0.5f;
            float noiseHeight = 0;

            // loop over octaves
            for (int y = 0; y < octaves; y++)
            {
                float mapZ = z / scale * frequency + octaveOffsets[y].y;
                float mapX = x / scale * frequency + octaveOffsets[y].x;

                //The *2-1 is to create a flat floor level
                float perlinValue = (Mathf.PerlinNoise(mapZ, mapX)) * 2 - 1;
                noiseHeight += heightCurve.Evaluate(perlinValue) * amplitude;
                frequency *= lacunarity;
                amplitude *= persistence;
            }
            return noiseHeight;
        }

        [Server]
        private void SetMinMaxHeights(float noiseHeight)
        {
            // Set min and max height of map for color gradient
            if (noiseHeight > maxTerrainheight)
                maxTerrainheight = noiseHeight;
            if (noiseHeight < minTerrainheight)
                minTerrainheight = noiseHeight;
        }

        [Server]
        private void CreateTriangles()
        {
            // Need 6 vertices to create a square (2 triangles)
            triangles = new int[xSize * zSize * 6];

            int vert = 0;
            int tris = 0;
            // Go to next row
            for (int z = 0; z < zSize; z++)
            {
                // fill row
                for (int x = 0; x < xSize; x++)
                {
                    triangles[tris + 0] = vert + 0;
                    triangles[tris + 1] = vert + xSize + 1;
                    triangles[tris + 2] = vert + 1;
                    triangles[tris + 3] = vert + 1;
                    triangles[tris + 4] = vert + xSize + 1;
                    triangles[tris + 5] = vert + xSize + 2;

                    vert++;
                    tris += 6;
                }
                vert++;
            }
        }

        [Server]
        private void ColorMap()
        {
            colors = new Color[vertices.Length];

            // Loop over vertices and apply a color from the depending on height (y axis value)
            for (int i = 0, z = 0; z < vertices.Length; z++)
            {
                float height = Mathf.InverseLerp(minTerrainheight, maxTerrainheight, vertices[i].y);
                colors[i] = gradient.Evaluate(height);
                i++;
            }
        }

        [Server]
        private void MapEmbellishments()
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                // find actual position of vertices in the game
                Vector3 worldPt = transform.TransformPoint(mesh.vertices[i]);
                var noiseHeight = worldPt.y;
                // Stop generation if height difference between 2 vertices is too steep
                if (System.Math.Abs(lastNoiseHeight - worldPt.y) < 25)
                {
                    // min height for object generation
                    if (noiseHeight > 75)
                    {
                        // Chance to generate
                        if (UnityEngine.Random.Range(1, 6) == 1)
                        {
                            GameObject objectToSpawn = objects[UnityEngine.Random.Range(0, objects.Length)];
                            var spawnAboveTerrainBy = noiseHeight + 10f;
                            GameObject spawnedObject = Instantiate(objectToSpawn, new Vector3(mesh.vertices[i].x * MESH_SCALE, spawnAboveTerrainBy, mesh.vertices[i].z * MESH_SCALE), Quaternion.identity);
                            NetworkServer.Spawn(spawnedObject);

                            Debug.DrawLine(
                                new Vector3(
                                    spawnedObject.transform.position.x,
                                    spawnedObject.transform.position.y,
                                    spawnedObject.transform.position.z
                                ),
                                new Vector3(
                                    spawnedObject.transform.position.x,
                                    spawnedObject.transform.position.y + 80f,
                                    spawnedObject.transform.position.z
                                ),
                                Color.greenYellow,
                                120f
                            );

                        }
                    }
                }
                lastNoiseHeight = noiseHeight;
            }
        }

        [Server]
        private void UpdateMesh()
        {
            mesh.Clear();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors = colors;
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            GetComponent<MeshCollider>().sharedMesh = mesh;
            gameObject.transform.localScale = new Vector3(MESH_SCALE, MESH_SCALE, MESH_SCALE);

            MapEmbellishments();
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
        public override void OnStopLocalPlayer() {}

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
