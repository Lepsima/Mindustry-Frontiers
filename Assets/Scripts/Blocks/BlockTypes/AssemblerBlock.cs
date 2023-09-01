using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Frontiers.Content;
using Frontiers.Assets;
using Frontiers.Content.VisualEffects;

namespace Frontiers.Content {
    public class AssemblerBlockType : BlockType {
        public int minBuildSize = 1, maxBuildSize = 1;
        public Sprite thrusterSprite;

        public ArmData[] arms;

        public AssemblerBlockType(string name, Type type, int tier) : base(name, type, tier) {
            thrusterSprite = AssetLoader.GetSprite(name + "-thruster");
        }
    }

    public class ArmData {
        public Sprite baseSprite, middleSprite, endSprite;
        public Effect effect = Effects.weldSparks;

        public Vector2 idlePosition, middleArmOffset, minPosition, maxPosition, maxTargetOffset;
        public float idleAngle, minBaseAngle, maxBaseAngle, minTime, maxTime;

        public ArmData(string name) {
            baseSprite = AssetLoader.GetSprite(name + "-arm-base");
            middleSprite = AssetLoader.GetSprite(name + "-arm-middle");
            endSprite = AssetLoader.GetSprite(name + "-arm-end");
        }
    }
}

public class AssemblerBlock : Block {
    public new AssemblerBlockType Type { get => (AssemblerBlockType)base.Type; protected set => base.Type = value; }

    CoreBlock linkedCore;
    ArmAnimationController armAnimationController;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        HandleLandAnimation();
        armAnimationController = new(transform, Type.arms);
    }

    private void HandleLandAnimation() {
        // Hide until animation ends
        ShowSprites(false);

        // Instantiate animation prefab
        GameObject animationPrefab = AssetLoader.GetPrefab("AssemblerLandAnimationPrefab");
        GameObject instance = Instantiate(animationPrefab, GetPosition(), Quaternion.identity);

        // Get thruster animators
        ThrusterAnimationTrigger mainTrigger = instance.transform.Find("main-block/engine-trigger").GetComponent<ThrusterAnimationTrigger>();
        ThrusterAnimationTrigger fairingTrigger = instance.transform.Find("main-block/fairing-trigger").GetComponent<ThrusterAnimationTrigger>();

        // Set thruster offsets
        mainTrigger.SetBlockSize((int)size);
        fairingTrigger.SetBlockSize((int)size);

        // Set sprites
        instance.transform.Find("main-block").GetComponent<SpriteRenderer>().sprite = Type.sprite;
        instance.transform.Find("main-block/block-fairing").GetComponent<SpriteRenderer>().sprite = Type.topSprite;
        instance.transform.Find("main-block/block-thruster").GetComponent<SpriteRenderer>().sprite = Type.thrusterSprite;

        // Subscribe to event
        fairingTrigger.OnAnimationEnd += OnAnimationEnd;
    }

    public void OnAnimationEnd(object sender, System.EventArgs e) {
        ShowSprites(true);
    }
}