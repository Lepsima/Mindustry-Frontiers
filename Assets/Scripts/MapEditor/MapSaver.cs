using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content.Maps;
using Frontiers.Content;
using Frontiers.Assets;
using System.Diagnostics;
using Frontiers.Settings;

public class MapSaver : MonoBehaviour {
    public bool saveMode = true;
    public Map map;

    [Header("Save settings")]
    public string saveName;

    [Header("Load settings")]
    public string loadName;
    public int regionSize = 64;

    private void Start() {
        if (saveMode) {
        } else {
            LoadMap();
        }
        //Invoke(nameof(SaveMap), 5f);
    }

    public void LoadMap() {
        AssetLoader.LoadAssets();
        ContentLoader.LoadContents();

        Main.Map_RegionSize = regionSize;
        MapDisplayer.SetupAtlas();

        MapLoader.LoadMap(loadName);
    }
}