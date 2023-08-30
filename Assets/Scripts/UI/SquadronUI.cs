using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Squadrons;

public class SquadronUI : MonoBehaviour {
    public List<SquadronUIItem> squadronItems = new();
    public Transform squadronItemParent;
    public GameObject squadronItemPrefab;

    public static SquadronUI Instance;
    public Squadron selected;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        PlayerManager.Instance.OnEntitySelected += OnEntitySelected;
    }

    public void SelectSquadron(Squadron squadron) {
        selected.uiItem.OnDeselect();
        selected = squadron;
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