using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content.Maps;
using System.Linq;

public static class PowerGraphManager {
    public static List<PowerGraph> graphs = new();

    public class Connection {
        public PowerModule powerable;
        public bool isRanged;

        public Connection(PowerModule powerable, bool isRanged) {
            this.powerable = powerable;
            this.isRanged = isRanged;
        }
    };

    public static List<Connection> GetConnections(this Block block) {
        float range = block.Type.powerConnectionRange;
        int rangedConnections = 0;

        Vector2 blockPosition = block.GetPosition();
        List<Connection> connections = new();

        // Get the closest powerable from each graph and check if it's valid
        foreach (PowerGraph graph in graphs) {
            PowerModule closest = graph.GetClosestInRange(blockPosition, range);
            if (closest == null) continue;

            connections.Add(new(closest, true));
            rangedConnections++;
        }

        // Get all the adjacent powerables to discard ranged connections
        List<PowerModule> adjacentConnections = MapManager.Map.GetAdjacentPowerBlocks(block);

        foreach (PowerModule powerable in adjacentConnections) {
            Connection other = GetGraphEqualConnection(powerable);
  
            if (other != null) {
                // If the connection was added already, set it to not ranged
                other.isRanged = false;
                rangedConnections--;

            } else if (!ContainsGraph(powerable.GetGraph())){
                // If the connection's graph hasn't been added already, add it as not ranged
                connections.Add(new(powerable, false));
            }
        }

        // Positive if is higher than expected
        int maxConnections = block.Type.maxPowerConnections == 0 ? 99 : block.Type.maxPowerConnections;
        int diff = rangedConnections - maxConnections;

        // return if the connection amount is below expected
        if (diff <= 0) return connections;

        // If there are too many connections, remove till satisfied
        for (int i = connections.Count - 1; i >= 0; i--) {
            if (diff <= 0) break;

            Connection connection = connections[i];
            if (connection.isRanged) {
                connections.Remove(connection);
                diff--;
            }
        }

        return connections;

        Connection GetConnection(PowerModule powerable) {
            foreach (Connection connection in connections) if (connection.powerable.Equals(powerable)) return connection;
            return null;
        }

        Connection GetGraphEqualConnection(PowerModule powerable) {
            foreach (Connection connection in connections) if (connection.powerable.GetGraph().Equals(powerable.GetGraph())) return connection;
            return null;
        }

        bool ContainsGraph(PowerGraph graph) {
            foreach (Connection connection in connections) if (connection.powerable.GetGraph().Equals(graph)) return true;
            return false;
        }
    }

    public static void HandlePowerModule(PowerModule powerable) {
        List<PowerModule> connections = powerable.GetConnections();
        List<PowerGraph> connectedPowerGraphs = new();

        foreach (PowerModule connection in connections) {
            PowerGraph connectionGraph = connection.GetGraph();
            if (connectionGraph != null) connectedPowerGraphs.Add(connectionGraph);
        }

        if (connectedPowerGraphs.Count == 0) {
            // Not connected to anyone, should create a new graph
            new PowerGraph(powerable);

        } else {
            // Connected to at least 1 graph, should add itself to it
            connectedPowerGraphs[0].Handle(powerable);
        }

        if (connectedPowerGraphs.Count > 1) {
            // If connected to multiple graphs, should merge them together
            PowerGraph mainGraph = connectedPowerGraphs[0];

            for (int i = 1; i < connectedPowerGraphs.Count; i++) {
                mainGraph.Handle(connectedPowerGraphs[i]);       
            }
        }
    }

    public static void HandleDisconnection(PowerModule powerable) {
        // Get graph and connections
        List<PowerModule> connections = powerable.GetConnections();
        PowerGraph graph = powerable.GetGraph();

        // Loop through all connections
        foreach(PowerModule connection in connections) {

            if (connection.GetGraph() != graph) continue;

            // Create new graph
            PowerGraph newGraph = new PowerGraph(connection);

            // A queue with all the nodes that need to be evaluated
            Queue<PowerModule> queue = new();
            queue.Enqueue(connection);

            while (queue.Count > 0) {
                // Get the next powerable and it's connections
                PowerModule child = queue.Dequeue();
                List<PowerModule> childConnections = child.GetConnections();

                // Loop through all connections
                foreach (PowerModule childConnection in childConnections) {

                    // If isn't the removed powerable and hasnt been added already, set child's graph to new graph
                    if (childConnection == powerable || childConnection.GetGraph() == newGraph) continue;

                    newGraph.Handle(childConnection);
                    queue.Enqueue(childConnection);
                }
            }
        }

        // How the actual fuck did i forget to add this
        graphs.Remove(graph);
    }

    public static void UpdatePowerGraphs() {
        foreach (PowerGraph powerGraph in graphs) { 
            powerGraph.Update();
            //Debug.Log(powerGraph.powerUsage);
        }
    }
}