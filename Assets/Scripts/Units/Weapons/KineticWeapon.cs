using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.IO;
using Frontiers.Content;
using Frontiers.Assets;

public class KineticWeapon : Weapon {
    public override void Set(LoadableContent parentEntity, WeaponType weaponType, bool mirrored = false, float timeCode = 0, byte teamCode = 0) {
        base.Set(parentEntity, weaponType, mirrored, timeCode, teamCode);

        if (weaponType.bulletType.bulletClass == BulletClass.Instant) Debug.LogWarning(BulletClass.Instant + " is incompatible with" + typeof(KineticWeapon));

        shootParticleSystemEffect.transform.localPosition = Type.shootOffset;
    }
    
    public override void Shoot() {
        base.Shoot();
        if ((parentEntity as IView).PhotonView.IsMine) BulletGameObjectPool.CreateBullet(transform.position + (Vector3)Type.shootOffset, transform.eulerAngles.z, Type.bulletType.id, teamCode);
    }
}