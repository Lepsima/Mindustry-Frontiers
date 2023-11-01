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

    public BlockPowerModule(Block block, float usage, float storage) {
        this.block = block;
        powerUsage = usage;
        powerStorage = storage;
    }

    public void Init() {
        CreateConnections();

        Vector2 start = GetPosition();

        foreach (IPowerable powerable in connections) {
            Vector2 end = powerable.GetPosition();

            GameObject line = new("lInE", typeof(LineRenderer));
            LineRenderer lnRe = line.GetComponent<LineRenderer>();

            lnRe.SetPosition(0, start);
            lnRe.SetPosition(1, end);
        }
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

    public Vector2 GetPosition() {
        return block.GetPosition();
    }

    public PowerGraph GetGraph() {
        return graph;
    }

    public void SetGraph(PowerGraph graph) {
        this.graph = graph;
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