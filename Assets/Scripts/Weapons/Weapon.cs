using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Frontiers.Teams;
using Frontiers.Animations;
using Anim = Frontiers.Animations.Anim;
using Animator = Frontiers.Animations.Animator;
using Animation = Frontiers.Animations.Animation;
using System;
using Random = UnityEngine.Random;

public class Weapon : MonoBehaviour {
    private readonly List<Bullet> activeBullets = new();

    public WeaponType Type { private set; get; }
    private Vector2 weaponOffset;

    public Entity parentEntity, target;
    public int weaponID;

    private Barrel[] barrels;

    private ParticleSystem shootFX;
    private Animator animator;
    private bool hasAnimations;

    // Timers
    private float targetSearchTimer = 0f, avilableShootTimer = 0f;

    private bool isReloading = false, isActive = false, mirrored = false, hasMultiBarrel = false;
    private int currentRounds, barrelIndex;

    private void Update() {
        if (isReloading && hasAnimations) {
            animator.NextFrame(Animation.Case.Reload); 
        }

        if (Type.isIndependent) {
            HandleTargeting(); 
        }

        if (isActive && IsAvilable()) {
            Client.WeaponShoot(this);
        }

        UpdateRecoil();
    }

    private void UpdateRecoil() {
        transform.localPosition = Vector2.Lerp(transform.localPosition, weaponOffset, Type.returnSpeed * Time.deltaTime);
        if (!hasMultiBarrel) return;

        for (int i = 0; i < barrels.Length; i++) {
            Transform transform = barrels[i].transform;
            transform.localPosition = Vector2.Lerp(transform.localPosition, Vector2.zero, Type.returnSpeed * Time.deltaTime);
        }
    }

    private void ChangeTarget(Entity newTarget) {
        if (target) target.OnDestroyed -= OnTargetDestroyed;
        target = newTarget;
        if (target) target.OnDestroyed += OnTargetDestroyed;
    }

    private void OnTargetDestroyed(object sender, Entity.EntityArg e) {
        targetSearchTimer = 0;
    }

    private bool ValidTarget(Entity target) {
        if (!target) return false;
        return Vector2.Distance(target.GetPosition(), transform.position) < Type.Range;
    }

    private void HandleTargeting() {
        if (!ValidTarget(target) && targetSearchTimer <= Time.time) {
            targetSearchTimer = Time.time + 3f;
            ChangeTarget(GetTarget());
        }

        if (target) {
            Vector2 targetPosition = Type.predictTarget ? target.GetPredictedPosition(transform.position, transform.up * Type.bulletType.velocity) : target.GetPosition();

            //If is in shoot position, shoot
            RotateTowards(targetPosition);
            bool canShoot = IsInShootRange(targetPosition, Type.maxTargetDeviation, Type.Range);
            if (canShoot != isActive) isActive = canShoot;
        } else {
            isActive = false;
        }
    }

