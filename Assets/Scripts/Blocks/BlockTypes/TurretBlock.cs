using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Teams;
using Photon.Pun;

public class TurretBlock : ItemBlock, IArmed {
    public new TurretBlockType Type { get => (TurretBlockType)base.Type; protected set => base.Type = value; }
    public Weapon installedWeapon;

    public float ammo;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);

        Transform turretTransform = transform.Find("Empty");

        //Get weapon prefab
        GameObject weaponPrefab = AssetLoader.GetPrefab("WeaponPrefab");
        WeaponMount weaponMount = Type.mount;

        //Create and initialize new weapon
        GameObject weaponGameObject = Instantiate(weaponPrefab, turretTransform.position + (Vector3)weaponMount.position, Quaternion.identity, turretTransform);
        Weapon weapon = weaponGameObject.AddComponent<Weapon>();
        weapon.Set(this, weaponMount.weapon, weaponMount.onTop);
        installedWeapon = weapon;
    }

    public override bool CanReciveItem(Item item, int orientation = 0) {
        return item == Type.ammoItem && ammo <= Type.ammoAmount - 1f;
    }

    public override void ReciveItems(Item item, int amount = 1, int orientation = 0) {
        if (item != Type.ammoItem) return;
        ammo = Mathf.Clamp(ammo + amount, 0f, Type.ammoAmount);
    }

    public void ConsumeAmmo(float amount) {
        ammo -= amount;
    }

    public bool CanConsumeAmmo(float amount) {
        return ammo >= amount;
    }
}