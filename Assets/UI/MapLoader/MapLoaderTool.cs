using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MapLoaderTool : MonoBehaviour
{

    #region Variables

    ////////////////////////////////////////
    #region General 
    private bool initialized = false;
    private string selectedMapName = "";
    #endregion
    ////////////////////////////////////////

    ////////////////////////////////////////
    #region Minimap
    public GameObject minimapPrefab;
    public GameObject scrollParent;
    public GameObject minimapScrollPanel;
    private int remainingMinimaps = 0;
    public UnityEngine.UI.Image loadingBar;
    public GameObject loadingPanel;
    public GameObject selectPlayerBlockIcon;
    public Vector2 currentSelectedPlayerSpawn;
    public GameObject world;
    #endregion
    ////////////////////////////////////////

    ////////////////////////////////////////
    #region Maps List
    public GameObject mapScrollList;
    public GameObject mapTabPrefab;
    private Dictionary<string, GameObject> mapTabs = new Dictionary<string, GameObject>();
    #endregion
    ////////////////////////////////////////

    #endregion

    ////////////////////////////////////////
    #region General
    private void OnEnable()
    {
        if (!initialized)
        {
            Initialize();
        }
    }

    private void Update()
    {
        if (MinimapThread.ResetParentSize)
        {
            scrollParent.GetComponent<RectTransform>().sizeDelta = new Vector2((MinimapThread.max.y - MinimapThread.min.y + 1) * 100, (MinimapThread.max.x - MinimapThread.min.x + 1) * 100);
            remainingMinimaps = MinimapThread.minimapCount;
            //Vector2 pivot = new Vector2(128.0f / (MinimapThread.max.x - MinimapThread.min.x), 1- (128.0f / (MinimapThread.max.y - MinimapThread.min.y)));
            //scrollParent.GetComponent<RectTransform>().pivot = pivot;
            MinimapThread.ResetParentSize = false;
        }
        if (MinimapThread.MinimapDataQueue.Count > 0)
        {
            AssembleMinimap();
            remainingMinimaps--;
            loadingBar.fillAmount = 1 - ((float)remainingMinimaps / (float)MinimapThread.minimapCount);
        }
        if (loadingBar.fillAmount >= 0.95f)
        {
            if (loadingPanel.activeSelf)
                loadingPanel.SetActive(false);
        }
    }

    // Initialize Map Loader Tool //
    public void Initialize()
    {
        string mapPath = @"AIDX\Map";
        List<string> mapList = DataManager.GetFolderList(mapPath);
        ClearMapListButtons();
        PopulateMapList(mapList);
        initialized = true;
    }

    // Clicked the Load Full Map Button //
    public void ClickedLoadFull()
    {
        currentSelectedPlayerSpawn = new Vector2(MinimapThread.min.x + ((MinimapThread.max.x - MinimapThread.min.x) / 2), MinimapThread.min.y + ((MinimapThread.max.y - MinimapThread.min.y) / 2));
        Console.Log("Spawn : " + currentSelectedPlayerSpawn.x + " " + currentSelectedPlayerSpawn.y);
        world.GetComponent<World>().mapName = selectedMapName;
        world.GetComponent<World>().LoadFullWorld(selectedMapName, currentSelectedPlayerSpawn);
        //LoadingText.SetActive(true);
    }
    #endregion
    ////////////////////////////////////////

    ////////////////////////////////////////
    #region Minimaps
    // Select a Player Spawn when Right Clicking on a Minimap Block //
    public void SelectPlayerSpawn(GameObject minimapBlock)
    {
        selectPlayerBlockIcon.SetActive(true);
        selectPlayerBlockIcon.transform.SetParent(minimapBlock.transform);
        selectPlayerBlockIcon.GetComponent<RectTransform>().localPosition = new Vector2(50, -50);
        selectPlayerBlockIcon.GetComponent<RectTransform>().localScale = minimapBlock.transform.localScale;
        currentSelectedPlayerSpawn = minimapBlock.GetComponent<MinimapBlock>().minimapCoords;
    }

    // Create Minimap Blocks //
    public void Load(string mapName, GameObject ScrollParent)
    {
        scrollParent = ScrollParent;
        remainingMinimaps = 1; // resetting above 0
        MinimapThread.minimapCount = 0;
        loadingBar.fillAmount = 0;
        loadingPanel.SetActive(true);
        MinimapThread.currentMapName = mapName;
        System.Threading.Thread minimapThread = new System.Threading.Thread(MinimapThread.LoadThread);
        minimapThread.IsBackground = true;
        minimapThread.Priority = System.Threading.ThreadPriority.AboveNormal;
        minimapThread.Start();
        //MinimapThread.LoadThread(); // Nonthreaded, for debug
    }

    // Assemble the Minimap GameObjects //
    private void AssembleMinimap()
    {
        MinimapThread.MinimapBlockData blockData = MinimapThread.MinimapDataQueue.Dequeue();
        GameObject instance = Instantiate(minimapPrefab, Vector3.zero, Quaternion.identity);
        instance.transform.SetParent(scrollParent.transform, false);
        instance.GetComponent<RectTransform>().anchoredPosition = new Vector2((blockData.coords.y - MinimapThread.min.y) * 100, -(blockData.coords.x - MinimapThread.min.x) * 100);
        instance.name = "map" + blockData.coords.x + "_" + blockData.coords.y;
        instance.tag = "MinimapBlock";
        instance.GetComponent<MinimapBlock>().minimapCoords = blockData.coords;
        if (MinimapThread.minimapAvailability[(int)blockData.coords.x, (int)blockData.coords.y])
        {
            Texture2D tex = new Texture2D((int)blockData.textureInfo.width, (int)blockData.textureInfo.height, blockData.textureInfo.textureFormat, false);
            tex.LoadRawTextureData(blockData.minimapByteData);
            instance.GetComponent<RawImage>().texture = tex;
            instance.GetComponent<RawImage>().uvRect = new Rect(0, 0, 1, -1);
            tex.Apply();
        }
    }

    public void ClearMinimaps(GameObject minimapScrollPanel)
    {
        MinimapThread.MinimapDataQueue.Clear();
        foreach (Transform child in minimapScrollPanel.transform)
        {
            Destroy(child.gameObject);
        }
    }
    #endregion
    ////////////////////////////////////////

    ////////////////////////////////////////
    #region Map List
    private void ClearMapListButtons()
    {
        mapTabs.Clear();
        foreach (Transform child in mapScrollList.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void PopulateMapList(List<string> mapList)
    {
        for (int i = 0; i < mapList.Count; i++)
        {
            GameObject MapItem = Instantiate(mapTabPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            string fileName = Path.GetFileName(mapList[i]);
            mapTabs.Add(fileName, MapItem);
            MapItem.transform.SetParent(mapScrollList.transform);
            MapItem.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text = fileName;
        }
    }

    // Filter Buttons in the Map List Panel based on keyword //
    public void FilterMapList(string filter)
    {
        if (filter == null)
        {
            foreach (KeyValuePair<string, GameObject> entry in mapTabs)
            {
                entry.Value.SetActive(true);
            }
        }
        else
        {
            foreach (KeyValuePair<string, GameObject> entry in mapTabs)
            {
                if (StringComparer(entry.Key, filter))
                {
                    entry.Value.SetActive(true);
                }
                else
                {
                    entry.Value.SetActive(false);
                }
            }
        }
    }

    private bool StringComparer(string s1, string s2)
    {
        string capTestStr = s1.ToUpper();
        if (capTestStr.Contains(s2.ToUpper()))
        {
            return true;
        }
        return false;
    }

    // Map Selected in the Map List Panel //
    public void MapSelected(string mapName)
    {
        selectedMapName = mapName;
        ClearMinimaps(minimapScrollPanel);
        Load(mapName, minimapScrollPanel);
    }
    #endregion
    ////////////////////////////////////////
}
