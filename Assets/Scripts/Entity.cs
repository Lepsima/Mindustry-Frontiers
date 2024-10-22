using System.Collections.Generic;
using UnityEngine;
using System;
using Frontiers.Content.Upgrades;
using Frontiers.Content.SoundEffects;
using Frontiers.Content;
using Frontiers.Teams;

public abstract class Entity : SyncronizableObject, IDamageable, IMessager {
    public event EventHandler<EntityArg> OnDestroyed;
    public event EventHandler<EventArgs> OnDamaged;

    public class EntityArg { public Entity other; }

    private GameObject[] fires;

    protected Color teamColor;
    protected EntityType Type;
    protected AudioSource audioSource;

    protected int id;
    protected byte teamCode;
    protected float health;

    #region - Upgradable Stats -
    public List<short> appliedUpgrades = new();

    protected float maxHealth;
    protected int itemCapacity;

    #endregion

    public float size;

    public bool wasDestroyed = false;
    int fireCount;

    public virtual string GetName() {
        return Type.name;
    }

    public int GetID() {
        return id;
    }

    public void ApplyUpgrade(UpgradeType upgrade) {
        if (appliedUpgrades.Contains(upgrade.id)) return;
        ApplyUpgrageMultiplier(upgrade);
    }

    protected virtual void ApplyUpgrageMultiplier(UpgradeType upgrade) {
        EntityUpgradeMultipliers mult = upgrade.properties as EntityUpgradeMultipliers;

        maxHealth += maxHealth * mult.entity_health;
        itemCapacity += Mathf.RoundToInt(itemCapacity * mult.entity_itemCapacity);
    }

    public virtual void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) where T : EntityType {
        this.id = id;
        this.teamCode = teamCode;
        this.Type = type;

        fires = new GameObject[Type.maximumFires];
        teamColor = TeamUtilities.GetTeamColor(teamCode);
        audioSource = GetComponent<AudioSource>();

        health = maxHealth = Type.health;
        itemCapacity = Type.itemCapacity;

        SetLayerAllChildren(transform, GetTeamLayer());
        SetSprites();

        // Do not sync by default
        syncs = false;
        syncValues = 0;
        syncTime = 999999f;
    }

    protected void ShowSprites(bool state) {
        SpriteRenderer[] allRenderers = transform.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer spriteRenderer in allRenderers) spriteRenderer.enabled = state;
    }

    protected Color CellColor() {
        float hp = GetHealthPercent();
        float sin = Mathf.Sin(5f * Time.time * (2f - hp));
        return Color.Lerp(Color.black, teamColor, 1 - Mathf.Max(sin - hp * sin, 0));
    }

    protected virtual int GetTeamLayer(bool ignore = false) => TeamUtilities.GetTeamLayer(teamCode, ignore);

    protected virtual int GetTeamMask(bool ignore = false) => TeamUtilities.GetTeamMask(teamCode, ignore);

    public virtual Vector2 GetPosition() => transform.position;

    public virtual Vector2 GetPredictedPosition(Vector2 origin, Vector2 velocity) => transform.position;

    public byte GetTeam() => TeamUtilities.GetTeamByCode(teamCode);

    public bool IsLocalTeam() => TeamUtilities.GetLocalTeam() == GetTeam();

    public abstract EntityType GetEntityType();

    public static void SetLayerAllChildren(Transform root, int layer) {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children) child.gameObject.layer = layer;
    }

    public static SpriteRenderer SetOptionalSprite(Transform transform, Sprite sprite) {
        SpriteRenderer spriteRenderer = transform.GetComponent<SpriteRenderer>();

        if (!sprite) Destroy(transform.gameObject);
        if (!sprite || !spriteRenderer) return null;

        spriteRenderer.sprite = sprite;
        return spriteRenderer;
    }

    protected abstract void SetSprites();

    public float GetHealthPercent() {
        return health / maxHealth;
    }

    public void Damage(float amount) {
        health -= amount;
        OnHealthChange();
    }

    public void SetHealth(float health) {
        this.health = health;
        OnHealthChange();
    }

    protected virtual void OnHealthChange() {
        if (health <= 0) {
            if (this is Unit unit) Client.DestroyUnit(unit, true);
            else if (this is Block block) Client.DestroyBlock(block, true);
        } else {
            Client.SetHealth(this, health);
        }

        OnDamaged?.Invoke(this, EventArgs.Empty);

        if (!Type.canGetOnFire) return;
        fireCount = Mathf.CeilToInt(Type.maximumFires * Mathf.Clamp(0.5f - GetHealthPercent(), 0, 0.5f) * 2);

        if (fireCount == 0) return;

        if (!fires[fireCount - 1]) {
            float sizeMult = GetType().EqualsOrInherits(typeof(Unit)) ? 1f : size;
            Vector3 position = new Vector3(UnityEngine.Random.Range(-0.4f, 0.4f), UnityEngine.Random.Range(-0.4f, 0.4f), 0) * sizeMult;
            fires[fireCount - 1] = transform.CreateEffect(Type.hitSmokeFX, position, Quaternion.Euler(0, 0, UnityEngine.Random.Range(0f, 359.99f)), sizeMult).gameObject;
        }
    }

    public virtual void Kill(bool destroyed) {
        if (this is Unit unit) MapManager.Instance.DeleteUnit(unit, destroyed);
        else if (this is Block block) MapManager.Instance.DeleteBlock(block, destroyed);
    }

    public virtual void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;
        OnDestroyed?.Invoke(this, new EntityArg { other = null });
    }

    /// <summary>
    /// Sent regularly to update the entities
    /// Includes most variable values that can deviate over time
    /// </summary>
    /// <returns>The data needed to sync this entity</returns>
    public override int[] GetSyncData() {
        int[] data = base.GetSyncData();
        data[0] = (int)(health * 1000);
        return data;
    }

    public override void ApplySyncData(int[] values) {
        base.ApplySyncData(values);
        health = values[0] / 1000f;
    }

    /// <summary>
    /// Used to save the entity into a file
    /// </summary>
    /// <returns>The data needed to save/load this entity on a file</returns>
    public virtual string SaveDataToString(bool includeSyncID) {
        string data = includeSyncID ? SyncID + ":" : "";
        data += Type.id + ":";
        data += teamCode + ":";
        data += health + ":";
        return data;
    }

    public virtual void ApplySaveData(string[] values, int i = 3) {
        health = float.Parse(values[i]);
    }

    public bool IsBuilding() {
        return Content.TypeEquals(Type.GetType(), typeof(BlockType));
    }

    public void PlaySound(Sound sound) {
        audioSource.PlayOneShot(sound.clip);
    }
}