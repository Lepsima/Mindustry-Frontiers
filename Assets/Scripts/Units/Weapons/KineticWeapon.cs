using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.IO;
using Frontiers.Content;
using Frontiers.Assets;

public class KineticWeapon : Weapon {
    public override void Set(Entity parentEntity, int weaponID, WeaponType weaponType, bool mirrored = false, bool onTop = false) {
        base.Set(parentEntity, weaponID, weaponType, mirrored, onTop);
        shootParticleSystemEffect.transform.localPosition = Type.shootOffset;
    }

    public override void Shoot() {
        base.Shoot();
        /*
        if (PhotonNetwork.IsMasterClient) 
            BulletGameObjectPool.CreateBullet(
                transform.position + (Vector3)Type.shootOffset, 
                transform.eulerAngles.z, 
                Type.bulletType.id, 
                teamCode);
        */
    }
}