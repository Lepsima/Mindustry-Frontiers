using Frontiers.Assets;
using Frontiers.Content;
using Frontiers.Content.Maps;
using Frontiers.Settings;
using UnityEngine;
using MapLayer = Frontiers.Content.Maps.Map.MapLayer;

public class MapEditor : MonoBehaviour {
    public MapEditorCamera editorCamera;

    public int regionSize = 32;

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
    public int mainTile;
    public int secTile;

    TileType[] loadedTiles;

    int currentLayer;
    public bool placeEnabled = false;

    Tilemap tilemap;
    Map map;

    private void Awake() {
        AssetLoader.LoadAssets();
        ContentLoader.LoadContents();

        Main.Map_RegionSize = regionSize;
        MapDisplayer.SetupAtlas();

        MapLoader.OnMapLoaded += OnMapLoaded;
        loadedTiles = TileLoader.GetLoadedTiles();
    }

    public void LoadMap(string mapName) {
        MapLoader.LoadMap(mapName);
    }

    public void CreateMap(string mapName, Vector2Int size) {
        tilemap = new(size, Vector2Int.one * Main.Map_RegionSize);
        map = new Map(mapName, size.x, size.y, tilemap);
    }

    private void OnMapLoaded(object sender, MapLoader.MapLoadedEventArgs e) {
        map = e.loadedMap;
        editorCamera.transform.position = (Vector2)map.size / 2f;
    }

    private void Update() {
        if (map == null) return;
        Vector2Int mouseGridPos = Vector2Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        if (placeEnabled) {
            if (Input.GetMouseButtonDown(0)) {
                map.PlaceTile((MapLayer)currentLayer, mouseGridPos, loadedTiles[mainTile]);
            }

            if (Input.GetMouseButtonDown(1)) {
                map.PlaceTile((MapLayer)currentLayer, mouseGridPos, null);
            }
        }
    }

    public void SaveMap() {
        map.Save();
        MapLoader.SaveMap(map);
    }

    // Change the editing layer
    public void ChangeLayer(int layer) {
        currentLayer = layer;
    }

    // Change the place tile3w
    public void ChangeTile(int tile) {
        mainTile = tile;
    }

    public void Noise(string tile1Name, string tile2Name, float scale, float threshold, int octaves, float persistance, float lacunarity, float riverWidth, float shoreWidth) {
        map.tilemap.HoldMeshUpdate(true);

        mainTile = GetByName(tile1Name);
        secTile = GetByName(tile2Name);

        this.scale = scale;
        this.threshold = threshold;
        this.octaves = octaves;
        this.persistance = persistance;
        this.lacunarity = lacunarity;

        bool isRiver = riverWidth != -1f;
        minThreshold = isRiver ? threshold - riverWidth : -1;
        shoreThreshold = shoreWidth;

        overrideIfNull = (MapLayer)currentLayer == MapLayer.Ore && secTile == -1;

        ApplyNoise(-1);

        map.tilemap.HoldMeshUpdate(false);
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
            mainTile = GetByName(mapLayer.tile1Name);
            secTile = GetByName(mapLayer.tile2Name);
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

    public int GetByType(TileType type) {
        for (int i = 0; i < loadedTiles.Length; i++) if (loadedTiles[i] == type) return i;
        return -1;
    }

    public void ApplyReplace() {
        TileType targetTile = mainTile == -1 ? null : loadedTiles[mainTile];
        TileType replaceTile = secTile == -1 ? null : loadedTiles[secTile];

        if (targetTile == null) {
            Debug.LogWarning("Target tile not found for replace action");
            return;
        }

        for (int x = 0; x < map.size.x; x++) {
            for (int y = 0; y < map.size.y; y++) {
                Vector2Int position = new(x, y);
                if (map.GetMapTileTypeAt((MapLayer)currentLayer, position) == targetTile) map.PlaceTile((MapLayer)currentLayer, position, replaceTile);
            }
        }
    }

    public void ApplyNoise(int seed = -1) {
        TileType tile1 = mainTile == -1 ? null : loadedTiles[mainTile];
        TileType tile2 = secTile == -1 ? null : loadedTiles[secTile];

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
                map.PlaceTile((MapLayer)currentLayer, new Vector2Int(x, y), tileType);
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
}