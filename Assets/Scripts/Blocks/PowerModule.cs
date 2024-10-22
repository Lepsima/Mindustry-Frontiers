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
        public static float scale = 0.5f, spriteScale = 12f;

        public Transform instance;
        public PowerModule powerable1;
        public PowerModule powerable2;

        public SpriteRenderer spriteRenderer1, spriteRenderer2, spriteRenderer3;
        public static Color unpoweredColor, poweredColor;

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

            spriteRenderer1 = startTransform.GetComponent<SpriteRenderer>();
            spriteRenderer2 = lineTransform.GetComponent<SpriteRenderer>();
            spriteRenderer3 = endTransform.GetComponent<SpriteRenderer>();
        }

        public static void CalculateColor(float velocity) {
            float alpha = (Mathf.Sin(Time.time * velocity) * 0.5f + 1f) * 0.5f + 0.25f;

            unpoweredColor = new(1f, 0.3160377f, 0.3160377f);
            poweredColor = new(1f, 1f, 1f, alpha);
        }

        public void HandleGlow(bool powered) {
            Color color = powered ? poweredColor : unpoweredColor;
            spriteRenderer1.color = color;
            spriteRenderer2.color = color;
            spriteRenderer3.color = color;
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

    public PowerModule(Block block) {
        this.block = block;
        powerStorage = block.Type.powerStorage;
    }

    public void UpdatePowerLines() {
        bool powered = graph.powerUsage > 0f;
        foreach (PowerLineRenderer powerLineRenderer in powerLineRenderers) powerLineRenderer.HandleGlow(powered);
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
        return block.Type.consumesPower;
    }

    public bool GeneratesPower() {
        return block.Type.generatesPower;
    }

    public bool StoresPower() {
        return block.Type.powerStorage > 0;
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
        // If it has 0 max connections it works as unlimited connections, returns 99 just in case, but 1 should also work
        int maxConnections = block.Type.maxPowerConnections;
        return maxConnections == 0 ? 99 : maxConnections - connections.Count;
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