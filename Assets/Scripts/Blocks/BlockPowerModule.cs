using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockPowerModule : MonoBehaviour, IPowerable {
    public Block block;
    public float powerPercent; // The current amount of power usage given to this block
    public float powerStored; // The current amount of power stored
    public float powerUsage; // The amount of power this block uses, negative = consumes, positive = generates
    public float powerStorage; // The amount of power this block can store

    public BlockPowerModule(float usage, float storage) {
        this.powerUsage = usage;
        this.powerStorage = storage;
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
        return Type.transfersPower;
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

    public virtual List<IPowerable> GetConnections() {
        List<IPowerable> connections = MapManager.Map.GetAdjacentPowerBlocks(this);
        List<Entity> rangedConnections = block.Type.powerConnectionRange > 0 ? MapManager.Map.GetAllEntitiesInRange(block.GetPosition(), block.Type.powerConnectionRange) : null;

        foreach (Entity entity in rangedConnections) if (entity is Block other && other..UsesPower()) connections.Add(other);
        return connections;
    }
}