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
    public bool save = true;
    public MeshRenderer meshRenderer;

    public string saveName;
    public Map map;

    private void Start() {
        if (save) {
        } else {
            LoadMap();
        }
        //Invoke(nameof(SaveMap), 5f);
    }

    public void LoadMap() {
        AssetLoader.LoadAssets();
        ContentLoader.LoadContent();

        Main.RegionSize = 64;
        MapDisplayer.SetupAtlas();
        //new Tilemap(new Vector2Int(1024, 1024), Vector2Int.one * Main.RegionSize);
    }
}