using Frontiers.Content;
using Photon.Pun;

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
    public Weapon GetWeaponByID(byte ID);

    public void ConsumeAmmo(float amount);

    public bool CanConsumeAmmo(float amount);
}

public interface IMessager {
    public string GetName();
}