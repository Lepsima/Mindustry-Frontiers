using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Animations;
using Frontiers.Content.VisualEffects;

namespace Frontiers.Content {
    public class ExtractorBlockType : ItemBlockType {
        public MovementAnimation[] drillAnimations;
        public Effect drillFX;

        public float drillTime = 1f;
        public int drillAmount = 2;

        public ExtractorBlockType(string name, System.Type type, int tier) : base(name, type, tier) {

        }
    }
}


public class ExtractorBlock : ItemBlock {
    public new ExtractorBlockType Type { get => (ExtractorBlockType)base.Type; protected set => base.Type = value; }

    ParticleSystem drillFX;
    MovementAnimator animator;
    float progress;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        animator = new MovementAnimator(Type.name, "Blocks", 2, transform, Type.drillAnimations);
        drillFX = EffectPlayer.CreateEffect(transform, Type.drillFX, Vector2.zero, Quaternion.identity, 1f);
    }

    protected override void Update() {
        base.Update();

        OutputItems();
        bool canDrill = !inventory.Full(Items.copper);

        if (canDrill) {
            progress += Time.deltaTime;
            animator.Set(progress);

            if (progress >= Type.drillTime) OnDrill();

        } else {
            progress = 0f;
        }
    }

    private void OnDrill() {
        inventory.Add(Items.copper, Type.drillAmount);
        progress -= Type.drillTime;
        drillFX.Play();
    }
}
