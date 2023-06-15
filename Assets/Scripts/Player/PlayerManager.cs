using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Teams;
using UnityEngine.EventSystems;
using System;
using UnitMode = Unit.UnitMode;

public class PlayerManager : MonoBehaviour {
    [SerializeField] [Range(1, 50)] float zoomClampMax = 40f;

    public static PlayerManager Instance;
    public bool IsPlayerSpawned;

    public Transform target;
    public Entity selectedEntity;

    public GameObject PlayerGameObject { get; set; }
    public bool buildMode = false;
    public int unitFollowIndex = 0;

    public static Vector3 mousePos;

    public bool IsFollowingPlayer() {
        return target == PlayerGameObject.transform;
    }

    public static void InitializePlayerManager() {
        // Instantiate Spectator Camera
        GameObject spectatorCameraPrefab = AssetLoader.GetPrefab("SpectatorCameraPrefab");
        Instance = Instantiate(spectatorCameraPrefab, new Vector3(0, 0, -10f), Quaternion.identity).GetComponent<PlayerManager>();
        PlayerContentSelector.OnSelectedContentChanged += Instance.OnSelectedContentChanged;
    }

    public void Update() {
        // If player is dead or doesnt exist, check for closest core to spawn again
        if (!IsPlayerSpawned && TeamUtilities.LocalCoreBlocks.Count > 0) SpawnPlayer();
        if (!IsPlayerSpawned) return;

        if (!target) {
            Follow(PlayerGameObject.transform);
        }

        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Vector3 targetPosition = target.position;
        targetPosition.z = -10f;

        // Set new position
        transform.position = targetPosition;

        if (buildMode) HandleBuildMode();
        else HandleMainMode();

        if (Input.GetKeyDown(KeyCode.Q)) {
            ChangeFollowingUnit(-1);
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            ChangeFollowingUnit(1);
        }

        if (Input.GetKeyDown(KeyCode.F)) {
            FireController.CreateFire(MapManager.mouseGridPos);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            foreach (Unit unit in MapManager.units) Client.UnitChangeMode(unit, (int)UnitMode.Attack);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            foreach (Unit unit in MapManager.units) Client.UnitChangeMode(unit, (int)UnitMode.Patrol);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3)) {
            foreach (Unit unit in MapManager.units) Client.UnitChangeMode(unit, (int)UnitMode.Return);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4)) {
            foreach (Unit unit in MapManager.units) Client.UnitChangeMode(unit, (int)UnitMode.Assist);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5)) {
            foreach (Unit unit in MapManager.units) Client.UnitChangeMode(unit, (int)UnitMode.Idling);
        }
    }

    private void HandleBuildMode() {
        if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0)) PlayerContentSelector.CreateSelectedContent(mousePos, 0);
        if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(1)) PlayerContentSelector.CreateSelectedContent(mousePos, 1);

        if (!IsFollowingPlayer()) Follow(PlayerGameObject.transform);

        PlayerContentSelector.ChangeSelectedContentOrientation(Input.mouseScrollDelta.y);
    }

    private void HandleMainMode() {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            if (Input.GetMouseButtonDown(0)) {
                selectedEntity = null;

                Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Collider2D[] allColliders = Physics2D.OverlapCircleAll(mousePos, 0.05f);

                foreach (Collider2D collider in allColliders) {
                    if (collider.transform.TryGetComponent(out Entity content)) {
                        selectedEntity = content;
                        break;
                    }
                }

                InventoryViewer.Instance.SetInventory(selectedEntity ? selectedEntity.GetInventory() : null);
            }

            if (Input.GetMouseButtonDown(1)) {

            }

            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - Input.mouseScrollDelta.y * 10 * Time.deltaTime, 1, zoomClampMax);
        }
    }

    public void AddItems(int amount) {
        Content selectedContent = PlayerContentSelector.SelectedContent;

        if (selectedContent == null || !MapManager.TypeEquals(selectedContent.GetType(), typeof(Item))) return;
        if (selectedEntity == null || !selectedEntity.hasInventory || selectedEntity.GetInventory() == null) return;

        selectedEntity.GetInventory().Add(selectedContent as Item, amount);
    }

    public void OnSelectedContentChanged(object sender, PlayerContentSelector.ContentEventArgs e) {
        buildMode = e.content != null && !MapManager.TypeEquals(e.content.GetType(), typeof(Item));

        if (buildMode) {
            selectedEntity = null;
            InventoryViewer.Instance.SetInventory(null);
        }
    }

    public void ChangeFollowingUnit(int increment) {
        unitFollowIndex += increment;

        if (unitFollowIndex >= MapManager.units.Count) unitFollowIndex = 0;
        if (unitFollowIndex < 0) unitFollowIndex = MapManager.units.Count - 1;

        Transform unitTransform = MapManager.units[unitFollowIndex].transform;
        Follow(unitTransform);
    }

    public void Follow(Transform target) {
        this.target = target;
    }

    public void SpawnPlayer() {
        IsPlayerSpawned = true;
        GameObject playerPrefab = AssetLoader.GetPrefab("PlayerPrefab");
        PlayerGameObject = Instantiate(playerPrefab, TeamUtilities.GetClosestAllyCoreBlock(Vector2.zero).GetPosition(), Quaternion.identity);
        Follow(PlayerGameObject.transform);
    }

    public void DestroyPlayer() {
        IsPlayerSpawned = false;
        Destroy(PlayerGameObject);
    }
}
