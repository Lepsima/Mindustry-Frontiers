using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Animations;

namespace Frontiers.Content {
    public class ExtractorBlockType : ItemBlockType {
        public MovementAnimation[] drillAnimations;

        public ExtractorBlockType(string name, System.Type type, int tier) : base(name, type, tier) {
        }
    }
}


public class ExtractorBlock : ItemBlock {
    public new ExtractorBlockType Type { get => (ExtractorBlockType)base.Type; protected set => base.Type = value; }

    MovementAnimator animator;
    float nextBatchTime;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        animator = new MovementAnimator(Type.name, "Blocks", 3, transform, Type.drillAnimations);
    }

    protected override void Update() {
        base.Update();
        bool canDrill = !inventory.Full(Items.copper);

        if (canDrill) {
            animator.Update(Time.deltaTime);

            // Add copper, temporal
            if (nextBatchTime <= Time.time) {
                inventory.Add(Items.copper, 5);
                nextBatchTime = Time.time + 2;
            }
        }
    }
}
