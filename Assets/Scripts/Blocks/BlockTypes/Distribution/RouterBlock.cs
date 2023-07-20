using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public class RouterBlock : ItemBlock {
    public new RouterBlockType Type { get => (RouterBlockType)base.Type; protected set => base.Type = value; }

    protected override void Update() {
        base.Update();
        OutputItems();
    }
}