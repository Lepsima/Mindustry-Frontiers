using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content.Upgrades;
using TMPro;

public class UIUpgradeTreeNode : MonoBehaviour { 
    [SerializeField] Gradient unlockedGradient;
    [SerializeField] Gradient lockedGradient;

    [SerializeField] TMP_Text helpText;
    SpriteRenderer spriteRenderer;
    LineRenderer lineRenderer;
    UpgradeTree.Node node;

    float helpShowTime = 1.5f;
    float mouseOverTimer;

    public void Set(string name, Transform parent, Vector2 position, UpgradeTree.Node node) {
        this.name = name;
        transform.parent = parent;
        transform.localPosition = position;
        this.node = node;

        spriteRenderer = GetComponent<SpriteRenderer>();
        lineRenderer = GetComponent<LineRenderer>();

        if (!node.isMaster) {
            spriteRenderer.color = IsResearched() ? Color.green : Color.red;
            lineRenderer.colorGradient = IsResearched() ? unlockedGradient : lockedGradient;
            lineRenderer.SetPositions(new Vector3[] { transform.position, node.parentNode.transform.position });
        }
    }

    public void OnClick() {
        if (node.isMaster) return;

        // Try to revert research state
        if (IsResearched()) {
            if (CanBeReversed()) {
                UpgradeResearcher.Revert(node.upgrade);
                spriteRenderer.color = Color.red;
                lineRenderer.colorGradient = lockedGradient;
            }
        } else {
            if (CanBeResearched()) {
                UpgradeResearcher.Research(node.upgrade);
                spriteRenderer.color = Color.green;
                lineRenderer.colorGradient = unlockedGradient;
            }
        }
    }

    private void OnMouseOver() {
        mouseOverTimer += Time.deltaTime;
        if (mouseOverTimer >= helpShowTime) ShowNodeHelp(true); 
    }

    private void OnMouseExit() {
        mouseOverTimer = 0f;
        ShowNodeHelp(false);
    }

    private void ShowNodeHelp(bool value) {
        if (node.isMaster) return;

        helpText.gameObject.SetActive(value);
        helpText.text = node.upgrade.displayName;
    }

    public bool IsResearched() {
        return UpgradeResearcher.IsResearched(node.upgrade);
    }

    public bool CanBeResearched() {
        return node.upgrade.CanBeResearched();
    }

    public bool CanBeReversed() {
        // If any upgrade that depends on this one is unlocked it can't be reversed
        for (int i = 0; i < node.nextNodes.Count; i++) {
            UpgradeType upgrade = node.nextNodes[i].upgrade;
            if (UpgradeResearcher.IsResearched(upgrade)) return false;
        }

        // If none of them are unlocked, then it can be reversed
        return true;
    }
}
