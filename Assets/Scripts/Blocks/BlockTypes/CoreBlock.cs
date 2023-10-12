using Frontiers.Content;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Assets;
using Frontiers.Teams;

public class CoreBlock : StorageBlock {
    public new CoreBlockType Type { get => (CoreBlockType)base.Type; protected set => base.Type = value; }

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);
        TeamUtilities.AddCoreBlock(this);

        inventory.Add(Items.copper, 325);
        inventory.Add(Items.cocaine, 125);

        if (IsLocalTeam()) {
            // Hide until animation ends
            ShowSprites(false);

            // Instantiate animation prefab
            GameObject animationPrefab = AssetLoader.GetPrefab("CoreLandAnimationPrefab");
            GameObject instance = Instantiate(animationPrefab, GetPosition(), Quaternion.identity);

            // Subscribe to event
            instance.GetComponentInChildren<ThrusterAnimationTrigger>().OnAnimationEnd += OnAnimationEnd;

            // Disable loading screen
            PlayerUI.Instance.EnableLoadingScreen(false);

            // Follow animation
            PlayerManager.Instance.FixFollow(instance.transform.GetChild(0), 25f);
        }
    }

    public void OnAnimationEnd(object sender, System.EventArgs e) {
        Destroy(((ThrusterAnimationTrigger)sender).transform.root.gameObject);
        ShowSprites(true);
        PlayerManager.Instance.UnFollow(GetPosition());
    }

    public override void OnDestroy() {
        if (!gameObject.scene.isLoaded) return;

        TeamUtilities.RemoveCoreBlock(this);
        base.OnDestroy();
    }
}