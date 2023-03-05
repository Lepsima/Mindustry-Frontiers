using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Frontiers.Content;
using Frontiers.Assets;
using System;
using System.IO;
using Frontiers.Teams;

public class MapManager : MonoBehaviourPunCallbacks {
    public static MapManager Instance;

    public Vector2 shardCorePosition, cruxCorePosition;
    public Vector3Int mouseTilePos;

    public static List<Unit> units = new List<Unit>();
    public static List<Block> blocks = new List<Block>();
    public static Dictionary<float, LoadableContent> loadableContentDictionary = new Dictionary<float, LoadableContent>();

    private GameObject blockPrefab;
    public TileBase blockGridTile;

    public Transform mouseTile;
    public SpriteRenderer mouseTileSprite;

    public Tilemap groundTilemap;
    public Tilemap solidTilemap;

    public TileBase[] nonPlacableTiles = new TileBase[0];

    BlockType selectedBlock = Blocks.copperWall;
    bool allowPlace;


    #region - Management -

    public static void InitializeMapManager() {
        Instance = FindObjectOfType<MapManager>();
        Instance.Setup();
    }

    public void Setup() {
        blockPrefab = Assets.GetPrefab("BlockPrefab");
    }

    public void UpdateMapManager() {
        mouseTilePos = UpdateMouseTile(selectedBlock.size);
    }
    
    public static float GetCurrentTime() {
        return DateTime.Now.Millisecond + DateTime.Now.Second * 1000 + DateTime.Now.Minute * 60000;
    }

    public void InitializeCores() {
        CreateBlock(shardCorePosition, Blocks.coreShard, 1);
        CreateBlock(cruxCorePosition, Blocks.coreShard, 2);
    }

    public byte LocalTeam() => TeamUtilities.GetLocalPlayerTeam().Code;

    #endregion

    #region - Player -

    public void Spectate(LoadableContent loadableContent) {
        PlayerManager.Instance.Follow(loadableContent.transform);
    }

    public void HandlePlayerMouseInputDown(int button) {
        if (button == 0) { 
            if (allowPlace) CreateBlock((Vector3)mouseTilePos, selectedBlock, LocalTeam()); 
        } else if (button == 1) DeleteBlock((Vector3)mouseTilePos);
    }

    public void PlayerInput(KeyCode keyCode) {
        if (!Input.GetKeyDown(keyCode)) return;

        switch (keyCode) {
            case KeyCode.Alpha1:
                selectedBlock = Blocks.copperWall;
                break;

            case KeyCode.Alpha2:
                selectedBlock = Blocks.copperWallLarge;
                break;

            case KeyCode.Alpha3:
                selectedBlock = Blocks.landingPad;
                break;

            case KeyCode.Alpha4:
                selectedBlock = Blocks.tempest;
                break;

            case KeyCode.Alpha5:
                selectedBlock = Blocks.airFactory;
                break;

            case KeyCode.Alpha8:
                CreateUnit(new Vector2(0, 0), Units.flare, 1);
                break;

            case KeyCode.Alpha9:
                CreateUnit(new Vector2(0, 0), Units.flare, 2);
                break;

            case KeyCode.G:
                CreateUnit(new Vector2(0, 0), Units.flare, LocalTeam());
                break;
        }
    }

    #endregion 

    #region - TileMap -

