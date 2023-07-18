using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Frontiers.Content.Flags;
using Frontiers.Assets;
using Frontiers.Content;
using Frontiers.Content.Upgrades;

public class UpgradeTreeTest : MonoBehaviour {
    UpgradeTree upgradeTree;

    public Sprite nodeSprite;
    public Material nodeLineMaterial;

    public static UpgradeTreeTest instance;

    private void Awake() {
        instance = this;

        // Load assets and contents
        AssetLoader.LoadAssets();
        ContentLoader.LoadContents();

        // Create a new upgrade tree
        upgradeTree = new();
        upgradeTree.Instantiate();    
    }
}

public class UpgradeTree {
    public const float nodeUpSpacing = 2.25f;
    public const float nodeSideSpacing = 2f;
    public const float nodeSize = 1f;

    public static GameObject UINodePrefab;
    public Node masterNode;

    public UpgradeTree() {
        UINodePrefab = AssetLoader.GetPrefab("UIUpgradeNodePrefab");
        GenerateNodesFromUpgrades();
    }

    public void GenerateNodesFromUpgrades() {
        UpgradeType[] allUpgrades = UpgradeHandler.loadedUpgrades.Values.ToArray();

        Dictionary<short, Node> nodes = new();
        masterNode = new();

        for (int i = 0; i < allUpgrades.Length; i++) {
            UpgradeType upgrade = allUpgrades[i];
            UpgradeType parentUpgrade = upgrade.previousUpgrade;

            Node parentNode = parentUpgrade == null ? masterNode : nodes[parentUpgrade.id];
            Node currentNode = new(upgrade, parentNode);

            nodes[upgrade.id] = currentNode;
            parentNode.Add(currentNode);
        }
    }

    public void Instantiate() {
        masterNode.Instantiate(null, Vector2.zero);
    }

    public override string ToString() {
        return masterNode.ToString();
    }

    public class Node {
        public UpgradeType upgrade;

        public Transform transform;

        public List<Node> nextNodes = new();
        public Node parentNode;

        public bool isMaster = false;
        public float width;

        public Node(UpgradeType upgrade, Node parentNode) {
            this.upgrade = upgrade;
            this.parentNode = parentNode;
        }

        public Node() {
            upgrade = null;
            parentNode = null;
            isMaster = true;
        }

        public void Add(Node nextNode) {
            // Add the sent node to the next list and update it's spacing values
            nextNodes.Add(nextNode);
            nextNode.UpdateSpacing();
        }

        public void UpdateSpacing() {
            float totalNodeSize = 0;

            // Get this node width based on children nodes
            for (int i = 0; i < nextNodes.Count; i++) totalNodeSize += nextNodes[i].width + nodeSideSpacing;
            width = Mathf.Max(nodeSize, totalNodeSize - nodeSideSpacing);

            // If isn't master, update parent node
            if (!isMaster) parentNode.UpdateSpacing();
        }

        public void Instantiate(Transform parent, Vector2 position) {
            // Create this node gameobject
            transform = Object.Instantiate(UINodePrefab).transform;
            transform.GetComponent<UIUpgradeTreeNode>().Set(isMaster ? "master" : upgrade.name, parent, position, this);

            // Start from the left
            float widthProgress = width / -2f;

            for (int i = 0; i < nextNodes.Count; i++) {
                // Get the next node
                Node node = nextNodes[i];

                // Calculate the next node's position
                Vector2 nextPosition = new((node.width / 2f) + widthProgress, nodeUpSpacing);
                widthProgress += node.width + nodeSideSpacing;

                // Instantiate the next node
                node.Instantiate(transform, nextPosition);
            }
        }

        public override string ToString() {
            string value = upgrade.name;
            foreach (Node node in nextNodes) if (node.upgrade.IsResearched()) value += "," + node.ToString();
            return value;
        }
    }
}