    //Smooth point to position
    protected void RotateTowards(Vector3 position) {
        //Quirky quaternion stuff to make the transform rotate slowly
        Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, position - transform.position);
        desiredRotation = Quaternion.Euler(0, 0, desiredRotation.eulerAngles.z);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, Type.rotateSpeed * Time.fixedDeltaTime);
    }

    public bool IsInShootRange(Vector2 target, float maxDeviation, float range) {
        if (Vector2.Distance(target, transform.position) > range) return false;

        float cosAngle = Vector2.Dot((target - (Vector2)transform.position).normalized, transform.up);
        float angle = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;

        return angle < maxDeviation;
    }

    private Entity GetTarget(Type[] priorityList = null) {
        //Default priority targets
        if (priorityList == null) priorityList = new Type[3] { typeof(Unit), typeof(TurretBlock), typeof(Block) };

        foreach (Type type in priorityList) {
            //Search the next priority type
            Entity tempTarget = MapManager.Map.GetClosestEntity(transform.position, type, TeamUtilities.GetEnemyTeam(parentEntity.GetTeam()));

            //If target is valid, stop searching
            if (ValidTarget(tempTarget)) return tempTarget;
        }

        return null;
    }

    public virtual void Set(Entity parentEntity, int weaponID, WeaponType weaponType, bool mirrored = false, bool onTop = false) {
        this.parentEntity = parentEntity;
        this.weaponID = weaponID;
        this.mirrored = mirrored;
        Type = weaponType;

        weaponOffset = transform.localPosition;
        if (Type.consumesItems && !parentEntity.hasInventory) Debug.LogWarning(Type.name + " consumes items but " + parentEntity.GetEntityType().name + " doesn't have inventory, this may cause errors");

        EndReload();
        SetSprites(onTop);
        SetBarrels(Type.barrels);

        hasMultiBarrel = Type.barrels != null && Type.barrels.Length > 0;
        hasAnimations = SetAnimations(Type.animations);

        if (!hasMultiBarrel) {
            shootFX = transform.CreateEffect(Type.shootFX, Vector2.zero, Quaternion.identity, Type.shootFXSize);
            if (shootFX) { 
                shootFX.transform.position = transform.position + GetOffset();
                shootFX.transform.CreateEffect(Type.casingFX, new Vector2(0f, Type.casingFXOffset), Quaternion.identity, Type.casingFXSize);
            }
        }
    }

    private void SetBarrels(WeaponBarrel[] barrels) {
        if (barrels == null || barrels.Length == 0) return;
        this.barrels = new Barrel[barrels.Length];
        for(int i = 0; i < barrels.Length; i++) this.barrels[i] = new(this, barrels[i]); 
    }

    private void SetSprites(bool onTop) {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = Type.sprite;
        spriteRenderer.flipX = mirrored;
        spriteRenderer.sortingOrder = onTop ? 5 : 2;

        SetOptionalSprite(transform.Find("Outline"), Type.outlineSprite);
    }

    public void SetOptionalSprite(Transform transform, Sprite sprite) {
        SpriteRenderer spriteRenderer = transform.GetComponent<SpriteRenderer>();

        if (!sprite) Destroy(transform.gameObject);
        if (!sprite || !spriteRenderer) return;

        spriteRenderer.sprite = sprite;
        spriteRenderer.flipX = mirrored;
        transform.localScale = Vector3.one;
    }

    private bool SetAnimations(Animation[] allAnimations) {
        if (allAnimations == null || allAnimations.Length == 0) return false;
        animator = new Animator();

        foreach(Animation animation in allAnimations) {
            Anim anim = new(Type.name, "Blocks", 6, transform, animation);
            animator.AddAnimation(anim);
        }

        return true;
    }

    public bool IsAvilable() {
        if (isReloading || avilableShootTimer > Time.time) return false;

        if (currentRounds <= 0) {
            Reload();
            return false;
        }

        // Check if consumes ammo and if has enough
        if (Type.consumesItems && !parentEntity.GetInventory().Has(Type.ammoItem, 1)) return false;

        return true;
    }

    public void SetActive(bool state) {
        isActive = state;
    }

    public void Shoot() {
        currentRounds--;
        avilableShootTimer = Time.time + Type.shootTime;

        if (hasAnimations) animator.NextFrame(Animation.Case.Shoot);
        if (Type.consumesItems) parentEntity.GetInventory().Substract(Type.ammoItem, 1);

        if (hasMultiBarrel) {
            transform.position += transform.up * -Type.recoil / (barrels.Length * 2f);

            barrelIndex++;
            if (barrelIndex >= barrels.Length) barrelIndex = 0;

            activeBullets.Add(barrels[barrelIndex].Shoot(this));
        } else {
            transform.position += transform.up * -Type.recoil;

            Vector2 bulletOriginPoint = transform.position + GetOffset();
            float bulletAngle = transform.eulerAngles.z + Random.Range(-Type.spread, Type.spread);

            if (shootFX) shootFX.Play();
            activeBullets.Add(this.ShootBullet(bulletOriginPoint, bulletAngle));
        }
    }

    public void ReturnBullet(Bullet bullet) {
        activeBullets.Remove(bullet);
    }

    public void Reload() {
        isReloading = true;
        Invoke(nameof(EndReload), Type.reloadTime);
    }

    private void EndReload() {
        isReloading = false;
        currentRounds = Type.clipSize;
    }

    private Vector3 GetOffset() {
        Vector2 offset = hasMultiBarrel ? barrels[barrelIndex].shootOffset : Type.shootOffset;
        return (offset.x * transform.right) + (offset.y * transform.up);
    }

    public void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;

        StopAllCoroutines();

        int amount = activeBullets.Count;
        for (int i = amount - 1; i >= 0; i--) activeBullets[i].Return();
    }
}

public class Barrel {
    public Vector2 shootOffset;
    public Transform transform;
    public ParticleSystem shootFX;

    public Barrel(Weapon parent, WeaponBarrel weaponBarrel) {
        shootOffset = weaponBarrel.shootOffset;

        transform = new GameObject("barrel", typeof(SpriteRenderer)).transform;
        transform.parent = parent.transform;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        shootFX = transform.CreateEffect(parent.Type.shootFX, Vector2.zero, Quaternion.identity);
        if (shootFX) shootFX.transform.position = transform.position + GetOffset();

        SpriteRenderer barrelRenderer = transform.GetComponent<SpriteRenderer>();
        barrelRenderer.sprite = weaponBarrel.barrelSprite;
        barrelRenderer.sortingLayerName = "Units";
        barrelRenderer.sortingOrder = 3;

        Transform barrelOutlineTransform = new GameObject("outline", typeof(SpriteRenderer)).transform;
        barrelOutlineTransform.parent = transform;
        barrelOutlineTransform.localPosition = Vector3.zero;
        barrelOutlineTransform.localRotation = Quaternion.identity;

        SpriteRenderer outlineRenderer = barrelOutlineTransform.GetComponent<SpriteRenderer>();
        outlineRenderer.sprite = weaponBarrel.barrelOutlineSprite;
        outlineRenderer.sortingLayerName = "Units";
        outlineRenderer.sortingOrder = -1;
    }

    public Bullet Shoot(Weapon weapon) {
        if (shootFX) shootFX.Play();
        transform.position += transform.up * -weapon.Type.recoil;

        float bulletAngle = transform.eulerAngles.z + Random.Range(-weapon.Type.spread, weapon.Type.spread);
        return weapon.ShootBullet(transform.position + GetOffset(), bulletAngle);
    }

    public Vector3 GetOffset() {
        return (shootOffset.x * transform.right) + (shootOffset.y * transform.up);
    }
}