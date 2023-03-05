using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Teams;
using Photon.Pun;

public class TurretBlock : Block, IView {
    public PhotonView PhotonView { get; set; }
    Weapon installedWeapon;

    LoadableContent Target { 
        get => _targetTransform; 
        set =>_targetTransform = value;  
    }
    LoadableContent _targetTransform;

    Transform turretTransform;
    float rotationSpeed = 90, nextTargetSearchTime;
    bool IsFullAuto { get => installedWeapon.isFullAuto; }


    public override void Set(Vector2Int gridPosition, BlockType blockType, float timeCode, byte teamCode) {
        base.Set(gridPosition, blockType, timeCode, teamCode);
        turretTransform = transform.Find("Empty");
        Target = null;

        SetWeapon();
    }

    private void Update() {
        HandleTargeting();
        HandleShooting();
    }

    private void HandleTargeting() {
        if (!ValidTarget(Target) && nextTargetSearchTime < Time.time) {
            nextTargetSearchTime = Time.time + 3f;
            Target = GetTarget();
        }
    }

    private bool ValidTarget(LoadableContent target) {
        if (!target) return false;
        return Vector2.Distance(target.GetPosition(), GetPosition()) < installedWeapon.Type.Range;
    }

    private void HandleShooting() {
        if (Target) {
            //If is in shoot position, shoot
            RotateTowards(Target.GetPosition());
            bool canShoot = IsInShootRange(Target.GetPosition(), installedWeapon.Type.maxDeviation, installedWeapon.Type.Range);
            if (canShoot != IsFullAuto) installedWeapon.isFullAuto = canShoot;
        } else {
            installedWeapon.isFullAuto = false;
        }
    }

    //Smooth point to position
    protected void RotateTowards(Vector3 position) {
        //Quirky quaternion stuff to make the transform rotate slowly
        Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, position - turretTransform.position);
        desiredRotation = Quaternion.Euler(0, 0, desiredRotation.eulerAngles.z);
        turretTransform.rotation = Quaternion.RotateTowards(turretTransform.rotation, desiredRotation, rotationSpeed * Time.fixedDeltaTime);
    }

    public bool IsInShootRange(Vector2 target, float maxDeviation, float range) {
        if (Vector2.Distance(target, turretTransform.position) > range) return false;

        float cosAngle = Vector2.Dot((target - (Vector2)turretTransform.position).normalized, turretTransform.up);
        float angle = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;

        return angle < maxDeviation;
    }

    private void SetWeapon() {
        //Get weapon prefab
        GameObject weaponPrefab = Assets.GetPrefab("weaponPrefab");
        WeaponMount weaponMount = Type.weapon;

        //Create and initialize new weapon
        GameObject weaponGameObject = Instantiate(weaponPrefab, turretTransform.position + (Vector3)weaponMount.position, Quaternion.identity, turretTransform);
        Weapon weapon = (Weapon)weaponGameObject.AddComponent(weaponMount.weaponType.type);
        weapon.Set(this, weaponMount.weaponType, false, timeCode, teamCode);
        installedWeapon = weapon;
    }

    private LoadableContent GetTarget(System.Type[] priorityList = null) {
        //Default priority targets
        if (priorityList == null) priorityList = new System.Type[3] { typeof(Unit), typeof(TurretBlock), typeof(Block) };

        foreach (System.Type type in priorityList) {
            //Search the next priority type
            LoadableContent tempTarget = MapManager.Instance.GetClosestLoadableContent(GetPosition(), type, TeamUtilities.GetEnemyTeam(teamCode).Code);

            //If target is valid, stop searching
            if (ValidTarget(tempTarget)) return tempTarget;
        }

        return null;
    }
}