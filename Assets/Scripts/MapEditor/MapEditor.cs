using Frontiers.Assets;
using Frontiers.Content;
using Frontiers.Content.Maps;
using Frontiers.Settings;
using UnityEngine;
using MapLayer = Frontiers.Content.Maps.Map.MapLayer;

public class MapEditor : MonoBehaviour {
    public static MapEditor Instance;
    public MapEditorCamera editorCamera;
    public MenuManager menuManager;
    public Menu viewMenu;

    public int regionSize = 32;

    [Header("Editor Layers")]
    public MapEditorLayer[] layers;

    public MapLayer noiseLayer;

    public bool overrideIfNull;
    public int mainTile;
    public int secTile;

    TileType[] loadedTiles;

    int currentLayer;
    public bool placeEnabled = false;

    static Map map;

    private void Awake() {
        Instance = this;

        AssetLoader.LoadAssets();
        ContentLoader.LoadContents();

        Main.Map_RegionSize = regionSize;
        MapDisplayer.SetupAtlas();

        loadedTiles = TileLoader.GetLoadedTiles();
    }

    public void LoadMap(string mapName) {
        Map map = new(mapName, MapLoader.ReadMap(mapName));
        OnMapLoaded(map);
    }

    public void CreateMap(string mapName, Vector2Int size) {
        Map map = new(mapName, size.x, size.y);
        OnMapLoaded(map);
    }

    public static void OnMapLoaded(Map map) {
        MapEditor.map = map;
        MapEditor.Instance.Replace();
        Instance.editorCamera.SetPosition((Vector3)((Vector2)map.size / 2f) + new Vector3(0, 0, -10));
    }

    public void PlaceMode() {
        placeEnabled = true;
    }

    public void ViewMode() {
        placeEnabled = false;
    }

    private void Update() {
        editorCamera.AllowMove(menuManager.openMenu == viewMenu);

        if (map == null) return;
        Vector2Int mouseGridPos = Vector2Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        if (placeEnabled) {
            if (Input.GetMouseButton(0)) {
                map.UpdatePlaceTile((MapLayer)currentLayer, mouseGridPos, loadedTiles[mainTile]);
            }

            if (Input.GetMouseButton(1)) {
                map.UpdatePlaceTile((MapLayer)currentLayer, mouseGridPos, null);
            }
        }
    }

    public void Replace() {
        Vector2Int size = map.size;
        TileType ice = map.GetTileType("ice");

        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                Vector2Int pos = new(x, y);
                TileType tile = map.GetMapTileTypeAt(MapLayer.Ground, pos);

                if (tile.name.StartsWith("ore-")) {
                    map.PlaceTile(MapLayer.Ground, pos, ice);
                    map.PlaceTile(MapLayer.Ore, pos, tile);
                }
            }
        }

        map.tilemap.UpdateMesh();
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

    public void ApplyNoise(TileType mainTile, TileType secTile, float scale, float threshold, int octaves, float persistance, float lacunarity, float riverWidth, float shoreWidth) {
        bool isRiver = riverWidth != -1f;
        float minThreshold = isRiver ? threshold - riverWidth : -1;
        float shoreThreshold = shoreWidth;

        bool replaceIfNull = (MapLayer)currentLayer == MapLayer.Ore && secTile == null;

        int seed = Random.Range(0, 999999);

        float maxValue = float.MinValue;
        float minValue = float.MaxValue;

        float[,] values = new float[map.size.x, map.size.y];
        Vector2Int size = map.size;

        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                float perlinValue = 0f;
                float amplitude = 1f;
                float frequency = 1f;

                for (int i = 0; i < octaves; i++) {
                    float xCoord = x / scale * frequency + seed;
                    float yCoord = y / scale * frequency + seed;

                    float value = Mathf.PerlinNoise(xCoord, yCoord) * 2f - 1f;
                    perlinValue += value * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (perlinValue > maxValue) maxValue = perlinValue;
                if (perlinValue < minValue) minValue = perlinValue;

                values[x, y] = perlinValue;
            }
        }

        for (int x = 0; x < size.x; x++) {
            for (int y = 0; y < size.y; y++) {
                values[x, y] = Mathf.InverseLerp(minValue, maxValue, values[x, y]);

                bool isTile1 = minThreshold == -1 ? values[x, y] > threshold : values[x, y] < threshold && values[x, y] > minThreshold;
                bool isTile2 = (!isTile1 && (values[x, y] < threshold + shoreThreshold && values[x, y] > minThreshold - shoreThreshold));

                TileType tileType = isTile1 ? mainTile : (minThreshold == -1 || isTile2) ? secTile : null;
                
                if (!replaceIfNull && tileType == null) continue;
                map.PlaceTile((MapLayer)currentLayer, new Vector2Int(x, y), tileType);
            }
        }

        map.tilemap.UpdateMesh();
    }

    public void ApplyReplace(TileType targetTile, TileType replaceTile) {
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

        map.tilemap.UpdateMesh();
    }

    public int GetByName(string name) {
        for (int i = 0; i < loadedTiles.Length; i++) if (loadedTiles[i].name == name) return i;
        return -1;
    }

    public int GetByType(TileType type) {
        for (int i = 0; i < loadedTiles.Length; i++) if (loadedTiles[i] == type) return i;
        return -1;
    }
}