using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Frontiers.FluidSystem;
using Frontiers.Squadrons;
using Frontiers.Content;

public class PlayerManager : MonoBehaviour {
    public static PlayerManager Instance;
    public static Vector3 mousePos;

    public event EventHandler<Entity.EntityArg> OnEntitySelected;

    public Entity selectedEntity;
    public CameraController cameraController;
  
    public bool buildMode = false;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        PlayerContentSelector.OnSelectedContentChanged += Instance.OnSelectedContentChanged;
        cameraController = GetComponent<CameraController>();

        PlayerUI.Instance.EnableLoadingScreen(true);
        SquadronHandler.CreateSquadrons();
    }

    private void Update() {
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        if (buildMode) HandleBuildMode();
        else HandleMainMode();

        if (Input.GetKeyDown(KeyCode.Q)) {
            cameraController.ChangeFollowingUnit(-1);
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            cameraController.ChangeFollowingUnit(1);
        }

        if (Input.GetKeyDown(KeyCode.K)) {
            cameraController.CameraShake(2, 5, 2.5f, 0f);
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
                SelectEntity(GetEntityInPos(mousePos));
            }

            cameraController.ChangeCameraSize(Input.mouseScrollDelta.y);
        }
    }

    private void SelectEntity(Entity entity) {
        selectedEntity = entity;
        OnEntitySelected?.Invoke(this, new Entity.EntityArg() { other = selectedEntity });
        //InventoryViewer.Instance.SetInventory(selectedEntity ? selectedEntity.GetInventory() : null);
    }

    private Entity GetEntityInPos(Vector2 pos) {
        Collider2D[] allColliders = Physics2D.OverlapCircleAll(pos, 0.05f);
        Entity selected = null;

        foreach (Collider2D collider in allColliders) if (collider.transform.TryGetComponent(out Entity entity) && (selected == null || entity is Unit)) selected = entity;
        return selected;
    }

    public void AddItems(int amount) {
        Content selectedContent = PlayerContentSelector.SelectedContent;
        if (selectedContent == null || !selectedContent.GetType().EqualsOrInherits(typeof(Element)) || selectedEntity == null) return;

        if (selectedContent is Item item) {
            if (selectedEntity is SorterBlock sorterBlock) {
                sorterBlock.SetFilter(item);
            }

            if (selectedEntity is ItemBlock block && block.CanReciveItem(item)) {
                block.ReciveItems(item, amount);
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
            cameraController.Follow(null);
        }
    }
}