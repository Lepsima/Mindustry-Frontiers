using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using Frontiers.Squadrons;

public class SquadronUI : MonoBehaviour {
    public List<SquadronUIItem> squadronItems = new();
    public Transform squadronItemParent;
    public GameObject squadronItemPrefab;

    public GameObject squadronActionPanel;
    public GameObject inventoryViewerPanel;
    public Slider radiusSlider;
    public float maxRadius, minRadius;

    public static SquadronUI Instance;
    public Squadron selected;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        PlayerManager.Instance.OnEntitySelected += OnEntitySelected;
    }

    private void Update() {
        if (selected == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) {
            SelectAction(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2)) {
            SelectAction(2);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3)) {
            SelectAction(3);
        }
    }

    public void SelectSquadron(Squadron squadron) {
        if (selected != null) selected.uiItem.OnDeselect();
        if (squadron != null) squadron.uiItem.OnSelect();

        selected = squadron;
        squadronActionPanel.SetActive(selected != null);
        inventoryViewerPanel.SetActive(selected == null);
    }

    public void SelectAction(int actionIndex) {
        Vector2 position = PlayerManager.mousePos;
        float radius = radiusSlider.value * (maxRadius - minRadius) + minRadius;

        Action action = new(actionIndex, radius, position);
        selected.SetAction(action);

        selected.uiItem.OnDeselect();
        selected = null;
    }

    public void OnEntitySelected(object sender, Entity.EntityArg e) {
        Entity entity = e.other;

        if (entity == null) {
            SelectSquadron(null);
            return;
        }

        if (entity is Unit unit && selected != null) {
            if (selected.members.Contains(unit)) selected.Remove(unit);
            else selected.Add(unit);
        }
    }

    public SquadronUIItem Create(Squadron squadron) {
        GameObject instance = Instantiate(squadronItemPrefab, squadronItemParent);
        SquadronUIItem newItem = instance.GetComponent<SquadronUIItem>();

        squadronItems.Add(newItem);
        newItem.Set(squadron);

        return newItem;
    }

    public void Remove(Squadron squadron) {
        SquadronUIItem selectedItem = null;

        foreach (SquadronUIItem item in squadronItems) {
            if (item.squadron == squadron) selectedItem = item;
        }

        if (selectedItem != null) {
            squadronItems.Remove(selectedItem);
            Destroy(selectedItem.gameObject);
        }
    }
}