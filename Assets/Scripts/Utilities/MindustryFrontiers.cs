using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Settings;
using Frontiers.Squadrons;
using Frontiers.Teams;

namespace Frontiers.Settings {
    public static class State {
        /// <summary>
        /// The time interval each entity should sync their data to other players
        /// </summary>
        public const float SYNC_TIME = 5f;
    }
}

namespace Frontiers.Squadrons {
    public enum Action {
        Idle,
        Attack,
        Move,
        Land,
        TakeOff
    }

    public struct Order {
        public Action action;

        public Vector2 actionPosition;
        public Transform actionTarget;

        public Order(Action action, Vector2 actionPosition, Transform actionTarget = null) {
            this.action = action;
            this.actionPosition = actionPosition;
            this.actionTarget = actionTarget;
        }

        public Vector2 GetActionPosition() => actionPosition == Vector2.zero && actionTarget ? (Vector2)actionTarget.position : actionPosition;
    }

    public struct OrderSeq {
        public List<Order> orderList;

        public OrderSeq(List<Order> orderList) {
            this.orderList = orderList;
        }

        public void OrderComplete() {
            if (orderList.Count == 0) return;
            orderList.RemoveAt(0);
        }

        public Order GetOrder() => orderList.Count == 0 ? new Order(Action.Idle, Vector2.zero) : orderList[0];
    }

    public class DemoOrderSeq {
        public static OrderSeq takeOffAndLand;

        public static void Load() {
            takeOffAndLand = new OrderSeq(new List<Order>() {
                new Order(Action.TakeOff, Vector2.zero),
                new Order(Action.Move, new Vector2(10f, 50f)),
                new Order(Action.Land, Vector2.zero)
            }); 
        }
    }
}

namespace Frontiers.Teams {
    public static class TeamUtilities {
        public static readonly Color LocalTeamColor = new Color(1f, 0.827451f, 0.4980392f);
        public static readonly Color EnemyTeamColor = new Color(0.9490196f, 0.3333333f, 0.3333333f);

        public static List<CoreBlock> LocalCoreBlocks = new List<CoreBlock>();
        public static List<CoreBlock> EnemyCoreBlocks = new List<CoreBlock>();

        public static int GetTeamLayer(byte teamCode, bool ignore = false) => LayerMask.NameToLayer((ignore ? "IgnoreTeam" : "CollideTeam") + teamCode);

        public static int GetTeamMask(byte teamCode, bool ignore = false) => LayerMask.GetMask((ignore ? "IgnoreTeam" : "CollideTeam") + teamCode);

        public static int GetEnemyTeamLayer(byte teamCode, bool ignore = false) => GetTeamLayer(GetEnemyTeam(teamCode).Code, ignore);

        public static int GetEnemyTeamMask(byte teamCode, bool ignore = false) => GetTeamMask(GetEnemyTeam(teamCode).Code, ignore);

        public static void AddCoreBlock(CoreBlock coreBlock) {
            if (coreBlock.IsLocalTeam()) LocalCoreBlocks.Add(coreBlock);
            else EnemyCoreBlocks.Add(coreBlock);
        }

        public static void RemoveCoreBlock(CoreBlock coreBlock) {
            if (LocalCoreBlocks.Contains(coreBlock)) LocalCoreBlocks.Remove(coreBlock);
            if (EnemyCoreBlocks.Contains(coreBlock)) EnemyCoreBlocks.Remove(coreBlock);
        }

        public static CoreBlock GetClosestCoreBlock(Vector2 position, byte teamCode) => teamCode == GetLocalPlayerTeam().Code ? GetClosestAllyCoreBlock(position) : GetClosestEnemyCoreBlock(position);

        public static CoreBlock GetClosestAllyCoreBlock(Vector2 position) {
            float closestDistance = 9999f;
            CoreBlock closestCoreBlock = null;

            foreach(CoreBlock coreBlock in LocalCoreBlocks) {
                float distance = Vector2.Distance(coreBlock.GetGridPosition(), position);

                if (distance <= closestDistance) {
                    closestDistance = distance;
                    closestCoreBlock = coreBlock;
                }
            }

            return closestCoreBlock;
        }

