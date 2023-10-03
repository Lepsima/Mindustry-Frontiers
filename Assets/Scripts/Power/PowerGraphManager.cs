using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content.Maps;

public static class PowerGraphManager {
    public static List<PowerGraph> graphs = new();

    public static void HandleIPowerable(IPowerable powerable) {
        List<IPowerable> connections = powerable.GetConnections();
        List<PowerGraph> connectedPowerGraphs = new();

        foreach(IPowerable connection in connections) {
            PowerGraph connectionGraph = GetPowerGraphFrom(connection);
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
                PowerGraph graph = connectedPowerGraphs[i];

                // Merge and remove from graphs list
                mainGraph.Handle(graph);
                graphs.Remove(graph);
            }
        }
    }

    public static PowerGraph GetPowerGraphFrom(IPowerable powerable) {
        foreach(PowerGraph graph in graphs) if (graph.Contains(powerable)) return graph;
        return null;
    }
}