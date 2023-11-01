using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content.Maps;

public static class PowerGraphManager {
    public static List<PowerGraph> graphs = new();

    public static void HandleIPowerable(IPowerable powerable) {
        IPowerable[] connections = powerable.GetConnections();
        List<PowerGraph> connectedPowerGraphs = new();

        foreach (IPowerable connection in connections) {
            PowerGraph connectionGraph = connection.GetGraph();
            if (connectionGraph != null) connectedPowerGraphs.Add(connectionGraph);
        }

        if (connectedPowerGraphs.Count == 0) {
            // Not connected to anyone, should create a new graph
            graphs.Add(new PowerGraph(powerable));

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

    public static void HandleDisconnection(IPowerable powerable) {
        // Get graph and connections
        IPowerable[] connections = powerable.GetConnections();
        PowerGraph graph = powerable.GetGraph();

        // Loop through all connections
        foreach(IPowerable connection in connections) {

            if (connection.GetGraph() != graph) continue;

            // Create new graph
            PowerGraph newGraph = new(connection);

            // A queue with all the nodes that need to be evaluated
            Queue<IPowerable> queue = new();
            queue.Enqueue(connection);

            while (queue.Count > 0) {
                // Get the next powerable and it's connections
                IPowerable child = queue.Dequeue();
                IPowerable[] childConnections = child.GetConnections();

                // Loop through all connections
                foreach (IPowerable childConnection in childConnections) {

                    // If isn't the removed powerable and hasnt been added already, set child's graph to new graph
                    if (childConnection != powerable && childConnection.GetGraph() != newGraph) {
                        newGraph.Handle(childConnection);
                        queue.Enqueue(childConnection);
                    }
                }
            }
        }
    }
}