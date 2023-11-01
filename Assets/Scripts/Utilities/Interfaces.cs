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