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

        AssetLoader.LoadAssets();
        ContentLoader.LoadContents();

        upgradeTree = new();
        upgradeTree.Instantiate();
    }
}

public class UpgradeTree {
    Node[] nodes;
    Node masterNode;

    public UpgradeTree() {
        Generate();
    }

    public void Generate() {
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

        this.nodes = nodes.Values.ToArray();
    }

    public void Instantiate() {
        masterNode.Instantiate(null, Vector2.zero);
    }

    public class Node {
        public const float nodeUpSpacing = 2.25f;
        public const float nodeSideSpacing = 2f;
        public const float nodeSize = 1f;

        public UpgradeType upgrade;

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
            nextNodes.Add(nextNode);
            nextNode.UpdateSpacing();
        }

        public void UpdateSpacing() {
            float totalNodeSize = 0;

            // Get this node width based on children nodes
            for (int i = 0; i < nextNodes.Count; i++) totalNodeSize += nextNodes[i].width + nodeSideSpacing;
            width = Mathf.Max(nodeSize, totalNodeSize - nodeSideSpacing);

            // If has parent, update it
            if (parentNode != null) parentNode.UpdateSpacing();
        }

        public void Instantiate(Transform parent, Vector2 position) {
            string name = isMaster ? "master" : upgrade.name;
            Transform nodeTransform = new GameObject(name, typeof(SpriteRenderer), typeof(LineRenderer)).transform;

            nodeTransform.parent = parent;
            nodeTransform.localPosition = position;

            SpriteRenderer spriteRenderer = nodeTransform.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = UpgradeTreeTest.instance.nodeSprite;
            spriteRenderer.sortingOrder = 1;

            if (parent != null) {
                LineRenderer lineRenderer = nodeTransform.GetComponent<LineRenderer>();
                lineRenderer.SetPositions(new Vector3[2] { parent.transform.position, nodeTransform.position });
                lineRenderer.material = UpgradeTreeTest.instance.nodeLineMaterial;
            }

            // Start with a side space
            float widthProgress = width / -2f;

            for (int i = 0; i < nextNodes.Count; i++) {
                Node node = nextNodes[i];
                Vector2 nextPosition = new((node.width / 2f) + widthProgress, nodeUpSpacing);

                // Add node width and a space to the width progress;
                widthProgress += node.width + nodeSideSpacing;

                node.Instantiate(nodeTransform, nextPosition);
            }
        }
    }
}