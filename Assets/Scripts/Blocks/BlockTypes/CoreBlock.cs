using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Teams;

public class CoreBlock : StorageBlock {
    public override void Set(Vector2Int gridPosition, BlockType blockType, float timeCode, byte teamCode) {
        base.Set(gridPosition, blockType, timeCode, teamCode);
        TeamUtilities.AddCoreBlock(this);
        inventory.AddItem(new ItemStack(Items.copper, 125));
    }

    public override void Delete() {
        TeamUtilities.RemoveCoreBlock(this);
        base.Delete();
    }
}