using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SorterBlock : DistributionBlock {
    public new SorterBlockType Type { get => (SorterBlockType)base.Type; protected set => base.Type = value; }

    SpriteRenderer filterItemSpriteRenderer;

    public Item filterItem;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        filterItem = Items.sand;
    }

    protected override bool ForwardCondition() {
        return (filterItem.id == waitingItem.item.id) != Type.inverted;
    }

    protected override void SetSprites() {
        base.SetSprites();
        Transform transform = new GameObject("filterItem", typeof(SpriteRenderer)).transform;
        filterItemSpriteRenderer = transform.GetComponent<SpriteRenderer>();
        filterItemSpriteRenderer.sortingLayerName = "Blocks";
        filterItemSpriteRenderer.sortingOrder = 5;

        transform.parent = this.transform;
        transform.localPosition = Vector3.zero;
        transform.rotation = Quaternion.identity; // Rotation isn't local to keep the item aligned to the player screen instead of the block
        transform.localScale = Vector3.one * 0.5f;
    }

    public void ChangeFilterItem(Item item) {
        filterItem = item;
        filterItemSpriteRenderer.sprite = filterItem?.sprite;
    }
}