﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class MapTab : MonoBehaviour {

    private GameObject terrain;

    public void MapTabClicked ()
    {
        string selectedMap = gameObject.transform.GetChild(0).GetComponent<UnityEngine.UI.Text>().text;

        terrain = GameObject.Find("Canvas - MapLoader");
        MapLoaderTool terrainImport = terrain.GetComponent<MapLoaderTool>();
        terrainImport.MapSelected(selectedMap);
    }
}
