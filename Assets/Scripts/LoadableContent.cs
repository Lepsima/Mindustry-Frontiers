using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Photon.Pun;
using Photon.Realtime;
using Photon.Pun.UtilityScripts;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Teams;

public abstract class LoadableContent : MonoBehaviour {
    protected float timeCode, timeDiff;
    protected byte teamCode;
    public bool hasInventory = false;

    public void Set(float timeCode, byte teamCode) {
        this.timeCode = timeCode;
        timeDiff = Time.time - timeCode;

        this.teamCode = teamCode;
        SetLayerAllChildren(transform, GetTeamLayer());
    }

    public virtual void Set(float timeCode, byte teamCode, bool addToList) {
        if (addToList) MapManager.loadableContentDictionary.Add(timeCode, this); 
        Set(timeCode, teamCode);
    }

    protected virtual int GetTeamLayer(bool ignore = false) => TeamUtilities.GetTeamLayer(teamCode, ignore);

    protected virtual int GetTeamMask(bool ignore = false) => TeamUtilities.GetTeamMask(teamCode, ignore);

    public static void SetLayerAllChildren(Transform root, int layer) {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children) child.gameObject.layer = layer;
    }

    public static void SetOptionalSprite(Transform transform, Sprite sprite) {
        SpriteRenderer spriteRenderer = transform.GetComponent<SpriteRenderer>();

        if (!sprite) Destroy(transform.gameObject);
        if (!sprite || !spriteRenderer) return;

        spriteRenderer.sprite = sprite;
    }

    public static void SetOptionalSprite(Transform transform, Sprite sprite, out SpriteRenderer finalSpriteRenderer) {
        SpriteRenderer spriteRenderer = finalSpriteRenderer = transform.GetComponent<SpriteRenderer>();

        if (!sprite) Destroy(transform.gameObject);
        if (!sprite || !spriteRenderer) return;

        spriteRenderer.sprite = sprite;
    }

    private void OnDestroy() {
        Delete();
    }

    public virtual void Delete() {
        if (MapManager.loadableContentDictionary.ContainsKey(timeCode)) MapManager.loadableContentDictionary.Remove(timeCode);
        Destroy(gameObject);
    }

    public virtual Vector2 GetPosition() => transform.position;

    public float GetTimeCode() => timeCode;

    public PhotonTeam GetTeam() => TeamUtilities.GetTeamByCode(teamCode);

    public bool IsLocalTeam() => TeamUtilities.GetLocalPlayerTeam() == GetTeam();
}
