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

public class Block : Entity {
    public new BlockType Type { protected set; get; }
    private Vector2Int gridPosition;
    private int orientation;

    private SpriteRenderer glowSpriteRenderer;

    public float blinkInterval, blinkLenght, blinkOffset;
    private bool glows = false;

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

        if (!MapManager.TypeEquals(Type.type, typeof(ConstructionBlock))) Effect.PlayEffect("BuildFX", GetPosition(), size);

        MapManager.Map.AddBlock(this);
        Client.syncObjects.Add(SyncID, this);
    }

    protected override void SetSprites() {
        GetComponent<SpriteRenderer>().sprite = Type.sprite;

        SpriteRenderer teamSpriteRenderer = SetOptionalSprite(transform.Find("Team"), Type.teamSprite);
        SpriteRenderer glowSpriteRenderer = SetOptionalSprite(transform.Find("Glow"), Type.glowSprite);

        SetOptionalSprite(transform.Find("Top"), Type.topSprite);
        SetOptionalSprite(transform.Find("Bottom"), Type.bottomSprite);

        transform.Find("Shadow").localScale = 1.1f * Type.size * Vector2.one;

        if (teamSpriteRenderer) {
            teamSpriteRenderer.color = teamColor;
        }

        if (glowSpriteRenderer) {
            glowSpriteRenderer.color = teamColor;
            this.glowSpriteRenderer = glowSpriteRenderer;
            glows = true;
        }
    }

    protected virtual void Update() {
        if (!Type.updates) return;

        if (glows) {
            Color glowColor = teamColor;
            glowColor.a = Mathf.Clamp01((Mathf.Sin(Time.time / Type.blinkInterval + Type.blinkOffset) + Type.blinkLength) * 0.5f);
            glowSpriteRenderer.color = glowColor;
        }
    }

    public override void OnInventoryValueChange(object sender, EventArgs e) {

    }

    public override EntityType GetEntityType() => Type;

    public bool ExistsIn(Vector2Int position) {
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
        for (int i = 0; i < Type.size; i++) if (other.ExistsIn(GetGridPosition() + new Vector2Int(Type.size, i))) return true;
        for (int i = 0; i < Type.size; i++) if (other.ExistsIn(GetGridPosition() + new Vector2Int(i, Type.size))) return true;  
        for (int i = 0; i < Type.size; i++) if (other.ExistsIn(GetGridPosition() + new Vector2Int(-1, i))) return true;
        for (int i = 0; i < Type.size; i++) if (other.ExistsIn(GetGridPosition() + new Vector2Int(i, -1))) return true;

        return false;
    }

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

    public Block GetFacingBlock(int offset = 0) {
        return MapManager.Map.GetBlockAt(GetGridPosition() + GetFacingPosition(offset));
    }

    public Vector2Int GetGridPosition() => gridPosition;

    public override Vector2 GetPosition() => GetGridPosition() + (0.5f * Type.size * Vector2.one);

    public Quaternion GetRotation() => Quaternion.Euler(0, 0, orientation * 90f);

    public int GetOrientation() => orientation;

    public static int ToOrientation(Quaternion rotation) => Mathf.FloorToInt(rotation.eulerAngles.z / 90f);

    public static Quaternion ToQuaternion(Quaternion rotation) => Quaternion.Euler(0, 0, Mathf.FloorToInt(rotation.eulerAngles.z / 90f) * 90f);

    public static Quaternion ToQuaternion(int orientation) => Quaternion.Euler(0, 0, orientation * 90f);

    public Vector2 GetFacingEdgePosition() {
        float halfSize = Type.size / 2f;

        if (orientation == 0) return new Vector2(halfSize, 0);
        if (orientation == 1) return new Vector2(0, halfSize);
        if (orientation == 2) return new Vector2(-halfSize, 0);
        if (orientation == 3) return new Vector2(0, -halfSize);

        return Vector2.zero;
    }

    public Vector2 GetSharedEdgePosition(Block other) {
        float halfSize = Type.size / 2f;

        if (other.ExistsIn(GetGridPosition() + new Vector2Int(1, 0))) return new Vector2(halfSize, 0);
        if (other.ExistsIn(GetGridPosition() + new Vector2Int(0, 1))) return new Vector2(0, halfSize);
        if (other.ExistsIn(GetGridPosition() + new Vector2Int(-1, 0))) return new Vector2(-halfSize, 0);
        if (other.ExistsIn(GetGridPosition() + new Vector2Int(0, -1))) return new Vector2(0, -halfSize);

        return Vector2.zero;
    }

    public Vector2Int GetFacingPosition(int offset = 0) {
        int distance = Type.size;
        int o = (orientation + offset) % 4;

        if (o == 0) return new Vector2Int(distance, 0);
        if (o == 1) return new Vector2Int(0, distance);
        if (o == 2) return new Vector2Int(-distance, 0);
        if (o == 3) return new Vector2Int(0, -distance);

        return Vector2Int.zero;
    }

    public override void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;

        if (wasDestroyed) {
            string explosionEffect = "SmallExplosionFX";
            ItemBlock itemBlock = this as ItemBlock;

            if (itemBlock != null) {
                if (itemBlock.IsFlammable()) {
                    FireController.Spread(GetPosition(), Type.size, 2, 10);
                }

                if (itemBlock.IsExplosive()) {
                    explosionEffect = Type.explosionFX;
                }
            }

            Effect.PlayEffect(explosionEffect, GetPosition(), 2 * Type.size);
            RubbleGenerator.CreateRubble(GetPosition(), Type.size);
        }

        MapManager.Map.RemoveBlock(this);
        Client.syncObjects.Remove(SyncID);

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