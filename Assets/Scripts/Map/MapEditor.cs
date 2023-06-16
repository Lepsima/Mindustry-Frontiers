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

    [Header("Noise Settings")]
    public float scale = 3.25f;
    public float heightMultiplier = 1f;
    [Range(0f, 1f)] public float threshold = 0.5f;
    [Range(0, 10)] public int octaves = 4;
    [Range(0f, 0.999f)] public float persistance = 0.5f;
    public float lacunarity = 1f;

    TileType[] loadedTiles;
    int tileIndex;
    int otherTileIndex;

    int currentLayer;

    Tilemap tilemap;
    Map map;

    private void Awake() {
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
            ApplyNoise();
        }

        if (Input.GetMouseButtonDown(0)) {
            map.PlaceTile((MapLayer)currentLayer, mouseGridPos, loadedTiles[tileIndex]);
        }

        if (Input.GetMouseButtonDown(1)) {
            map.PlaceTile((MapLayer)currentLayer, mouseGridPos, null);
        }
    }

    public void ApplyNoise() {
        TileType tile1 = loadedTiles[tileIndex];
        TileType tile2 = loadedTiles[otherTileIndex];

        map.tilemap.HoldMeshUpdate(true);

        int seed = Random.Range(0, 999999);

        for (int x = 0; x < map.size.x; x++) {
            for (int y = 0; y < map.size.y; y++) {
                bool isTile1 = CalculateNoiseTile(x + seed, y + seed);
                map.PlaceTile(MapLayer.Ground, new Vector2Int(x, y), isTile1 ? tile1 : tile2);
            }
        }

        map.tilemap.HoldMeshUpdate(false);
    }

    public bool CalculateNoiseTile(int x, int y) {
        float perlinValue = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < octaves; i++) {
            float xCoord = x / frequency * scale;
            float yCoord = y / frequency * scale;

            perlinValue += Mathf.PerlinNoise(xCoord, yCoord) * amplitude;

            amplitude *= persistance;
            frequency *= lacunarity;
        }

        return (perlinValue * heightMultiplier) > threshold;
    }


    public void ChangeTile(int delta) {
        tileIndex += delta > 0 ? 1 : -1;

        if (tileIndex < 0) tileIndex = loadedTiles.Length - 1;
        else if (tileIndex >= loadedTiles.Length) tileIndex = 0;   
    }

    public void ChangeOtherTile(int delta) {
        otherTileIndex += delta > 0 ? 1 : -1;

        if (otherTileIndex < 0) otherTileIndex = loadedTiles.Length - 1;
        else if (otherTileIndex >= loadedTiles.Length) otherTileIndex = 0;
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