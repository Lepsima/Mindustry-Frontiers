using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using static PowerGraphManager;

public class PowerModule {
    public class PowerLineRenderer {
        public static GameObject linePrefab = AssetLoader.GetPrefab("power-line-prefab");
        public static float scale = 0.5f, spriteScale = 8f;

        public Transform instance;
        public PowerModule powerable1;
        public PowerModule powerable2;

        public PowerLineRenderer(PowerModule powerable1, PowerModule powerable2) {
            this.powerable1 = powerable1;
            this.powerable2 = powerable2;

            this.powerable1.powerLineRenderers.Add(this);
            this.powerable2.powerLineRenderers.Add(this);

            Update();
        }

        public void Destroy() {
            powerable1.powerLineRenderers.Remove(this);
            powerable2.powerLineRenderers.Remove(this);

            Object.Destroy(instance.gameObject);
        }

        private void GetPositions(out Vector2 v1, out Vector2 v2) {
            Vector2 v01 = powerable1.GetPosition();
            Vector2 v02 = powerable2.GetPosition();

            v1 = v01 + (powerable1.GetLineStartDistance() * scale * (v02 - v01).normalized);
            v2 = v02 + (powerable2.GetLineStartDistance() * scale * (v01 - v02).normalized);
        }

        public void Update() {
            GetPositions(out Vector2 start, out Vector2 end);

            // Parent transform
            instance = Object.Instantiate(linePrefab).transform;
            instance.localScale = Vector3.one * scale;

            // Get transforms
            Transform startTransform = instance.GetChild(0);
            Transform lineTransform = instance.GetChild(1);
            Transform endTransform = instance.GetChild(2);

            // Positions
            startTransform.position = start;
            lineTransform.position = Vector2.Lerp(start, end, 0.5f);
            endTransform.position = end;

            // Rotations
            startTransform.right = end - start;
            lineTransform.right = end - start;
            endTransform.right = start - end;

            // Scale
            float length = Vector2.Distance(start, end);
            lineTransform.localScale = new Vector3((length - scale * 0.5f) * spriteScale / scale, 1f, 1f);
        }
    }

    public Block block;
    public PowerGraph graph;
    public List<PowerModule> connections;
    public List<PowerLineRenderer> powerLineRenderers = new();

    public float powerPercent; // The current amount of power usage given to this block
    public float powerStored; // The current amount of power stored
    public float powerUsage; // The amount of power this block uses, negative = consumes, positive = generates
    public float powerStorage; // The amount of power this block can store

    public PowerModule(Block block, float usage, float storage) {
        this.block = block;
        powerUsage = usage;
        powerStorage = storage;
    }

    public void Initialize() {
        List<Connection> connections = block.GetConnections();
        this.connections = new List<PowerModule>(connections.Count);

        foreach (Connection connection in connections) {
            if (connection.isRanged) new PowerLineRenderer(this, connection.powerable);
            ConnectTo(connection.powerable);
        }

        HandlePowerModule(this);
    }

    public void Destroy() {
        HandleDisconnection(this);

        for (int i = connections.Count - 1; i >= 0; i--) DisconnectFrom(connections[i]);
        for (int i = powerLineRenderers.Count - 1; i >= 0; i--) powerLineRenderers[i].Destroy();
    }

    public bool Equals(PowerModule powerable) {
        return powerable.block.GetID() == block.GetID();
    }

    void ConnectTo(PowerModule powerable) {
        powerable.GetConnections().Add(this);
        connections.Add(powerable);
    }

    void DisconnectFrom(PowerModule powerable) {
        powerable.GetConnections().Remove(this);
        connections.Remove(powerable);
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

    public float GetConnectionDistance() {
        return block.Type.powerConnectionRange;
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

    public int GetFreeConections() {
        return block.Type.maxPowerConnections - connections.Count;
    }

    public float GetLineStartDistance() {
        return block.size * 0.75f;
    }

    public Vector2 GetPosition() {
        return block.GetPosition();
    }

    public PowerGraph GetGraph() {
        return graph;
    }

    public void SetGraph(PowerGraph graph) {
        this.graph = graph;
        block.transform.ChangeNumbers((int)graph.id);
    }

    /// <summary>
    /// Get the stored power connections
    /// </summary>
    public virtual List<PowerModule> GetConnections() {
        return connections;
    }

    /// <summary>
    /// Removes a valid connection from the array
    /// </summary>
    /// <param name="powerable">The connection to be removed</param>
    public void ForceRemoveConnection(PowerModule powerable) {
        if (connections.Contains(powerable)) connections.Remove(powerable);
    }
}