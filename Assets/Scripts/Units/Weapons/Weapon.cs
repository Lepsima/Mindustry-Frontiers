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

public abstract class Weapon : MonoBehaviour {
    protected ParticleSystem shootParticleSystemEffect;
    private readonly List<Bullet> activeBullets = new();

    public WeaponType Type { private set; get; }
    protected Vector2 weaponOffset;

    public Entity parentEntity;
    public int weaponID;

    protected Entity target;
    private float nextTargetSearchTime;

    private int currentRounds;
    private float avilableShootTimer = 0f;
    private bool isReloading = false;
    public bool isActive = false, mirrored = false;

    private bool hasMultiBarrel = false;
    private int barrelIndex;
    private Vector2[] barrelOffsets;
    private Transform[] barrels;

    private Animator animator;
    private bool hasAnimations;

    private void Update() {
        if (isReloading && hasAnimations) animator.NextFrame(Animation.Case.Reload);

        if (Type.isIndependent) {
            HandleTargeting();
            HandleShooting();
        }

        if (isActive && IsAvilable()) {
            Client.WeaponShoot(this);
        }

        transform.localPosition = Vector2.Lerp(transform.localPosition, weaponOffset, Type.returnSpeed * Time.deltaTime);

        if (hasMultiBarrel) {
            for(int i = 0; i < barrels.Length; i++) {
                Transform transform = barrels[i];
                transform.localPosition = Vector2.Lerp(transform.localPosition, Vector2.zero, Type.returnSpeed * Time.deltaTime);
            }
        }
    }

    private void HandleTargeting() {
        if (!ValidTarget(target) && nextTargetSearchTime <= Time.time) {
            nextTargetSearchTime = Time.time + 3f;
            ChangeTarget(GetTarget());
        }
    }

    private void ChangeTarget(Entity newTarget) {
        if (target) target.OnDestroyed -= OnTargetDestroyed;
        target = newTarget;
        if (target) target.OnDestroyed += OnTargetDestroyed;
    }

    private void OnTargetDestroyed(object sender, EventArgs e) {
        nextTargetSearchTime = 0;
    }

    private void HandleShooting() {
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

    private bool ValidTarget(Entity target) {
        if (!target) return false;
        return Vector2.Distance(target.GetPosition(), transform.position) < Type.Range;
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

    private Entity GetTarget(System.Type[] priorityList = null) {
        //Default priority targets
        if (priorityList == null) priorityList = new System.Type[3] { typeof(Unit), typeof(TurretBlock), typeof(Block) };

        foreach (System.Type type in priorityList) {
            //Search the next priority type
            Entity tempTarget = MapManager.Map.GetClosestEntity(transform.position, type, TeamUtilities.GetEnemyTeam(parentEntity.GetTeam())) as Entity;

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
        shootParticleSystemEffect = transform.Find("ShootParticleSystem").GetComponent<ParticleSystem>();
        shootParticleSystemEffect.transform.localPosition = Type.shootOffset;

        if (Type.consumesItems && !parentEntity.hasInventory) Debug.LogWarning(Type.name + " consumes items but " + parentEntity.GetEntityType().name + " doesn't have inventory, this may cause errors");

        EndReload();
        SetSprites(onTop);
        SetBarrels(Type.barrels);

        hasMultiBarrel = Type.barrels != null && Type.barrels.Length > 0;
        hasAnimations = SetAnimations(Type.animations);
    }

    private void SetBarrels(WeaponBarrel[] barrels) {
        if (barrels == null || barrels.Length == 0) return;
        this.barrels = new Transform[barrels.Length];
        this.barrelOffsets = new Vector2[barrels.Length];

        for(int i = 0; i < barrels.Length; i++) {
            WeaponBarrel barrel = barrels[i];

            Transform barrelTransform = new GameObject("barrel", typeof(SpriteRenderer)).transform;
            barrelTransform.parent = transform;
            barrelTransform.localPosition = Vector3.zero;
            barrelTransform.localRotation = Quaternion.identity;

            SpriteRenderer barrelRenderer = barrelTransform.GetComponent<SpriteRenderer>();
            barrelRenderer.sprite = barrel.barrelSprite;
            barrelRenderer.sortingLayerName = "Units";
            barrelRenderer.sortingOrder = 3;

            Transform barrelOutlineTransform = new GameObject("outline", typeof(SpriteRenderer)).transform;
            barrelOutlineTransform.parent = barrelTransform;
            barrelOutlineTransform.localPosition = Vector3.zero;
            barrelOutlineTransform.localRotation = Quaternion.identity;

            SpriteRenderer outlineRenderer = barrelOutlineTransform.GetComponent<SpriteRenderer>();
            outlineRenderer.sprite = barrel.barrelOutlineSprite;
            outlineRenderer.sortingLayerName = "Units";
            outlineRenderer.sortingOrder = -1;

            this.barrels[i] = barrelTransform;
            this.barrelOffsets[i] = barrel.shootOffset;
        }
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

    public void Shoot() {
        currentRounds--;
        avilableShootTimer = Time.time + Type.shootTime;

        if (hasAnimations) animator.NextFrame(Animation.Case.Shoot);
        if (Type.consumesItems) parentEntity.GetInventory().Substract(Type.ammoItem, 1);

        shootParticleSystemEffect.Play();

        if (hasMultiBarrel) {
            barrelIndex++;
            if (barrelIndex >= barrelOffsets.Length) barrelIndex = 0;
            barrels[barrelIndex].position += barrels[barrelIndex].up * -Type.recoil;
        }

        if (hasMultiBarrel) transform.position += transform.up * -Type.recoil / (barrels.Length * 2f);
        else transform.position += transform.up * -Type.recoil;

        Vector2 originPoint = transform.position + GetOffset();
        shootParticleSystemEffect.transform.position = originPoint;

        float angle = transform.eulerAngles.z + Random.Range(-Type.spread, Type.spread); ;
        Bullet bullet = this.ShootBullet(Type.bulletType, originPoint, angle);
        activeBullets.Add(bullet);
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
        Vector2 offset = hasMultiBarrel ? barrelOffsets[barrelIndex] : Type.shootOffset;
        return (offset.x * transform.right) + (offset.y * transform.up);
    }

    public void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;

        StopAllCoroutines();

        int amount = activeBullets.Count;
        for (int i = amount - 1; i >= 0; i--) activeBullets[i].Return();
    }
}