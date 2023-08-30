using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Frontiers.Squadrons;

public class SquadronUIItem : MonoBehaviour {
    public List<SquadronUnitUIItem> unitUIItems = new();
    public TMP_Text unitCountText;
    public TMP_Text squadronNameText;

    public Image leaderIcon;
    public GameObject selectedIndicator;

    public Transform unitUIItemParent;
    public GameObject unitUIItemPrefab;

    public Squadron squadron;

    public int maxUnitCount;

    public void Set(Squadron squadron) {
        this.squadron = squadron;
        squadronNameText.text = squadron.name;
        if (unitUIItems.Count == 0) leaderIcon.enabled = false;
    }

    public void OnClick() {
        SquadronUI.Instance.SelectSquadron(squadron);
    }

    public void OnSelect() {
        selectedIndicator.SetActive(true);
    }

    public void OnDeselect() {
        selectedIndicator.SetActive(false);
    }

    public void Add(Unit unit) {
        GameObject instance = Instantiate(unitUIItemPrefab, unitUIItemParent);
        SquadronUnitUIItem newItem = instance.GetComponent<SquadronUnitUIItem>();

        newItem.Set(this, unit);
        unitUIItems.Add(newItem);

        maxUnitCount = unitUIItems.Count;
        unitCountText.text = unitUIItems.Count + "/" + maxUnitCount;

        leaderIcon.sprite = unitUIItems[0].unit.Type.spriteFull;
        leaderIcon.enabled = true;
    }

    public void Remove(Unit unit) {
        SquadronUnitUIItem selectedItem = null;

        foreach(SquadronUnitUIItem item in unitUIItems) {
            if (item.unit == unit) selectedItem = item;
        }

        if (selectedItem != null) {
            unitUIItems.Remove(selectedItem);
            Destroy(selectedItem.gameObject);
        }

        unitCountText.text = unitUIItems.Count + "/" + maxUnitCount;

        if (unitUIItems.Count == 0) leaderIcon.enabled = false;
    }
}