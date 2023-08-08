using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Frontiers.Content.Maps;
using Frontiers.Content.Upgrades;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Teams;
using UnityEngine.EventSystems;
using System;
using Cinemachine;
using UnitMode = Unit.UnitMode;
using Frontiers.FluidSystem;

public class PlayerManager : MonoBehaviour {
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float zoomSpeed = 30f;
    [SerializeField] float zoomInMultiplier = 2f;
    [SerializeField] float zoomOutMultiplier = 1f;
    [SerializeField] [Range(1, 50)] float zoomClampMin = 10f;
    [SerializeField] [Range(1, 50)] float zoomClampMax = 50f;

    public static PlayerManager Instance;
    public Entity selectedEntity;

    private CinemachineVirtualCamera virtualCamera;
    private Transform playerTransform;

    public bool buildMode = false;
    public int unitFollowIndex = 0;

    public static Vector3 mousePos;

    private void Start() {
        Instance = this;
        PlayerContentSelector.OnSelectedContentChanged += Instance.OnSelectedContentChanged;

        playerTransform = transform.GetChild(0);
        playerTransform.parent = null;

        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        Follow(playerTransform);

        playerTransform.position = TeamUtilities.GetClosestAllyCoreBlock(playerTransform.position).GetPosition();
    }

    public bool IsFollowingPlayer() {
        return virtualCamera.Follow == playerTransform;
    }

    public void Update() {
        playerTransform.position += Time.deltaTime * virtualCamera.m_Lens.OrthographicSize * new Vector3(Input.GetAxis("Horizontal") * moveSpeed, Input.GetAxis("Vertical") * moveSpeed, 0);
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

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
            foreach (Unit unit in MapManager.Map.units) Client.UnitChangeMode(unit, (int)UnitMode.Attack);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            foreach (Unit unit in MapManager.Map.units) Client.UnitChangeMode(unit, (int)UnitMode.Patrol);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3)) {
            foreach (Unit unit in MapManager.Map.units) Client.UnitChangeMode(unit, (int)UnitMode.Return);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4)) {
            foreach (Unit unit in MapManager.Map.units) Client.UnitChangeMode(unit, (int)UnitMode.Assist);
        }

        if (Input.GetKeyDown(KeyCode.Alpha5)) {
            foreach (Unit unit in MapManager.Map.units) Client.UnitChangeMode(unit, (int)UnitMode.Idling);
        }
    }

    private void HandleBuildMode() {
        if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0)) PlayerContentSelector.CreateSelectedContent(mousePos, 0);
        if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(1)) PlayerContentSelector.CreateSelectedContent(mousePos, 1);

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


            float delta = Input.mouseScrollDelta.y;
            float change = delta * zoomSpeed * ( delta < 0f ? zoomOutMultiplier : zoomInMultiplier) * Time.deltaTime;
            virtualCamera.m_Lens.OrthographicSize = Mathf.Clamp(virtualCamera.m_Lens.OrthographicSize - change, zoomClampMin, zoomClampMax);
        }
    }

    public void AddItems(int amount) {
        Content selectedContent = PlayerContentSelector.SelectedContent;
        if (selectedContent == null || !selectedContent.GetType().EqualsOrInherits(typeof(Element)) || selectedEntity == null) return;

        if (selectedContent is Item item) {
            if (selectedEntity is SorterBlock sorterBlock) {
                sorterBlock.SetFilter(item);
            }

            if (selectedEntity.CanReciveItem(item)) {
                selectedEntity.ReciveItems(item, amount);
            }

        } else if (selectedContent is Fluid fluid) {
            if (selectedEntity is FluidFilterBlock filterBlock) {
                filterBlock.SetFilter(fluid);
            }
        }
    }

    public void OnSelectedContentChanged(object sender, PlayerContentSelector.ContentEventArgs e) {
        buildMode = e.content != null && !e.content.GetType().EqualsOrInherits(typeof(Element));

        if (buildMode) {
            selectedEntity = null;
            InventoryViewer.Instance.SetInventory(null);
            Follow(playerTransform);
        }
    }

    public void ChangeFollowingUnit(int increment) {
        unitFollowIndex += increment;

        if (unitFollowIndex >= MapManager.Map.units.Count) unitFollowIndex = 0;
        if (unitFollowIndex < 0) unitFollowIndex = MapManager.Map.units.Count - 1;
        
        Unit unit = MapManager.Map.units[unitFollowIndex];
        Transform unitTransform = unit.transform;

        unit.OnDestroyed += OnFollowingUnitDestroyed;

        Follow(unitTransform);
    }

    public void OnFollowingUnitDestroyed(object sender, Entity.EntityArg e) {
        // If there's a registered killer, follow that entity
        Follow(e.other ? e.other.transform : playerTransform);
    }

    public void Follow(Transform target) {
        virtualCamera.Follow = target;
    }

    public void CameraShake() {

    }
}
