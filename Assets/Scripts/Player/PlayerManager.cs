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
using Frontiers.Squadrons;

public class PlayerManager : MonoBehaviour {
    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float zoomSpeed = 30f;
    [SerializeField] float zoomInMultiplier = 2f;
    [SerializeField] float zoomOutMultiplier = 1f;
    [SerializeField] [Range(1, 50)] float zoomClampMin = 10f;
    [SerializeField] [Range(1, 50)] float zoomClampMax = 50f;

    public static PlayerManager Instance;
    public Entity selectedEntity;

    public event EventHandler<Entity.EntityArg> OnEntitySelected;

    private CinemachineVirtualCamera virtualCamera;
    private Transform playerTransform;

    public bool buildMode = false, forceFollow = false;
    public int unitFollowIndex = 0;

    public static Vector3 mousePos;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        track = new(new Vector2[2] { Vector2.zero, Vector2.zero });

        tool.Init();
        tool.editingTrack = track;

        PlayerContentSelector.OnSelectedContentChanged += Instance.OnSelectedContentChanged;

        playerTransform = transform.GetChild(0);
        playerTransform.parent = null;

        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        Follow(playerTransform);

        PlayerUI.Instance.EnableLoadingScreen(true);
    }

    public bool IsFollowingPlayer() {
        return virtualCamera.Follow == playerTransform;
    }

    public void Update() {
        if (!forceFollow) playerTransform.position += Time.deltaTime * virtualCamera.m_Lens.OrthographicSize * new Vector3(Input.GetAxis("Horizontal") * moveSpeed, Input.GetAxis("Vertical") * moveSpeed, 0);
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (buildMode) HandleBuildMode();
        else HandleMainMode();

        if (Input.GetKeyDown(KeyCode.Q)) {
            ChangeFollowingUnit(-1);
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            ChangeFollowingUnit(1);
        }
    }

    private void HandleBuildMode() {
        if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(0)) PlayerContentSelector.CreateSelectedContent(mousePos, 0);
        if (!EventSystem.current.IsPointerOverGameObject() && Input.GetMouseButtonDown(1)) PlayerContentSelector.CreateSelectedContent(mousePos, 1);

        PlayerContentSelector.ChangeSelectedContentOrientation(Input.mouseScrollDelta.y);
    }

    public static TrainTrack track;
    List<Vector2> trackPoints = new() { Vector2.zero };
    TrackPlacerTool tool = new();

    private void HandleMainMode() {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            if (Input.GetMouseButtonDown(0)) {
                selectedEntity = null;

                Collider2D[] allColliders = Physics2D.OverlapCircleAll(mousePos, 0.05f);

                foreach (Collider2D collider in allColliders) {
                    if (collider.transform.TryGetComponent(out Entity content)) {
                        selectedEntity = content;
                        OnEntitySelected?.Invoke(this, new Entity.EntityArg() { other = selectedEntity });
                        break;
                    }
                }

                InventoryViewer.Instance.SetInventory(selectedEntity ? selectedEntity.GetInventory() : null);
            }

            if (Input.GetMouseButtonDown(1)) {
                tool.Place();
            }

            if (Input.GetKeyDown(KeyCode.T)) {
                if (track != null) {
                    Client.CreateUnit(mousePos, 0f, Units.train, TeamUtilities.GetLocalTeam());
                }
            }

            if (Input.GetKeyDown(KeyCode.H)) {
                if (track != null) {
                    Client.CreateUnit(mousePos, 0f, Units.turretTrain, TeamUtilities.GetLocalTeam());
                }
            }

            if (Input.GetKeyDown(KeyCode.N)) {
                SquadronHandler.CreateSquadron("Squadron " + SquadronHandler.squadrons.Count);
            }


            tool.Update();

            float delta = Input.mouseScrollDelta.y;
            float change = delta * zoomSpeed * ( delta < 0f ? zoomOutMultiplier : zoomInMultiplier) * Time.deltaTime;
            if (!forceFollow) virtualCamera.m_Lens.OrthographicSize = Mathf.Clamp(virtualCamera.m_Lens.OrthographicSize - change, zoomClampMin, zoomClampMax);
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
        if (MapManager.Map.units.Count == 0) return;

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
        Follow(e.other != null ? e.other.transform : null);
    }

    public void FixFollow(Transform target, float fovSize) {
        virtualCamera.Follow = target == null ? playerTransform : target;
        virtualCamera.m_Lens.OrthographicSize = fovSize;
        forceFollow = true;
    }

    public void Follow(Transform target, bool forceFollow = false) {
        if (this.forceFollow && virtualCamera.Follow != null) return;

        virtualCamera.Follow = target == null ? playerTransform : target;
        this.forceFollow = forceFollow;
    }

    public void UnFollow(Vector2 position) {
        forceFollow = false;
        playerTransform.position = position;
        Follow(playerTransform);
    }

    public void CameraShake() {

    }
}
