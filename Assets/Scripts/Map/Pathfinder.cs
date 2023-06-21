using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Content.Maps;
using System.Linq;

namespace Frontiers.Content.Maps {
    public class Pathfinder {
        private static Vector2Int[] childrenPositions;
        private static Tilemap tilemap;

        private static Node startNode;
        private static Node endNode;

        private static List<Node> openList = new();
        private static List<Node> closedList = new();

        public static void Initialize() {
            tilemap = MapManager.Map.tilemap;
            childrenPositions = new Vector2Int[8] { new(0, -1), new(0, 1), new(-1, 0), new(1, 0), new(-1, -1), new (-1, 1), new(1, -1), new(1, 1) };
        }

        public static List<Vector2Int> Pathfind(Vector2Int startPosition, Vector2Int endPosition) {
            // Initialize base nodes
            startNode = new Node(null, startPosition);
            endNode = new Node(null, endPosition);

            // Initialize lists
            openList = new();
            closedList = new();

            // Add the first node
            openList.Add(startNode);

            while (openList.Count > 0) {
                Node currentNode = openList[0];

                // Get the next node, the lowest cost one
                for (int i = 0; i < openList.Count; i++) {
                    Node node = openList[i];
                    if (node.f < currentNode.f) currentNode = node;
                }

                // Change from list the current node
                openList.Remove(currentNode);
                closedList.Add(currentNode);

                // Return path if current node is the end node
                if (currentNode.Equals(endNode)) {
                    List<Vector2Int> path = new();
                    Node current = currentNode;

                    // Trace back through the list
                    while(current != null) {
                        path.Add(current.position);
                        current = current.parent;
                    }

                    // Reverse the path
                    path.Reverse();
                    return path;
                }

                // Find all the valid children for the current node
                List<Node> children = FindChildren(currentNode);

                // Evaluate all the found children of the current node
                for (int i = 0; i < 8; i++) EvaluateChild(children[i]);
            }

            return null;
        }

        private static List<Node> FindChildren(Node parent) {
            List<Node> children = new();

            for (int i = 0; i < 8; i++) {
                Vector2Int childPosition = childrenPositions[i] + parent.position;

                // Check if the node is out of bounds
                if (childPosition.x >= tilemap.size.x || childPosition.x < 0 || childPosition.y >= tilemap.size.y || childPosition.y < 0) {
                    continue;
                }

                // Check if the node is valid
                if (tilemap.GetTile(childPosition).IsSolid()) {
                    continue;
                }

                // Create new node and add to the list
                children.Add(new(parent, childPosition));
            }

            return children;
        }

        private static void EvaluateChild(Node child) {
            // Check if the node has been already evaluated
            foreach (Node closedNode in closedList) {
                if (child.Equals(closedNode)) return;
            }

            // Calculate g
            child.g = child.parent.g + 1;

            // Calculate h
            float x = (child.position.x - endNode.position.x) ^ 2;
            float y = (child.position.y - endNode.position.y) ^ 2;
            child.h = x + y;

            // Calculate f
            child.f = child.g + child.h;

            // Check if this is the shortest path and override longer ones
            foreach (Node openNode in openList) {
                if (child.Equals(openNode)) {
                    if (child.g <= openNode.g) openNode.Override(child);
                    return;
                }
            }

            // Add the child to the open list
            openList.Add(child);
        }

        public class Node {
            public Node parent;
            public Vector2Int position;
            public float f, h;
            public int g;

            public Node(Node parent, Vector2Int position) {
                this.parent = parent;
                this.position = position;
            }

            public bool Equals(Node other) {
                return position == other.position;
            }

            public void Override(Node other) {
                parent = other.parent;
                position = other.position;
                f = other.f;
                h = other.h;
                g = other.g;
            }
        }
    }
}
