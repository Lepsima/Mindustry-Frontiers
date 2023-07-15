using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frontiers.Content.Flags;
using Frontiers.Assets;
using Frontiers.Content;
using Frontiers.Content.Upgrades;

public class UpgradeTreeTest : MonoBehaviour {
    TreeNode[] nodes;
    TreeNode masterNode;

    private void Awake() {
        AssetLoader.LoadAssets();
        ContentLoader.LoadContents();

        CreateNodes();
        InstantiateNodes();
    }

    public void CreateNodes() {
        UpgradeType[] allUpgrades = UpgradeHandler.loadedUpgrades.Values.ToArray();

        Dictionary<short, TreeNode> nodes = new();
        masterNode = new();

        for (int i = 0; i < allUpgrades.Length; i++) {
            UpgradeType upgrade = allUpgrades[i];
            UpgradeType parentUpgrade = upgrade.previousUpgrade;

            TreeNode parentNode = parentUpgrade == null ? masterNode : nodes[parentUpgrade.id];
            TreeNode currentNode = new(upgrade, parentNode);

            nodes[upgrade.id] = currentNode;
            parentNode.Add(currentNode);
        }

        this.nodes = nodes.Values.ToArray();
    }

    public void InstantiateNodes() {
        masterNode.Instantiate(Vector2.zero);
    }

    private class TreeNode {
        public UpgradeType upgrade;

        public List<TreeNode> nextNodes = new();
        public TreeNode parentNode;

        public bool isMaster = false;

        public static Vector2 offset = Vector2.one;
        public float xHalfSize;

        public TreeNode(UpgradeType upgrade, TreeNode parentNode) {
            this.upgrade = upgrade;
            this.parentNode = parentNode;
        }

        public TreeNode() {
            upgrade = null;
            parentNode = null;
            isMaster = true;
        }

        public void Add(TreeNode nextNode) {
            nextNodes.Add(nextNode);
            xHalfSize = nextNodes.Count / 2f;
        }

        public void Instantiate(Vector2 position) {
            int nodes = nextNodes.Count;
            float xHalfLength = offset.x * nodes / 2f;

            string name = isMaster ? "master" : upgrade.name;
            GameObject nodeGO = new(name, typeof(SpriteRenderer));
            nodeGO.transform.position = position;

            for (int i = 0; i < nodes; i++) {
                TreeNode node = nextNodes[i];
                Vector2 nextPosition = new Vector2(i * offset.x - xHalfLength, offset.y) + position;
                node.Instantiate(nextPosition);
            }
        }
    }
}