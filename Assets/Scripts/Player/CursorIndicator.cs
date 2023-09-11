using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public class CursorIndicator : MonoBehaviour {
    public static CursorIndicator Instance;

    public SpriteRenderer contentRenderer;
    public GameObject unitArrowGameObject;
    public GameObject blockArrowGameObject;

    float rotation, size;
    bool rotates, clampToGrid;

    private void Awake() {
        Instance = this;
    }

    private void Update() {
        Vector2 halfSize = 0.5f * size * Vector2.one;
        Vector2 mousePos = clampToGrid ? MapManager.mouseGridPos : PlayerManager.mousePos;

        transform.position = mousePos + (clampToGrid ? halfSize : Vector2.zero);
        transform.localScale = Vector2.one;
    }

    public void ChangeRotation(float newRotation) {
        rotation = newRotation;
        transform.eulerAngles = rotates ? new Vector3(0, 0, rotation) : Vector3.zero;
    }

    public void SetContent(Content content) {
        EntityType entityType = content as EntityType;
        rotates = entityType != null && entityType.hasOrientation;

        transform.eulerAngles = rotates ? new Vector3(0, 0, rotation) : Vector3.zero;

        unitArrowGameObject.SetActive(rotates && content is UnitType);
        blockArrowGameObject.SetActive(rotates && content is BlockType);

        gameObject.SetActive(entityType != null);
        contentRenderer.sprite = content?.spriteFull;

        clampToGrid = entityType is BlockType;
        if (clampToGrid) size = (entityType as BlockType).size;
    }
}