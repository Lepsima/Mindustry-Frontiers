using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;

public class StorageBlock : ItemBlock {
    public new StorageBlockType Type { get => (StorageBlockType)base.Type; protected set => base.Type = value; }
}
