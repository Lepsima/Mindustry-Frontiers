using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Teams;

public class Block : LoadableContent, IDamageable {

    private Vector2Int gridPosition;
    public BlockType Type { protected set; get; }

    public float health;

    public virtual void Set(Vector2Int gridPosition, BlockType blockType, float timeCode, byte teamCode) {
        base.Set(timeCode, teamCode, true);

        //Set block type
        Type = blockType;

        //Set position
        transform.position = gridPosition + (0.5f * blockType.size * Vector2.one);
        this.gridPosition = gridPosition;

        //Set collider size according to block size
        GetComponent<BoxCollider2D>().size = Vector2.one * Type.size;

        //Set current health
        health = blockType.health;

        //Set sprites
        SetSprites();
        MapManager.blocks.Add(this);
    }

    void SetSprites() {
        GetComponent<SpriteRenderer>().sprite = Type.sprite;

        SetOptionalSprite(transform.Find("Team"), Type.teamSprite, out SpriteRenderer teamSpriteRenderer);
        SetOptionalSprite(transform.Find("Top"), Type.topSprite);
        SetOptionalSprite(transform.Find("Bottom"), Type.bottomSprite);

        teamSpriteRenderer.color = teamCode == TeamUtilities.GetLocalPlayerTeam().Code ? TeamUtilities.LocalTeamColor : TeamUtilities.EnemyTeamColor;
    }

    public bool ExistsIn(Vector2Int position) {
        if (Type.size == 1 && gridPosition == position) return true;

        for (int x = 0; x < Type.size; x++) {
            for (int y = 0; y < Type.size; y++) {
                if (position == new Vector2Int(x, y) + gridPosition) return true;
            }
        }
        return false;
    }

    public Vector2Int GetGridPosition() => gridPosition;

    public override Vector2 GetPosition() => GetGridPosition() + (0.5f * Type.size * Vector2.one);

    public void Damage(float amount) {
        if (amount < health) health -= amount;
        else MapManager.Instance.DestroyBlock(this);
    }

    public override void Delete() {
        MapManager.blocks.Remove(this);
        base.Delete();
    }
}