        public static CoreBlock GetClosestEnemyCoreBlock(Vector2 position) {
            float closestDistance = 9999f;
            CoreBlock closestCoreBlock = null;

            foreach (CoreBlock coreBlock in EnemyCoreBlocks) {
                float distance = Vector2.Distance(coreBlock.GetGridPosition(), position);

                if (distance <= closestDistance) {
                    closestDistance = distance;
                    closestCoreBlock = coreBlock;
                }
            }

            return closestCoreBlock;
        }

        public static bool IsMaster() => PhotonNetwork.IsMasterClient;

        public static PhotonTeam GetLocalPlayerTeam() => PhotonNetwork.LocalPlayer.GetPhotonTeam();

        public static PhotonTeam GetEnemyTeam(byte code) => code == 1 ? GetTeamByCode(2) : GetTeamByCode(1);

        public static PhotonTeam GetDefaultTeam() => GetTeamByCode(1);

        public static PhotonTeam GetTeamByCode(byte code) {
            RoomManager.Instance.photonTeamsManager.TryGetTeamByCode(code, out PhotonTeam team);
            return team;
        }

        public static Player[] TryGetTeamMembers(byte code) {
            RoomManager.Instance.photonTeamsManager.TryGetTeamMembers(code, out Player[] members);
            return members;
        }

    }
}

namespace Frontiers.Assets {
    public static class Assets {
        private static Sprite[] sprites;
        private static GameObject[] prefabs;
        private static UnityEngine.Tilemaps.TileBase[] tiles;


        private static UnityEngine.Object[] assets;

        public static void LoadAssets() {
            sprites = Resources.LoadAll<Sprite>("Sprites");
            prefabs = Resources.LoadAll<GameObject>("Prefabs");
            tiles = Resources.LoadAll<UnityEngine.Tilemaps.TileBase>("Tiles");
            assets = Resources.LoadAll<UnityEngine.Object>("");

            Debug.Log("All Assets Loaded!");
        }

        public static Sprite GetSprite(string name, bool suppressWarnings = false) {
            foreach (Sprite sprite in sprites) if (sprite.name == name) return sprite;

            if (!suppressWarnings) Debug.LogWarning("No sprite was found with the name: " + name);
            return null;
        }

        public static GameObject GetPrefab(string name, bool suppressWarnings = false) {
            foreach (GameObject prefab in prefabs) if (prefab.name == name) return prefab;

            if (!suppressWarnings) Debug.LogWarning("No prefab was found with the name: " + name);
            return null;
        }

        /*public static T GetAsset<T>(string name, bool suppressWarnings = false) where T : Object {
            foreach (UnityEngine.Object prefab in assets) if (prefab.name == name) return prefab;

            if (!suppressWarnings) Debug.LogWarning("No prefab was found with the name: " + name);
            return null;
        }*/
    }
}

namespace Frontiers.Content {


    public static class ContentLoader {
        public static List<Content> contentMap;

        public static void LoadContent() {
            contentMap = new List<Content>();

            Items.Load();
            Bullets.Load();
            Weapons.Load();
            Units.Load();
            Blocks.Load();

            Debug.Log(contentMap.Count + " Contents Loaded!");
        }

        public static void HandleContent(Content content) {
            if (GetContentByName(content.name) != null) throw new ArgumentException("Two content objects cannot have the same name! (issue: '" + content.name + "')");

            contentMap.Add(content);
        }

        public static Content GetContentById(short id) {
            foreach (Content content in contentMap) if (content.id == id) return content;
            return null;
        }

        public static Content GetContentByName(string name) {
            foreach (Content content in contentMap) if (content.name == name) return content;
            return null;
        }
    }

    public abstract class Content {
        public string name;
        public short id;
        public Sprite sprite;

        public Content(string name) {
            this.name = name;
            sprite = Assets.Assets.GetSprite(name);

            id = (short)ContentLoader.contentMap.Count;
            ContentLoader.HandleContent(this);
        }
    }

    #region - Blocks -

    public class BlockType : Content {
        public Sprite teamSprite, topSprite, bottomSprite;
        public ItemList buildCost;
        public WeaponMount weapon;
        public Type type;

        public bool beakable = true, solid = true;
        public float health = 100;
        public int size = 1, unitCapacity = 0;

        public int itemCapacity;
        public CraftPlan craftPlan;
        public UnitPlan unitPlan;

        public Vector2[] landPositions;

