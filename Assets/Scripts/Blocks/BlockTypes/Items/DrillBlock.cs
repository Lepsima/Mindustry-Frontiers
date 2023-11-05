using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using UnityEngine.Tilemaps;
using Frontiers.Assets;
using MapLayer = Frontiers.Content.Maps.Map.MapLayer;
using Frontiers.Content.Upgrades;
using Frontiers.Content.Maps;

public class DrillBlock : ItemBlock {
    public new DrillBlockType Type { get => (DrillBlockType)base.Type; protected set => base.Type = value; }

    public float drillTime = 5f;
    private float nextDrillTime = 0f;

    private float rotorVelocity = 0f;
    private float maxRotorVelocity = 0f;
    private readonly float rotorVelocityChange = 50f;

    public Item drillItem;
    public Transform rotorTransfrom;
    
    #region - Upgradable Stats - 

    protected float hardness, rate;

    #endregion

    protected override void ApplyUpgrageMultiplier(UpgradeType upgrade) {
        base.ApplyUpgrageMultiplier(upgrade);

        BlockUpgradeMultipliers mult = upgrade.properties as BlockUpgradeMultipliers;

        hardness += hardness * mult.drill_hardness;
        rate += rate * mult.drill_rate;

        UpdateDrillValues();
    }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);

        hardness = Type.drillHardness;
        rate = Type.drillRate;

        UpdateDrillValues();
    }

    protected virtual void UpdateDrillValues() {
        drillItem = GetItemFromTiles(out float yieldPercent);
        drillTime = GetDrillTime(yieldPercent);

        nextDrillTime = Time.time + drillTime;

        allowedOutputItems = new Item[1] { drillItem };
        inventory.SetAllowedItems(allowedOutputItems);

        maxRotorVelocity = yieldPercent * 150f;
    }

    protected override void SetSprites() {
        base.SetSprites();

        rotorTransfrom = transform.Find("Empty");
        rotorTransfrom.gameObject.AddComponent<SpriteRenderer>();

        SpriteRenderer rotorSpriteRenderer = SetOptionalSprite(rotorTransfrom, Type.rotorSprite);

        if (rotorSpriteRenderer) {
            rotorSpriteRenderer.sortingLayerName = "Blocks";
            rotorSpriteRenderer.sortingOrder = 4;
            rotorSpriteRenderer.material = new(AssetLoader.GetAsset<Material>("RotationShader"));
        }
    }

    public override bool CanReciveItem(Item item, int orientation = 0) {
        return false;
    }

    public bool CanDrill(Item item) => !inventory.Full(item);

    protected override void Update() {
        base.Update();

        if (drillTime == -1f || drillItem == null) return;

        OutputItems();
        bool canDrill = CanDrill(drillItem);

        rotorVelocity = Mathf.Clamp(rotorVelocity + (canDrill ? rotorVelocityChange : -rotorVelocityChange) * Time.deltaTime, 0, maxRotorVelocity);
        rotorTransfrom.eulerAngles += new Vector3(0, 0, rotorVelocity * Time.deltaTime);

        if (nextDrillTime <= Time.time && canDrill) {
            inventory.Add(drillItem, 1);
            nextDrillTime = Time.time + drillTime;
        }
    }

    public Item GetItemFromTiles(out float yieldPercent) {
        Item priorityItem = null;
        int itemCount = 0;
        int totalTiles = Type.size * Type.size;

        for (int x = 0; x < Type.size; x++) {
            for (int y = 0; y < Type.size; y++) {
                Vector2 position = GetGridPosition() + new Vector2(x, y);

                TileType tile = MapManager.Map.GetMapTileTypeAt(MapLayer.Ore, position);
                if (tile == null || tile.drop == null) tile = MapManager.Map.GetMapTileTypeAt(MapLayer.Ground, position);

                Item item;
                if (tile != null && tile.drop is Item drop) item = drop;
                else continue;

                if ((priorityItem == null || priorityItem.hardness < item.hardness) && item.hardness <= hardness) {
                    priorityItem = item;
                    itemCount = 1;
                } else if (priorityItem == item) {
                    itemCount++;
                }
            }
        }

        yieldPercent = (float)itemCount / totalTiles;
        return priorityItem;
    }

    public float GetDrillTime(float yieldPercent) {
        if (drillItem == null) return -1f;
        return (drillTime + 0.833f * drillItem.hardness) / yieldPercent / rate;
    }
}