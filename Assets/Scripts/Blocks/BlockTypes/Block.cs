using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Frontiers.Content;
using Frontiers.Content.Maps;
using Frontiers.Assets;
using Frontiers.Teams;
using MapLayer = Frontiers.Content.Maps.Map.MapLayer;
using Frontiers.Content.VisualEffects;

public class Block : Entity {
    public new BlockType Type { protected set; get; }
    private Vector2Int gridPosition;
    private int orientation;

    private SpriteRenderer[] glowSpriteRenderers;

    public float blinkInterval, blinkLenght, blinkOffset;
    private bool glows = false;
    private float glowSpriteOffset;

    static readonly Vector2Int[] adjacentPositions = new Vector2Int[4] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        Type = type as BlockType;

        if (Type == null) {
            Debug.LogError("Specified type: " + type + ", is not valid for a block");
            return;
        }

        base.Set(position, rotation, type, id, teamCode);

        health = Type.health;

        //Set position
        transform.SetPositionAndRotation(Vector2Int.CeilToInt(position) + (0.5f * Type.size * Vector2.one), ToQuaternion(rotation));
        orientation = ToOrientation(rotation);
        gridPosition = Vector2Int.CeilToInt(position);

        //Set collider size according to block size
        GetComponent<BoxCollider2D>().size = Vector2.one * Type.size;
        size = Type.size;
        syncTime = 10f;

        if (!MapManager.TypeEquals(Type.type, typeof(ConstructionBlock))) EffectPlayer.PlayEffect(Effects.build, GetPosition(), size);

