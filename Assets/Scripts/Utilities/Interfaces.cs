using Frontiers.Content;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable {
    public void Damage(float amount);

    public bool IsBuilding();
}

public interface IView {
    public PhotonView PhotonView { get; set; }
}

public interface IInventory {
    public Inventory GetInventory();
}

public interface IArmed {
    public void ConsumeAmmo(float amount);

    public bool CanConsumeAmmo(float amount);
}

public interface IMessager {
    public string GetName();
}

public interface IPowerable {
    public bool UsesPower();

    public bool ConsumesPower();

    public bool GeneratesPower();

    public bool StoresPower();

    public bool TransfersPower();

    public float GetPowerConsumption();

    public float GetPowerGeneration();

    public float GetPowerCapacity();

    public float GetStoredPower();

    public float GetMaxStorage();

    public void ChargePower(float amount);

    public void DischargePower(float amount);

    public void SetPowerPercent(float amount);

    public Vector2 GetPosition();

    public PowerGraph GetGraph();

    public void SetGraph(PowerGraph graph);

    public IPowerable[] GetConnections();

    public void ForceRemoveConnection(IPowerable powerable);
}