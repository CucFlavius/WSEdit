using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainBlock : MonoBehaviour
{
    private Vector3[] blockCorners;
    private Camera cameraMain;
    private int materialLoDState;

    public bool reCheck;
    public Vector2 coords;
    public string mapName;

    private void Start()
    {
        Vector3 position = transform.position;

        cameraMain = Camera.main;
        blockCorners = new Vector3[4];
        blockCorners[0] = new Vector3(position.x - World.AREA_SIZE / 2, 0, position.z - World.AREA_SIZE / 2);
        blockCorners[1] = new Vector3(position.x + World.AREA_SIZE / 2, 0, position.z + World.AREA_SIZE / 2);
        blockCorners[2] = new Vector3(position.x - World.AREA_SIZE / 2, 0, position.z + World.AREA_SIZE / 2);
        blockCorners[3] = new Vector3(position.x + World.AREA_SIZE / 2, 0, position.z - World.AREA_SIZE / 2);
        materialLoDState = 1;
    }

    public void UnloadAsset()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
            Destroy(transform.GetChild(i).GetComponent<MeshFilter>().sharedMesh);
            Destroy(transform.GetChild(i).GetComponent<TerrainChunk>().mesh);
            for (int ln = 1; ln < 4; ln++)
            {
                /*
                try
                {
                    if (transform.GetChild(i).GetComponent<TerrainChunk>().high != null)
                        Destroy(transform.GetChild(i).GetComponent<TerrainChunk>().high.GetTexture("_blend" + ln));
                }
                catch
                {
                    Debug.Log("Memory Cleaner - Error: Couldn't find " + "_blend" + ln);
                }
                */
            }
            if (transform.GetChild(i).GetComponent<TerrainChunk>().low != null)
            {
                /*
                Destroy(transform.GetChild(i).GetComponent<TerrainChunk>().low.GetTexture("_MainTex2"));
                Destroy(transform.GetChild(i).GetComponent<TerrainChunk>().low);
                */
            }
            if (transform.GetChild(i).GetComponent<TerrainChunk>().high != null)
                Destroy(transform.GetChild(i).GetComponent<TerrainChunk>().high);
        }
        Destroy(gameObject);
    }

    private void Update()
    {
        UpdatePosition();
    }

    private void Low()
    {
        for (int i = 1; i <= 4; i++)
        {
            for (int j = (256 / 4) * (i - 1); j < (256 / 4) * i; j++)
            {
               // transform.GetChild(j).GetComponent<TerrainChunk>().UpdateDistance(1);
            }
        }
    }

    private void High()
    {
        for (int i = 1; i <= 4; i++)
        {
            for (int j = (256 / 4) * (i - 1); j < (256 / 4) * i; j++)
            {
                //transform.GetChild(j).GetComponent<TerrainChunk>().UpdateDistance(0);
            }
        }
    }

    public void UpdatePosition()
    {
        // find minimum corner distance //
        float distance = 10000;
        for (int i = 0; i < 4; i++)
        {
            Vector3 heading = blockCorners[i] - cameraMain.transform.position;
            float currentDistance = Vector3.Dot(heading, cameraMain.transform.forward);
            if (currentDistance < distance)
            {
                distance = currentDistance;
            }
        }
        if (distance < WorldSettings.terrainMaterialDistance)
        {
            if (materialLoDState == 1 || reCheck)
            {
                materialLoDState = 0;
                reCheck = false;
                High();
            }
        }
        else
        {
            if (materialLoDState == 0 || reCheck)
            {
                materialLoDState = 1;
                reCheck = false;
                Low();
            }
        }
    }
}
