using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Pooling;
using System.Linq;
using Frontiers.Content.Upgrades;

public class ConveyorBlock : ItemBlock {
    public new ConveyorBlockType Type { get => (ConveyorBlockType)base.Type; protected set => base.Type = value; }

    public static GameObjectPool conveyorItemPool;

    public class ConveyorItem {
        public GameObject itemGameObject;
        public Item item;
        public Vector2 startPosition;
        public float time;

        public ConveyorItem(Item item, Vector2 startPosition) {
            this.item = item;

            itemGameObject = conveyorItemPool.Take();
            itemGameObject.GetComponent<SpriteRenderer>().sprite = this.item.sprite;

            this.startPosition = startPosition;
            time = 0;
            itemGameObject.transform.position = startPosition;
        }

        public void ChangeConveyor(Vector2 startPosition) {
            this.startPosition = startPosition;
            time = 0;
            itemGameObject.transform.position = startPosition;
        }

        public void Update(Vector2 endPosition) {
            itemGameObject.transform.position = Vector2.Lerp(startPosition, endPosition, time);
        }

        public void End() {
            conveyorItemPool.Return(itemGameObject);
        }
    }

    public List<ConveyorItem> items = new();
    public float backSpace;
    public bool aligned, isStuck;

    public ItemBlock next;
    public ConveyorBlock nextAsConveyor;

    protected SpriteRenderer conveyorRenderer;

    protected Vector2 endPosition;
    protected float itemSpace;

    protected int variant = 0;
    protected bool mirroredSprite = false;
    protected float frameTime = 0f;

    #region - Upgradable Stats -

    protected float itemSpeed;

    #endregion

    protected override void ApplyUpgrageMultiplier(UpgradeType upgrade) {
        base.ApplyUpgrageMultiplier(upgrade);

        BlockUpgradeMultipliers mult = upgrade.properties as BlockUpgradeMultipliers;
        itemSpeed += itemSpeed * mult.conveyor_itemSpeed;
    }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        conveyorRenderer = GetComponent<SpriteRenderer>();

        base.Set(position, rotation, type, id, teamCode);
        endPosition = GetFacingEdgePosition() + GetPosition();
        itemSpeed = Type.itemSpeed;

        UpdateVariant();
    }

    public override void SetInventory() {
        inventory = null;
        itemSpace = 1f / Type.itemCapacity;
        hasItemInventory = true;
    }

    protected override void Update() {
        base.Update();
        UpdateAnimation();
    }

    protected void FixedUpdate() {
        UpdateItems();
    }

    private void UpdateAnimation() {
        //if (isStuck || items.Count == 0) return;
        int frame = Mathf.FloorToInt((Time.time * ConveyorBlockType.frames * itemSpeed * 2) % ConveyorBlockType.frames);

        Sprite sprite = Type.allConveyorSprites[variant, frame];
        conveyorRenderer.sprite = sprite;
    }

    private void UpdateItems() {
        int len = items.Count;
        if (len == 0) return;

        backSpace = 1f;

        float nextMax = 1f;
        float moved = Time.fixedDeltaTime * itemSpeed;

        for (int i = 0; i < len; i++) {
            float nextPos = (i == 0 ? 100f : items[i - 1].time) - itemSpace;
            float maxMove = Mathf.Clamp(nextPos - items[i].time, 0, moved);

            if (i == 0) isStuck = items[i].time >= 1f;

            items[i].time += maxMove;
            items[i].Update(endPosition);

            if (items[i].time > nextMax) items[i].time = nextMax;

            if (items[i].time >= 1f && Pass(items[i])) {
                items.RemoveAt(i);
                len = Mathf.Min(i, len);
            } else if (items[i].time < backSpace) {
                backSpace = items[i].time; 
            }
        }
    }

    public override void GetAdjacentBlocks() {
        base.GetAdjacentBlocks();

        next = GetFacingBlock() as ItemBlock;
        nextAsConveyor = next as ConveyorBlock;
        aligned = nextAsConveyor != null && nextAsConveyor.GetOrientation() == GetOrientation();

        UpdateVariant();
    }

    private void UpdateVariant() {
        bool left = HasSenderBlockAt(1);
        bool back = HasSenderBlockAt(2);
        bool right = HasSenderBlockAt(3);

        variant = ConveyorBlockType.GetVariant(right, left, back, out mirroredSprite);
        conveyorRenderer.flipY = mirroredSprite;
    }

    private bool HasSenderBlockAt(int orientation) {
        ItemBlock itemBlock = GetFacingBlock(orientation) as ItemBlock;
        if (!itemBlock) return false;

        if (itemBlock is ConveyorBlock conveyor) return conveyor.nextAsConveyor == this;
        else return true;   
    }

    public override bool CanReciveItem(Item item, int orientation = 0) {
        bool timeSpacing = items.Count == 0 || items.Last().time >= itemSpace;
        return timeSpacing && items.Count < Type.itemCapacity;
    }

    public override void ReciveItems(Item item, int amount = 1, int orientation = 0) {
        if (items.Count >= Type.itemCapacity) return;

        items.Add(new ConveyorItem(item, GetSharedEdgePosition(orientation + 2) + GetPosition()));
    }

    public void ReciveItem(ConveyorItem conveyorItem, int orientation) {
        if (items.Count >= Type.itemCapacity) return;

        items.Add(conveyorItem);
        conveyorItem.ChangeConveyor(GetSharedEdgePosition(orientation + 2) + GetPosition());
    }

    private bool Pass(ConveyorItem convItem) {
        Item item = convItem.item;
        if (item == null || next == null || next.GetTeam() != GetTeam() || !next.CanReciveItem(item, GetOrientation())) return false;

        if (nextAsConveyor != null) {
            nextAsConveyor.ReciveItem(convItem, GetOrientation());
        } else {
            convItem.End();
            next.ReciveItems(item, 0, GetOrientation());
        }

        return true;
    }

    public override bool IsFlammable() {
        foreach (ConveyorItem convItem in items) if (convItem.item.flammability > 0) return true;
        return false;
    }

    public override bool IsExplosive() {
        foreach (ConveyorItem convItem in items) if (convItem.item.explosiveness > 0) return true;
        return false;
    }

    public override void OnDestroy() {
        base.OnDestroy();

        if (!gameObject.scene.isLoaded) return;
        foreach (ConveyorItem convItem in items) convItem.End();
    }
}