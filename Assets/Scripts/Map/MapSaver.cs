using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Frontiers.Content.Maps;
using Frontiers.Content;
using Frontiers.Assets;
using System.Diagnostics;

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

        Stopwatch watch = new();
        watch.Start();

        MapDisplayer.meshFilter = meshFilter;
        MapDisplayer.meshRenderer = meshRenderer;
        MapDisplayer.spriteRenderer = spriteRenderer;
        MapDisplayer.DisplayTexture(new Vector2Int(128, 128));

        UnityEngine.Debug.Log("Time to load map: " + watch.Elapsed.TotalMilliseconds + " milliseconds");
        watch.Stop();
    }

    public void SaveMap() {
        // Here starts the point where i have to guess how the fuck to replace the unity tilemaps with custom ones
        //map = new Map(saveName, tilemaps[1].size.x, tilemaps[1].size.y, tilemaps);
        map.Save();
        MapLoader.SaveMap(map);
    }
}