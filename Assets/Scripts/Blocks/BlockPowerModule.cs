using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockPowerModule : IPowerable {
    public Block block;
    public PowerGraph graph;
    public IPowerable[] connections;

    public float powerPercent; // The current amount of power usage given to this block
    public float powerStored; // The current amount of power stored
    public float powerUsage; // The amount of power this block uses, negative = consumes, positive = generates
    public float powerStorage; // The amount of power this block can store

    public BlockPowerModule(float usage, float storage) {
        powerUsage = usage;
        powerStorage = storage;

        CreateConnections();
    }

    public bool UsesPower() {
        return powerUsage != 0 || powerStorage != 0;
    }

    public bool ConsumesPower() {
        return powerUsage < 0;
    }

    public bool GeneratesPower() {
        return powerUsage > 0;
    }

    public bool StoresPower() {
        return powerStorage > 0;
    }

    public bool TransfersPower() {
        return block.Type.transfersPower;
    }

    public float GetPowerConsumption() {
        // Invert because consumption is stored as negative but operated as positive
        return -powerUsage;
    }

    public float GetPowerGeneration() {
        return powerUsage;
    }

    public float GetPowerCapacity() {
        return powerStorage - powerStored;
    }

    public float GetStoredPower() {
        return powerStored;
    }

    public float GetMaxStorage() {
        return powerStorage;
    }

    public void ChargePower(float amount) {
        // Dont pass a negative value plsss
        powerStored = Mathf.Min(powerStored + amount, powerStorage);
    }

    public void DischargePower(float amount) {
        // Dont pass a negative value plsss
        powerStored = Mathf.Max(powerStored - amount, 0);
    }

    public void SetPowerPercent(float amount) {
        powerPercent = amount;
    }

    public PowerGraph GetGraph() {
        return graph;
    }

    public void SetGraph(PowerGraph graph) {
        this.graph = graph;
    }

    /// <summary>
    /// Creates the power connection array, doesn't actually connect any power graphs together
    /// </summary>
    public void CreateConnections() {
        // A list of all the graphs that this block will be connected to
        List<PowerGraph> connectedGraphs = new();

        // A list of all the blocks that transfer power by touch
        List<IPowerable> connections = MapManager.Map.GetAdjacentPowerBlocks(block);

        // Add all the touch transfer blocks graph's to the graph list
        foreach(IPowerable powerable in connections) {
            // If graph is null or already in the list, skip
            PowerGraph otherGraph = powerable.GetGraph();
            if (otherGraph == null || connectedGraphs.Contains(otherGraph)) continue;
            
            // Add to list
            connectedGraphs.Add(otherGraph);
        }

        // All the entities that are in connection range of this block !!INCLUDES UNITS AND ALL BLOCKS!!
        List<Entity> rangedConnections = block.Type.powerConnectionRange > 0 ? MapManager.Map.GetAllEntitiesInRange(block.GetPosition(), block.Type.powerConnectionRange) : null;

        // Filter the ranged connections to valid power blocks
        foreach (Entity entity in rangedConnections) {

            // If the entity is a block, uses power, isn't in the adjacent connection list, and it's power graph hasnt been registered previously, include as a connection
            if (entity is Block other && other.UsesPower() && !connections.Contains(other.powerModule) && !connectedGraphs.Contains(other.powerModule.GetGraph())) {

                // Add to connection list and also add it's power graph to the list
                connections.Add(other.powerModule);
                connectedGraphs.Add(other.powerModule.GetGraph());
            }
        }

        // Convert to array
        this.connections = connections.ToArray();
    }

    /// <summary>
    /// Get the stored power connections
    /// </summary>
    public virtual IPowerable[] GetConnections() {
        return connections;
    }

    /// <summary>
    /// Removes a valid connection from the array
    /// </summary>
    /// <param name="powerable">The connection to be removed</param>
    public void ForceRemoveConnection(IPowerable powerable) {
        if (!connections.Contains(powerable)) return;

        // Inneficient solution, live with it or fix it
        List<IPowerable> powerables = connections.ToList();
        powerables.Remove(powerable);
        connections = powerables.ToArray();
    }
}