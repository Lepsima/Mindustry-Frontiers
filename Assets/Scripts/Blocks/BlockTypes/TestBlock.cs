using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using Frontiers.Content;

public class TestBlock : Block {
    TextMeshProUGUI damageDisplayText;
    float startTime = -1f;

    private void OnEnable() {
        //OnHealthChange += UpdateText;
        damageDisplayText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void OnDisable() {
        //OnHealthChange -= UpdateText;
    }

    public void UpdateText(object sender, EventArgs e) {
        if (startTime == -1f) startTime = Time.time;
        float damage = Type.health - health;
        float timePassed = Time.time - startTime;
        damageDisplayText.text = damage / timePassed + " damage/sec";
    }
}
