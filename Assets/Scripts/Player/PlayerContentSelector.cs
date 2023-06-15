using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using Frontiers.Content;
using Frontiers.Content.Maps;
using Frontiers.Assets;
using System;
using System.IO;
using Frontiers.Teams;

public static class PlayerContentSelector {
    public static Content SelectedContent => selectedContent;
    private static Content selectedContent = Blocks.copperWall;
    private static float rotation;
    private static bool canBeRotated;

    public static event EventHandler<ContentEventArgs> OnSelectedContentChanged;
    public class ContentEventArgs {
        public ContentEventArgs(Content content) {
            this.content = content;
        }
        public Content content;
    }


    public static int GetOrientation => Mathf.FloorToInt(rotation / 90f);

    public static bool TypeEquals(Type target, Type reference) => target == reference || target.IsSubclassOf(reference);

    public static void CreateSelectedContent(Vector3 mousePos, int mode) {
        if (selectedContent == null) return;

        if (selectedContent is BlockType blockType) {
            if (mode == 0) {

                if (!MapManager.mouseGridAllowsPlace) return;
                int orientation = blockType.rotates ? GetOrientation : 0;
                Client.CreateBlock(MapManager.mouseGridPos, orientation, false, selectedContent, TeamUtilities.GetLocalTeam());

            } else if (mode == 1) {

                Block block = MapManager.Map.GetBlockAt(MapManager.mouseGridPos);
                if (block == null || !block.Type.breakable) return;
                Client.DestroyBlock(block);

            }
        }

        if (selectedContent is UnitType) {
            if (mode == 0) Client.CreateUnit(mousePos, rotation, selectedContent, TeamUtilities.GetLocalTeam());
            else if (mode == 1) Client.CreateUnit(mousePos, rotation, selectedContent, TeamUtilities.GetEnemyTeam(TeamUtilities.GetLocalTeam()));           
        }
    }

    public static void SetSelectedContent(Content content) {
        Content previousContent = selectedContent;

        if (content == selectedContent) selectedContent = null;
        else selectedContent = content;

        if (previousContent != selectedContent) OnSelectedContentChanged?.Invoke(null, new ContentEventArgs(selectedContent));
        canBeRotated = selectedContent != null && (TypeEquals(selectedContent.GetType(), typeof(UnitType)) || (TypeEquals(selectedContent.GetType(), typeof(BlockType)) && ((BlockType)selectedContent).rotates));

        PlayerUI.Instance.SetSelectedContent(selectedContent);
        CursorIndicator.Instance.SetContent(selectedContent);
    }

    public static void ChangeSelectedContentOrientation(float value) {
        if (selectedContent == null || !canBeRotated) return;

        if (selectedContent is BlockType) {
            value = value == 0 ? 0 : value > 0 ? 1 : -1;
            rotation = Mathf.Floor((value * 90 + rotation) / 90f) * 90f;
        } else {
            rotation += value * 2f;
        }

        if (rotation < 0) rotation += 360f;
        else if (rotation > 360f) rotation -= 360f;

        CursorIndicator.Instance.ChangeRotation(rotation);
    }
}