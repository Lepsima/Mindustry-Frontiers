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

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);

        Transform turretTransform = transform.Find("Empty");

        //Get weapon prefab
        GameObject weaponPrefab = AssetLoader.GetPrefab("WeaponPrefab");
        WeaponMount weaponMount = Type.weapon;

        //Create and initialize new weapon
        GameObject weaponGameObject = Instantiate(weaponPrefab, turretTransform.position + (Vector3)weaponMount.position, Quaternion.identity, turretTransform);
        Weapon weapon = weaponGameObject.AddComponent<Weapon>();
        weapon.Set(this, 1, weaponMount.weapon, weaponMount.onTop);
        installedWeapon = weapon;
    }

    public override void SetInventory() {
        base.SetInventory();

        WeaponType weapon = Type.weapon.weapon;
        if (weapon.consumesItems) inventory.SetAllowedItems(new Item[1] { weapon.ammoItem });
    }

    public Weapon GetWeaponByID(byte id) {
        return installedWeapon;
    }

    public void ConsumeAmmo(float amount) {

    }
}