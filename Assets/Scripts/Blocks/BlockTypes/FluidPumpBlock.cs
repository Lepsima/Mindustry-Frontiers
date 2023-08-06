using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using System;
using Frontiers.Assets;
using static Frontiers.Content.Maps.Map;
using Frontiers.Content.Upgrades;
using Frontiers.FluidSystem;
using Frontiers.Content.Maps;

public class FluidPumpBlock : ItemBlock {
    public new FluidPumpBlockType Type { get => (FluidPumpBlockType)base.Type; protected set => base.Type = value; }

    public float pumpRate = 5f;

    private float rotorVelocity = 0f;
    private float maxRotorVelocity = 0f;
    private readonly float rotorVelocityChange = 0.02f;

    public Fluid extractFluid;
    public Transform rotorTransfrom;

    #region - Upgradable Stats - 

    protected float rate;

    #endregion

    protected override void ApplyUpgrageMultiplier(UpgradeType upgrade) {
        base.ApplyUpgrageMultiplier(upgrade);

        BlockUpgradeMultipliers mult = upgrade.properties as BlockUpgradeMultipliers;

        rate += rate * mult.drill_rate;

        UpdatePumpValues();
    }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);

        rate = Type.extractRate;

        UpdatePumpValues();
    }

    protected virtual void UpdatePumpValues() {
        extractFluid = GetFluidFromTiles(out float yieldPercent);
        pumpRate = GetPumpRate(yieldPercent);

        maxRotorVelocity = yieldPercent;
    }

    protected override void SetSprites() {
        base.SetSprites();

        rotorTransfrom = transform.Find("Empty");
        rotorTransfrom.gameObject.AddComponent<SpriteRenderer>();

        SpriteRenderer rotorSpriteRenderer = SetOptionalSprite(rotorTransfrom, AssetLoader.GetSprite(Type.name + "-rotator"));

        if (rotorSpriteRenderer) {
            rotorSpriteRenderer.sortingLayerName = "Blocks";
            rotorSpriteRenderer.sortingOrder = 4;
        }
    }

    public override bool CanReciveItem(Item item, int orientation = 0) {
        return false;
    }

    protected override void Update() {
        base.Update();
        if (pumpRate == -1f || extractFluid == null) return;

        bool canExtract = !fluidInventory.Full();

        rotorVelocity = Mathf.Clamp(rotorVelocity + (canExtract ? rotorVelocityChange : -rotorVelocityChange), 0, maxRotorVelocity);
        rotorTransfrom.eulerAngles += new Vector3(0, 0, rotorVelocity);

        if (canExtract) fluidInventory.AddLiters(extractFluid, rate * Time.deltaTime);    
    }

    public Fluid GetFluidFromTiles(out float yieldPercent) {
        Fluid priorityFluid = null;
        int fluidTileCount = 0;
        int totalTiles = Type.size * Type.size;

        for (int x = 0; x < Type.size; x++) {
            for (int y = 0; y < Type.size; y++) {
                Vector2 position = GetGridPosition() + new Vector2(x, y);

                TileType tile = MapManager.Map.GetMapTileTypeAt(MapLayer.Ore, position);
                if (tile == null || tile.drop == null) tile = MapManager.Map.GetMapTileTypeAt(MapLayer.Ground, position);

                Fluid fluid;
                if (tile != null && tile.drop is Fluid drop) fluid = drop;
                else continue;

                if (priorityFluid == null) {
                    priorityFluid = fluid;
                    fluidTileCount = 1;
                } else if (priorityFluid == fluid) {
                    fluidTileCount++;
                }
            }
        }

        yieldPercent = (float)fluidTileCount / totalTiles;
        return priorityFluid;
    }

    public float GetPumpRate(float yieldPercent) {
        if (extractFluid == null) return -1f;
        return yieldPercent / (rate * pumpRate);
    }
}

public class FluidPumpBlockType : ItemBlockType {
    public Sprite rotorSprite;
    public float extractRate;

    public FluidPumpBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
        rotorSprite = AssetLoader.GetSprite(name + "-rotator");
        updates = true;
    }
}