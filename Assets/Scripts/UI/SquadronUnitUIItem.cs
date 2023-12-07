using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class SquadronUnitUIItem : MonoBehaviour {
    public Image healthBarPreview;
    public Image unitIcon;
    public TMP_Text unitName;
    public Unit unit;

    public SquadronUIItem squadronItem;

    public void Set(SquadronUIItem squadronItem, Unit unit) {
        this.squadronItem = squadronItem;
        this.unit = unit;

        unitIcon.sprite = unit.Type.spriteFull;
        unitName.text = unit.squadronName;

        unit.OnDamaged += OnUnitHealthChange;
        healthBarPreview.fillAmount = unit.GetHealthPercent();
    }

    public void OnClick() {
        CameraController.Instance.Follow(unit.transform);
    }

    public void OnQuitClick() {
        squadronItem.Remove(unit);
    }

    public void OnUnitHealthChange(object sender, System.EventArgs e) {
        healthBarPreview.fillAmount = unit.GetHealthPercent();
    }


    private void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;
        unit.OnDamaged -= OnUnitHealthChange;
    }
}