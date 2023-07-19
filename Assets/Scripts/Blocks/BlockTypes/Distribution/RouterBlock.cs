using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RouterBlock : ItemBlock {
    protected override void Update() {
        base.Update();
        OutputItems();
    }
}