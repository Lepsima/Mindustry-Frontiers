using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Frontiers.Content;
using Frontiers.Content.Maps;
using Frontiers.Assets;
using System;
using System.IO;
using Frontiers.Teams;
using MapLayer = Frontiers.Content.Maps.Map.MapLayer;

public class MapManager : MonoBehaviourPunCallbacks {
    public static MapManager Instance;
    public static Map Map;

    public static Vector2Int mouseGridPos;
    public static bool mouseGridAllowsPlace;

    public static List<Unit> units = new();
    public static List<Block> blocks = new();
    public static int nextID;

    public Vector2 shardCorePosition, cruxCorePosition;
    public Tilemap[] tilemaps;

    public SpriteRenderer spriteRenderer;

    private GameObject blockPrefab;
    private GameObject unitPrefab;
    private TileBase blockGridTile;

    public static void InitializeMapManager() {
        Instance = FindObjectOfType<MapManager>();
        Instance.Setup();
    }

    public void Setup() {
        blockPrefab = AssetLoader.GetPrefab("BlockPrefab");
        unitPrefab = AssetLoader.GetPrefab("UnitPrefab");
        blockGridTile = Tiles.blockTile.GetRandomTileVariant();
        MapLoader.OnMapLoaded += OnMapLoaded;
    }

    public void OnMapLoaded(object sender, MapLoader.MapLoadedEvent e) {
        Map = e.loadedMap;
    }

    public void SaveMap() {
        MapLoader.SaveMap(Map);
    }

    public static bool IsLoaded() {
        return Map != null;
    }

    public void UpdateMapManager() {
        if (Map == null) return;
        Content selectedContent = PlayerContentSelector.SelectedContent;
        int size = selectedContent == null ? 1 : TypeEquals(selectedContent.GetType(), typeof(BlockType)) ? ((BlockType)selectedContent).size : 1;
        Vector2Int mouseGridPos = Vector2Int.CeilToInt(PlayerManager.mousePos - (Vector3.one * 0.5f) - (0.5f * size * Vector3.one));

        mouseGridAllowsPlace = Map.CanPlaceBlockAt(mouseGridPos, size);
        MapManager.mouseGridPos = mouseGridPos;
    }

    public void InitializeCores() {
        Client.CreateBlock(shardCorePosition, 0, false, Blocks.coreShard, 1);
        Client.CreateBlock(cruxCorePosition, 0, false, Blocks.coreShard, 2);
    }

    public static bool TypeEquals(Type target, Type reference) => target == reference || target.IsSubclassOf(reference);

    public int GetID() {
        int ID = nextID;
        nextID++;
        return ID;
    }

    public Block InstantiateBlock(Vector2 position, int orientation, short contentID, int syncID, byte teamCode) {
        Vector2Int gridPosition = Vector2Int.CeilToInt(position);
        BlockType blockType = (BlockType)ContentLoader.GetContentById(contentID);

        GameObject blockGameObject = Instantiate(blockPrefab, (Vector2)gridPosition, Quaternion.identity);
        Block block = (Block)blockGameObject.AddComponent(blockType.type);

        Map.PlaceTile(MapLayer.Solid, gridPosition, blockGridTile, blockType.size);

        block.Set(syncID);
        block.Set(gridPosition, Quaternion.Euler(0, 0, orientation * 90f), blockType, GetID(), teamCode);

        Map.AddBlock(block);
        Client.syncObjects.Add(block.SyncID, block);

        return block;
    }

    public ConstructionBlock InstantiateConstructionBlock(Vector2 position, int orientation, short contentID, int syncID, byte teamCode) {
        Vector2Int gridPosition = Vector2Int.CeilToInt(position);
        BlockType blockType = (BlockType)ContentLoader.GetContentById(contentID);

        GameObject blockGameObject = Instantiate(blockPrefab, (Vector2)gridPosition, Quaternion.identity);
        ConstructionBlock block = blockGameObject.AddComponent<ConstructionBlock>();

        Map.PlaceTile(MapLayer.Solid, gridPosition, blockGridTile, blockType.size);

        block.Set(syncID);
        block.Set(gridPosition, Quaternion.Euler(0, 0, orientation * 90f), blockType, GetID(), teamCode);

        Map.AddBlock(block);
        Client.syncObjects.Add(block.SyncID, block);

        return block;
    }

    public void DeleteBlock(Block block, bool destroyed) {
        Map.RemoveBlock(block);
        Client.syncObjects.Remove(block.SyncID);
        block.wasDestroyed = destroyed;
        Destroy(block.gameObject);
    }

    public Unit InstantiateUnit(Vector2 position, float rotation, short contentID, int syncID, byte teamCode) {
        UnitType unitType = ContentLoader.GetContentById(contentID) as UnitType;
        GameObject unitGameObject = Instantiate(unitPrefab, position, Quaternion.identity);
        Unit unit = (Unit)unitGameObject.AddComponent(unitType.type);

        unit.Set(syncID);
        unit.Set(position, Quaternion.Euler(0, 0, rotation), unitType, GetID(), teamCode);
        unit.SetVelocity(unitGameObject.transform.forward * 0.5f);

        Map.AddUnit(unit);
        Client.syncObjects.Add(unit.SyncID, unit);

        return unit;
    }

    public void DeleteUnit(Unit unit, bool destroyed) {
        Map.RemoveUnit(unit);
        Client.syncObjects.Remove(unit.SyncID);
        unit.wasDestroyed = destroyed;
        Destroy(unit.gameObject);
    }
}