        public BlockType(string name, Type type) : base(name) {
            this.type = type;

            teamSprite = Assets.Assets.GetSprite(name + "-team", true);
            topSprite = Assets.Assets.GetSprite(name + "-top", true);
            bottomSprite = Assets.Assets.GetSprite(name + "-bottom", true);
        }
    }

    public class Blocks {
        public const BlockType none = null;
        public static BlockType copperWall, copperWallLarge, coreShard, landingPad, tempest, airFactory;

        public static void Load() {
            copperWall = new BlockType("copper-wall", typeof(Block)) {
                buildCost = new ItemList(new ItemStack(Items.copper, 6)),
                health = 140
            };

            copperWallLarge = new BlockType("copper-wall-large", typeof(Block)) {
                buildCost = new ItemList(new ItemStack(Items.copper, 24)),
                health = 600,
                size = 2
            };

            coreShard = new BlockType("core-shard", typeof(CoreBlock)) {
                buildCost = new ItemList(new Dictionary<Item, ItemStack>() {
                    { Items.copper, new ItemStack(Items.copper, 1000) },
                    { Items.lead, new ItemStack(Items.lead, 500) },
                    { Items.titanium, new ItemStack(Items.titanium, 100) },
                }),

                beakable = false,
                health = 1600,
                size = 3,

                itemCapacity = 1000
            };

            landingPad = new BlockType("landingPad", typeof(LandPadBlock)) {
                buildCost = new ItemList(new Dictionary<Item, ItemStack>() {
                    { Items.copper, new ItemStack(Items.copper, 250) },
                    { Items.titanium, new ItemStack(Items.titanium, 75) },
                }),

                health = 250,
                size = 3,
                solid = false,
                unitCapacity = 4,

                landPositions = new Vector2[] {
                    new Vector2(1, 1),
                    new Vector2(1, 2),
                    new Vector2(2, 1),
                    new Vector2(2, 2)
                }
            };

            tempest = new BlockType("tempest", typeof(TurretBlock)) {
                weapon = new WeaponMount(Weapons.tempestWeapon, Vector2.zero),
                health = 230f,
                size = 2,
            };

            airFactory = new BlockType("air-factory", typeof(UnitFactoryBlock)) {
                unitPlan = new UnitPlan(Units.flare, 4f, new ItemStack[1] {
                    new ItemStack(Items.silicon, 20)
                }),

                health = 250f,
                size = 3,
                itemCapacity = 50
            };
        }
    }

    #endregion

    #region - Units -

    public class UnitType : Content {
        public WeaponMount[] weapons;

        public Sprite cellSprite;
        public Type type;

        // Base stats
        public float health = 100f;

        // Movement
        public float velocityCap = 2f, accel = 1f, drag = 1f, bankAmount = 25f, bankSpeed = 5f, rotationSpeed = 90f;
        public bool canVTOL = false;

        // Targeting
        public float range = 15f, fov = 95;

        // Consumables
        public float fuelCapacity = 60f, fuelConsumption = 1.5f, fuelRefillRate = 7.5f;
        public int itemCapacity = 20;


        public UnitType(string name, Type type) : base(name) {
            this.type = type;

            cellSprite = Assets.Assets.GetSprite(name + "-cell");
        }
    }

    public class Units {
        public const UnitType none = null;
        public static UnitType flare;

        public static void Load() {
            flare = new UnitType("flare", typeof(Unit)) {
                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.smallAutoWeapon, new Vector2(0.43675f, 0.15f), true),
                },

