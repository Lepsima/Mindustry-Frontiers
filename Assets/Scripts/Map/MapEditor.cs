using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Content.Maps;
using Frontiers.Assets;
using MapLayer = Frontiers.Content.Maps.Map.MapLayer;
using Frontiers.Settings;

public class MapEditor : MonoBehaviour {

    [Header("Main Settings")]
    public string saveName;
    public int regionSize = 32;
    public Vector2Int size = new(512, 512);

    [Header("Editor Layers")]
    public MapEditorLayer[] layers;

    [Header("Noise Settings")]
    public float scale = 3.25f;
    [Range(0f, 1f)] public float threshold = 0.5f;
    float minThreshold = -1f;
    float shoreThreshold = -1f;

    [Range(0, 10)] public int octaves = 4;
    [Range(0f, 1f)] public float persistance = 0.5f;
    public float lacunarity = 1f;

    public MapLayer noiseLayer;

    public bool overrideIfNull;
    public int tile1;
    public int tile2;

    TileType[] loadedTiles;
    int tileIndex;
    int otherTileIndex;

    int currentLayer;
    public bool placeEnabled = false;

    Tilemap tilemap;
    Map map;

    private void Start() {
        AssetLoader.LoadAssets();
        ContentLoader.LoadContent();

        Main.RegionSize = regionSize;
        MapDisplayer.SetupAtlas();
        tilemap = new(size, Vector2Int.one * Main.RegionSize);

        loadedTiles = ContentLoader.GetContentByType<TileType>();
        map = new Map(saveName, size.x, size.y, tilemap);
    }

    private void Update() {
        Vector2Int mouseGridPos = Vector2Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        if (Input.GetKeyDown(KeyCode.X)) {
            placeEnabled = !placeEnabled;
        }

        if (Input.GetKeyDown(KeyCode.L)) {
            ExecuteAllLayers();
        }

        if (Input.GetKeyDown(KeyCode.K)) {
            SaveMap();
        }

        if (!placeEnabled) {
            return;
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            ChangeTile(1);
        }

        if (Input.GetKeyDown(KeyCode.F)) {
            ChangeTile(-1);
        }

        if (Input.GetKeyDown(KeyCode.T)) {
            ChangeOtherTile(1);
        }

        if (Input.GetKeyDown(KeyCode.G)) {
            ChangeOtherTile(-1);
        }

        if (Input.GetKeyDown(KeyCode.N)) {
            map.tilemap.HoldMeshUpdate(true);
            ApplyNoise();
            map.tilemap.HoldMeshUpdate(false);
        }

        if (Input.GetMouseButtonDown(0)) {
            map.PlaceTile((MapLayer)currentLayer, mouseGridPos, loadedTiles[tileIndex]);
        }

        if (Input.GetMouseButtonDown(1)) {
            map.PlaceTile((MapLayer)currentLayer, mouseGridPos, null);
        }
    }

    public void SaveMap() {
        map.Save();
        MapLoader.SaveMap(map);
    }

    public void ExecuteAllLayers() {
        map.tilemap.HoldMeshUpdate(true);
        foreach (MapEditorLayer mapLayer in layers) {
            scale = mapLayer.scale;
            threshold = mapLayer.threshold;
            octaves = mapLayer.octaves;
            persistance = mapLayer.persistance;
            lacunarity = mapLayer.lacunarity;
            noiseLayer = mapLayer.noiseLayer;
            overrideIfNull = mapLayer.overrideIfNull;
            tile1 = GetByName(mapLayer.tile1Name);
            tile2 = GetByName(mapLayer.tile2Name);
            minThreshold = mapLayer.layerAction == MapEditorLayer.LayerAction.River ? threshold - mapLayer.riverWidth : -1;
            shoreThreshold = mapLayer.shoreWidth;

            ApplyNoise(mapLayer.seed);
        }
        map.tilemap.HoldMeshUpdate(false);
    }

    public int GetByName(string name) {
        for (int i = 0; i < loadedTiles.Length; i++) if (loadedTiles[i].name == name) return i;
        return -1;
    }

    public void ApplyNoise(int seed = -1) {
        TileType tile1 = this.tile1 == -1 ? null : loadedTiles[this.tile1];
        TileType tile2 = this.tile2 == -1 ? null : loadedTiles[this.tile2];

        seed = seed == -1 ? Random.Range(0, 999999) : seed;

        float maxValue = float.MinValue;
        float minValue = float.MaxValue;

        float[,] values = new float[map.size.x, map.size.y];

        for (int x = 0; x < map.size.x; x++) {
            for (int y = 0; y < map.size.y; y++) {
                float value = CalculateNoiseTile(x + seed, y + seed);
                
                if (value > maxValue) maxValue = value; 
                if (value < minValue) minValue = value;          

                values[x, y] = value;
            }
        }

        for (int x = 0; x < map.size.x; x++) {
            for (int y = 0; y < map.size.y; y++) {
                values[x, y] = Mathf.InverseLerp(minValue, maxValue, values[x, y]);

                bool isTile1 = minThreshold == -1 ? values[x, y] > threshold : values[x, y] < threshold && values[x, y] > minThreshold;

                bool isTile2 = (!isTile1 && (values[x, y] < threshold + shoreThreshold && values[x, y] > minThreshold - shoreThreshold));

                TileType tileType = null;

                if (minThreshold == -1) {
                    tileType = isTile1 ? tile1 : tile2;
                } else {
                    if (isTile1) tileType = tile1;
                    else if (isTile2) tileType = tile2;
                }
         
                if (!overrideIfNull && tileType == null) continue;
                map.PlaceTile(noiseLayer, new Vector2Int(x, y), tileType);
            }
        }
    }

    public float CalculateNoiseTile(int x, int y) {
        float perlinValue = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < octaves; i++) {
            float xCoord = x / scale * frequency;
            float yCoord = y / scale * frequency;

            float value = Mathf.PerlinNoise(xCoord, yCoord) * 2f - 1f;
            perlinValue += value * amplitude;

            amplitude *= persistance;
            frequency *= lacunarity;
        }

        return perlinValue;
    }


    public void ChangeTile(int delta) {
        tileIndex += delta > 0 ? 1 : -1;

        if (tileIndex < 0) tileIndex = loadedTiles.Length - 1;
        else if (tileIndex >= loadedTiles.Length) tileIndex = 0;

        tile1 = tileIndex;
    }

    public void ChangeOtherTile(int delta) {
        otherTileIndex += delta > 0 ? 1 : -1;

        if (otherTileIndex < 0) otherTileIndex = loadedTiles.Length - 1;
        else if (otherTileIndex >= loadedTiles.Length) otherTileIndex = 0;

        tile2 = otherTileIndex;
    }

    public void ChangeLayer(int delta) {
        currentLayer += delta > 0 ? 1 : -1;

        if (currentLayer < 0) currentLayer = (int)MapLayer.Total - 1;
        else if (currentLayer >= (int)MapLayer.Total) currentLayer = 0;
    }

    public void PlaceTile() {

    }

    public void RemoveTile() {

    }
}