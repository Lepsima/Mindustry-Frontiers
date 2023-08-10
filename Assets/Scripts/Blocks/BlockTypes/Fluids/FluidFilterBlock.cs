using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.FluidSystem;

public class FluidFilterBlock : ItemBlock {
    public Fluid filterFluid;
    SpriteRenderer filterFluidSpriteRenderer;

    public void SetFilter(Fluid filterFluid) {
        this.filterFluid = filterFluid;
        filterFluidSpriteRenderer.sprite = filterFluid?.sprite;
        allowedInputFluids = new Fluid[1] { filterFluid };
    }

    protected override void SetSprites() {
        base.SetSprites();
        Transform transform = new GameObject("filterFluid", typeof(SpriteRenderer)).transform;
        filterFluidSpriteRenderer = transform.GetComponent<SpriteRenderer>();
        filterFluidSpriteRenderer.sortingLayerName = "Blocks";
        filterFluidSpriteRenderer.sortingOrder = 5;

        transform.parent = this.transform;
        transform.localPosition = Vector3.zero;
        transform.rotation = Quaternion.identity; // Rotation isn't local to keep the item aligned to the player screen instead of the block
        transform.localScale = Vector3.one * 0.5f;
    }
}