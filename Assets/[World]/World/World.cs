using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class World : MonoBehaviour
{
    //////////////////
    #region Variables

    public const float AREA_SIZE = 512.0f;
    public const int MAX_WORLD_SIZE = 128;

    public int drawDistanceLOD0;
    public int drawDistanceLOD1;
    public GameObject terrainBlockObject;
    public string mapName;
    public static bool loadedFullWorld = false;

    private int[,] areaMatrix;
    private List<GameObject> loadedAreaBlocks;
    private GameObject[,] areaLowMatrix;
    private bool[,] availableAreas;
    private int[,] previousTerrainLod;
    private int[,] currentTerrainLod;
    private GameObject mainCamera;
    private int previousCamX;
    private int previousCamY;
    private Transform terrainParent;
    private Terrain terrainHandler;
    private int pullFrom = 0;

    #endregion
    //////////////////

    //////////////////
    #region Methods

    private void Start()
    {
        mainCamera = Camera.main.gameObject;
        terrainParent = transform.GetChild(0);
        terrainHandler = terrainParent.GetComponent<Terrain>();

        // Create Matrices //
        areaMatrix = new int[MAX_WORLD_SIZE, MAX_WORLD_SIZE];
        loadedAreaBlocks = new List<GameObject>();
        areaLowMatrix = new GameObject[MAX_WORLD_SIZE, MAX_WORLD_SIZE];
        availableAreas = new bool[MAX_WORLD_SIZE, MAX_WORLD_SIZE];
        previousTerrainLod = new int[MAX_WORLD_SIZE, MAX_WORLD_SIZE];
        currentTerrainLod = new int[MAX_WORLD_SIZE, MAX_WORLD_SIZE];

        ClearMatrix();
    }
    
    private void Update()
    {
        if (loadedFullWorld)
        {
            // check spatial position //
            int currentCamX = (int)Mathf.Floor(64 + (-mainCamera.transform.position.z / AREA_SIZE));
            int currentCamY = (int)Mathf.Floor(64 + (-mainCamera.transform.position.x / AREA_SIZE));
            if (currentCamX != previousCamX || currentCamY != previousCamY)
            {
                previousCamX = currentCamX;
                previousCamY = currentCamY;
                UpdateLodMatrices(currentCamX, currentCamY);
                Loader();
            }

            if (loadedAreaBlocks.Count > 60)
            {
                if (!loadedAreaBlocks[pullFrom].activeSelf)
                {
                    GameObject PulledObj = loadedAreaBlocks[pullFrom];
                    Vector2 coords = PulledObj.GetComponent<TerrainBlock>().coords;
                    string mapName = PulledObj.GetComponent<TerrainBlock>().mapName;
                    areaMatrix[(int)coords.x, (int)coords.y] = 0;
                    Terrain.QueueItem queueItem = new Terrain.QueueItem();
                    queueItem.x = (int)coords.x;
                    queueItem.y = (int)coords.y;
                    queueItem.mapName = mapName;
                    queueItem.Block = PulledObj;
                    if (terrainHandler.areaTexturesQueue.Contains(queueItem))
                    {
                        terrainHandler.areaTexturesQueue.Remove(queueItem);
                    }
                    PulledObj.GetComponent<TerrainBlock>().UnloadAsset();
                    loadedAreaBlocks.RemoveAt(pullFrom);
                    terrainHandler.loadedQueueItems.Remove(queueItem);
                    pullFrom = 0;
                }
                else
                {
                    pullFrom++;
                }
            }
        }
    }

    private void ClearMatrix()
    {
        for (int x = 0; x < MAX_WORLD_SIZE - 1; x++)
        {
            for (int y = 0; y < MAX_WORLD_SIZE - 1; y++)
            {
                availableAreas[x, y] = false;
                areaMatrix[x, y] = 0;
            }
        }
    }

    public void ClearLoDArray(int[,] array)
    {
        for (int x = 0; x < MAX_WORLD_SIZE - 1; x++)
        {
            for (int y = 0; y < MAX_WORLD_SIZE - 1; y++)
            {
                array[x, y] = 10;
            }
        }
    }

    public void UpdateLodMatrices(int currentPosX, int currentPosY)
    {
        ClearLoDArray(currentTerrainLod);
        Spiral(currentPosX, currentPosY);
    }

    private void Spiral(int X, int Y)
    {
        int x, y, dx, dy;
        x = y = dx = 0;
        dx = 0;
        dy = -1;
        int t = drawDistanceLOD1 * 2 + 1;
        int maxI = t * t;
        for (int i = 0; i < maxI; i++)
        {
            if (((x + X) > 0) && ((x + X) < MAX_WORLD_SIZE) && ((y + Y) > 0) && ((y + Y) < MAX_WORLD_SIZE))
            {
                if (Mathf.Abs(x) <= drawDistanceLOD0 && Mathf.Abs(y) <= drawDistanceLOD0)
                {
                    currentTerrainLod[x + X, y + Y] = 0;
                    SpiralLoader(x + X, y + Y);
                }
                else if (Mathf.Abs(x) <= drawDistanceLOD1 && Mathf.Abs(y) <= drawDistanceLOD1)
                {
                    currentTerrainLod[x + X, y + Y] = 1;
                    SpiralLoader(x + X, y + Y);
                }
            }
            if ((x == y) || ((x < 0) && (x == -y)) || ((x > 0) && (x == 1 - y)))
            {
                t = dx;
                dx = -dy;
                dy = t;
            }
            x += dx;
            y += dy;
        }
    }

    private void SpiralLoader(int x, int y)
    {
        if (availableAreas[x, y])
        {
            if (currentTerrainLod[x, y] != previousTerrainLod[x, y])
            {
                float zPos = (64 - x) * AREA_SIZE;
                float xPos = (64 - y) * AREA_SIZE;
                // no terrain exists - load high quality
                if (currentTerrainLod[x, y] == 0 && previousTerrainLod[x, y] == 10)
                {
                    if (areaMatrix[x, y] == 0)
                    {
                        bool Exists = false;
                        foreach (Transform child in terrainParent)
                        {
                            Vector2 coords = child.gameObject.GetComponent<TerrainBlock>().coords;
                            if (coords == new Vector2(x, y))
                            {
                                Exists = true;
                                areaMatrix[x, y] = 1;
                                child.gameObject.SetActive(true);
                            }
                        }
                        if (!Exists)
                        {
                            areaMatrix[x, y] = 1;
                            GameObject areaBlock = Instantiate(terrainBlockObject, new Vector3(xPos, 0, zPos), Quaternion.identity);
                            areaBlock.transform.SetParent(terrainParent.transform);
                            areaBlock.GetComponent<TerrainBlock>().coords = new Vector2(x, y);
                            areaBlock.GetComponent<TerrainBlock>().mapName = mapName;
                            areaBlock.name = x + "_" + y;
                            terrainParent.GetComponent<Terrain>().AddToQueue(mapName, x, y, areaBlock);
                            loadedAreaBlocks.Add(areaBlock);
                        }
                    }
                    else
                    {
                        //ADTMatrix[x, y].SetActive(true);
                    }
                    //ADTMatrix[x, y].SetActive(true);
                }
                // no terrain exists - load low quality
                if (currentTerrainLod[x, y] == 1 && previousTerrainLod[x, y] == 10)
                {
                    if (areaLowMatrix[x, y] == null)
                    {
                        //ADTLowMatrix[x, y] = Instantiate(ADTLowBlockObject, new Vector3(xPos-20, 0, zPos-20), Quaternion.identity);
                        //ADTLowMatrix[x, y].transform.SetParent(TerrainParent.transform);
                    }
                    //ADTLowMatrix[x, y].SetActive(true);
                }
                // high quality exists - load low quality
                if (currentTerrainLod[x, y] == 1 && previousTerrainLod[x, y] == 0)
                {
                    if (areaLowMatrix[x, y] == null)
                    {
                        //ADTLowMatrix[x, y] = Instantiate(ADTLowBlockObject, new Vector3(xPos-20, 0, zPos-20), Quaternion.identity);
                        //ADTLowMatrix[x, y].transform.SetParent(TerrainParent.transform);
                    }
                    else
                    {
                        //ADTLowMatrix[x, y].SetActive(true);
                    }
                    /*
                    if (ADTMatrix[x, y] != null)
                    {
                        ADTMatrix[x, y].SetActive(false);
                    }
                    */
                }
                // low quality exists - load high quality
                if (currentTerrainLod[x, y] == 0 && previousTerrainLod[x, y] == 1)
                {
                    if (areaMatrix[x, y] == 0)
                    {
                        bool Exists = false;
                        foreach (Transform child in terrainParent.transform)
                        {
                            Vector2 coords = child.gameObject.GetComponent<TerrainBlock>().coords;
                            if (coords == new Vector2(x, y))
                            {
                                Exists = true;
                                areaMatrix[x, y] = 1;
                                child.gameObject.SetActive(true);
                            }
                        }
                        if (!Exists)
                        {
                            areaMatrix[x, y] = 1;
                            GameObject areaBlock = Instantiate(terrainBlockObject, new Vector3(xPos, 0, zPos), Quaternion.identity);
                            areaBlock.transform.SetParent(terrainParent.transform);
                            areaBlock.GetComponent<TerrainBlock>().coords = new Vector2(x, y);
                            areaBlock.GetComponent<TerrainBlock>().mapName = mapName;
                            areaBlock.name = x + "_" + y;
                            terrainParent.GetComponent<Terrain>().AddToQueue(mapName, x, y, areaBlock);
                            loadedAreaBlocks.Add(areaBlock);
                        }
                    }
                    else
                    {
                        //ADTMatrix[x, y].SetActive(true);
                    }
                    if (areaLowMatrix[x, y] != null)
                    {
                        //ADTLowMatrix[x, y].SetActive(false);
                    }
                }
                // destroy both low and high quality
                if (currentTerrainLod[x, y] == 10 && previousTerrainLod[x, y] != 10)
                {
                    if (areaMatrix[x, y] != 0)
                    {
                        //ADTMatrix[x, y].SetActive(false);
                        foreach (Transform child in terrainParent.transform)
                        {
                            Vector2 coords = child.gameObject.GetComponent<TerrainBlock>().coords;
                            if (coords == new Vector2(x, y))
                            {
                                child.gameObject.SetActive(false);
                                areaMatrix[x, y] = 0;
                            }
                        }
                    }
                    if (areaLowMatrix[x, y] != null)
                    {
                        //Destroy(ADTLowMatrix[x, y].gameObject);
                        //ADTLowMatrix[x, y] = null;
                    }
                }

                previousTerrainLod[x, y] = currentTerrainLod[x, y];
            }
        }
    }

    public void Loader()
    {
        for (int x = 0; x < MAX_WORLD_SIZE - 1; x++)
        {
            for (int y = 0; y < MAX_WORLD_SIZE - 1; y++)
            {
                if (availableAreas[x, y])
                {
                    if (currentTerrainLod[x, y] != previousTerrainLod[x, y])
                    {
                        float zPos = (64 - x) * AREA_SIZE;
                        float xPos = (64 - y) * AREA_SIZE;
                        // no terrain exists - load high quality
                        if (currentTerrainLod[x, y] == 0 && previousTerrainLod[x, y] == 10)
                        {
                            if (areaMatrix[x, y] == 0)
                            {
                                bool Exists = false;
                                foreach (Transform child in terrainParent.transform)
                                {
                                    Vector2 coords = child.gameObject.GetComponent<TerrainBlock>().coords;
                                    if (coords == new Vector2(x, y))
                                    {
                                        Exists = true;
                                        areaMatrix[x, y] = 1;
                                        child.gameObject.SetActive(true);
                                    }
                                }
                                if (!Exists)
                                {
                                    Console.Log(x + " " + y);
                                    areaMatrix[x, y] = 1;
                                    GameObject areaBlock = Instantiate(terrainBlockObject, new Vector3(xPos, 0, zPos), Quaternion.identity);
                                    areaBlock.transform.SetParent(terrainParent.transform);
                                    areaBlock.GetComponent<TerrainBlock>().coords = new Vector2(x, y);
                                    areaBlock.GetComponent<TerrainBlock>().mapName = mapName;
                                    areaBlock.name = x + "_" + y;
                                    terrainParent.GetComponent<Terrain>().AddToQueue(mapName, x, y, areaBlock);
                                    loadedAreaBlocks.Add(areaBlock);
                                }
                            }
                            else
                            {
                                //ADTMatrix[x, y].SetActive(true);
                            }
                            //ADTMatrix[x, y].SetActive(true);
                        }
                        // no terrain exists - load low quality
                        if (currentTerrainLod[x, y] == 1 && previousTerrainLod[x, y] == 10)
                        {
                            if (areaLowMatrix[x, y] == null)
                            {
                                //ADTLowMatrix[x, y] = Instantiate(ADTLowBlockObject, new Vector3(xPos-20, 0, zPos-20), Quaternion.identity);
                                //ADTLowMatrix[x, y].transform.SetParent(TerrainParent.transform);
                            }
                            //ADTLowMatrix[x, y].SetActive(true);
                        }
                        // high quality exists - load low quality
                        if (currentTerrainLod[x, y] == 1 && previousTerrainLod[x, y] == 0)
                        {
                            if (areaLowMatrix[x, y] == null)
                            {
                                //ADTLowMatrix[x, y] = Instantiate(ADTLowBlockObject, new Vector3(xPos-20, 0, zPos-20), Quaternion.identity);
                                //ADTLowMatrix[x, y].transform.SetParent(TerrainParent.transform);
                            }
                            else
                            {
                                //ADTLowMatrix[x, y].SetActive(true);
                            }
                            /*
                            if (ADTMatrix[x, y] != null)
                            {
                                ADTMatrix[x, y].SetActive(false);
                            }
                            */
                        }
                        // low quality exists - load high quality
                        if (currentTerrainLod[x, y] == 0 && previousTerrainLod[x, y] == 1)
                        {
                            if (areaMatrix[x, y] == 0)
                            {
                                bool Exists = false;
                                foreach (Transform child in terrainParent.transform)
                                {
                                    Vector2 coords = child.gameObject.GetComponent<TerrainBlock>().coords;
                                    if (coords == new Vector2(x, y))
                                    {
                                        Exists = true;
                                        areaMatrix[x, y] = 1;
                                        child.gameObject.SetActive(true);
                                    }
                                }
                                if (!Exists)
                                {
                                    areaMatrix[x, y] = 1;
                                    Console.Log(x + " " + y);
                                    GameObject areaBlock = Instantiate(terrainBlockObject, new Vector3(xPos, 0, zPos), Quaternion.identity);
                                    areaBlock.transform.SetParent(terrainParent.transform);
                                    areaBlock.GetComponent<TerrainBlock>().coords = new Vector2(x, y);
                                    areaBlock.GetComponent<TerrainBlock>().mapName = mapName;
                                    areaBlock.name = x + "_" + y;
                                    terrainParent.GetComponent<Terrain>().AddToQueue(mapName, x, y, areaBlock);
                                    loadedAreaBlocks.Add(areaBlock);
                                }
                            }
                            else
                            {
                                //ADTMatrix[x, y].SetActive(true);
                            }
                            if (areaLowMatrix[x, y] != null)
                            {
                                //ADTLowMatrix[x, y].SetActive(false);
                            }
                        }
                        // destroy both low and high quality
                        if (currentTerrainLod[x, y] == 10 && previousTerrainLod[x, y] != 10)
                        {
                            if (areaMatrix[x, y] != 0)
                            {
                                //ADTMatrix[x, y].SetActive(false);
                                foreach (Transform child in terrainParent.transform)
                                {
                                    Vector2 coords = child.gameObject.GetComponent<TerrainBlock>().coords;
                                    if (coords == new Vector2(x, y))
                                    {
                                        child.gameObject.SetActive(false);
                                        areaMatrix[x, y] = 0;
                                    }
                                }
                            }
                            if (areaLowMatrix[x, y] != null)
                            {
                                //Destroy(ADTLowMatrix[x, y].gameObject);
                                //ADTLowMatrix[x, y] = null;
                            }
                        }

                        previousTerrainLod[x, y] = currentTerrainLod[x, y];
                    }
                }
            }
        }
    }

    public void LoadFullWorld(string map_name, Vector2 playerSpawn)
    {
        Area.working = true;
        terrainParent.GetComponent<Terrain>().frameBusy = false;
        pullFrom = 0;
        mapName = map_name;

        // clear Matrix //
        ClearMatrix();

        // find available areas
        Dictionary<string, Archive.FileEntry> fileEntries = DataManager.GetFileList(@"AIDX\Map\" + mapName);
        List<string> Areas = new List<string>(fileEntries.Keys);
        foreach (string map in Areas)
        {
            string[] split = map.Split("."[0]);
            if (Path.GetExtension(map).ToLower() == ".area")
            {
                string hexCoords = split[1];
                int x = int.Parse((new string(new char[] { hexCoords[0], hexCoords[1] }).ToUpper()), System.Globalization.NumberStyles.HexNumber);
                int y = int.Parse((new string(new char[] { hexCoords[2], hexCoords[3] }).ToUpper()), System.Globalization.NumberStyles.HexNumber);
                availableAreas[x, y] = true;
            }
        }

        // Initial spawn //
        ClearLoDArray(previousTerrainLod);

        playerSpawn = new Vector2(playerSpawn.y, playerSpawn.x);

        // position camera obj //
        mainCamera.transform.position = new Vector3((64 - playerSpawn.x) * AREA_SIZE, 30f, (64 - playerSpawn.y) * AREA_SIZE);

        int currentCamX = (int)playerSpawn.y;
        int currentCamY = (int)playerSpawn.x;
        previousCamX = currentCamX;
        previousCamY = currentCamY;

        UpdateLodMatrices(currentCamX, currentCamY);

        loadedFullWorld = true;
        //Loader();
    }

    #endregion
    //////////////////
}