                health = 75,
                velocityCap = 4f,
                accel = 1.75f,
                drag = 1f,
                rotationSpeed = 80f,
                bankAmount = 30f,
                range = 25f,
                fov = 90f
            };
        }
    }

    #endregion

    #region - Weapons -

    public class WeaponType : Content {
        public Type type;
        public BulletType bulletType;
        public Vector2 shootOffset = Vector2.zero;
        public int clipSize = 50;
        public float maxDeviation = 5f, recoil = 0.75f, returnSpeed = 4f, shootTime = 1f, reloadTime = 1f, rotateSpeed = 90f;

        public WeaponType(string name, Type type, BulletType bulletType) : base(name) {
            this.type = type;
            this.bulletType = bulletType;
        }

        public float Range { get => bulletType.speed * bulletType.lifeTime; }
    }

    public class Weapons {
        public const Weapon none = null;
        public static WeaponType smallWeapon, smallAutoWeapon, tempestWeapon;

        public static void Load() {
            smallWeapon = new WeaponType("small-weapon", typeof(KineticWeapon), Bullets.basicBulletType) {
                shootOffset = new Vector2(0, 0.37f),
                recoil = 0f,
                returnSpeed = 1f,
                clipSize = 5,
                shootTime = 0.5f,
                reloadTime = 2.25f
            };

            smallAutoWeapon = new WeaponType("small-auto-weapon", typeof(RaycastWeapon), Bullets.instantBulletType) {
                shootOffset = new Vector2(0, 0.37f),
                recoil = 0f,
                returnSpeed = 1f,
                clipSize = 25,
                shootTime = 0.15f,
                reloadTime = 5f
            };

            tempestWeapon = new WeaponType("tempest-weapon", typeof(RaycastWeapon), Bullets.instantBulletType) {
                shootOffset = new Vector2(0, 0.5f),
                recoil = 0.1f,
                returnSpeed = 2f,
                clipSize = 15,
                shootTime = 0.075f,
                reloadTime = 3f,
                rotateSpeed = 90f
            };
        }
    }

    public class BulletType : Content {
        public BulletClass bulletClass;
        public float damage = 10f, speed = 2f, lifeTime = 2f;

        public BulletType(string name, BulletClass bulletClass) : base(name) {
            this.bulletClass = bulletClass;
        }
    }

    public class Bullets {
        public const BulletType none = null;
        public static BulletType basicBulletType, instantBulletType;

        public static void Load() {
            basicBulletType = new BulletType("BasicBulletType", BulletClass.ConstantPhysical) {
                damage = 2f,
                speed = 100f
            };

            instantBulletType = new BulletType("InstantBulletType", BulletClass.Instant) {
                damage = 2.5f,
                speed = 150f
            };
        }
    }

    #endregion

    #region - Items -

    public class Item : Content {
        public Color color;
        public float explosiveness = 0, flammability = 0, radioactivity = 0, charge = 0;
        public float hardness = 0, cost = 1;

        public bool lowPriority = false, buildable = true;

        public Item(string name, Color color) : base(name) {
            this.color = color;
        }
    }

    public class Items {
        public static Item copper, lead, titanium, coal, graphite, metaglass, sand, silicon, thorium;

        public static void Load() {
            copper = new Item("copper", new Color(0xD9, 0x9D, 0x73)) {
                hardness = 1,
                cost = 0.5f
            };

            lead = new Item("lead", new Color(0x8c, 0x7f, 0xa9)) {
                hardness = 1,
                cost = 0.7f
            };

            metaglass = new Item("metaglass", new Color(0xeb, 0xee, 0xf5)) {
                cost = 1.5f
            };

            graphite = new Item("graphite", new Color(0xb2, 0xc6, 0xd2)) {
                cost = 1f
            };

            sand = new Item("sand", new Color(0xf7, 0xcb, 0xa4)) {
                lowPriority = true,
                buildable = false
            };

            coal = new Item("coal", new Color(0x27, 0x27, 0x27)) {
                explosiveness = 0.2f,
                flammability = 1f,
                hardness = 2,
                buildable = false
            };

            titanium = new Item("titanium", new Color(0x8d, 0xa1, 0xe3)) {
                hardness = 3,
                cost = 1f
            };

            thorium = new Item("thorium", new Color(0xf9, 0xa3, 0xc7)) {
                explosiveness = 0.2f,
                hardness = 4,
                radioactivity = 1f,
                cost = 1.1f
            };

            silicon = new Item("silicon", new Color(0x53, 0x56, 0x5c)) {
                cost = 0.8f
            };
        }
    }

    #endregion

    #region - Structures - 

    public enum BulletClass {
        Instant,          //Laser, raycast bulletType
        ConstantPhysical, //Basic bullet, speed keeps constant
        SlowDownPhysical, //Grenade or similar, speed is afected by drag
        HomingPhysical    //Missile bullet type, once fired it's fully autonomous
    }

    /// <summary>
    /// Stores the amount of a defined item
    /// </summary>
    public class ItemStack {
        public Item item;
        public int amount;
        public int amountCap;

        public bool IsEmpty() => item == null || amount == 0;

        public ItemStack(Item item, int amount = 0, int amountCap = -1) {
            if (item == null) item = Items.copper;

            this.item = item;
            this.amountCap = amountCap;
            this.amount = 0;

            if (amount != 0) AddAmount(amount);
        }

        public ItemStack AddAmount(int amount) {
            this.amount += amount;

            if (!Lower()) {
                int extraAmount = this.amount - amountCap;
                this.amount = amountCap;
                return new ItemStack(item, extraAmount);
            }

            return new ItemStack(null);
        }

        public ItemStack SubstractAmount(int amount) { 
            this.amount -= amount;

            if (!Greater()) {
                int extraAmount = -this.amount;
                this.amount = 0;
                return new ItemStack(item, extraAmount);
            }

            return new ItemStack(null);
        }

        public ItemStack Copy() => new ItemStack(item, amount, amountCap);

        public bool Greater(int amount = 0) => this.amount >= amount;

        public bool Lower(int amount = 0) => amountCap == -1 || this.amount + amount < amountCap;
    }

    /// <summary>
    /// Stores a list of item stacks, can be used to perform item usage operations
    /// </summary>
    public class ItemList {
        public event EventHandler OnItemListUpdated;
        public Dictionary<Item, ItemStack> itemStacks;
        public Item[] allowedItems;

        public bool addNewItems;
        public int maxCapacity = -1;

        public bool IsEmpty() => itemStacks.Count == 0 || GetStack().IsEmpty();

        public ItemList(Dictionary<Item, ItemStack> itemStacks, int maxCapacity = -1, bool addNewItems = true) {
            this.itemStacks = new Dictionary<Item, ItemStack>(itemStacks);
            this.addNewItems = addNewItems;
            this.maxCapacity = maxCapacity;
        }

        public ItemList(ItemStack itemStack, int maxCapacity = -1, bool addNewItems = true) {
            itemStacks = new Dictionary<Item, ItemStack> { { itemStack.item, itemStack } };
            this.addNewItems = addNewItems;
            this.maxCapacity = maxCapacity;
        }

        public ItemList(int maxCapacity = -1, bool addNewItems = true) {
            itemStacks = new Dictionary<Item, ItemStack>();
            this.addNewItems = addNewItems;
            this.maxCapacity = maxCapacity;
        }

        public void SetAllowedItems(Item[] items) {
            allowedItems = items;
        }

        public bool IsAllowed(Item _item) {
            if (allowedItems == null) return true;
            foreach (Item item in allowedItems) if (item == _item) return true;
            return false;
        }

        public ItemStack GetStack() {
            // Return first itemStack
            foreach (ItemStack itemStack in itemStacks.Values) return itemStack;

            // Else return none
            return new ItemStack(Items.copper, 0, maxCapacity);
        }

        public ItemStack GetStack(Item item) {
            // Find and return the stack with the same item
            if (itemStacks.ContainsKey(item)) return itemStacks[item];

            // If "addNewItems" isn't allowed, return
            if (!addNewItems && !IsEmpty()) return new ItemStack(null);

            // If not found, create a new one and add to item stacks list
            ItemStack newItemStack = new ItemStack(item, 0, maxCapacity);
            itemStacks.Add(item, newItemStack);
            return newItemStack;
        }

        public bool ContainsItemAmount(ItemStack _itemStack) {
            // Find and return true if found same itemStack with greater amount
            Item item = _itemStack.item;
            return itemStacks.ContainsKey(item) && itemStacks[item].Greater(_itemStack.amount);
        }

        public bool ContainsItemAmount(ItemList itemList) {
            foreach (ItemStack itemStack in itemList.itemStacks.Values) if (!ContainsItemAmount(itemStack)) return false;
            return true;
        }

        public bool ContainsItemAmount(ItemStack[] itemStacks) {
            foreach (ItemStack itemStack in itemStacks) if (!ContainsItemAmount(itemStack)) return false;
            return true;
        }

        public ItemStack AddItem(ItemStack _itemStack, bool update = true) {
            // If not found or there's no item, return all value else add value and return extra
            ItemStack itemStack = _itemStack.IsEmpty() && IsAllowed(_itemStack.item) ? _itemStack : GetStack(_itemStack.item).AddAmount(_itemStack.amount);

            if (update) UpdateList();
            return itemStack;
        }

        public ItemList AddItems(ItemStack[] itemStacks) {
            // Adds items to list
            ItemList extraItemList = new ItemList(new Dictionary<Item, ItemStack>());

            // Foreach wanted item stack, try to add and put back the extra on the extra list
            foreach (ItemStack itemStack in itemStacks) extraItemList.AddItem(AddItem(itemStack.Copy(), false));

            // Return the extra list
            UpdateList();
            return extraItemList;
        }

        public ItemList AddItems(ItemList itemList) {
            // Adds items to list
            ItemList extraItemList = new ItemList(new Dictionary<Item, ItemStack>());

            // Foreach wanted item stack, try to add and put back the extra on the extra list
            foreach (ItemStack itemStack in itemList.itemStacks.Values) extraItemList.AddItem(AddItem(itemStack.Copy(), false));

            // Return the extra list
            UpdateList();
            return extraItemList;
        }

        public ItemStack SubstractItem(ItemStack _itemStack, bool update = true) {
            // Get the stack of wanted items
            ItemStack itemStack = GetStack(_itemStack.item);

            if (itemStack.IsEmpty()) {
                itemStacks.Remove(itemStack.item);
                return _itemStack;
            }

            // If not found or there's no item, return all value else substract value and return extra
            if (_itemStack.IsEmpty()) return _itemStack;
            else {
                ItemStack retItemStack = itemStack.SubstractAmount(_itemStack.amount);
                if (itemStack.IsEmpty()) itemStacks.Remove(itemStack.item);

                if (update) UpdateList();
                return retItemStack;
            }
        }

        public ItemList SubstractItems(ItemStack[] itemStacks) {
            // Adds items to list
            ItemList extraItemList = new ItemList(new Dictionary<Item, ItemStack>());

            // Foreach wanted item stack, try to substract and put back the extra on the extra list
            foreach (ItemStack itemStack in itemStacks) extraItemList.AddItem(SubstractItem(itemStack.Copy(), false));

            // Return the extra list
            UpdateList();
            return extraItemList;
        }

        public ItemList SubstractItems(ItemList itemList) {
            // Adds items to list
            ItemList extraItemList = new ItemList(new Dictionary<Item, ItemStack>());

            // Foreach wanted item stack, try to substract and put back the extra on the extra list
            foreach (ItemStack itemStack in itemList.itemStacks.Values) extraItemList.AddItem(SubstractItem(itemStack.Copy(), false));
            
            // Return the extra list
            UpdateList();
            return extraItemList;
        }

        public void UpdateList() {
            OnItemListUpdated?.Invoke(this, EventArgs.Empty);
        }
    }

    public struct WeaponMount {
        public WeaponType weaponType;
        public Vector2 position;
        public bool mirrored;

        public WeaponMount(WeaponType weaponType, Vector2 position, bool mirrored = false) {
            this.weaponType = weaponType;
            this.position = position;
            this.mirrored = mirrored;
        }
    }

    public class UnitPlan {
        public UnitType unit;
        public ItemStack[] materialList;
        public float craftTime;

        public UnitPlan(UnitType unit, float craftTime, ItemStack[] materialList) {
            this.unit = unit;
            this.materialList = materialList;
            this.craftTime = craftTime;
        }
    }

    public class CraftPlan {
        public ItemStack productStack;
        public ItemStack[] materialList;
        public float craftTime;

        public CraftPlan(ItemStack productStack, float craftTime, ItemStack[] materialList) {
            this.productStack = productStack;
            this.materialList = materialList;
            this.craftTime = craftTime;
        }
    }

    #endregion
}

public interface IDamageable {
    public float GetTimeCode();
    public void Damage(float amount);
}

public interface IView {
    public PhotonView PhotonView { get; set; }
}

public interface IInventory {
    /// <summary>
    /// Adds items to target's inventory
    /// </summary>
    /// <param name="value">The amount of items to add</param>
    /// <returns>The amount of items that couldn't be added</returns>
    public ItemStack AddItems(ItemStack value);
    public ItemList AddItems(ItemStack[] value);

    /// <summary>
    /// Substracts items from target's inventory
    /// </summary>
    /// <param name="value">The amount of items to substract</param>
    /// <returns>The amount of items that couldn't be substracted</returns>
    public ItemStack SubstractItems(ItemStack value);
    public ItemList SubstractItems(ItemStack[] value);

    /// <summary>
    /// Gets the target's inventory
    /// </summary>
    /// <returns>A copy of the target's inventory</returns>
    public ItemList GetItemList();
}