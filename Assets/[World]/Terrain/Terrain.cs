using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terrain : MonoBehaviour {

    public Material terrainDefault;
    public GameObject subChunkPrefab;
    public bool frameBusy = false;
    public bool working = false;

    private int[] triangles19x19;
    private int[] triangles17x17;
    private Vector2[] UVTemplate;
    private List<Vector2[]> UVTemplateLarge;

    public List<QueueItem> loadedQueueItems;
    private List<QueueItem> areaRootQueue;
    private Queue<QueueItem> currentLoadingHTerrainBlock;
    public List<QueueItem> areaTexturesQueue;
    private QueueItem currentHTerrain;
    private System.Threading.Thread areaThread;
    private Coroutine coHTBlock;

    private List<Material> loadedTerrainLoD1Materials;

    private void Start()
    {
        frameBusy = false;
        working = false;
        loadedQueueItems = new List<QueueItem>();
        areaRootQueue = new List<QueueItem>();
        currentLoadingHTerrainBlock = new Queue<QueueItem>();
        areaTexturesQueue = new List<QueueItem>();
        loadedTerrainLoD1Materials = new List<Material>();
        Initialize();
    }

    private void Update()
    {
        if (World.loadedFullWorld)
        {
            // Parsers //
            if (areaRootQueue.Count > 0 && !Area.threadWorkingMesh)
            {
                Console.Log("areaRootQueue.Count" + areaRootQueue.Count);
                Area.threadWorkingMesh = true;
                QueueItem q = areaRootQueue[0];
                areaRootQueue.RemoveAt(0);
                areaThreadRun(q);
            }
        }
        // Assemblers //
        if (frameBusy == false)
        {
            // if there's Hterrain data ready
            if (AreaData.areas.Count > 0)
            {
                Console.Log(AreaData.areas.Count);
                frameBusy = true;
                working = true;
                //AssembleHTBlock();
                coHTBlock = StartCoroutine(AssembleHTBlock());
            }
        }
    }

    public void Initialize()
    {
        triangles17x17 = GenerateTriangleTemplate17x17();
        UVTemplate = GenerateUVTemplate();
        UVTemplateLarge = GenerateUVTemplateLarge();
    }

    IEnumerator AssembleHTBlock()
    {
        if (working && currentLoadingHTerrainBlock.Count > 0)
        {
            Area.AreaDataType areaData = AreaData.areas.Dequeue();
            QueueItem HTGroupItem = currentLoadingHTerrainBlock.Dequeue();
            if (HTGroupItem.Block != null)
            {
                HTGroupItem.Block.SetActive(false);
                loadedTerrainLoD1Materials.Add(new Material(terrainDefault));
                int frameSpread = 8; // spreading terrain chunks creation over multiple frames
                for (int i = 1; i <= frameSpread; i++)
                {
                    GenerateQuarterAreaTerrain(frameSpread, i, areaData, HTGroupItem.Block, HTGroupItem.mapName, HTGroupItem.x, HTGroupItem.y);
                    yield return null;
                }
                HTGroupItem.Block.transform.SetParent(transform);
                HTGroupItem.Block.transform.localScale = new Vector3(-1, 1, -1);
                HTGroupItem.Block.SetActive(true);

                // LoD Material //
                TexFile texFile = new TexFile();
                string mapName = HTGroupItem.mapName;
                int x = HTGroupItem.x;
                int y = HTGroupItem.y;
                string texPath = @"AIDX\Map\" + mapName + @"\" + mapName + "." + x.ToString("X").ToLower() + y.ToString("X").ToLower() + ".tex";
                Texture2D LoDTexture = texFile.ReadToTexture2D(texPath);
                if (LoDTexture != null)
                    loadedTerrainLoD1Materials[loadedTerrainLoD1Materials.Count - 1].mainTexture = LoDTexture;

            }
        }
        frameBusy = false;
    }
    public void GenerateQuarterAreaTerrain(int fS, int Q, Area.AreaDataType areaData, GameObject Block, string mapName, int x, int y)
    {
        // Area Terrain Object //
        Block.isStatic = true;
        for (int i = (256 / fS) * (Q - 1); i < (256 / fS) * Q; i++)
        {
            int chunkX = i % 16;
            int chunkY = i / 16;
            Area.SubChunkData subChunkData = areaData.subChunks[chunkX + chunkY * 16];

            if (subChunkData.flags.hasHeightmap)
            {
                // SubChunk Object //
                GameObject subChunkObj = Instantiate(subChunkPrefab);
                subChunkObj.name = "SubChunk_" + chunkX + "_" + chunkY;
                subChunkObj.isStatic = true;
                subChunkObj.transform.SetParent(Block.transform);
                subChunkObj.transform.position = new Vector3(chunkX * 32 + Block.transform.position.x, Block.transform.position.y, chunkY * 32 + Block.transform.position.z);
                subChunkObj.transform.rotation = Quaternion.identity;

                // Mesh //
                Mesh mesh = new Mesh
                {
                    vertices = Trim19x19to17x17(subChunkData.vertexData.position),
                    normals = Trim19x19to17x17(subChunkData.vertexData.normal),
                    triangles = triangles17x17,
                    //uv = UVTemplate,
                    uv = UVTemplateLarge[chunkX + chunkY * 16]
                };
                //mesh.RecalculateTangents();
                //mesh.RecalculateBounds();
                MeshFilter meshFilter = subChunkObj.GetComponent<MeshFilter>();
                meshFilter.mesh = mesh;
                MeshRenderer renderer = subChunkObj.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = loadedTerrainLoD1Materials[loadedTerrainLoD1Materials.Count - 1];

                // BlendMaterial //
                if (subChunkData.flags.hasColorMapDXT)
                {
                    // DXT
                    Texture2D colorMap = new Texture2D(65, 65, TextureFormat.DXT5, false);
                    colorMap.wrapMode = TextureWrapMode.Clamp;
                    colorMap.LoadRawTextureData(subChunkData.colorMap);
                    colorMap.Apply();
                    //renderer.material = new Material(terrainDefault);
                    //renderer.material.mainTexture = colorMap;
                    //renderer.material.SetTexture("_BlendTexAmount1", colorMap);
                }

                if (subChunkData.flags.hasBlendMapDXT)
                {
                    Texture2D blendMaps = new Texture2D(65, 65, TextureFormat.DXT1, false);
                    blendMaps.wrapMode = TextureWrapMode.Clamp;
                    blendMaps.LoadRawTextureData(subChunkData.blendMaps);
                    blendMaps.Apply();
                    //renderer.material = new Material(terrainDefault);
                    //renderer.material.mainTexture = blendMaps;
                }

                if (subChunkData.flags.unk13)
                {
                    Texture2D test = new Texture2D(65, 65, TextureFormat.DXT1, false);
                    test.wrapMode = TextureWrapMode.Clamp;
                    test.LoadRawTextureData(subChunkData.unknownData);
                    test.Apply();
                    //renderer.material = new Material(terrainDefault);
                    //renderer.material.mainTexture = test;
                }
                /*
                // PROPS //
                if (subChunkData.propEntries != null)
                {
                    foreach (UInt32 entry in subChunkData.propEntries)
                    {
                        Area.PropInfo propInfo = areaData.propInfoList[entry];

                        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        cube.transform.SetParent(subChunkObj.transform);
                        cube.transform.position = propInfo.Position;
                        cube.transform.localRotation = propInfo.Rotation;
                        cube.transform.localScale = new Vector3(propInfo.Scale, propInfo.Scale, propInfo.Scale);
                    }
                }
                */
            }
        }
    }

    public void AddToQueue(string mapName, int x, int y, GameObject Block)
    {
        QueueItem item = new QueueItem();
        item.mapName = mapName;
        item.x = x;
        item.y = y;
        item.Block = Block;
        areaRootQueue.Add(item);
        currentLoadingHTerrainBlock.Enqueue(item);
    }

    public class QueueItem
    {
        public string mapName;
        public int x;
        public int y;
        public GameObject Block;
    }

    /////////////////////////////////////////////
    #region Parsing/Processing Threads
    public void areaThreadRun(QueueItem queueItem)
    {
        currentHTerrain = queueItem;
        areaThread = new System.Threading.Thread(areaThreadRun);
        areaThread.IsBackground = true;
        areaThread.Priority = System.Threading.ThreadPriority.AboveNormal;
        areaThread.Start();
        //areaThreadRun(); // nonthreaded - for testing purposes
    }

    public void areaThreadRun()
    {
        string areaPath = @"AIDX\Map\" + currentHTerrain.mapName + @"\" + currentHTerrain.mapName + "." + ((int)currentHTerrain.x).ToString("X").ToLower() + ((int)currentHTerrain.y).ToString("X").ToLower() + ".area";
        Area.Load(areaPath);
    }
    #endregion
    /////////////////////////////////////////////

    /////////////////////////////////////////////
    #region Mesh Calculations

    private Vector3[] Trim19x19to17x17(Vector3[] vector3in)
    {
        Vector3[] vector3out = new Vector3[17 * 17];
        for (int x = 1; x <= 17; x++)
        {
            for (int y = 1; y <= 17; y++)
            {
                vector3out[(x - 1) + (y - 1)*17] = vector3in[x + y * 19];
            }
        }
        return vector3out;
    }

    private Vector2[] Trim19x19to17x17(Vector2[] vector3in)
    {
        Vector2[] vector3out = new Vector2[17 * 17];
        for (int x = 1; x <= 17; x++)
        {
            for (int y = 1; y <= 17; y++)
            {
                vector3out[(x - 1) + (y - 1) * 17] = vector3in[x + y * 19];
            }
        }
        return vector3out;
    }

    private Vector2[] GenerateUVTemplate()
    {
        Vector2[] UVs = new Vector2[17 * 17];
        for (int u = 0; u < 17; u++)
        {
            for (int v = 0; v < 17; v++)
            {
                UVs[u + v * 17] = new Vector2(u / 16.0f, v / 16.0f);
            }
        }
        return UVs;
    }

    private List<Vector2[]> GenerateUVTemplateLarge()
    {
        List<Vector2[]> uvTemplate = new List<Vector2[]>();
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 16; y++)
            {
                Vector2[] UVs = new Vector2[17 * 17];
                for (int u = 0; u < 17; u++)
                {
                    for (int v = 0; v < 17; v++)
                    {
                        UVs[u + v * 17] = new Vector2(
                           (u/16.0f * 0.0625f) + (y * 0.0625f),
                           (v/16.0f * 0.0625f) + (x * 0.0625f)
                            );
                    }
                }
                uvTemplate.Add(UVs);
            }
        }
        return uvTemplate;
    }

    private int[] GenerateTriangleTemplate19x19()
    {
        int[] triangles = new int[18 * 18 * 2 * 3];
        int triOffset = 0;
        for (int strip = 0; strip < 18; strip++)
        {
            //   case Up-Left   //
            for (int t = 0; t < 18; t++)
            {
                triangles[triOffset + 2] = t + strip * 19;
                triangles[triOffset + 1] = t + 1 + strip * 19;
                triangles[triOffset + 0] = t + (strip + 1) * 19;
                triOffset = triOffset + 3;
            }
            //   case Down-Right   //
            for (int t = 0; t < 18; t++)
            {
                triangles[triOffset + 2] = t + 1 + strip * 19;
                triangles[triOffset + 1] = t + 1 + (strip + 1) * 19;
                triangles[triOffset + 0] = t + (strip + 1) * 19;
                triOffset = triOffset + 3;
            }
        }
        return triangles;
    }

    private int[] GenerateTriangleTemplate17x17()
    {
        int[] triangles = new int[16 * 16 * 2 * 3];
        int triOffset = 0;
        for (int strip = 0; strip < 16; strip++)
        {
            //   case Up-Left   //
            for (int t = 0; t < 16; t++)
            {
                triangles[triOffset + 2] = t + strip * 17;
                triangles[triOffset + 1] = t + 1 + strip * 17;
                triangles[triOffset + 0] = t + (strip + 1) * 17;
                triOffset = triOffset + 3;
            }
            //   case Down-Right   //
            for (int t = 0; t < 16; t++)
            {
                triangles[triOffset + 2] = t + 1 + strip * 17;
                triangles[triOffset + 1] = t + 1 + (strip + 1) * 17;
                triangles[triOffset + 0] = t + (strip + 1) * 17;
                triOffset = triOffset + 3;
            }
        }
        return triangles;
    }

    #endregion
    /////////////////////////////////////////////
}