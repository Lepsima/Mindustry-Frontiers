using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Assets;
using Frontiers.Teams;

public class CoreBlock : StorageBlock {
    static bool hasShownAnimation;

    public new CoreBlockType Type { get => (CoreBlockType)base.Type; protected set => base.Type = value; }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        TeamUtilities.AddCoreBlock(this);

        inventory.Add(Items.copper, 325);
        inventory.Add(Items.cocaine, 125);

        if (IsLocalTeam() && !hasShownAnimation) {
            Invoke(nameof(StartAnimation), 3f);
            hasShownAnimation = true;
        }
    }

    public void StartAnimation() {
        // Hide until animation ends
        ShowSprites(false);

        // Instantiate animation prefab
        GameObject animationPrefab = AssetLoader.GetPrefab("CoreLandAnimationPrefab");
        GameObject instance = Instantiate(animationPrefab, GetPosition(), Quaternion.identity);

        // Subscribe to event
        AnimationInfo animationInfo = instance.GetComponentInChildren<AnimationInfo>();
        animationInfo.OnAnimationEnd += OnAnimationEnd;

        // Disable loading screen
        PlayerUI.Instance.EnableLoadingScreen(false);

        // Follow animation
        CameraController.Instance.FixFollow(instance.transform.GetChild(0), 25f);
        CameraController.Instance.CameraShake(7f, animationInfo.length * 0.9f, animationInfo.length * 0.75f, 0f);
    }

    public void OnAnimationEnd(object sender, System.EventArgs e) {
        Destroy(((AnimationInfo)sender).transform.root.gameObject);
        ShowSprites(true);
        CameraController.Instance.UnFollow(GetPosition());
    }

    public override void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;

        TeamUtilities.RemoveCoreBlock(this);
        base.OnDestroy();
    }
}