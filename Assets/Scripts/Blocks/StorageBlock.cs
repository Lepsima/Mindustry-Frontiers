using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public class StorageBlock : ItemBlock {
    public override void SetInventory() {
        inventory = new ItemList(Type.itemCapacity, true);
        base.SetInventory();
    }
}
