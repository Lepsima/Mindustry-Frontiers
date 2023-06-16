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
    public MeshFilter meshFilter;
    public SpriteRenderer spriteRenderer;

    public string saveName;
    public Map map;

    private void Start() {
        if (save) {
            SaveMap();
        } else {
            LoadMap();
        }
        //Invoke(nameof(SaveMap), 5f);
    }

    public void LoadMap() {
        AssetLoader.LoadAssets();
        ContentLoader.LoadContent();

        MapDisplayer.meshFilter = meshFilter;
        MapDisplayer.meshRenderer = meshRenderer;
        MapDisplayer.spriteRenderer = spriteRenderer;

        Main.RegionSize = 64;
        Tilemap tilemap = new(new Vector2Int(1024, 1024), Vector2Int.one * Main.RegionSize);

        MapDisplayer.DisplayTexture(tilemap);
    }

    public void SaveMap() {
        // Here starts the point where i have to guess how the fuck to replace the unity tilemaps with custom ones
        //map = new Map(saveName, tilemaps[1].size.x, tilemaps[1].size.y, tilemaps);
        map.Save();
        MapLoader.SaveMap(map);
    }
}