        // Add this block to the map lists
        MapManager.Map.AddBlock(this);
    }

    protected override void SetSprites() {
        // Main sprite
        GetComponent<SpriteRenderer>().sprite = Type.sprite;

        // Team sprite
        SpriteRenderer teamSpriteRenderer = SetOptionalSprite(transform.Find("Team"), Type.teamSprite);

        // Top and bottom sprites
        SetOptionalSprite(transform.Find("Top"), Type.topSprite);
        SetOptionalSprite(transform.Find("Bottom"), Type.bottomSprite);

        // Shadow sprite
        transform.Find("Shadow").localScale = 1.1f * Type.size * Vector2.one;

        // If has team sprite, set it's color to the team color
        if (teamSpriteRenderer) {
            teamSpriteRenderer.color = teamColor;
        }

        // Glow sprites
        if (Type.glowSprites == null) return;

        glowSpriteRenderers = new SpriteRenderer[Type.glowSprites.Length];
        glowSpriteOffset = 1.5f / Type.glowSprites.Length;

        Material glowMaterial = AssetLoader.GetAsset<Material>("SpriteGlow");

        for (int i = 0; i < glowSpriteRenderers.Length; i++) {
            Transform glowTransform = new GameObject("Glow" + i, typeof(SpriteRenderer)).transform;
            glowTransform.parent = transform;
            glowTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            SpriteRenderer spriteRenderer = glowTransform.GetComponent<SpriteRenderer>();
            glowSpriteRenderers[i] = spriteRenderer;

            spriteRenderer.sprite = Type.glowSprites[i];
            spriteRenderer.color = teamColor;
            spriteRenderer.sortingLayerName = "Blocks";
            spriteRenderer.sortingOrder = 5;
            spriteRenderer.material = glowMaterial;

            glows = true;
        }

    }

    protected virtual void Update() {
        if (!Type.updates) return;

        // Update the glow sprite
        if (glows) {
            for (int i = 0; i < glowSpriteRenderers.Length; i++) {
                Color glowColor = teamColor;
                float offset = Type.blinkOffset + (i * glowSpriteOffset);
                glowColor.a = Mathf.Clamp01((Mathf.Sin(Time.time / Type.blinkInterval + offset) + Type.blinkLength) * 0.5f);
                glowSpriteRenderers[i].color = glowColor;
            }
        }
    }

    // This needs to be here because the base is abstract
    public override void OnInventoryValueChange(object sender, EventArgs e) {

    }

    public override EntityType GetEntityType() => Type;

    // Unused
    /*
    public bool ExistsIn(Vector2Int position) {
        return MapManager.Map.blockPositions[position] == this;
        
        if (Type.size == 1 && GetGridPosition() == position) return true;

        for (int x = 0; x < Type.size; x++) {
            for (int y = 0; y < Type.size; y++) {
                if (position == new Vector2Int(x, y) + GetGridPosition()) return true;
            }
        }
        return false;
    }

    public bool IsNear(Block other) {
        // Try to discard by distance
        float distance = Vector2.Distance(GetPosition(), other.GetPosition());
        if (distance > (Type.size + other.Type.size)) return false;

        // Check if the other block exists in any of the border tiles
        for (int i = 0; i < Type.size; i++) if (MapManager.Map.blockPositions[GetGridPosition() + new Vector2Int(Type.size, i)] == other) return true;
        for (int i = 0; i < Type.size; i++) if (MapManager.Map.blockPositions[GetGridPosition() + new Vector2Int(i, Type.size)] == other) return true;  
        for (int i = 0; i < Type.size; i++) if (MapManager.Map.blockPositions[GetGridPosition() + new Vector2Int(-1, i)] == other) return true;
        for (int i = 0; i < Type.size; i++) if (MapManager.Map.blockPositions[GetGridPosition() + new Vector2Int(i, -1)] == other) return true;

        return false;
    }

    // Gets the tiles that this block sits in
    public TileType[] GetTiles() {
        TileType[] allTiles = new TileType[Type.size * Type.size];

        for(int x = 0; x < Type.size; x++) {
            for (int y = 0; y < Type.size; y++) {
                Vector2Int tilePosition = new Vector2Int(x, y) + GetGridPosition();
                TileType tile = MapManager.Map.GetMapTileTypeAt(MapLayer.Ground, tilePosition);

                int i = x + Type.size * y;
                allTiles[i] = tile;
            }
        }

        return allTiles;
    }
    */

    // Get the block in front of this using the orientation
    // Only works properly with 1x1 blocks, bigger blocks shouldn't have orientation
    public Block GetFacingBlock(int offset = 0) {
        return MapManager.Map.GetBlockAt(GetGridPosition() + GetFacingPosition(offset));
    }

    // Get the position of this block on the grid
    public Vector2Int GetGridPosition() => gridPosition;

    // Get the position of the center of this block 
    public override Vector2 GetPosition() => GetGridPosition() + (0.5f * Type.size * Vector2.one);

    // Get the orientation as a quaternion
    public Quaternion GetRotation() => Quaternion.Euler(0, 0, orientation * 90f);

    // Get the orientation
    public int GetOrientation() => orientation;

    // Transform a compatible quaternion to a orientation
    public static int ToOrientation(Quaternion rotation) => Mathf.FloorToInt(rotation.eulerAngles.z / 90f);

    // Transform a quaternion to a compatible quaternion (set unused axis to 0) 
    public static Quaternion ToQuaternion(Quaternion rotation) => Quaternion.Euler(0, 0, Mathf.FloorToInt(rotation.eulerAngles.z / 90f) * 90f);

    // Transform a orientation to a compatible quaternion
    public static Quaternion ToQuaternion(int orientation) => Quaternion.Euler(0, 0, orientation * 90f);

    // Conveyor things, name describes pretty well
    public Vector2 GetFacingEdgePosition() => 0.5f * Type.size * (Vector2)adjacentPositions[orientation % 4];

    // More magic shit for conveyors, name describes this better than i could
    public Vector2 GetSharedEdgePosition(int orientation) => 0.5f * Type.size * (Vector2)adjacentPositions[orientation % 4];

    // Gets the position of the block that should be in front
    // Only works properly with 1x1 blocks, bigger blocks shouldn't have orientation
    public Vector2Int GetFacingPosition(int offset = 0) {
        Vector2Int pos = adjacentPositions[(orientation + offset) % 4] * Type.size;
        return new Vector2Int(Mathf.Max(pos.x, -1), Mathf.Max(pos.y, -1));
    }

    public override void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;

        // If this block was destroyed by damage, create fx
        if (wasDestroyed) {
            // Default fx
            Effect explosionEffect = Effects.explosion;
            ItemBlock itemBlock = this as ItemBlock;

            // If can contain items check for extra properties
            if (itemBlock != null) {

                // Check if contains flammable items
                if (itemBlock.IsFlammable()) {
                    FireController.Spread(GetPosition(), Type.size, 2, 10);
                }

                // Check if contains explosive items
                if (itemBlock.IsExplosive()) {
                    explosionEffect = Type.deathFX;
                }
            }

            // Play the effects and generate rubble
            EffectPlayer.PlayEffect(explosionEffect, GetPosition(), 2 * Type.size);
            RubbleGenerator.CreateRubble(GetPosition(), Type.size);
        }

        MapManager.Map.RemoveBlock(this);
        base.OnDestroy();
    }

    public override string ToString() {
        string data = base.ToString();
        data += GetGridPosition().x + ":";
        data += GetGridPosition().y + ":";
        data += orientation + ":";
        return data;
    }
}