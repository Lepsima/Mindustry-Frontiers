using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Frontiers.Teams;
using Frontiers.Animations;
using SpriteAnim = Frontiers.Animations.SpriteAnim;
using SpriteAnimator = Frontiers.Animations.SpriteAnimator;
using SpriteAnimation = Frontiers.Animations.SpriteAnimation;
using System;
using Random = UnityEngine.Random;

public class Weapon : MonoBehaviour {
    public WeaponType Type { private set; get; }
    private Vector2 weaponOffset;

    public Entity parentEntity, target;
    public IArmed iArmed;

    private Barrel[] barrels;

    private ParticleSystem shootFX;
    private SpriteAnimator animator;
    private bool hasAnimations;

    private SpriteRenderer heatSpriteRenderer;
    private float currentHeat;

    // Timers
    private float targetSearchTimer = 0f, avilableShootTimer = 0f, chargeShotCooldownTimer = -1f;

    private bool isReloading = false, isActive = false, mirrored = false, hasMultiBarrel = false, hasHeat = false, hasBarrelHeat = false;
    private int currentRounds, barrelIndex, chargeUpProgress;

    private float cameraShakeTime;
    private float cameraShakeDecrease;

    private void Update() {
        if (cameraShakeTime > Time.time) CameraController.Instance.CameraShake(transform.position, Type.cameraShakeRange, Type.cameraShakeIntensity, cameraShakeTime, cameraShakeDecrease);

        if (isReloading && hasAnimations) {
            animator.NextFrame(SpriteAnimation.Case.Reload); 
        }

        if (Type.independent) {
            HandleTargeting(); 
        }

        if (isActive && IsAvilable()) { 
            Shoot();
        }

        // If is charged and can but not shooting, start to lose charge progress
        if (chargeUpProgress > 0 && avilableShootTimer < Time.time) {
            chargeShotCooldownTimer -= Time.deltaTime;

            if (chargeShotCooldownTimer <= 0f) {
                chargeUpProgress--;
                chargeShotCooldownTimer = Type.chargeShotCooldown;
            }
        } else {
            chargeShotCooldownTimer = Type.chargeShotCooldown;
        }

        UpdateRecoil();

        // Update heat renderers color
        if (hasHeat) ChangeHeat(-Type.heatFadeTime * Time.deltaTime);
        if (hasBarrelHeat) foreach (Barrel barrel in barrels) barrel.ChangeHeat(-Type.heatFadeTime * Time.deltaTime, Type.heatSpriteAlpha);
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

    private void ChangeHeat(float value) {
        currentHeat = Mathf.Clamp01(currentHeat + value);
        heatSpriteRenderer.color = new Color(1f, 1f, 1f, currentHeat * Type.heatSpriteAlpha);
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

    public virtual void Set(Entity parentEntity, WeaponType weaponType, bool mirrored = false, bool onTop = false) {
        iArmed = parentEntity as IArmed;

        if (iArmed == null) {
            Debug.LogError("The parent entity doesn't contain the IArmed interface required to operate weapons");
            return;
        }

        this.parentEntity = parentEntity;
        this.mirrored = mirrored;
        Type = weaponType;

        weaponOffset = transform.localPosition;

        EndReload();
        SetSprites(onTop);
        SetBarrels(Type.barrels);

        hasMultiBarrel = Type.barrels != null && Type.barrels.Length > 0;
        hasAnimations = SetAnimations(Type.animations);

        if (!hasMultiBarrel) {
            shootFX = transform.CreateEffect(Type.shootFX, GetOffset(), Quaternion.identity, Type.shootFXSize);
            if (shootFX) shootFX.transform.CreateEffect(Type.casingFX, new Vector2(0f, Type.casingFXOffset), Quaternion.identity, Type.casingFXSize);
        }
    }

    private void SetBarrels(WeaponBarrel[] barrels) {
        if (barrels == null || barrels.Length == 0) return;
        this.barrels = new Barrel[barrels.Length];
        for(int i = 0; i < barrels.Length; i++) this.barrels[i] = new(this, barrels[i]); 
    }

    private void SetSprites(bool onTop) {
        if (Type.heatSprite != null) {
            hasHeat = true;
            Transform heatTransform = new GameObject("heat", typeof(SpriteRenderer)).transform;
            heatTransform.parent = transform;
            heatTransform.localPosition = Vector3.zero;
            heatTransform.localRotation = Quaternion.identity;

            heatSpriteRenderer = heatTransform.GetComponent<SpriteRenderer>();
            heatSpriteRenderer.sprite = Type.heatSprite;
            heatSpriteRenderer.sortingLayerName = "Units";
            heatSpriteRenderer.sortingOrder = onTop ? 7 : 4;
            heatSpriteRenderer.color = new Color(1f, 1f, 1f, 0f);
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = Type.sprite;
        spriteRenderer.flipX = mirrored;
        spriteRenderer.sortingLayerName = "Units";
        spriteRenderer.sortingOrder = onTop ? 5 : 2;

        Transform outlineTransform = transform.Find("Outline");
        SpriteRenderer outlineSpriteRenderer = outlineTransform.GetComponent<SpriteRenderer>();

        if (!Type.outlineSprite) Destroy(outlineTransform.gameObject);
        if (!Type.outlineSprite || !outlineSpriteRenderer) return;

        outlineSpriteRenderer.sprite = Type.outlineSprite;
        outlineSpriteRenderer.flipX = mirrored;
        spriteRenderer.sortingLayerName = "Units";
        outlineSpriteRenderer.sortingOrder = onTop ? 5 : -1;
        outlineTransform.localScale = Vector3.one;
    }

    private bool SetAnimations(SpriteAnimation[] allAnimations) {
        if (allAnimations == null || allAnimations.Length == 0) return false;
        animator = new SpriteAnimator();

        foreach(SpriteAnimation animation in allAnimations) {
            SpriteAnim anim = new(Type.name, "Blocks", 6, transform, animation);
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

        return iArmed.CanConsumeAmmo(Type.ammoPerShot);
    }

    public void SetActive(bool state) {
        isActive = state;
    }


    public void Shoot() {
        // Consume ammo
        currentRounds--;
        float time = Type.shootTime;


        // Charge-up ability
        if (Type.chargesUp) {
            if (chargeUpProgress < Type.shotsToChargeUp) chargeUpProgress++;
            time = Mathf.Lerp(Type.shootTime, Type.chargedShootTime, (float)chargeUpProgress / Type.shotsToChargeUp);
        }

        // Shoot spacing
        avilableShootTimer = Time.time + time;

        // Shoot animations
        if (hasAnimations) animator.NextFrame(SpriteAnimation.Case.Shoot);

        // Ammo consumption
        iArmed.ConsumeAmmo(Type.ammoPerShot);

        // Camera shake
        cameraShakeTime = Type.cameraShakeTime + Time.time;
        cameraShakeDecrease = Type.cameraShakeDecrease == -1 ? cameraShakeTime : Type.cameraShakeDecrease + Time.time;

        if (hasMultiBarrel) {
            // Add recoil to the weapon body
            transform.position += transform.up * -Type.recoil / (barrels.Length * 2f);

            // Change index
            barrelIndex++;
            if (barrelIndex >= barrels.Length) barrelIndex = 0;

            // Shoot barrel
            barrels[barrelIndex].Shoot(this);
            barrels[barrelIndex].ChangeHeat(Type.heatIncrease, Type.heatSpriteAlpha);
        } else {
            // Add recoil
            transform.position += transform.up * -Type.recoil;

            // Get bullet position and angle
            Vector2 bulletOriginPoint = transform.position + GetOffset();
            float bulletAngle = transform.eulerAngles.z + Random.Range(-Type.spread, Type.spread);

            // Play effects and shoot bullet
            if (shootFX) shootFX.Play();
            this.ShootBullet(bulletOriginPoint, bulletAngle);

            // Heat sprite
            if (hasHeat) ChangeHeat(Type.heatIncrease);
        }
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
}

public class Barrel {
    public Vector2 shootOffset;
    public Transform transform;
    public ParticleSystem shootFX;

    readonly SpriteRenderer heatRenderer;
    private float currentHeat;

    public Barrel(Weapon parent, WeaponBarrel weaponBarrel) {
        shootOffset = weaponBarrel.shootOffset;

        transform = new GameObject("barrel", typeof(SpriteRenderer)).transform;
        transform.parent = parent.transform;
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        shootFX = transform.CreateEffect(parent.Type.shootFX, GetOffset(), Quaternion.identity, parent.Type.shootFXSize);
        if (shootFX) shootFX.transform.CreateEffect(parent.Type.casingFX, new Vector2(0f, parent.Type.casingFXOffset), Quaternion.identity, parent.Type.casingFXSize);

        SpriteRenderer barrelRenderer = transform.GetComponent<SpriteRenderer>();
        barrelRenderer.sprite = weaponBarrel.barrelSprite;
        barrelRenderer.sortingLayerName = "Units";
        barrelRenderer.sortingOrder = weaponBarrel.sortingOrder;

        Transform barrelOutlineTransform = new GameObject("outline", typeof(SpriteRenderer)).transform;
        barrelOutlineTransform.parent = transform;
        barrelOutlineTransform.localPosition = Vector3.zero;
        barrelOutlineTransform.localRotation = Quaternion.identity;

        SpriteRenderer outlineRenderer = barrelOutlineTransform.GetComponent<SpriteRenderer>();
        outlineRenderer.sprite = weaponBarrel.barrelOutlineSprite;
        outlineRenderer.sortingLayerName = "Units";
        outlineRenderer.sortingOrder = -1;

        // Skip if there's no heat sprite
        if (weaponBarrel.barrelHeatSprite == null) return;

        Transform barrelHeatTransform = new GameObject("heat", typeof(SpriteRenderer)).transform;
        barrelHeatTransform.parent = transform;
        barrelHeatTransform.localPosition = Vector3.zero;
        barrelHeatTransform.localRotation = Quaternion.identity;

        heatRenderer = barrelOutlineTransform.GetComponent<SpriteRenderer>();
        heatRenderer.sprite = weaponBarrel.barrelOutlineSprite;
        heatRenderer.sortingLayerName = "Units";
        heatRenderer.sortingOrder = weaponBarrel.sortingOrder + 1;
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

    public void ChangeHeat(float value, float alpha) {
        if (heatRenderer == null) return;

        currentHeat = Mathf.Clamp01(currentHeat + value);
        heatRenderer.color = new Color(1f, 1f, 1f, currentHeat * alpha);
    }
}