    private Vector3Int UpdateMouseTile(int size) {
        Vector3Int mousePos = Vector3Int.CeilToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition) - (Vector3.one * 0.5f) - (0.5f * size * Vector3.one));
        mousePos.z = 0;

        mouseTile.position = mousePos + (Vector3)(0.5f * size * Vector2.one);
        mouseTile.localScale = Vector3.one * size;

        allowPlace = CanPlaceTileIn(mousePos, size);
        mouseTileSprite.color = allowPlace ? Color.yellow : Color.red;
        return mousePos;
    }

    public TileBase GetMapTileAt(Vector3 position) => groundTilemap.GetTile(Vector3Int.CeilToInt(position));

    public TileBase GetSolidTileAt(Vector3 position) => solidTilemap.GetTile(Vector3Int.CeilToInt(position));

    public bool CanPlaceTileIn(Vector3Int position, int size) {
        if (size == 1) return CanPlaceTileIn(position);
        
        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                Vector3Int sizePosition = position + new Vector3Int(x, y, 0);
                if (!CanPlaceTileIn(sizePosition)) return false;
            }
        }

        return true;
    }

    public bool CanPlaceTileIn(Vector3Int position) => !solidTilemap.HasTile(position) && !ContainsAnyTile(position, nonPlacableTiles);

    public void PlaceTile(Vector3Int position, TileBase tile, int size) {
        if (size == 1) {
            PlaceTile(position, tile);
            return;
        }

        for (int x = 0; x < size; x++) {
            for (int y = 0; y < size; y++) {
                Vector3Int sizePosition = position + new Vector3Int(x, y, 0);
                PlaceTile(sizePosition, tile);
            }
        }
    }

    public void PlaceTile(Vector3Int position, TileBase tile) {
        solidTilemap.SetTile(position, tile);
    }

    private bool ContainsAnyTile(Vector3Int position, TileBase[] tiles) {
        foreach (TileBase tile in tiles) if (groundTilemap.GetTile(position) == tile) return true;
        return false;
    }

    [PunRPC]
    public void RPC_PlaceTile(Vector2 position, int size) {
        PlaceTile(Vector3Int.CeilToInt(position), blockGridTile, size);
    }

    #endregion

    #region - Blocks -

    public LoadableContent GetClosestLoadableContent(Vector2 position, Type type, byte teamCode) {
        LoadableContent closestLoadableContent = null;
        float closestDistance = 99999f;

        foreach (LoadableContent loadableContent in loadableContentDictionary.Values) {
            //If content doesn't match the filter, skip
            if (!(loadableContent.GetTeam().Code == teamCode && loadableContent.GetType() == type)) continue;

            //Get distance to content
            float distance = Vector2.Distance(position, loadableContent.GetPosition());

            //If distance is lower than previous closest distance, set this as the closest content
            if (distance < closestDistance) {
                closestDistance = distance;
                closestLoadableContent = loadableContent;
            }
        }

        return closestLoadableContent;
    }

    public LoadableContent GetClosestLoadableContentInView(Vector2 position, Vector2 direction, float fov, Type type, byte teamCode) {
        LoadableContent closestLoadableContent = null;
        float closestDistance = 99999f;

        foreach (LoadableContent loadableContent in loadableContentDictionary.Values) {
            //If content doesn't match the filter, skip
            if (!(loadableContent.GetTeam().Code == teamCode && loadableContent.GetType() == type)) continue;

            //If is not in view range continue to next
            float cosAngle = Vector2.Dot((loadableContent.GetPosition() - position).normalized, direction);
            float angle = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;


            //Get distance to content
            float distance = Vector2.Distance(position, loadableContent.GetPosition());
            if (angle > fov) continue;

            //If distance is lower than previous closest distance, set this as the closest content
            if (distance < closestDistance) {
                closestDistance = distance;
                closestLoadableContent = loadableContent;
            }
        }

        return closestLoadableContent;
    }

    public Block GetClosestBlock(Vector2 position, Type type, byte teamCode) {
        Block closestBlock = null;
        float closestDistance = 99999f;

        foreach(Block block in blocks) {
            //If block doesn't match the filter, skip
            if (!(block.GetTeam().Code == teamCode && block.GetType() == type)) continue;

            //Get distance to block
            float distance = Vector2.Distance(position, block.GetPosition());

            //If distance is lower than previous closest distance, set this as the closest block
            if (distance < closestDistance) {
                closestDistance = distance;
                closestBlock = block;
            }
        }

        return closestBlock;
    }

    public LandPadBlock GetClosestLandPad(Vector2 position, byte teamCode) {
        LandPadBlock closestLandPad = null;
        float closestDistance = 99999f;

        foreach (Block block in blocks) {
            if (block is LandPadBlock landPad) {

                //If block doesn't match the filter, skip
                if (landPad.IsFull() || landPad.GetTeam().Code != teamCode) continue;

                //Get distance to block
                float distance = Vector2.Distance(position, landPad.GetGridPosition());

                //If distance is lower than previous closest distance, set this as the closest block
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestLandPad = landPad;
                }
            }
        }

        return closestLandPad;
    }


    public Block GetBlockAtMousePos() => GetBlockAt((Vector2Int)mouseTilePos);

    public Block GetBlockAt(Vector2Int position) {
        foreach (Block block in blocks) if (block.GetGridPosition() == position) return block;
        foreach (Block block in blocks) if (block.ExistsIn(Vector2Int.CeilToInt(position))) return block;
        return null;
    }

    public void CreateBlock(Vector2 position, Content content, byte teamCode) {
        photonView.RPC(nameof(RPC_CreateNewBlock), RpcTarget.All, position, content.id, GetCurrentTime(), teamCode);
    }

    public void DestroyBlock(Block block) {
        photonView.RPC(nameof(RPC_DeleteBlock), RpcTarget.All, (Vector2)block.GetGridPosition());
    }

    public void DeleteBlock(Vector2 position) {
        Block block = GetBlockAt(Vector2Int.CeilToInt(position));
        if (!block || block.GetTeam() != TeamUtilities.GetLocalPlayerTeam() || !block.Type.beakable) return;

        photonView.RPC(nameof(RPC_DeleteBlock), RpcTarget.All, (Vector2)block.GetGridPosition());
    }

    [PunRPC]
    public void RPC_CreateNewBlock(Vector2 position, short id, float timeCode, byte teamCode) {
        Vector3Int gridPosition = Vector3Int.CeilToInt(position);
        BlockType blockType = ContentLoader.GetContentById(id) as BlockType;

        GameObject blockGameObject = Instantiate(blockPrefab, gridPosition, Quaternion.identity);
        Block block = (Block)blockGameObject.AddComponent(blockType.type);

        PlaceTile(gridPosition, blockGridTile, blockType.size);

        block.Set((Vector2Int)gridPosition, blockType, timeCode, teamCode);
    }

    [PunRPC]
    public void RPC_DeleteBlock(Vector2 position) {
        Vector3Int gridPosition = Vector3Int.CeilToInt(position);

        Block block = GetBlockAt((Vector2Int)gridPosition);
        blocks.Remove(block);

        PlaceTile(gridPosition, null, block.Type.size);
        block.Delete();
    }

    #endregion

    #region - Units -

    public int CreateUnit(Vector2 position, Content content, byte teamCode) {
        GameObject unitGameObject = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "UnitPrefab"), Vector3.zero, Quaternion.identity);
        photonView.RPC(nameof(RPC_CreateUnit), RpcTarget.All, position, content.id, GetCurrentTime(), teamCode, unitGameObject.GetPhotonView().ViewID);
        return unitGameObject.GetPhotonView().ViewID;
    }

    [PunRPC]
    public void RPC_CreateUnit(Vector2 position, short id, float timeCode, byte teamCode, int viewID) {
        UnitType unitType = ContentLoader.GetContentById(id) as UnitType;
        GameObject unitGameObject = PhotonNetwork.GetPhotonView(viewID).gameObject;

        Unit unit = (Unit)unitGameObject.AddComponent(unitType.type);
        unit.Set(position, Vector2.up * 0.5f, unitType, timeCode, teamCode);
    }
    #endregion

    #region - Damage -

    public void BulletHit(IDamageable damageable, BulletType bulletType) {
        photonView.RPC(nameof(RPC_BulletHit), RpcTarget.All, damageable.GetTimeCode(), bulletType.id);
    }

    [PunRPC]
    public void RPC_BulletHit(float timeCode, short id) {
        if (!loadableContentDictionary.ContainsKey(timeCode)) return; 

        LoadableContent loadableContent = loadableContentDictionary[timeCode];
        BulletType bulletType = (BulletType)ContentLoader.GetContentById(id);

        //Using bullet type and the hitted content position, create area damage or special bullet things, no need to RPC, this is already called on everyone

        if (loadableContent.TryGetComponent(out IDamageable damageable)) damageable.Damage(bulletType.damage);    
    }

    #endregion

    #region - Items -

    public void AddItems(LoadableContent loadableContent, ItemStack itemStack) {
        photonView.RPC(nameof(RPC_AddItems), RpcTarget.All, loadableContent.GetTimeCode(), itemStack.item.id, itemStack.amount);
    }

    public void SubstractItems(LoadableContent loadableContent, ItemStack itemStack) {
        photonView.RPC(nameof(RPC_SubstractItems), RpcTarget.All, loadableContent.GetTimeCode(), itemStack.item.id, itemStack.amount);
    }

    [PunRPC]
    public void RPC_AddItems(float timeCode, short itemId, int amount) {
        if (!loadableContentDictionary.ContainsKey(timeCode)) return;

        LoadableContent loadableContent = loadableContentDictionary[timeCode];
        if (!loadableContent.hasInventory) return;

        Item item = ContentLoader.GetContentById(itemId) as Item;
        loadableContent.GetComponent<IInventory>().AddItems(new ItemStack(item, amount));
    }

    [PunRPC]
    public void RPC_SubstractItems(float timeCode, short itemId, int amount) {
        if (!loadableContentDictionary.ContainsKey(timeCode)) return;

        LoadableContent loadableContent = loadableContentDictionary[timeCode];
        if (!loadableContent.hasInventory) return;

        Item item = ContentLoader.GetContentById(itemId) as Item;
        loadableContent.GetComponent<IInventory>().SubstractItems(new ItemStack(item, amount));
    }
    #endregion
}