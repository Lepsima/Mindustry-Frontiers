using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Teams;

public class PlayerManager : MonoBehaviour {
    [SerializeField] [Range(1, 50)] float zoomClampMax = 40f;

    public static PlayerManager Instance;
    public bool IsPlayerSpawned; 

    public Transform target;
    public InventoryViewer inventoryViewer;
    public Block selectedBlock;

    public GameObject PlayerGameObject { get; set; }

    public bool buildMode = false;
    public int unitFollowIndex = 0;
    
    public bool IsFollowingPlayer() {
        return target == PlayerGameObject.transform;
    }

    public static void InitializePlayerManager() {
        // Instantiate Spectator Camera
        GameObject spectatorCameraPrefab = Assets.GetPrefab("SpectatorCameraPrefab");
        Instance = Instantiate(spectatorCameraPrefab, new Vector3(0, 0, -10f), Quaternion.identity).GetComponent<PlayerManager>();

        // Instantiate Inventory Viewer
        GameObject inventoryViewerPrefab = Assets.GetPrefab("InventoryViewerPrefab"); 
        Instance.inventoryViewer = Instantiate(inventoryViewerPrefab, Vector3.zero, Quaternion.identity).GetComponent<InventoryViewer>();
    }

    public void LateUpdate() {
        // If player is dead or doesnt exist, check for closest core to spawn again
        if (IsPlayerSpawned == false && TeamUtilities.LocalCoreBlocks.Count > 0) SpawnPlayer();

        if (IsPlayerSpawned) {
            if (target) {
                // Interpolate Current position with target position for a smooth transition
                Vector3 targetPosition = Vector2.Lerp(target.position, transform.position, Time.deltaTime * 75f);

                // Set the needed position for camera depth
                targetPosition.z = -10f;

                // Set new position
                transform.position = targetPosition;
            } else {
                Follow(PlayerGameObject.transform);
            }

            // Update zoom
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - Input.mouseScrollDelta.y * 10 * Time.deltaTime, 1, zoomClampMax);
        }

        HandleInput();
    }

    public void HandleInput() {
        // Toggle build mode
        if (Input.GetKeyDown(KeyCode.B)) { 
            buildMode = !buildMode;
            selectedBlock = null;
            inventoryViewer.SetItemList(null, Vector2.zero);
        }

        if (buildMode && IsPlayerSpawned) {
            // Handle Build Mode
            if (Input.GetMouseButtonDown(0)) MapManager.Instance.HandlePlayerMouseInputDown(0);
            if (Input.GetMouseButtonDown(1)) MapManager.Instance.HandlePlayerMouseInputDown(1);
            
            if (!IsFollowingPlayer()) Follow(PlayerGameObject.transform);

        } else if (!buildMode) {
            if (Input.GetMouseButtonDown(0)) { 
                Block newBlock = MapManager.Instance.GetBlockAtMousePos();
                selectedBlock = newBlock == selectedBlock || !newBlock || !newBlock.IsLocalTeam() ? null : newBlock;

                if (selectedBlock && selectedBlock.hasInventory) inventoryViewer.SetItemList(((ItemBlock)selectedBlock).GetItemList(), selectedBlock.GetGridPosition() + (Vector2.one * selectedBlock.Type.size));
                else inventoryViewer.SetItemList(null, Vector2.zero);
            }

            if (Input.GetMouseButtonDown(1)) {
                if (selectedBlock && selectedBlock.hasInventory) MapManager.Instance.AddItems(selectedBlock, new ItemStack(Items.silicon, 5));
            }
            
            if (Input.GetKeyDown(KeyCode.Q)) {
                // Follow the previous unit of the list
                if (unitFollowIndex <= 0) unitFollowIndex = MapManager.units.Count - 1;
                else unitFollowIndex--;
                
                Transform unitTransform = MapManager.units[unitFollowIndex];
                Follow(unitTransform);
            }
            
            if (Input.GetKeyCode(KeyCode.E)) {
                // Follow the next unit of the list
                if (unitFollowIndex >= MapManager.units.Count) unitFollowIndex = 0;
                else unitFollowIndex++;
                
                Transform unitTransform = MapManager.units[unitFollowIndex];
                Follow(unitTransform);
            }
        }
    }

    public void Follow(Transform target) {
        this.target = target;
    }

    public void SpawnPlayer() {
        IsPlayerSpawned = true;
        PlayerGameObject = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerPrefab"), TeamUtilities.GetClosestAllyCoreBlock(Vector2.zero).GetPosition(), Quaternion.identity);
        Follow(PlayerGameObject.transform);
    }

    public void DestroyPlayer() {
        IsPlayerSpawned = false;
        PhotonNetwork.Destroy(PlayerGameObject.GetPhotonView());
    }
}
