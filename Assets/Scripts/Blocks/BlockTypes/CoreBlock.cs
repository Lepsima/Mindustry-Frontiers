using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Teams;

public class CoreBlock : StorageBlock {
    public new CoreBlockType Type { get => (CoreBlockType)base.Type; protected set => base.Type = value; }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        TeamUtilities.AddCoreBlock(this);

        inventory.Add(Items.copper, 325);
        inventory.Add(Items.titanium, 125);
    }

    public override void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;

        TeamUtilities.RemoveCoreBlock(this);
        base.OnDestroy();
    }
}