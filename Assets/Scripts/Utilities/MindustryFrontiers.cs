using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Frontiers.Content;
using Frontiers.Content.Flags;
using Frontiers.Content.Sounds;
using Frontiers.Content.Upgrades;
using Frontiers.Content.Maps;
using Frontiers.Settings;
using Frontiers.Squadrons;
using Frontiers.Teams;
using Frontiers.Pooling;
using Frontiers.Animations;
using Frontiers.Assets;
using CI.QuickSave.Core.Serialisers;
using CI.QuickSave.Core.Settings;
using CI.QuickSave.Core.Storage;
using CI.QuickSave.Core.Converters;
using CI.QuickSave;
using Newtonsoft.Json;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Animation = Frontiers.Animations.Animation;
using MapLayer = Frontiers.Content.Maps.Map.MapLayer;
using Region = Frontiers.Content.Maps.Tilemap.Region;
using UnityEditor;

namespace Frontiers.Animations {
    public class Animator {
        readonly Dictionary<Animation.Case, Anim> animations = new();

        public void AddAnimation(Anim anim) {
            if (animations.ContainsKey(anim.GetCase())) return;
            animations.Add(anim.GetCase(), anim);
        }

        public void NextFrame() {
            if (animations.Count == 0) return;
            animations[0].NextFrame();
        }

        public void NextFrame(Animation.Case useCase) {
            if (!animations.ContainsKey(useCase)) return;
            animations[useCase].NextFrame();
        }
    }

    public class Anim {
        readonly SpriteRenderer animationRenderer;
        readonly Sprite[] animationFrames;

        Animation animation;
        int frame;

        public Anim(string baseName, string layerName, int layerOrder, Transform parent, Animation animation) {
            if (animation.frames == 0) return;
            this.animation = animation;

            // Get all the frames from the animation
            animationFrames = new Sprite[animation.frames];
            for (int i = 0; i < animation.frames; i++) {
                animationFrames[i] = AssetLoader.GetSprite(baseName + animation.name + "-" + i);
            }

            // Create a new gameObject to hold the animation
            GameObject animGameObject = new("animation" + animation.name, typeof(SpriteRenderer));

            // Set the position && rotation to 0
            animGameObject.transform.parent = parent;
            animGameObject.transform.localPosition = Vector3.zero;
            animGameObject.transform.localRotation = Quaternion.identity;

            // Get the sprite renderer component
            animationRenderer = animGameObject.GetComponent<SpriteRenderer>();
            animationRenderer.sortingLayerName = layerName;
            animationRenderer.sortingOrder = layerOrder;
        }

        public void NextFrame() {
            frame++;
            if (frame >= animation.frames) frame = 0;
            animationRenderer.sprite = animationFrames[frame];
        }

        public Animation.Case GetCase() => animation.useCase;
    }

    public struct Animation {
        public string name;
        public int frames;
        public Case useCase;

        public enum Case {
            Reload,
            Shoot
        }

        public Animation(string name, int frames, Case useCase) {
            this.name = name;
            this.frames = frames;
            this.useCase = useCase;
        }
    }
}

namespace Frontiers.Pooling {
    public class PoolManager : MonoBehaviour {
        public static Dictionary<string, GameObjectPool> allPools = new();

        public static GameObjectPool GetOrCreatePool(GameObject prefab, int targetAmount, string name = null) {
            if (name == null) name = prefab.name;
            return allPools.ContainsKey(name) ? allPools[name] : NewPool(prefab, targetAmount, name);
        }

        public static GameObjectPool NewPool(GameObject prefab, int targetAmount, string name = null) {
            GameObjectPool newPool = new(prefab, targetAmount);
            if (name == null) name = prefab.name;

            allPools.Add(name, newPool);
            return newPool;
        }
    }

    public class GameObjectPool {
        // The hard limit of gameobjects in the pool, only used if the pool gets too big where creating/destroying a gameobject is better than storing it
        public int targetAmount;

        public GameObject prefab;
        public Queue<GameObject> pooledGameObjects;

        public event EventHandler<PoolEventArgs> OnGameObjectCreated;
        public event EventHandler<PoolEventArgs> OnGameObjectDestroyed;

        public class PoolEventArgs {
            public GameObject target;
        }

        public GameObjectPool(GameObject prefab, int targetAmount) {
            this.prefab = prefab;
            this.targetAmount = targetAmount;
            pooledGameObjects = new Queue<GameObject>();
        }

        public bool CanTake() => pooledGameObjects.Count > 0;

        public bool CanReturn() => targetAmount == -1 || pooledGameObjects.Count < targetAmount;

        public GameObject Take() {
            bool canTake = CanTake();

            GameObject gameObject = canTake ? pooledGameObjects.Dequeue() : Object.Instantiate(prefab);
            if (!canTake) OnGameObjectCreated?.Invoke(this, new PoolEventArgs { target = gameObject });

            gameObject.SetActive(true);
            return gameObject;
        }

        public void Return(GameObject gameObject) {
            gameObject.SetActive(false);

            if (CanReturn()) { 
                pooledGameObjects.Enqueue(gameObject); 
            } else {
                OnGameObjectDestroyed?.Invoke(this, new PoolEventArgs() { target = gameObject });
                Object.Destroy(gameObject);
            }
        }
    }
}

namespace Frontiers.Settings {
    public static class Main {
        /// <summary>
        /// The time interval each entity should sync their data to other players
        /// </summary>
        public static float SYNC_TIME = 5f;

        /// <summary>
        /// The amount of pixels per meter/unit
        /// </summary>
        public static int PixelsPerUnit = 32;

        /// <summary>
        /// The amount of tiles per each region (only one side, so if 4 is set then 4^2 = "16 tiles") 
        /// </summary>
        public static int RegionSize = 32;
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
        public static readonly Color LocalTeamColor = new(1f, 0.827451f, 0.4980392f);
        public static readonly Color EnemyTeamColor = new(0.9490196f, 0.3333333f, 0.3333333f);

        public static List<CoreBlock> LocalCoreBlocks = new();
        public static List<CoreBlock> EnemyCoreBlocks = new();

        public static int GetTeamLayer(byte teamCode, bool ignore = false) => LayerMask.NameToLayer((ignore ? "IgnoreTeam" : "CollideTeam") + teamCode);

        public static int GetTeamMask(byte teamCode, bool ignore = false) => LayerMask.GetMask((ignore ? "IgnoreTeam" : "CollideTeam") + teamCode);

        public static int GetEnemyTeamLayer(byte teamCode, bool ignore = false) => GetTeamLayer(GetEnemyTeam(teamCode), ignore);

        public static int GetEnemyTeamMask(byte teamCode, bool ignore = false) => GetTeamMask(GetEnemyTeam(teamCode), ignore);

        public static Color GetTeamColor(byte teamCode) => teamCode == GetLocalTeam() ? LocalTeamColor : EnemyTeamColor;

        public static void AddCoreBlock(CoreBlock coreBlock) {
            if (coreBlock.IsLocalTeam()) LocalCoreBlocks.Add(coreBlock);
            else EnemyCoreBlocks.Add(coreBlock);
        }

        public static void RemoveCoreBlock(CoreBlock coreBlock) {
            if (LocalCoreBlocks.Contains(coreBlock)) LocalCoreBlocks.Remove(coreBlock);
            if (EnemyCoreBlocks.Contains(coreBlock)) EnemyCoreBlocks.Remove(coreBlock);
        }

        public static CoreBlock GetClosestCoreBlock(Vector2 position, byte teamCode) => teamCode == GetLocalTeam() ? GetClosestAllyCoreBlock(position) : GetClosestEnemyCoreBlock(position);

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

        public static byte GetLocalTeam() => PhotonNetwork.LocalPlayer.GetPhotonTeam().Code;

        public static byte GetEnemyTeam(byte code) => code == 1 ? GetTeamByCode(2) : GetTeamByCode(1);

        public static byte GetDefaultTeam() => GetTeamByCode(1);

        public static byte GetTeamByCode(byte code) {
            RoomManager.Instance.photonTeamsManager.TryGetTeamByCode(code, out PhotonTeam team);
            return team.Code;
        }

        public static Player[] TryGetTeamMembers(byte code) {
            RoomManager.Instance.photonTeamsManager.TryGetTeamMembers(code, out Player[] members);
            return members;
        }

    }
}

namespace Frontiers.Assets {
    public static class Directories {
        public static string mods = Path.Combine(Application.persistentDataPath, "QuickSave", "Mods");
        public static string maps = Path.Combine(Application.persistentDataPath, "QuickSave", "Maps");

        public static void RegenerateFolders() {
            Launcher.SetState("Locating Content Folders...");

            // If any directory is not found, create it
            if (!Directory.Exists(mods)) Directory.CreateDirectory(mods);
            if (!Directory.Exists(maps)) Directory.CreateDirectory(maps);

            Launcher.SetState("Content Folders Located");
        }
    }

    public static class AssetLoader {
        private static Sprite[] sprites;
        private static GameObject[] prefabs;

        private static Object[] assets;

        public static void LoadAssets() {
            Launcher.SetState("Loading Assets...");

            sprites = Resources.LoadAll<Sprite>("Sprites");
            prefabs = Resources.LoadAll<GameObject>("Prefabs");
            assets = Resources.LoadAll<Object>("");

            Launcher.SetState("Assets Loaded");
        }

        public static Sprite GetSprite(string name, bool suppressWarnings = false) {
            foreach (Sprite sprite in sprites) if (sprite.name == name) return sprite;

            if (!suppressWarnings) Debug.LogWarning("No sprite was found with the name: " + name);
            return null;
        }

        public static Sprite GetSprite(string name, string alt) {
            foreach (Sprite sprite in sprites) if (sprite.name == name) return sprite;
            foreach (Sprite sprite in sprites) if (sprite.name == alt) return sprite;

            return null;
        }

        public static GameObject GetPrefab(string name, bool suppressWarnings = false) {
            foreach (GameObject prefab in prefabs) if (prefab.name == name) return prefab;

            if (!suppressWarnings) Debug.LogWarning("No prefab was found with the name: " + name);
            return null;
        }

        public static GameObject GetPrefab(string name, string alt) {
            foreach (GameObject prefab in prefabs) if (prefab.name == name) return prefab;
            foreach (GameObject prefab in prefabs) if (prefab.name == alt) return prefab;

            return null;
        }

        public static T GetAsset<T>(string name, bool suppressWarnings = false) where T : Object {
            foreach (Object asset in assets) if (asset.name == name && asset is T) return asset as T;

            if (!suppressWarnings) Debug.LogWarning("No asset was found with the name: " + name);
            return null;
        }

        public static T GetAsset<T>(string name, string alt) where T : Object {
            foreach (Object asset in assets) if (asset.name == name && asset is T) return asset as T;
            foreach (Object asset in assets) if (asset.name == alt && asset is T) return asset as T;

            return null;
        }
    }

    [Serializable]
    public class Wrapper<T> {
        public T[] array;

        public Wrapper(T[] items) {
            this.array = items;
        }
    }

    [Serializable]
    public class Wrapper2D<T> {
        public T[,] array2D;

        public Wrapper2D(T[,] items) {
            this.array2D = items;
        }
    }

    public class TypeWrapper {
        public static Type GetSystemType(string name) => Type.GetType(name + ", Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

        public static string GetString(Type type) {
            string fullName = type.AssemblyQualifiedName;
            return fullName.Remove(fullName.IndexOf(","));
        }
    }
}

namespace Frontiers.Content {

    public static class ContentLoader {
        public static Dictionary<string, Content> loadedContents;
        public static List<Mod> modList;

        public static void LoadContents() {
            Launcher.SetState("Loading Base Contents...");

            loadedContents = new();
            modList = new List<Mod>();

            // These two don't inherit from the base content class
            SoundTypes.Load();
            FlagTypes.Load();

            Items.Load();
            Tiles.Load();

            // These need flags to be loaded
            UpgradeTypes.Load();

            // These need items to be loaded
            Bullets.Load();
            Weapons.Load();
            
            // These need weapons to be loaded
            Units.Load();
            Blocks.Load();

            Launcher.SetState("Base Contents Loaded");

            // Handle Mods
            Launcher.SetState("Loading Mods...");
            LoadMods();
            Launcher.SetState(modList.Count + " Mods Loaded");

            InitializeObjectPools();
        }

        public static void InitializeObjectPools() {
            ConveyorBlock.conveyorItemPool = PoolManager.NewPool(Assets.AssetLoader.GetPrefab("conveyorItem"), 100);
        }

        public static void HandleContent(Content content) {
            content.id = (short)loadedContents.Count;
            if (content.name == null) content.name = "content num. " + content.id;

            if (GetContentByName(content.name) != null) throw new ArgumentException("Two content objects cannot have the same name! (issue: '" + content.name + "')");
            loadedContents.Add(content.name, content);
        }

        /*
        public static void TEST_SaveContents() {
            foreach(Content content in contentMap) {
                SaveContent(content);
            }

            SaveMod(new Mod());
        }
        */ // Json content/mod generator

        public static void LoadMods() {
            List<string> modsToLoad = GetModNames();
            foreach (string modName in modsToLoad) modList.Add(ReadMod(modName));
            foreach (Mod mod in modList) mod.LoadMod();
        }

        public static List<string> GetModNames() {
            List<string> modNames = new();

            string[] modFolders = Directory.GetDirectories(Directories.mods);
            foreach (string modFolderPath in modFolders) modNames.Add(Path.GetFileName(modFolderPath));

            return modNames;
        }

        public static void SaveMod(Mod mod) {
            mod.InitMod();

            string modName = Path.Combine("Mods", mod.name, mod.name);
            QuickSaveRaw.Delete(modName + ".json");
            QuickSaveWriter writer = QuickSaveWriter.Create(modName);

            writer.Write("mod", mod);
            writer.Commit();
        }

        public static Mod ReadMod(string modName) {
            modName = Path.Combine("Mods", modName, modName);
            QuickSaveReader reader = QuickSaveReader.Create(modName);
            Debug.Log(modName);
            return reader.Read<Mod>("mod");
        }

        public static void SaveContent(Content content) {
            string contentName = Path.Combine("Base", content.name);

            QuickSaveRaw.Delete(contentName + ".json");
            QuickSaveWriter writer = QuickSaveWriter.Create(contentName);

            content.Wrap();
            writer.Write("type", TypeWrapper.GetString(content.GetType()));
            writer.Write("data", content);
            writer.Commit();
        }

        public static Content ReadContent(string contentName) {
            QuickSaveReader reader = QuickSaveReader.Create(contentName);
            Type type = TypeWrapper.GetSystemType(reader.Read<string>("type"));
            return (Content)reader.Read("data", type);
        }

        public static Content GetContentById(short id) {
            if (loadedContents.Count <= id) return null;
            return loadedContents.ElementAt(id).Value;
        }

        public static Content GetContentByName(string name) {
            if (!loadedContents.ContainsKey(name)) return null;
            return loadedContents[name];
        }

        public static T[] GetContentByType<T>() where T : Content{
            List<T> foundMatches = new();
            foreach(Content content in loadedContents.Values) if (TypeEquals(content.GetType(), typeof(T))) foundMatches.Add(content as T);
            return foundMatches.ToArray();
        }

        public static int GetContentCountOfType<T>(bool countHidden = false) where T : Content {
            int count = 0;
            foreach (Content content in loadedContents.Values) if (TypeEquals(content.GetType(), typeof(T)) && (countHidden || !content.hidden)) count++;
            return count;
        }

        public static bool TypeEquals(Type target, Type reference) => target == reference || target.IsSubclassOf(reference);
    }

    [Serializable]
    public class Mod {
        public string name;
        public int version;
        public Wrapper<string> tiles;
        public Wrapper<string> items;
        public Wrapper<string> bullets;
        public Wrapper<string> weapons;
        public Wrapper<string> units;
        public Wrapper<string> blocks;

        public void InitMod() {
            name = "Example Mod";
            version = 1;
            tiles = new Wrapper<string>(new string[1] { "magma-tile" });
            items = new Wrapper<string>(new string[1] { "plastanium" });
            bullets = new Wrapper<string>(new string[1] { "bombBulletType" });
            weapons = new Wrapper<string>(new string[2] { "horizon-weapon", "duo-weapon" });
            units = new Wrapper<string>(new string[1] { "horizon" });
            blocks = new Wrapper<string>(new string[3] { "titanium-wall", "titanium-wall-large", "duo-turret" });
        }

        public void LoadMod() {
            LoadContent(tiles, Path.Combine("Mods", name, "Content", "Tiles"));
            LoadContent(items, Path.Combine("Mods", name, "Content", "Items"));
            LoadContent(bullets, Path.Combine("Mods", name, "Content", "Bullets"));
            LoadContent(weapons, Path.Combine("Mods", name, "Content", "Weapons"));
            LoadContent(units, Path.Combine("Mods", name, "Content", "Units"));
            LoadContent(blocks, Path.Combine("Mods", name, "Content", "Blocks"));

            Debug.Log("Loaded mod:" + name + " with version " + version);
        }

        public void LoadContent(Wrapper<string> content, string path = "") {
            string[] contentNames = content.array;
            foreach (string contentName in contentNames) ContentLoader.ReadContent(Path.Combine(path, contentName));
        }
    }

    [Serializable]
    public abstract class Content {
        public string name;
        public bool hidden = false;

        // Used to classificate this content, not shown to the player
        public Flag[] flags;

        // The main function of this content
        public string function;

        // The description of this content
        public string description;

        // Some details about this content
        public string details;

        [JsonIgnore] public short id;
        [JsonIgnore] public Sprite sprite;
        [JsonIgnore] public Sprite spriteFull;

        public Content(string name) {
            this.name = name;
            ContentLoader.HandleContent(this);

            sprite = AssetLoader.GetSprite(name);
            spriteFull = AssetLoader.GetSprite(name + "-full", name);
        }

        public virtual void Wrap() { }

        public virtual void UnWrap() { }

        public static bool TypeEquals(Type target, Type reference) => target == reference || target.IsSubclassOf(reference);
    }

    #region - Blocks -

    public class EntityType : Content {
        [JsonIgnore] public Type type;
        public string typeName;

        public int tier = 1;

        public ItemStack[] buildCost;

        public float health = 100f, itemMass = -1f;
        public int itemCapacity = 20;
        public bool hasOrientation = false, allowsSingleItem = false;

        public int maximumFires = 0;
        public bool canGetOnFire = false, canSpreadFire = false;

        public float blinkInterval = 0.5f, blinkOffset = 0f, blinkLength = 1f;

        public string hitSmokeFX = "HitSmokeFX", deathFX = "ExplosionFX";
        public string loopSound = "", deathSound = "Bang";

        public EntityType(string name, Type type, int tier = 1) : base(name) {
            typeName = TypeWrapper.GetString(type);
            this.type = type;
            this.tier = tier;
        }

        public override void Wrap() {
            base.Wrap();
            typeName = TypeWrapper.GetString(type);
        }


        public override void UnWrap() {
            base.UnWrap();
            type = TypeWrapper.GetSystemType(typeName);
        }
    }

    public class BlockType : EntityType {
        [JsonIgnore] public Sprite teamSprite, glowSprite, topSprite, bottomSprite;

        public bool updates = false, breakable = true, solid = true;
        public int size = 1;

        public BlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            teamSprite = AssetLoader.GetSprite(name + "-team", true);
            glowSprite = AssetLoader.GetSprite(name + "-glow", true);
            topSprite = AssetLoader.GetSprite(name + "-top", true);
            bottomSprite = AssetLoader.GetSprite(name + "-bottom", true);
            this.type = type;

            canGetOnFire = false;
            maximumFires = 3;
        }
    }

    public class DelayedItem {
        public Item item;
        public float exitTime;
        public int enterOrientation;

        public DelayedItem(Item item, float exitTime) {
            this.item = item;
            this.exitTime = exitTime;
        }

        public DelayedItem(Item item, float exitTime, int enterOrientation) {
            this.item = item;
            this.exitTime = exitTime;
            this.enterOrientation = enterOrientation;
        }

        public bool CanExit() {
            return Time.time >= exitTime;
        }
    }

    public class ItemBlockType : BlockType {
        public ItemBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class DistributionBlockType : ItemBlockType {
        public float itemSpeed = 1f;
        public bool inverted = false;

        public DistributionBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class DrillBlockType : ItemBlockType {
        [JsonIgnore] public Sprite drillRotorSprite;
        public float drillHardness, drillRate;

        public DrillBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            drillRotorSprite = AssetLoader.GetSprite(name + "-rotator");
            updates = true;
        }
    }

    public class ConveyorBlockType : ItemBlockType {
        [JsonIgnore] public Sprite[,] allConveyorSprites;
        public float frameTime = 0.25f;
        public float itemSpeed = 1;

        public const int frames = 4;
        public const int variants = 5;

        public ConveyorBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            hasOrientation = true;
            updates = true;

            frameTime = 1 / (frames * itemSpeed);  
            allConveyorSprites = new Sprite[variants, frames];

            for (int v = 0; v < variants; v++) {
                for (int f = 0; f < frames; f++) {
                    allConveyorSprites[v, f] = AssetLoader.GetSprite($"{name}-{v}-{f}");
                }
            }         
        }

        public static int GetVariant(bool right, bool left, bool back, out bool mirrored) {
            // Only Back
            if (!(right || left) && back) {
                mirrored = false;
                return 0;
            }

            // One side
            if ((right ^ left) && !back) {
                mirrored = right;
                return 1;
            }

            // One side and back
            if ((right ^ left) && back) {
                mirrored = left;
                return 2;
            }

            // All
            if (right && left && back) {
                mirrored = false;
                return 3;
            }

            // Only sides
            if (right && left && !back) {
                mirrored = false;
                return 4;
            }

            // This could never happer, but who knows
            mirrored = false;
            return 0;
        }
    }

    public class RouterBlockType : DistributionBlockType {
        public RouterBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            allowsSingleItem = true;
        }
    }

    public class JunctionBlockType : DistributionBlockType {      
        public JunctionBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class SorterBlockType : DistributionBlockType {
        public SorterBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class OverflowGateBlockType : DistributionBlockType {
        public OverflowGateBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class CrafterBlockType : ItemBlockType {
        public CraftPlan craftPlan;

        public CrafterBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            updates = true;
        }
    }

    public class StorageBlockType : ItemBlockType {
        public StorageBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class TurretBlockType : ItemBlockType {
        public WeaponMount weapon;

        public TurretBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class CoreBlockType : StorageBlockType {
        [SerializeField] UpgradeType baseUpgrade;
        private string baseUpgradeName;

        public CoreBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            breakable = false;
        }

        public override void UnWrap() {
            base.UnWrap();
            if (baseUpgradeName != null) baseUpgrade = UpgradeHandler.GetUpgradeByName(baseUpgradeName);
        }

        public override void Wrap() {
            base.Wrap();
            baseUpgradeName = baseUpgrade?.name; 
        }
    }

    public class UnitFactoryBlockType : ItemBlockType {
        public UnitPlan unitPlan;

        public UnitFactoryBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class LandPadBlockType : StorageBlockType {
        [JsonIgnore] public Vector2[] landPositions;
        public Wrapper<Vector2> landPositionsList;

        public int unitCapacity = 0;
        public float unitSize = 1.5f;

        public LandPadBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }

        public override void Wrap() {
            base.Wrap();
            landPositionsList = new Wrapper<Vector2>(landPositions);
        }

        public override void UnWrap() {
            base.UnWrap();
            landPositions = landPositionsList.array;
        }
    }



    public class Blocks {
        public const BlockType none = null;
        public static BlockType
            copperWall, copperWallLarge,

            coreShard, container,

            landingPad, landingPadLarge,

            tempest, stinger, path, spread, 
            
            airFactory, 
            
            graphitePress, siliconSmelter, kiln,
            
            conveyor, router, junction, sorter, overflowGate, 
            
            mechanicalDrill, pneumaticDrill;

        public static void Load() {
            copperWall = new BlockType("copper-wall", typeof(Block), 1) {
                buildCost = ItemStack.With(Items.copper, 6),

                flags = new Flag[] { FlagTypes.wall }, 

                health = 140,
            };

            copperWallLarge = new BlockType("copper-wall-large", typeof(Block), 1) {
                buildCost = ItemStack.With(Items.copper, 24),

                flags = new Flag[] { FlagTypes.wall },

                health = 600,
                size = 2
            };

            coreShard = new CoreBlockType("core-shard", typeof(CoreBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 1000, Items.lead, 500, Items.titanium, 100),

                hidden = true,
                breakable = false,
                health = 1600,
                size = 3,

                itemCapacity = 1000,

                canGetOnFire = true,
            };

            container = new StorageBlockType("container", typeof(StorageBlock), 2) {
                buildCost = ItemStack.With(Items.copper, 100, Items.titanium, 25),

                health = 150,
                size = 2,
                itemCapacity = 200,

                canGetOnFire = true,
            };

            landingPad = new LandPadBlockType("landingPad", typeof(LandPadBlock), 2) {
                buildCost = ItemStack.With(Items.copper, 250, Items.titanium, 75),

                health = 250,
                size = 3,
                solid = false,
                updates = true,
                unitCapacity = 4,
                unitSize = 2.5f,

                landPositions = new Vector2[] {
                    new Vector2(0.8f, 0.8f),
                    new Vector2(0.8f, 2.2f),
                    new Vector2(2.2f, 0.8f),
                    new Vector2(2.2f, 2.2f)
                }
            };

            landingPadLarge = new LandPadBlockType("landingPad-large", typeof(LandPadBlock), 3) {
                buildCost = ItemStack.With(Items.copper, 250, Items.titanium, 75),

                health = 300,
                size = 3,
                solid = false,
                unitCapacity = 1,
                unitSize = 5f,

                landPositions = new Vector2[] {
                    new Vector2(1.5f, 1.5f)
                }
            };

            tempest = new TurretBlockType("tempest", typeof(TurretBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 250, Items.titanium, 75),
                weapon = new WeaponMount(Weapons.tempestWeapon, Vector2.zero),

                health = 230f,
                size = 2,

                canGetOnFire = true,
            };

            stinger = new TurretBlockType("stinger", typeof(TurretBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 250, Items.titanium, 75),
                weapon = new WeaponMount(Weapons.stingerWeapon, Vector2.zero),

                health = 320f,
                size = 2,

                canGetOnFire = true,
            };

            path = new TurretBlockType("path", typeof(TurretBlock), 3) {
                buildCost = ItemStack.With(Items.copper, 125, Items.graphite, 55, Items.silicon, 35),
                weapon = new WeaponMount(Weapons.pathWeapon, Vector2.zero),

                health = 275f,
                size = 2,

                canGetOnFire = true,
            };

            spread = new TurretBlockType("spread", typeof(TurretBlock), 3) {
                buildCost = ItemStack.With(Items.copper, 250, Items.titanium, 65, Items.silicon, 80),
                weapon = new WeaponMount(Weapons.spreadWeapon, Vector2.zero),

                health = 245f,
                size = 2,

                canGetOnFire = true,
            };

            airFactory = new UnitFactoryBlockType("air-factory", typeof(UnitFactoryBlock), 2) {
                buildCost = ItemStack.With(Items.copper, 250, Items.titanium, 75),

                unitPlan = new UnitPlan(Units.flare, 4f, new ItemStack[1] {
                    new ItemStack(Items.silicon, 20)
                }),

                health = 250f,
                size = 3,
                itemCapacity = 50,

                canGetOnFire = true,
            };

            siliconSmelter = new CrafterBlockType("silicon-smelter", typeof(CrafterBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 50, Items.lead, 45),

                craftPlan = new CraftPlan() {
                    productStack = new ItemStack(Items.silicon, 1),
                    materialList = ItemStack.With(Items.sand, 2, Items.coal, 1),
                    craftTime = 0.66f
                },

                health = 125,
                size = 2,
                itemCapacity = 30,

                canGetOnFire = true,
            };

            graphitePress = new CrafterBlockType("graphite-press", typeof(CrafterBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 75, Items.lead, 25),

                craftPlan = new CraftPlan() {
                    productStack = new ItemStack(Items.graphite, 1),
                    materialList = ItemStack.With(Items.coal, 2),
                    craftTime = 1.5f
                },

                health = 95,
                size = 2,
                itemCapacity = 10,

                canGetOnFire = true,
            };

            kiln = new CrafterBlockType("kiln", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.copper, 100, Items.lead, 35, Items.graphite, 15),

                craftPlan = new CraftPlan() {
                    productStack = new ItemStack(Items.metaglass, 1),
                    materialList = ItemStack.With(Items.sand, 1, Items.lead, 1),
                    craftTime = 0.5f
                },

                health = 120,
                size = 2,
                itemCapacity = 16,

                canGetOnFire = true,
            };

            conveyor = new ConveyorBlockType("conveyor", typeof(ConveyorBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 2),
                health = 75f,
                size = 1,
                itemCapacity = 3,
                itemSpeed = 4f,
                hasOrientation = true,
            };

            router = new RouterBlockType("router", typeof(RouterBlock), 1) {
                health = 90f,
                size = 1,
                itemSpeed = 4f,
                itemCapacity = 3,
            };

            junction = new JunctionBlockType("junction", typeof(JunctionBlock), 1) {
                health = 80f,
                size = 1,
                itemCapacity = 4,
                itemSpeed = 4f,
            };

            sorter = new SorterBlockType("sorter", typeof(SorterBlock), 1) {
                health = 80f,
                size = 1,
                itemCapacity = 3,
                itemSpeed = 4f,
                inverted = false,
            };

            overflowGate = new OverflowGateBlockType("overflow-gate", typeof(OverflowGateBlock), 1) {
                health = 80f,
                size = 1,
                itemCapacity = 3,
                itemSpeed = 4f,
                inverted = false,
            };

            mechanicalDrill = new DrillBlockType("mechanical-drill", typeof(DrillBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 16),
                health = 100f,
                size = 2,
                itemCapacity = 10,

                drillHardness = 2.5f,
                drillRate = 1f,

                canGetOnFire = true,
            };

            pneumaticDrill = new DrillBlockType("pneumatic-drill", typeof(DrillBlock), 2) {
                buildCost = ItemStack.With(Items.copper, 24, Items.graphite, 10),
                health = 175f,
                size = 2,
                itemCapacity = 12,

                drillHardness = 3.5f,
                drillRate = 1.75f,

                canGetOnFire = true,
            };
        }
    }

    #endregion

    #region - Units -
    [Serializable]
    public class UnitType : EntityType {
        [JsonIgnore] public Sprite cellSprite, outlineSprite;
        [JsonIgnore] public Type[] priorityList = null;

        public float size = 1.5f;
        public float maxVelocity = 2f, rotationSpeed = 90f;

        public float itemPickupDistance = 3f, buildSpeedMultiplier = 1f;

        public float range = 10f, searchRange = 15f, fov = 95;
        public float groundHeight = 18f;

        public float fuelCapacity = 60f, fuelConsumption = 1.5f, fuelRefillRate = 7.5f, fuelLeftToReturn = 10f;
        public float emptyMass = 10f, fuelDensity = 0.0275f;

        public WeaponMount[] weapons = new WeaponMount[0];

        public UnitType(string name, Type type, int tier = 1) : base(name, type, tier) {
            cellSprite = AssetLoader.GetSprite(name + "-cell");
            outlineSprite = AssetLoader.GetSprite(name + "-outline");
            typeName = TypeWrapper.GetString(type);
            this.type = type;
            hasOrientation = true;

            canGetOnFire = true;
            maximumFires = 2;
        }

        public virtual void Rotate(Unit unit, Vector2 position) {

        }

        public virtual void Move(Unit unit, Vector2 position) {

        }

        public virtual void UpdateBehaviour(Unit unit, Vector2 position) {

        }
    }

    public class MechUnitType : UnitType {
        [JsonIgnore] public Sprite legSprite, baseSprite;
        public float baseRotationSpeed = 90f, legStepDistance = 0.2f, sideSway = 0.075f, frontSway = 0.01f;
        public MechUnitType(string name, Type type, int tier = 1) : base(name, type, tier) {
            legSprite = AssetLoader.GetSprite(name + "-leg");
            baseSprite = AssetLoader.GetSprite(name + "-base");
        }

        public override void Rotate(Unit unit, Vector2 position) {
            if (!unit.CanRotate()) return;

            // Quirky quaternion stuff to make the unit rotate slowly -DO NOT TOUCH-
            Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, (position - unit.GetPosition()).normalized);
            desiredRotation = Quaternion.Euler(0, 0, desiredRotation.eulerAngles.z);

            float speed = rotationSpeed * Time.fixedDeltaTime;
            unit.transform.rotation = Quaternion.RotateTowards(unit.transform.rotation, desiredRotation, speed);
        }

        public override void Move(Unit unit, Vector2 position) {
            if (!unit.CanMove()) {
                unit.SetVelocity(Vector2.zero);
                return;
            }

            // Get the direction
            Vector2 targetDirection = (position - unit.GetPosition()).normalized;

            float similarity = unit.GetSimilarity(unit.transform.up, targetDirection);
            float enginePower = unit.GetEnginePower();

            // Set velocity
            unit.SetVelocity(similarity * enginePower * maxVelocity * targetDirection);
        }

        public override void UpdateBehaviour(Unit unit, Vector2 position) {
            // Consume fuel based on fuelConsumption x enginePower
            unit.ConsumeFuel(fuelConsumption * unit.GetEnginePower() * Time.fixedDeltaTime);
            Move(unit, position);      
            Rotate(unit, unit.GetTargetPosition());        
        }
    }

    public class AircraftUnitType : UnitType {
        public float drag = 1f, force = 500f;
        public float bankAmount = 25f, bankSpeed = 5f;
        public bool useAerodynamics = true, hasDragTrails = true;

        public float takeoffTime = 3f, takeoffHeight = 0.5f; // Takeoff height is measured in a percentage of ground height
        public float maxLiftVelocity = 3f;

        public bool hasWreck = false;
        public float wreckHealth = 0f;

        public Vector2 trailOffset = Vector2.zero;

        public AircraftUnitType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }

        public override void Rotate(Unit unit, Vector2 position) {
            if (!unit.CanRotate()) return;

            // Power is reduced if: g-forces are high, is close to the target or if the behavoiur is fleeing
            float rotationPower = unit.GetRotationPower();

            // Quirky quaternion stuff to make the unit rotate slowly -DO NOT TOUCH-
            Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, (position - unit.GetPosition()).normalized);
            desiredRotation = Quaternion.Euler(0, 0, desiredRotation.eulerAngles.z);

            float speed = rotationSpeed * rotationPower * Time.fixedDeltaTime;
            float prevRotation = unit.transform.eulerAngles.z;

            unit.transform.rotation = Quaternion.RotateTowards(unit.transform.rotation, desiredRotation, speed);
            float gForce = (unit.transform.eulerAngles.z - prevRotation) * Time.fixedDeltaTime * 10f;
            unit.Tilt(gForce * bankAmount);
        }

        public override void Move(Unit unit, Vector2 position) {
            if (!unit.CanMove()) return;

            // A value from 0 to 1 that indicates the power output percent of the engines
            float enginePower = unit.GetEnginePower();

            // Get the direction
            Vector2 direction = unit.GetDirection(position);

            if (unit.Mode == Unit.UnitMode.Attack && !unit.IsFleeing()) {
                Vector2 targetDirection = (position - unit.GetPosition()).normalized;

                // Get acceleration and drag values based on direction
                float similarity = unit.GetSimilarity(unit.transform.up, targetDirection);
                enginePower *= Mathf.Clamp01(similarity * 2f);
            }
            
            // Accelerate
            unit.Accelerate(enginePower * force * direction.normalized);
        }

        public virtual void WreckBehaviour(Unit unit) {
            
        }

        public override void UpdateBehaviour(Unit unit, Vector2 position) {
            if (unit.IsWreck()) {
                WreckBehaviour(unit);
            } else {
                // Consume fuel based on fuelConsumption x enginePower
                unit.ConsumeFuel(fuelConsumption * unit.GetEnginePower() * Time.fixedDeltaTime);
                Move(unit, position);
                Rotate(unit, position);
            }
        }
    }

    public class CopterUnitType : AircraftUnitType {
        public UnitRotor[] rotors;

        // Degrees / second
        public float wreckSpinAccel = 50f;
        public float wreckSpinMax = 270f;

        public string bladeSound = "copter-blade-loop";

        public CopterUnitType(string name, Type type, int tier = 1) : base(name, type, tier) {
            useAerodynamics = hasDragTrails = false; // Copters don't do that
        }

        public override void WreckBehaviour(Unit unit) {
            CopterUnit copter = (CopterUnit)unit;
            copter.wreckSpinVelocity = Mathf.Clamp(wreckSpinAccel * Time.deltaTime + copter.wreckSpinVelocity, 0, wreckSpinMax);
            copter.transform.eulerAngles += new Vector3(0, 0, copter.wreckSpinVelocity * Time.deltaTime);
        }
    }

    public class Units {
        public const UnitType none = null;
        public static UnitType 
            flare, horizon, zenith,  // Assault - air
            poly,                    // Support - air
            sonar, foton,            // Copter - air
            dagger, fortress;        // Assault - ground

        public static void Load() {
            flare = new AircraftUnitType("flare", typeof(AircraftUnit), 1) {
                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.flareWeapon, new(-0.25f, 0.3f), true),
                },

                flags = new Flag[] { FlagTypes.aircraft, FlagTypes.fighter, FlagTypes.light, FlagTypes.fast, FlagTypes.lightArmored },
                priorityList = new Type[5] { typeof(Unit), typeof(TurretBlock), typeof(CoreBlock), typeof(ItemBlock), typeof(Block) },
                useAerodynamics = true,

                trailOffset = new(0.375f, -0.45f),

                health = 75f,
                size = 1.5f,
                maxVelocity = 20f,
                drag = 0.1f,

                rotationSpeed = 160f,
                bankAmount = 30f,

                range = 10f,
                searchRange = 15f,
                fov = 100f,
                groundHeight = 18f,

                fuelCapacity = 120f,
                fuelConsumption = 1.25f,
                fuelRefillRate = 8.25f,

                force = 500f,
                emptyMass = 10f,
                itemMass = 3f,
            };

            horizon = new AircraftUnitType("horizon", typeof(AircraftUnit), 2) {
                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.horizonBombBay, Vector2.zero, false),
                },

                flags = new Flag[] { FlagTypes.aircraft, FlagTypes.bomber, FlagTypes.slow, FlagTypes.moderateArmored },
                priorityList = new Type[4] { typeof(TurretBlock), typeof(ItemBlock), typeof(CoreBlock), typeof(Block) },
                useAerodynamics = true,

                health = 215f,
                size = 2.25f,
                maxVelocity = 10f,
                itemCapacity = 25,
                drag = 0.2f,

                rotationSpeed = 100f,
                bankAmount = 40f,

                range = 3f,
                searchRange = 20f,
                fov = 110f,
                groundHeight = 12f,

                fuelCapacity = 240f,
                fuelConsumption = 2.15f,
                fuelRefillRate = 14.5f,

                force = 800f,
                emptyMass = 15.5f,
                itemMass = 10f,
            };

            zenith = new AircraftUnitType("zenith", typeof(AircraftUnit), 3) {
                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.zenithMissiles, new(0.25f, 0f), true, true),
                },

                flags = new Flag[] { FlagTypes.aircraft, FlagTypes.slow, FlagTypes.heavy, FlagTypes.heavyArmored },
                priorityList = new Type[5] { typeof(TurretBlock), typeof(Unit), typeof(ItemBlock), typeof(Block), typeof(CoreBlock) },
                useAerodynamics = false,

                health = 825f,
                size = 3.5f,
                maxVelocity = 7.5f,
                itemCapacity = 50,
                drag = 2f,

                rotationSpeed = 70f,
                bankAmount = 20f,

                range = 15f,
                searchRange = 20f,
                fov = 90f,
                groundHeight = 12f,

                fuelCapacity = 325f,
                fuelConsumption = 2.25f,
                fuelRefillRate = 20.75f,

                force = 1030f,
                emptyMass = 25.25f,
                itemMass = 6.5f,

                maximumFires = 3,
            };

            poly = new AircraftUnitType("poly", typeof(AircraftUnit), 2) {
                priorityList = new Type[0],
                useAerodynamics = false,

                flags = new Flag[] { FlagTypes.aircraft, FlagTypes.support, FlagTypes.slow, FlagTypes.lightArmored },

                health = 255f,
                size = 1.875f,
                maxVelocity = 9f,
                itemCapacity = 120,
                drag = 1f,

                rotationSpeed = 120f,
                bankAmount = 10f,

                range = 10f,
                searchRange = 25f,
                fov = 180f,
                groundHeight = 9f,

                fuelCapacity = 580f,
                fuelConsumption = 0.22f,
                fuelRefillRate = 23.5f,

                force = 865f,
                emptyMass = 5.5f,
                itemMass = 10.5f,

                buildSpeedMultiplier = 1f,
                itemPickupDistance = 6f,
            };

            sonar = new CopterUnitType("sonar", typeof(CopterUnit), 2) {
                rotors = new UnitRotor[1] {
                    new UnitRotor("sonar-rotor", new(0f, 0.14f), 3f, 1f, 0.667f, 1f),
                },

                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.zenithMissiles, new Vector2(0.4f, 0.1562f), true),
                },

                flags = new Flag[] { FlagTypes.copter, FlagTypes.slow, FlagTypes.heavyArmored },
                priorityList = new Type[5] { typeof(MechUnit), typeof(Unit), typeof(TurretBlock), typeof(CoreBlock), typeof(Block) },

                hasWreck = true,
                wreckHealth = 125f,

                health = 395f,
                size = 2.25f,
                maxVelocity = 7f,
                itemCapacity = 35,
                drag = 3f,

                rotationSpeed = 80f,
                bankAmount = 80f,

                range = 12f,
                searchRange = 17.5f,
                fov = 160f,
                groundHeight = 8f,

                fuelCapacity = 560f,
                fuelConsumption = 3.65f,
                fuelRefillRate = 18.25f,

                force = 600f,
                emptyMass = 13.5f,
                itemMass = 12.25f,
                fuelDensity = 7.25f,
            };

            foton = new CopterUnitType("foton", typeof(CopterUnit), 3) {
                rotors = new UnitRotor[] {
                    new UnitRotor("foton-rotor", new(0f, 0.15f), 6f, 1.5f, 1.5f, 2.25f, new UnitRotorBlade[2] {
                        new UnitRotorBlade(0f, false),
                        new UnitRotorBlade(0f, true)
                    }),
                },

                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.fotonWeapon, new Vector2(0.26f, 0.145f), true),
                },

                flags = new Flag[] { FlagTypes.copter, FlagTypes.slow, FlagTypes.heavy, FlagTypes.heavyArmored },
                priorityList = new Type[5] { typeof(MechUnit), typeof(Unit), typeof(TurretBlock), typeof(CoreBlock), typeof(Block) },

                hasWreck = true,
                wreckHealth = 225f,

                health = 750f,
                size = 3.75f,
                maxVelocity = 9f,
                itemCapacity = 50,
                drag = 3.5f,

                rotationSpeed = 60f,
                bankAmount = 0f,

                range = 12f,
                searchRange = 17.5f,
                fov = 360f,
                groundHeight = 12f,

                fuelCapacity = 950f,
                fuelConsumption = 5.5f,
                fuelRefillRate = 30.45f,

                force = 985f,
                emptyMass = 21.75f,
                itemMass = 15.25f,
                fuelDensity = 10f,
            };

            dagger = new MechUnitType("dagger", typeof(MechUnit), 1) {
                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.daggerWeapon, new Vector2(0.29187f, 0.1562f), true, true),
                },

                flags = new Flag[] { FlagTypes.mech, FlagTypes.slow, FlagTypes.light, FlagTypes.moderateArmored },
                priorityList = new Type[5] { typeof(Unit), typeof(TurretBlock), typeof(CoreBlock), typeof(ItemBlock), typeof(Block) },

                health = 140f,
                size = 1.5f,
                maxVelocity = 3.75f,

                baseRotationSpeed = 90f,
                legStepDistance = 0.6f,
                sideSway = 0.075f,
                frontSway = 0.01f,

                rotationSpeed = 80f,

                range = 15f,
                searchRange = 20f,
                fov = 80f,
                groundHeight = 0.2f,

                fuelCapacity = 525f,
                fuelConsumption = 2.5f,
                fuelRefillRate = 24.5f,

                emptyMass = 7.05f,
                itemMass = 2.2f,
                fuelDensity = 12.25f,
            };

            fortress = new MechUnitType("fortress", typeof(MechUnit), 3) {
                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.fortressWeapon, new Vector2(0.32f, 0.04f), true, true),
                },

                flags = new Flag[] { FlagTypes.mech, FlagTypes.slow, FlagTypes.heavy, FlagTypes.heavyArmored },
                priorityList = new Type[5] { typeof(MechUnit), typeof(TurretBlock), typeof(CoreBlock), typeof(ItemBlock), typeof(Block) },

                health = 140f,
                size = 3.125f,
                maxVelocity = 3.22f,

                baseRotationSpeed = 50f,
                legStepDistance = 1.25f,
                sideSway = 0.075f,
                frontSway = 0.01f,

                rotationSpeed = 60f,

                range = 20f,
                searchRange = 25f,
                fov = 100f,
                groundHeight = 0.2f,

                fuelCapacity = 1080f,
                fuelConsumption = 5.25f,
                fuelRefillRate = 54.75f,

                emptyMass = 17.5f,
                itemMass = 3f,
                fuelDensity = 23.5f,
            };
        }
    }

    #endregion

    #region - Weapons -
    [Serializable]
    public class WeaponType : Content {
        [JsonIgnore] public Item ammoItem;
        [JsonIgnore] public Sprite outlineSprite;
        [JsonIgnore] public Animation[] animations;
        [JsonIgnore] public WeaponBarrel[] barrels;

        public Vector2 shootOffset = Vector2.zero;
        public BulletType bulletType;

        public string shootFX = "MuzzleFX";
        public string casingFX = "";
        public float shootFXSize = 1f, casingFXSize = 1f, casingFXOffset = -0.5f;

        private string ammoItemName;
        private Wrapper<Animation> animationWrapper;
        private Wrapper<WeaponBarrel> barrelWrapper;

        public bool isIndependent = false;
        public bool consumesItems = false;
        public bool predictTarget = true;

        public int clipSize = 10;
        public float maxTargetDeviation = 15f, spread = 5f, recoil = 0.75f, returnSpeed = 1f, shootTime = 1f, reloadTime = 1f, rotateSpeed = 90f;

        public WeaponType(string name) : base(name) {
            outlineSprite = AssetLoader.GetSprite(name + "-outline", true);
        }

        public WeaponType(string name, Item ammoItem) : base(name) {
            outlineSprite = AssetLoader.GetSprite(name + "-outline", true);
            ammoItemName = ammoItem.name;
            this.ammoItem = ammoItem;
        }

        public float Range { get => bulletType.Range; }

        public override void Wrap() {
            base.Wrap();
            ammoItemName = ammoItem == null ? "empty" : ammoItem.name;
            animationWrapper = new Wrapper<Animation>(animations);
            barrelWrapper = new Wrapper<WeaponBarrel>(barrels);
        }

        public override void UnWrap() {
            base.UnWrap();
            ammoItem = ammoItemName == "empty" ? null : ContentLoader.GetContentByName(ammoItemName) as Item;
            animations = animationWrapper.array;
            barrels = barrelWrapper.array;
        }
    }

    public class Weapons {
        public const Weapon none = null;

        // Base weapons
        public static WeaponType smallAutoWeapon, tempestWeapon, stingerWeapon, pathWeapon, spreadWeapon;

        //Unit weapons
        public static WeaponType 
            flareWeapon, horizonBombBay, zenithMissiles,
            sonarWeapon, fotonWeapon,
            daggerWeapon, fortressWeapon;

        // Item related weapons 
        public static WeaponType missileRack;

        public static void Load() {

            smallAutoWeapon = new WeaponType("small-auto-weapon") {
                bulletType = Bullets.basicBullet,
                shootOffset = new Vector2(0, 0.37f),

                recoil = 0f,
                clipSize = 25,
                shootTime = 0.15f,
                reloadTime = 5f,

                casingFX = "CasingFX"
            };

            flareWeapon = new WeaponType("flare-weapon") {
                bulletType = Bullets.basicBullet,
                shootOffset = new Vector2(0, 0.37f),

                recoil = 0.075f,
                returnSpeed = 3f,
                clipSize = 25,
                shootTime = 0.15f,
                reloadTime = 5f,

                casingFX = "CasingFX"
            };

            fotonWeapon = new WeaponType("foton-weapon") {
                bulletType = Bullets.missileBullet,
                shootOffset = new Vector2(0, 0.37f),

                recoil = 0f,
                clipSize = 3,
                shootTime = 0.2f,
                reloadTime = 2f
            };

            daggerWeapon = new WeaponType("dagger-weapon") {
                bulletType = Bullets.bigBullet,
                shootOffset = new Vector2(0, 0.24f),
                
                recoil = 0.1f,
                returnSpeed = 2f,
                clipSize = 3,
                shootTime = 0.33f,
                reloadTime = 1.5f,

                casingFX = "CasingFX"
            };

            fortressWeapon = new WeaponType("fortress-weapon") {
                bulletType = Bullets.bigBullet,
                shootOffset = new Vector2(0.1f, 0.64f),

                recoil = 0.1f,
                returnSpeed = 2f,
                clipSize = 3,
                shootTime = 0.33f,
                reloadTime = 1.5f,

                casingFX = "CasingFX"
            };

            horizonBombBay = new WeaponType("horizon-bomb-bay") {
                bulletType = Bullets.bombBullet,

                recoil = 0f,
                returnSpeed = 1f,

                clipSize = 10,
                shootTime = 0.2f,
                reloadTime = 5f,

                maxTargetDeviation = 360f,
                rotateSpeed = 0f,

                shootFX = "",
            };

            zenithMissiles = new WeaponType("zenith-missiles") {
                bulletType = new BulletType() {
                    damage = 7.5f,
                    lifeTime = 0.5f,
                    velocity = 100f
                },

                shootOffset = new Vector2(0, 0.25f),

                isIndependent = true,
                recoil = 0.05f,
                returnSpeed = 2f,
                clipSize = 10,
                shootTime = 0.2f,
                reloadTime = 3.5f,
                rotateSpeed = 115f
            };

            tempestWeapon = new WeaponType("tempest-weapon") {
                bulletType = Bullets.basicBullet,
                shootOffset = new Vector2(0, 0.5f),

                isIndependent = true,
                recoil = 0.1f,
                returnSpeed = 2f,
                clipSize = 12,
                shootTime = 0.075f,
                reloadTime = 2f,
                rotateSpeed = 90f,

                casingFX = "CasingFX"
            };

            stingerWeapon = new WeaponType("stinger-weapon") {
                bulletType = new BulletType() {
                    damage = 15f,
                    lifeTime = 0.5f,
                    velocity = 200f
                },
                shootOffset = new Vector2(0, 0.5f),

                isIndependent = true,
                recoil = 0.2f,
                clipSize = 5,
                shootTime = 0.3f,
                reloadTime = 4f,
                rotateSpeed = 60f,

                casingFX = "CasingFX"
            };

            pathWeapon = new WeaponType("path-weapon") {
                bulletType = new BulletType() {
                    damage = 3f,
                    lifeTime = 0.35f,
                    velocity = 150f
                },
                shootOffset = new Vector2(0, 0.75f),

                isIndependent = true,
                animations = new Animation[1] { new Animation("-belt", 3, Animation.Case.Shoot) },
                recoil = 0.02f,
                clipSize = 50,
                shootTime = 0.03f,
                reloadTime = 6f,
                rotateSpeed = 120f,

                casingFX = "CasingFX"
            };

            spreadWeapon = new WeaponType("spread-weapon") {
                bulletType = Bullets.basicBullet,
                shootOffset = Vector2.zero,

                barrels = new WeaponBarrel[4] {
                    new WeaponBarrel("spread-weapon", 1, new Vector2(-0.6f, 0.75f)),
                    new WeaponBarrel("spread-weapon", 4, new Vector2(0.6f, 0.75f)),
                    new WeaponBarrel("spread-weapon", 2, new Vector2(-0.4f, 0.55f)),
                    new WeaponBarrel("spread-weapon", 3, new Vector2(0.4f, 0.55f)),
                },

                isIndependent = true,
                recoil = 0.1f,
                clipSize = 20,
                shootTime = 0.085f,
                reloadTime = 6f,
                rotateSpeed = 100f,

                casingFX = "CasingFX"
            };

            missileRack = new WeaponType("missileRack", Items.missileX1) {
                bulletType = Bullets.missileBullet,
                shootOffset = new Vector2(0, 0.5f),

                isIndependent = true,
                consumesItems = true,
                maxTargetDeviation = 360f,

                clipSize = 1,
                reloadTime = 5f,
                rotateSpeed = 0f
            };
        }
    }

    [Serializable]
    public class BulletType : Content {
        [JsonIgnore] public GameObjectPool pool;
        public string bulletName;

        public float damage = 10f, buildingDamageMultiplier = 1f, velocity = 100f, lifeTime = 1f, size = 0.05f;
        public float blastRadius = -1f, blastRadiusFalloff = -1f, minimumBlastDamage = 0f;

        public float Range { get => velocity * lifeTime; }

        public string hitFX = "BulletHitFX", despawnFX = "DespawnFX";

        public BulletType(string name = null, string bulletName = "tracer") : base(name) {
            pool = PoolManager.GetOrCreatePool(AssetLoader.GetPrefab(bulletName + "-prefab", "tracer-prefab"), 100);
            this.bulletName = bulletName;

            pool.OnGameObjectCreated += OnPoolObjectCreated;
            pool.OnGameObjectDestroyed += OnPoolObjectDestroyed;
        }

        public float Multiplier(IDamageable damageable) {
            return damageable.IsBuilding() ? buildingDamageMultiplier : 1f;
        }

        public float Damage(IDamageable damageable, float distance) {
            float mult = Multiplier(damageable);
            return HasBlastDamage() ? Mathf.Lerp(damage * mult, minimumBlastDamage * mult, distance / blastRadius) : damage * mult;
        }

        public bool HasBlastDamage() {
            return blastRadius > 0;
        }

        public DamageHandler.Area Area() {
            return new DamageHandler.Area(damage, minimumBlastDamage, blastRadius, buildingDamageMultiplier, blastRadiusFalloff);
        }

        public virtual IEnumerator BulletBehaviour(Bullet bullet) {
            Transform transform = bullet.transform;
            int mask = bullet.mask;

            Vector2 startPosition = transform.position;
            Vector2 hitPos = (Vector2)transform.up * Range + startPosition;

            float distance = Vector2.Distance(startPosition, hitPos);
            float startingDistance = distance;

            transform.GetComponent<TrailRenderer>().Clear();

            while (distance > 0) {
                if (!transform) break;

                transform.position = Vector2.Lerp(startPosition, hitPos, 1 - (distance / startingDistance));
                distance -= Time.deltaTime * velocity;

                if (Physics2D.OverlapCircle(transform.position, size, mask)) {
                    bullet.OnBulletCollision();
                    break;
                }

                yield return null;
            }

            bullet.Return();
        }


        public virtual void OnPoolObjectCreated(object sender, GameObjectPool.PoolEventArgs e) {

        }

        public virtual void OnPoolObjectDestroyed(object sender, GameObjectPool.PoolEventArgs e) {

        }
    }

    [Serializable]
    public class BasicBulletType : BulletType {
        [JsonIgnore] public Sprite backSprite;

        public BasicBulletType(string name = null, string bulletName = "bullet") : base(name, bulletName) {
            sprite = AssetLoader.GetSprite(bulletName, true);
            backSprite = AssetLoader.GetSprite(bulletName + "-back", true);
        }

        public override void OnPoolObjectCreated(object sender, GameObjectPool.PoolEventArgs e) {
            if (!e.target) return;

            Transform transform = e.target.transform;
            Transform back = transform.GetChild(0);

            SpriteRenderer renderer;
            renderer = transform.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 1f, 1f);

            renderer = back.GetComponent<SpriteRenderer>();
            renderer.sprite = backSprite;
            renderer.color = new Color(0.8f, 0.8f, 0.8f);
        }

    }

    [Serializable]
    public class MissileBulletType : BasicBulletType {
        public float homingStrength = 30f;
        public bool canUpdateTarget = true, explodeOnDespawn = true;
        
        public MissileBulletType(string name = null, string bulletName = "missile") : base(name, bulletName) {
            despawnFX = "BulletHitFX";
        }


        public override IEnumerator BulletBehaviour(Bullet bullet) {
            // Get the bullet transform;
            Transform transform = bullet.transform;

            // The target the missile should follow
            byte enemyTeam = TeamUtilities.GetEnemyTeam(bullet.GetTeam());
            Transform target = MapManager.Map.GetClosestEntity(transform.position, enemyTeam).transform;

            bool hasCollided = false;
            transform.GetComponent<TrailRenderer>().Clear();

            float lifeTime = Time.time + this.lifeTime;
            while (Time.time < lifeTime) {
                if (!transform) break;

                // Try to update target
                if (!target && canUpdateTarget) {
                    Entity nTarget = MapManager.Map.GetClosestEntity(transform.position, enemyTeam);
                    if (!nTarget) break;
                    target = nTarget.transform;
                }
                
                // Move forward
                transform.position += Time.deltaTime * velocity * transform.up;

                // If has target rotate towards it
                if (target) {
                    Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, target.position - transform.position);
                    desiredRotation = Quaternion.Euler(0, 0, desiredRotation.eulerAngles.z);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, homingStrength * Time.fixedDeltaTime);
                }

                // Check for collision
                if (Physics2D.OverlapCircle(transform.position, size, bullet.mask)) {
                    bullet.OnBulletCollision();
                    hasCollided = true;
                    break;
                }

                yield return null;
            }

            // If has not collided, explode anyways
            if (!hasCollided && explodeOnDespawn) {
                bullet.OnDespawn();
            }

            bullet.Return();
        }
    }

    [Serializable]
    public class BombBulletType : BasicBulletType {
        public float fallVelocity = 3f, initialSize = 1f, finalSize = 0.5f;

        public BombBulletType(string name = null, string bulletName = "bomb") : base(name, bulletName) {
            hitFX = "ExplosionFX";
            despawnFX = "ExplosionFX";
        }

        public override void OnPoolObjectCreated(object sender, GameObjectPool.PoolEventArgs e) {
            base.OnPoolObjectCreated(sender, e);
            if (!e.target) return;

            Transform shadow = e.target.transform.GetChild(1);

            SpriteRenderer renderer = shadow.GetComponent<SpriteRenderer>();
            renderer.sprite = backSprite;
            renderer.color = new Color(0, 0, 0, 0.5f);
        }

        public override IEnumerator BulletBehaviour(Bullet bullet) {
            Transform transform = bullet.transform;
            Transform shadow = transform.GetChild(1);
            int mask = bullet.mask;

            // Bomb bullets don't work on block turrets
            float height = ((Unit)bullet.weapon.parentEntity).GetHeight();
            float maxHeight = ((Unit)bullet.weapon.parentEntity).Type.groundHeight;

            while (height > 0f) {
                height -= fallVelocity * Time.deltaTime;
                transform.localScale = Vector3.one * Mathf.Lerp(initialSize, finalSize, 1 - height / maxHeight);
                shadow.position = -Vector3.one * (height * 0.2f) + transform.position;

                yield return null;
            }

            if (Physics2D.OverlapCircle(transform.position, size, mask)) bullet.OnBulletCollision();
            else Effect.PlayEffect(despawnFX, transform.position, 1f);
            
            bullet.Return();
        }
    }

    public class Bullets {
        public const BulletType none = null;
        public static BulletType basicBullet, bigBullet, bombBullet, missileBullet;

        public static void Load() {
            basicBullet = new BulletType() {
                damage = 7.5f,
                lifeTime = 0.35f,
                buildingDamageMultiplier = 2f,
                velocity = 90f
            };

            bigBullet = new BulletType() {
                damage = 15f,
                lifeTime = 0.45f,
                buildingDamageMultiplier = 3f,
                velocity = 70f
            };

            bombBullet = new BombBulletType() {
                damage = 25f,
                minimumBlastDamage = 5f,
                blastRadius = 3f,
                buildingDamageMultiplier = 5f,
                fallVelocity = 4f
            };

            missileBullet = new MissileBulletType() {
                damage = 100f,
                minimumBlastDamage = 25f,
                blastRadius = 1f,
                buildingDamageMultiplier = 2f,
                velocity = 15f,
                lifeTime = 5f,
                homingStrength = 120f,
            };
        }
    }

    #endregion

    #region - Map -

    [Serializable]
    public class TileType : Content {
        [JsonIgnore] public Sprite[] allVariantSprites;
        [JsonIgnore] private Vector4[] allVariantSpriteUVs;
        [JsonIgnore] public Item itemDrop;
        public Color color;

        public int variants;
        public bool allowBuildings = true, flammable = false, isWater = false;
        public string itemDropName;

        public TileType(string name, int variants = 1, Item itemDrop = null) : base(name) {
            if (itemDrop != null) {
                this.itemDrop = itemDrop;
                itemDropName = itemDrop.name;
            }

            this.variants = variants;

            if (this.variants < 1) this.variants = 1;

            allVariantSprites = new Sprite[variants];
            allVariantSpriteUVs = new Vector4[variants];

            allVariantSprites[0] = sprite;
            for (int i = 1; i < this.variants; i++) allVariantSprites[i] = AssetLoader.GetAsset<Sprite>(name + (i + 1));

            color = sprite.texture.GetPixel(sprite.texture.width / 2, sprite.texture.height / 2);
        }

        public virtual Sprite[] GetAllTiles() {
            if (variants == 1) return new Sprite[1] { sprite };
            else return allVariantSprites;
        }

        public virtual Sprite GetRandomTileVariant() {
            if (variants == 1) return sprite;
            return allVariantSprites[Random.Range(0, variants - 1)];
        }

        public void SetSpriteUV(int index, Vector2 uv00, Vector2 uv11) {
            allVariantSpriteUVs[index] = new Vector4(uv00.x, uv00.y, uv11.x, uv11.y);
        }

        public Vector4 GetUV() {
            return GetSpriteVariantUV(0);
        }

        public Vector4 GetSpriteVariantUV(int index) {
            return allVariantSpriteUVs[index];
        }

        public override void Wrap() {
            base.Wrap();
            itemDropName = itemDrop == null ? "empty" : itemDrop.name;
        }

        public override void UnWrap() {
            base.UnWrap();
            itemDrop = itemDropName == "empty" ? null : ContentLoader.GetContentByName(itemDropName) as Item;
        }
    }

    [Serializable]
    public class OreTileType : TileType {
        public float oreThreshold, oreScale;

        public OreTileType(string name, int variants, Item itemDrop) : base(name, variants, itemDrop) {

        }
    }

    public class Tiles {
        public const TileType none = null;
        //Base tiles
        public static TileType darksandWater, darksand, deepWater, grass, ice, metalFloor, metalFloor2, metalFloorWarning, metalFloorDamaged, sandFloor, sandWater, shale, snow, stone, water;

        //Ore tiles
        public static TileType copperOre, leadOre, titaniumOre, coalOre, thoriumOre;

        //Wall tiles
        public static TileType daciteWall, dirtWall, duneWall, iceWall, saltWall, sandWall, shaleWall, grassWall, snowWall, stoneWall;

        public static void Load() {
            darksandWater = new TileType("darksand-water") {
                isWater = true,
            };

            darksand = new TileType("darksand", 3, Items.sand);

            deepWater = new TileType("deep-water") {
                allowBuildings = false,
                isWater = true,
            };

            grass = new TileType("grass", 3);

            ice = new TileType("ice", 3);

            metalFloor = new TileType("metal-floor");

            metalFloor2 = new TileType("metal-floor-2");

            metalFloorWarning = new TileType("metal-floor-warning");

            metalFloorDamaged = new TileType("metal-floor-damaged", 3);

            sandFloor = new TileType("sand-floor", 3, Items.sand);

            sandWater = new TileType("sand-water") {
                isWater = true,
            };

            shale = new TileType("shale", 3);

            snow = new TileType("snow", 3);

            stone = new TileType("stone", 3);

            water = new TileType("water") {
                allowBuildings = false,
                isWater = true,
            };

            copperOre = new OreTileType("ore-copper", 3, Items.copper);

            leadOre = new OreTileType("ore-lead", 3, Items.lead);

            titaniumOre = new OreTileType("ore-titanium", 3, Items.titanium);

            coalOre = new OreTileType("ore-coal", 3, Items.coal);

            thoriumOre = new OreTileType("ore-thorium", 3, Items.thorium);

            daciteWall = new TileType("dacite-wall", 2);

            dirtWall = new TileType("dirt-wall", 2);

            duneWall = new TileType("dune-wall", 2);

            iceWall = new TileType("ice-wall", 2);

            saltWall = new TileType("salt-wall", 2);

            sandWall = new TileType("sand-wall", 2);

            shaleWall = new TileType("shale-wall", 2);

            grassWall = new TileType("shrubs", 2);

            snowWall = new TileType("snow-wall", 2);

            stoneWall = new TileType("stone-wall", 2);
        }
    }

    public class MapType : Content{
        public Dictionary<int, TileType> tileReferences = new Dictionary<int, TileType>();

        public Wrapper2D<int> wrapper2D;
        public int[,] tileMapIDs;

        public MapType(string name, Wrapper2D<int> wrapper2D) : base(name) {
            this.wrapper2D = wrapper2D;
            this.wrapper2D.array2D.CopyTo(tileMapIDs, 0);
        }

        public TileType GetTileTypeAt(Vector2Int position) {
            int id = tileMapIDs[position.x, position.y];
            return tileReferences[id];
        }
    }

    #endregion

    #region - Items -

    [Serializable]
    public class Item : Content {
        public Color color;
        public float explosiveness = 0, flammability = 0, radioactivity = 0, charge = 0;
        public float hardness = 0, cost = 1;

        // Mass in tons per item piece
        public float mass = 0.01f;

        public bool lowPriority = false, buildable = true;

        public Item(string name) : base(name) {

        }
    }

    public class Items {
        public static Item copper, lead, titanium, coal, graphite, metaglass, sand, silicon, thorium;
        public static Item basicAmmo, missileX1;

        public static void Load() {
            copper = new Item("copper") {
                color = new Color(0xD9, 0x9D, 0x73),
                hardness = 1,
                cost = 0.5f
            };

            lead = new Item("lead") {
                color = new Color(0x8c, 0x7f, 0xa9),
                hardness = 1,
                cost = 0.7f
            };

            metaglass = new Item("metaglass") {
                color = new Color(0xeb, 0xee, 0xf5),
                cost = 1.5f
            };

            graphite = new Item("graphite") {
                color = new Color(0xb2, 0xc6, 0xd2),
                cost = 1f
            };

            sand = new Item("sand") {
                color = new Color(0xf7, 0xcb, 0xa4),
                lowPriority = true,
                buildable = false
            };

            coal = new Item("coal") {
                color = new Color(0x27, 0x27, 0x27),
                explosiveness = 0.2f,
                flammability = 1f,
                hardness = 2,
                buildable = false
            };

            titanium = new Item("titanium") {
                color = new Color(0x8d, 0xa1, 0xe3),
                hardness = 3,
                cost = 1f
            };

            thorium = new Item("thorium") {
                color = new Color(0xf9, 0xa3, 0xc7),
                explosiveness = 0.2f,
                hardness = 4,
                radioactivity = 1f,
                cost = 1.1f
            };

            silicon = new Item("silicon") {
                color = new Color(0x53, 0x56, 0x5c),
                cost = 0.8f
            };

            basicAmmo = new Item("basicAmmo") {
                color = new Color(0xD9, 0x9D, 0x73),
                flammability = 0.1f,
                buildable = false
            };

            missileX1 = new Item("missileX1") {
                color = new Color(0x53, 0x56, 0x5c),
                explosiveness = 0.75f,
                flammability = 0.2f,
                charge = 0.1f,
                cost = 5,
                buildable = false
            };
        }
    }

    #endregion

    #region - Structures - 

    /// <summary>
    /// Stores the amount of a defined item
    /// </summary>
    [Serializable]
    public class ItemStack {
        [JsonIgnore]
        public Item item;

        public string itemName;
        public int amount;

        public ItemStack() {
            item = Items.copper;
        }

        public ItemStack(Item item, int amount = 0) {
            if (item == null) item = Items.copper;
            this.item = item;
            this.amount = amount;

            itemName = item.name;
        }

        public ItemStack Set(Item item, int amount) {
            this.item = item;
            this.amount = amount;
            return this;
        }

        public ItemStack Copy() {
            return new ItemStack(item, amount);
        }

        public bool Equals(ItemStack other) {
            return other != null && other.item == item && other.amount == amount;
        }

        public static Item[] ToItems(ItemStack[] stacks) {
            Item[] items = new Item[stacks.Length];
            for(int i = 0; i < stacks.Length; i++) items[i] = stacks[i].item;
            return items;
        }

        public static ItemStack Multiply(ItemStack stack, float amount) {
            return new ItemStack(stack.item, Mathf.RoundToInt(stack.amount * amount));
        }

        public static ItemStack[] Multiply(ItemStack[] stacks, float amount) {
            ItemStack[] copy = new ItemStack[stacks.Length];
            for (int i = 0; i < copy.Length; i++) {
                copy[i] = new ItemStack(stacks[i].item, Mathf.RoundToInt(stacks[i].amount * amount));
            }
            return copy;
        }

        public static ItemStack[] With(params object[] items) {
            ItemStack[] stacks = new ItemStack[items.Length / 2];
            for (int i = 0; i < items.Length; i += 2) {
                stacks[i / 2] = new ItemStack((Item)items[i], (int)items[i + 1]);
            }
            return stacks;
        }

        public static int[] Serialize(ItemStack[] stacks) {
            int[] serializedArray = new int[stacks.Length * 2];
            for (int i = 0; i < serializedArray.Length; i += 2) {
                serializedArray[i] = stacks[i].item.id;
                serializedArray[i + 1] = stacks[i].amount;
            }
            return serializedArray;
        }

        public static ItemStack[] DeSerialize(int[] serializedArray) {
            ItemStack[] stacks = new ItemStack[serializedArray.Length / 2];
            for (int i = 0; i < serializedArray.Length; i += 2) {
                stacks[i / 2] = new ItemStack((Item)ContentLoader.GetContentById((short)serializedArray[i]), (int)serializedArray[i + 1]);
            }
            return stacks;
        }

        public static List<ItemStack> List(params object[] items) {
            List<ItemStack> stacks = new(items.Length / 2);
            for (int i = 0; i < items.Length; i += 2) {
                stacks.Add(new ItemStack((Item)items[i], (int)items[i + 1]));
            }
            return stacks;
        }
    }

    public class Inventory {
        public event EventHandler OnAmountChanged;

        public Dictionary<Item, int> items;
        public Item[] allowedItems;

        public int amountCap;
        public float maxMass;

        public bool singleItem;

        public Inventory(int amountCap = -1, float maxMass = -1f, Item[] allowedItems = null) {
            items = new Dictionary<Item, int>();
            this.amountCap = amountCap;
            this.maxMass = maxMass;
            this.allowedItems = allowedItems;
            singleItem = false;
        }

        public Inventory(bool singleItem, int amountCap = -1, float maxMass = -1f) {
            items = new Dictionary<Item, int>();
            this.amountCap = amountCap;
            this.maxMass = maxMass;
            this.singleItem = singleItem;
        }

        public void Clear() {
            items = new Dictionary<Item, int>();
        }

        public ItemStack[] ToArray() {
            ItemStack[] stacks = new ItemStack[items.Count];
            int i = 0;

            foreach(KeyValuePair<Item, int> valuePair in items) {
                stacks[i] = new(valuePair.Key, valuePair.Value);
                i++;
            }
        
            return stacks;
        }

        public Item[] ToItems() {
            return items.Keys.ToArray();
        }

        public void SetAllowedItems(Item[] allowedItems) {
            this.allowedItems = allowedItems;
        }

        public Item First() {
            foreach (Item item in items.Keys) if (items[item] != 0) return item;
            return null;
        }

        public Item First(Item[] filter) {
            foreach (Item item in items.Keys) if (filter.Contains(item) && items[item] != 0) return item;
            return null;
        }

        public bool Empty() {
            foreach (int amount in items.Values) if (amount != 0) return false;   
            return true;
        }

        public bool Empty(Item[] filter) {
            foreach (Item item in items.Keys) if (filter.Contains(item) && items[item] != 0) return false;
            return true;
        }

        public bool Contains(Item item) {
            return items.ContainsKey(item);
        }

        public bool Has(Item item, int amount) {
            if (!Contains(item)) return false;
            else return items[item] >= amount;
        }

        public bool Has(ItemStack stack) {
            return Has(stack.item, stack.amount);
        }

        public bool Has(ItemStack[] stacks) {
            foreach(ItemStack itemStack in stacks) if (!Has(itemStack)) return false;    
            return true;
        }

        public bool HasToMax(Item item, int amount) {
            if (!Contains(item)) return false;
            else return items[item] >= amount || items[item] == amountCap;
        }

        public bool HasToMax(ItemStack stack) {
            return HasToMax(stack.item, stack.amount);
        }

        public bool HasToMax(ItemStack[] stacks) {
            foreach (ItemStack itemStack in stacks) if (!HasToMax(itemStack)) return false;
            return true;
        }

        public bool Allowed(Item item) {
            return (allowedItems == null || allowedItems.Contains(item)) && AllowedSingleItem(item);
        }

        private bool AllowedSingleItem(Item item) {
            return !singleItem || items.Count == 0 || items.ElementAt(0).Key == item;
        }

        public bool Allowed(Item[] items) {
            foreach (Item item in items) if (!Allowed(item)) return false;
            return true;
        }

        public bool Empty(Item item) {
            return Contains(item) && items[item] == 0;
        }

        public bool Full(Item item) {
            return amountCap != -1f && Has(item, amountCap);
        }

        public int AmountToFull(Item item) {
            if (!Contains(item)) return amountCap;
            return amountCap - items[item];
        }

        public int Add(Item item, int amount, bool update = true) {
            if (Full(item) || amount == 0 || !Allowed(item)) return amount;
            if (!Contains(item)) items.Add(item, 0);

            int amountToReturn = amountCap == -1 ? 0 : Mathf.Clamp(items[item] + amount - amountCap, 0, amount);
            items[item] += amount - amountToReturn;

            if (update) AmountChanged();
            return amountToReturn;
        }

        public int Add(ItemStack stack, bool update = true) {
            int value = Add(stack.item, stack.amount, false);
            if (update) AmountChanged();
            return value;
        }

        public void Add(ItemStack[] stacks) {
            foreach (ItemStack itemStack in stacks) Add(itemStack, false);
            AmountChanged();
        }

        public int Max(Item item) {
            if (!Contains(item)) {
                if (!Allowed(item)) return 0;
                else return amountCap;
            }
            return amountCap - items[item];
        }

        public int[] Max(Item[] items) {
            int[] maxItems = new int[items.Length];
            for (int i = 0; i < items.Length; i++) maxItems[i] = Max(items[i]);
            return maxItems;
        }

        public bool Fits(Item item, int amount) {
            return amount <= Max(item) && Allowed(item);
        }

        public bool Fits(ItemStack stack) {
            return Fits(stack.item, stack.amount);
        }

        public bool Fits(ItemStack[] stacks) {
            foreach (ItemStack stack in stacks) if (!Fits(stack)) return false;
            return true;
        }

        public int Substract(Item item, int amount, bool update = true) {
            if (!Contains(item) || Empty(item) || amount == 0 || !Allowed(item)) return amount;

            int amountToReturn = Mathf.Clamp(amount - items[item], 0, amount);
            items[item] -= amount - amountToReturn;

            if (items[item] == 0) items.Remove(item);

            if (update) AmountChanged();
            return amountToReturn;
        }

        public int Substract(ItemStack itemStack, bool update = true) {
            int value = Substract(itemStack.item, itemStack.amount, false);
            if (update) AmountChanged();
            return value;
        }

        public void Substract(ItemStack[] itemStacks) {
            foreach (ItemStack itemStack in itemStacks) Substract(itemStack, false);
            AmountChanged();
        }

        public int MaxTransferAmount(Inventory other, Item item) {
            if (!Contains(item) || !other.Allowed(item)) return 0;
            return Mathf.Min(items[item], other.Max(item));
        }

        public int MaxTransferAmount(Inventory other, ItemStack stack) {
            if (!Contains(stack.item) || !other.Allowed(stack.item)) return 0;
            return Mathf.Min(stack.amount, items[stack.item], other.Max(stack.item));
        }

        public int MaxTransferSubstractAmount(Inventory other, ItemStack stack) {
            if (!other.Contains(stack.item) || !Contains(stack.item)) return 0;
            return Mathf.Min(stack.amount, items[stack.item], other.items[stack.item]);
        }

        public void TransferAll(Inventory other) {
            ItemStack[] stacksToSend = ToArray();
        
            for(int i = 0; i < stacksToSend.Length; i++) {
                ItemStack stack = stacksToSend[i];
                stack.amount = MaxTransferAmount(other, stack.item);
            }

            Transfer(other, stacksToSend);
        }

        public void TransferAll(Inventory other, Item[] filter) {
            ItemStack[] stacksToSend = new ItemStack[filter.Length];

            for (int i = 0; i < filter.Length; i++) {
                Item item = filter[i];
                int amountToSend = MaxTransferAmount(other, item);
                stacksToSend[i] = new ItemStack(item, amountToSend);
            }

            Transfer(other, stacksToSend);
        }

        public void TransferAmount(Inventory other, ItemStack[] stacks) {
            ItemStack[] stacksToSend = new ItemStack[stacks.Length];

            for (int i = 0; i < stacks.Length; i++) {
                ItemStack stack = stacks[i];
                int amountToSend = MaxTransferAmount(other, stack);
                stacksToSend[i] = new ItemStack(stack.item, amountToSend);
            }

            Transfer(other, stacksToSend);
        }

        public void TransferSubstractAmount(Inventory other, ItemStack[] stacks) {
            ItemStack[] stacksToSend = new ItemStack[stacks.Length];

            for (int i = 0; i < stacks.Length; i++) {
                ItemStack stack = stacks[i];
                int amountToSend = MaxTransferSubstractAmount(other, stack);
                stacksToSend[i] = new ItemStack(stack.item, amountToSend);
            }

            TransferSubstract(other, stacksToSend);
        }

        public void Transfer(Inventory other, ItemStack[] stacks) {
            Substract(stacks);
            other.Add(stacks);
        }

        public void TransferSubstract(Inventory other, ItemStack[] stacks) {
            Substract(stacks);
            other.Substract(stacks);
        }

        public void AmountChanged() {
            OnAmountChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public struct SerializableItemList {
        public ItemStack[] itemStacks;
        public int maxCapacity;
        public float maxMass;

        public SerializableItemList(Dictionary<Item, ItemStack> itemStacks, int maxCapacity, float maxMass) {
            this.itemStacks = new ItemStack[itemStacks.Count];
            itemStacks.Values.CopyTo(this.itemStacks, 0);
            this.maxCapacity = maxCapacity;
            this.maxMass = maxMass;
        }
    }

    public struct WeaponMount {
        [JsonIgnore] public WeaponType weapon;
        public string weaponName;
        public Vector2 position;
        public bool mirrored;
        public bool onTop;

        public WeaponMount(WeaponType weapon, Vector2 position, bool mirrored = false, bool onTop = false) {
            this.weapon = weapon;
            this.weaponName = weapon.name;
            this.position = position;
            this.mirrored = mirrored;
            this.onTop = onTop;
        }
    }

    public struct WeaponBarrel {
        [JsonIgnore] public Sprite barrelSprite;
        [JsonIgnore] public Sprite barrelOutlineSprite;
        public Vector2 shootOffset;

        public WeaponBarrel(string name, int barrelNum, Vector2 shootOffset) {
            barrelSprite = AssetLoader.GetSprite(name + "-barrel" + barrelNum);
            barrelOutlineSprite = AssetLoader.GetSprite(name + "-barrel" + "-outline" + barrelNum);
            this.shootOffset = shootOffset;
        }
    }

    public struct UnitPlan {
        public string unitName;
        public float craftTime;
        public ItemStack[] materialList;

        public UnitPlan(UnitType unit, float craftTime, ItemStack[] materialList) {
            this.unitName = unit.name;
            this.materialList = materialList;
            this.craftTime = craftTime;
        }

        public UnitType GetUnit() => (UnitType)ContentLoader.GetContentByName(unitName);
    }

    public struct CraftPlan {
        public ItemStack productStack;
        public float craftTime;
        public ItemStack[] materialList;

        public CraftPlan(ItemStack productStack, float craftTime, ItemStack[] materialList) {
            this.productStack = productStack;
            this.materialList = materialList;
            this.craftTime = craftTime;
        }
    }

    public struct UnitRotor {
        [JsonIgnore] public Sprite sprite, blurSprite, topSprite;
        public UnitRotorBlade[] blades;
        public Vector2 offset;
        public float
            velocity,        // The maximum rotor angular velocity
            velocityIncrease, // The maximum velocity gain per second
            blurStart,        // The angular vel at wich the rotor sprite starts to interpolate to the blur sprite
            blurEnd;          // The angular vel at wich the rotor completely switches to the blur sprite

        /// <summary>
        /// Creates a rotor container
        /// </summary>
        /// <param name="unitName">The name of the unit and the rotor, example "flare-rotor"</param>
        public UnitRotor(string unitName, Vector2 offset, float velocity, float velocityIncrease, float blurStart, float blurEnd, UnitRotorBlade[] blades) {
            sprite = AssetLoader.GetSprite($"{unitName}");
            blurSprite = AssetLoader.GetSprite($"{unitName}-blur");
            topSprite = AssetLoader.GetSprite($"{unitName}-top");

            this.blades = blades;
            this.offset = offset;

            this.velocity = velocity;
            this.velocityIncrease = velocityIncrease;
            this.blurStart = blurStart;
            this.blurEnd = blurEnd;
        }

        /// <summary>
        /// Creates a rotor container
        /// </summary>
        /// <param name="unitName">The name of the unit and the rotor, example "flare-rotor"</param>
        public UnitRotor(string unitName, Vector2 offset, float velocity, float velocityIncrease, float blurStart, float blurEnd, int blades = 1) {
            sprite = AssetLoader.GetSprite($"{unitName}");
            blurSprite = AssetLoader.GetSprite($"{unitName}-blur");
            topSprite = AssetLoader.GetSprite($"{unitName}-top");

            this.blades = new UnitRotorBlade[blades];
            for (int i = 0; i < blades; i++) this.blades[i] = new(360f / blades * i);

            this.offset = offset;

            this.velocity = velocity;
            this.velocityIncrease = velocityIncrease;
            this.blurStart = blurStart;
            this.blurEnd = blurEnd;
        }

        public float BlurValue(float velocity) {
            return Mathf.Clamp01(velocity - blurStart / blurEnd - blurStart);
        }
    }

    public struct UnitRotorBlade {
        public bool counterClockwise;
        public float offset;

        public UnitRotorBlade(float offset, bool counterClockwise = false) {
            this.offset = offset;
            this.counterClockwise = counterClockwise;
        }
    }

    #endregion
}

namespace Frontiers.Content.Maps {
    public class MapLoader {
        public const int TilesPerString = 1000;
        public static string[] mapNames;

        public static event EventHandler<MapLoadedEventArgs> OnMapLoaded;
        public class MapLoadedEventArgs {
            public Map loadedMap;
        }

        public static void RefreshMapNames() {
            string[] mapDirectories = Directory.GetDirectories(Directories.maps);
            for (int i = 0; i < mapDirectories.Length; i++) mapNames[i] = Path.GetDirectoryName(mapDirectories[i]);
        }

        public static void ReciveMap(string name, Vector2 size, string[] tileData) {
            MapData mapData = CreateMap(Vector2Int.CeilToInt(size), tileData);
            Map map = new(name, mapData);
            OnMapLoaded?.Invoke(null, new MapLoadedEventArgs() { loadedMap = map });
        }

        public static void LoadMap(string name) {
            MapData mapData = ReadMap(name);       
            Map map = new(name, mapData);
            OnMapLoaded?.Invoke(null, new MapLoadedEventArgs() { loadedMap = map });
        }

        public static void SaveMap(Map map) {
            StoreMap(map.name, map.GetMapData());
        }

        public static void StoreMap(string name, MapData mapData) {
            string mapName = Path.Combine("Maps", name);
            QuickSaveRaw.Delete(mapName + ".json");
            QuickSaveWriter writer = QuickSaveWriter.Create(mapName);

            writer.Write("data", mapData);
            writer.Commit();
        }

        public static MapData ReadMap(string name) {
            name = Path.Combine("Maps", name);
            QuickSaveReader reader = QuickSaveReader.Create(name);
            return reader.Read<MapData>("data");    
        }

        public static MapData CreateMap(Vector2Int size, string[] tileData) {
            return new MapData(size, tileData);
        }
    }

    public static class MapDisplayer {
        public static Material atlasMaterial;
        public static Texture2D atlas;

        public static void SetupAtlas() {
            Launcher.SetState("Loading Map Atlas...");

            // Generate tile texture atlas
            Texture2D atlas = MapTextureGenerator.GenerateTileTextureAtlas();
            MapDisplayer.atlas = atlas;

            // Create atlas material
            atlasMaterial = AssetLoader.GetAsset<Material>("Atlas Material");
            atlasMaterial.mainTexture = atlas;

            Launcher.SetState("Map Atlas Loaded");
        }
    }

    public class RegionDisplayer {
        readonly MeshFilter meshFilter;

        public RegionDisplayer(Region region) {
            Transform transform = new GameObject("Map Region", typeof(MeshRenderer), typeof(MeshFilter)).transform;
            transform.position = (Vector2)region.offset;

            // Get the mesh renderer component and apply the atlas material
            MeshRenderer meshRenderer = transform.GetComponent<MeshRenderer>();
            meshRenderer.material = MapDisplayer.atlasMaterial;

            // Get the mesh filter component and apply the region mesh to it
            meshFilter = transform.GetComponent<MeshFilter>();
            meshFilter.mesh = MapMeshGenerator.GenerateMesh(region);
        }

        public void Update(Region region) {
            // Update the region's mesh
            meshFilter.mesh = MapMeshGenerator.GenerateMesh(region);
        }
    }

    public static class MapMeshGenerator {
        public static TileType[] allTiles;

        public static TileType RANDOMGEN() {
            allTiles ??= ContentLoader.GetContentByType<TileType>();

            return allTiles[Random.Range(0, allTiles.Length)];
        }

        public static Mesh GenerateMesh(Region region) {
            // Initialize the needed variables
            Tile[,] tilemap = region.tilemap;
            Vector2Int size = new(tilemap.GetLength(0), tilemap.GetLength(1));
            MeshData meshData = new(region.GetRenderedTileCount());

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    // Get the tile UVs corresponding to the region coords
                    Tile tile = tilemap[x, y];

                    // Get solid tile
                    TileType tileType = tile.Layer(MapLayer.Solid);

                    // If is not solid, try to render the ground and ore layers
                    if (tileType == null) { 
                        tileType = tile.Layer(MapLayer.Ground); 
                        if (tileType != null) AddTileToMesh(x, y, meshData, tileType);
                        
                        tileType = tile.Layer(MapLayer.Ore);
                        if (tileType != null) AddTileToMesh(x, y, meshData, tileType);
                    } else {
                        AddTileToMesh(x, y, meshData, tileType);
                    }
                }
            }

            return meshData.CreateMesh();
        }

        public static void AddTileToMesh(int x, int y, MeshData meshData, TileType tileType) {
            if (tileType == null) return;
            Vector4 UVs = tileType.GetSpriteVariantUV(Random.Range(0, tileType.variants));

            // Get the atlas UVs
            Vector2 uv00 = new(UVs.x, UVs.y);
            Vector2 uv11 = new(UVs.z, UVs.w);

            meshData.AddQuad(x, y, uv00, uv11);
        }

        public static TileType TryGetBaseTileType(Tile tile) {
            TileType tileType = tile.Layer(MapLayer.Solid);
            if (tileType == null) tileType = tile.Layer(MapLayer.Ground);
            return tileType;
        }

        public class MeshData {
            readonly Vector3[] vertices;
            readonly Vector2[] uvs;
            readonly int[] triangles;

            int index = 0;

            public MeshData(int tiles) {
                vertices = new Vector3[tiles * 4];
                uvs = new Vector2[tiles * 4];
                triangles = new int[tiles * 6];
            }

            public void AddQuad(int x, int y, Vector2 uv00, Vector2 uv11) {
                // Some funky shit that is needed to create a quad
                triangles[index * 6 + 0] = index * 4 + 0;
                triangles[index * 6 + 1] = index * 4 + 1;
                triangles[index * 6 + 2] = index * 4 + 2;

                triangles[index * 6 + 3] = index * 4 + 0;
                triangles[index * 6 + 4] = index * 4 + 2;
                triangles[index * 6 + 5] = index * 4 + 3;

                uvs[index * 4 + 0] = new Vector2(uv00.x, uv00.y);
                uvs[index * 4 + 1] = new Vector2(uv00.x, uv11.y);
                uvs[index * 4 + 2] = new Vector2(uv11.x, uv11.y);
                uvs[index * 4 + 3] = new Vector2(uv11.x, uv00.y);
                
                vertices[index * 4 + 0] = new Vector2(x, y);
                vertices[index * 4 + 1] = new Vector2(x, y + 1);
                vertices[index * 4 + 2] = new Vector2(x + 1, y + 1);
                vertices[index * 4 + 3] = new Vector2(x + 1, y);

                index++;
            }

            public Mesh CreateMesh() {
                // Create an actual mesh from the given data 
                Mesh mesh = new();
                mesh.vertices = vertices;
                mesh.uv = uvs;
                mesh.triangles = triangles;
                mesh.RecalculateNormals();
                return mesh;
            }
        }
    }

    public static class MapTextureGenerator {

        public static Texture2D GenerateTileTextureAtlas() {
            // Get all tiles loaded by the content loader
            TileType[] tiles = ContentLoader.GetContentByType<TileType>();
            List<SpriteLink> links = new();

            // Foreach sprite and variant in each tile, add them to a list and link them to their parent tile
            for (int i = 0; i < tiles.Length; i++) {
                TileType tileType = tiles[i];

                for (int v = 0; v < tileType.variants; v++) {
                    links.Add(new SpriteLink(tileType, v));
                }
            }

            // All sprites in the array should be squares with the same size
            int spriteSize = links[0].sprite.texture.width;

            // Get the maximum side length the atlas can go to with the given sprites
            int atlasSize = Mathf.CeilToInt(Mathf.Sqrt(links.Count));

            // Create the texture with the given size
            Texture2D atlasTexture = new(atlasSize * spriteSize, atlasSize * spriteSize);
            int index = 0;
            
            for (int x = 0; x < atlasSize; x++) {
                for (int y = 0; y < atlasSize; y++) {

                    // If there are no more sprites, add a transparent gap
                    // This happens because the atlas is an exact square, so the shape often cant fit perfectly all the sprites
                    if (index >= links.Count) {
                        for (int px = 0; px < spriteSize; px++) {
                            for (int py = 0; py < spriteSize; py++) {

                                // Get the pixel coord and set it to transparent
                                Vector2Int atlasPixelPosition = new(x * spriteSize + px, y * spriteSize + py);
                                atlasTexture.SetPixel(atlasPixelPosition.x, atlasPixelPosition.y, new Color(0, 0, 0, 0));
                            }
                        }

                        continue;
                    }

                    // Get the current sprite that will be added to the atlas
                    SpriteLink link = links[index];
                    Texture2D spriteTexture = link.sprite.texture;

                    // Add each pixel from the sprite to the atlas
                    for (int px = 0; px < spriteSize; px++) {
                        for (int py = 0; py < spriteSize; py++) {

                            // Get the pixel position in the sprite and the one relative to the atlas
                            Vector2Int spritePixelPosition = new(px, py); 
                            Vector2Int atlasPixelPosition = new(x * spriteSize + px, y * spriteSize + py);

                            // Apply the sprite pixel on the atlas
                            Color pixelColor = spriteTexture.GetPixel(spritePixelPosition.x, spritePixelPosition.y);
                            atlasTexture.SetPixel(atlasPixelPosition.x, atlasPixelPosition.y, pixelColor);
                        }
                    }

                    // Get the UVs of the sprite on the atlas (reminder for me: these are the sprite positions on the canvas)
                    Vector2 uv00 = new Vector2(x, y) / atlasSize;
                    Vector2 uv11 = new Vector2(x + 1, y + 1) / atlasSize;

                    // Assign the UVs to the tile type
                    TileType tileType = link.tileType;
                    tileType.SetSpriteUV(link.variant, uv00, uv11);

                    index++;
                }
            }

            // Some settings that need to be aplied to the texture2d for this case
            atlasTexture.filterMode = FilterMode.Point;
            atlasTexture.wrapMode = TextureWrapMode.Clamp;
            atlasTexture.Apply();

            return atlasTexture;
        }

        private class SpriteLink {
            public Sprite sprite;
            public TileType tileType;
            public int variant;

            // Links a sprite to a tile type, only used for the creation of the atlas
            public SpriteLink(TileType tileType, int variant) {
                sprite = tileType.allVariantSprites[variant];
                this.tileType = tileType;
                this.variant = variant;
            }
        }
    }

    public class Tile {
        public Vector2Int position;
        public TileType[] tiles;
        public Block block;

        public Tile(Vector2Int position) {
            this.position = position;
            tiles = new TileType[(int)MapLayer.Total];
            block = null;
        }

        public void Set(TileType tileType, MapLayer layer) {
            tiles[(int)layer] = tileType;
        }
        
        public void Set(Block block) {
            this.block = block;
        }

        public TileType Layer(MapLayer layer) {
            return tiles[(int)layer];
        }

        public bool HasTile(MapLayer layer) {
            return tiles[(int)layer] != null;
        }

        public bool HasBlock() {
            return block != null;
        }

        public bool IsSolid() {
            return HasBlock() || HasTile(MapLayer.Solid);
        }

        public bool IsWalkable() {
            return !tiles[(int)MapLayer.Ground].isWater;
        }

        public void LoadTile(string data) {
            // Loads all layers of the tile from a single string
            // Used to recive map data across the network 
            for(int i = 0; i < data.Length; i++) {
                int id = Convert.ToInt32(data[i]) - 32;
                if (id == 0) continue;
                Set((TileType)ContentLoader.GetContentById((short)id), (MapLayer)i);
            }
        }

        public override string ToString() {
            // Writes all the ids of the tiles on all layers in a single string
            // Used to transfer the map data across the network
            string data = "";

            for (int i = 0; i < (int)MapLayer.Total; i++) {
                TileType tileType = tiles[i];
                data += tileType == null ? (char)32 : (char)(tileType.id + 32); 
            }

            return data;
        }

        public string[] ToNames() {
            // Saves the names of all the tiles to a list of strings
            // Used for faster tile management but ineficient storage
            string[] names = new string[(int)MapLayer.Total];

            for(int i = 0; i < names.Length; i++) {
                TileType tileType = tiles[i];
                names[i] = tileType == null ? null : tiles[i].name;
            }

            return names;
        }
    }

    public class Tilemap {
        public Region[,] regions;
        public Vector2Int regionSize;
        public Vector2Int regionCount;
        public Vector2Int size;

        public struct Region {
            public Tile[,] tilemap;
            public Vector2Int size;

            public Vector2Int offset;
            public RegionDisplayer displayer;

            public bool holdMeshUpdate;
            public bool wasChanged;

            public Region(Vector2Int size, Vector2Int offset) {
                // Store given parameters
                this.size = size;
                this.offset = offset;
                displayer = null;
                holdMeshUpdate = false;
                wasChanged = false;

                // Create a new tile array
                tilemap = new Tile[size.x, size.y];

                // Create all the tiles (now they are set randomly temporarely)
                for (int x = 0; x < size.x; x++) {
                    for (int y = 0; y < size.y; y++) {
                        tilemap[x, y] = new Tile(new Vector2Int(x, y));
                    }
                }

                displayer = new RegionDisplayer(this);
            }

            public Tile GetTile(Vector2Int position) {
                // Get a local tile from a world position
                Vector2Int localPosition = position - offset;
                return tilemap[localPosition.x, localPosition.y];
            }

            public void SetTile(TileType tileType, Vector2Int position, MapLayer layer) {
                // Sets a tile type of a tile
                GetTile(position).Set(tileType, layer);

                if (holdMeshUpdate) wasChanged = true; 
                else displayer.Update(this);
            }

            public void SetTile(string data, Vector2Int position) {
                // Load a tile from the given string data
                GetTile(position).LoadTile(data);
            }

            public void HoldMeshUpdate(bool state) {
                holdMeshUpdate = state;

                if (state) {
                    wasChanged = false;
                } else {
                    if (wasChanged) displayer.Update(this);
                    wasChanged = false;
                }
            }

            public int GetRenderedTileCount() {
                // Get the total tile count that should be renderer;
                int counter = 0;

                for (int x = 0; x < size.x; x++) {
                    for (int y = 0; y < size.y; y++) {
                        Tile tile = tilemap[x, y];

                        if (tile.Layer(MapLayer.Solid) != null) counter++;
                        else {
                            if (tile.Layer(MapLayer.Ground) != null) counter++;
                            if (tile.Layer(MapLayer.Ore) != null) counter++;
                        }
                    }
                }

                return counter;
            }
        }

        public Tilemap(Vector2Int size, Vector2Int regionSize) {
            // Store given parameters
            this.size = size;
            this.regionSize = regionSize;

            // Create the region array
            regionCount = new(size.x / regionSize.x, size.y / regionSize.y);
            regions = new Region[regionCount.x, regionCount.y];


            // Create all the region structs
            for (int x = 0; x < regionCount.x; x++) {
                for (int y = 0; y < regionCount.y; y++) {
                    regions[x, y] = new Region(regionSize, new Vector2Int(x, y) * regionSize);                 
                }
            }
        }

        public void HoldMeshUpdate(bool state) {
            // Used when updating the whole map is needed, to not call update 5000000 times, only once finished
            for (int x = 0; x < regions.GetLength(0); x++) {
                for (int y = 0; y < regions.GetLength(1) ; y++) {
                    regions[x, y].HoldMeshUpdate(state);
                }
            }
        }

        public Tile GetTile(Vector2Int position) {
            // Get a tile from a region
            int x = position.x / regionSize.x;
            int y = position.y / regionSize.y;
            return regions[x, y].GetTile(position);
        }

        public TileType GetTile(Vector2Int position, MapLayer layer) {
            // Get a tile type from a region
            return GetTile(position).Layer(layer);
        }

        public void SetTile(TileType tileType, Vector2Int position, MapLayer layer) {
            // Set a region's tile
            regions[position.x / regionSize.x, position.y / regionSize.y].SetTile(tileType, position, layer);
        }

        public void SetBlock(Vector2Int position, Block block) {
            // Set a block on a tile
            GetTile(position).Set(block);
        }

        public void SetTile(Vector2Int position, string data) {
            // Load a tile from the given string data
            GetTile(position).LoadTile(data);
        }
    }

    public class Map {
        public string name;

        public List<Entity> loadedEntities = new();
        public Dictionary<TileBase, TileType> tileTypeDictionary = new();

        public Tilemap tilemap;

        public Dictionary<Vector2Int, Block> blockPositions = new();
        public List<Block> blocks = new();
        public List<Unit> units = new();

        public MapData mapData;

        public Vector2Int size;
        public bool loaded;

        // All the layers on the map, the last enum should have as number the amount of layers without counting itself
        public enum MapLayer {
            Ground = 0,
            Ore = 1,
            Solid = 2,
            Total = 3
        }

        public Map(string name, int width, int height, Tilemap tilemap) {
            // Create a map
            this.name = name;
            this.tilemap = tilemap;
            size = new(width, height);

            loaded = true;
        }

        public Map(string name, MapData mapData) {
            // Create a map with the given data

            // Store basic values
            this.name = name;
            this.mapData = mapData;
            size = mapData.size;

            // Create an empty tilemap
            tilemap = new(size, Vector2Int.one * Main.RegionSize);

            // Fill the tilemap with the given tile data
            LoadTilemapData(mapData.tilemapData.DecodeThis());

            // End loading
            loaded = true;
        }

        public void LoadTilemapData(string[,,] tileNameArray) {
            // Load the tilemap from an array of the names of the tiles 

            // Get the amount of layers
            int layers = (int)MapLayer.Total;

            tilemap.HoldMeshUpdate(true);

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    for (int z = 0; z < layers; z++) {
                        // Get the tile corresponding to the name and set on the tilemap
                        string name = tileNameArray[x, y, z];
                        if (name != null) tilemap.SetTile(GetTileType(name), new Vector2Int(x, y), (MapLayer)z);
                    }
                }
            }

            tilemap.HoldMeshUpdate(false);
        }

        public string[,,] SaveTilemapData() {
            // Save the current state of the tilemap into an array of strings with the names of all the tiles

            // Initialize arrays
            int layers = (int)MapLayer.Total;
            string[,,] returnArray = new string[size.x, size.y, layers];

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {

                    // Get the names of the current tile
                    string[] names = tilemap.GetTile(new Vector2Int(x, y)).ToNames();

                    // Store each given name on the main array
                    for (int layer = 0; layer < layers; layer++) {
                        string name = names[layer];
                        returnArray[x, y, layer] = name;
                    }
                }
            }
            return returnArray;
        }


        public string[] TilemapsToStringArray() {
            // Encode the current state of the tilemap to a single string array
            // Used for network transmission

            // Initialize arrays

            // Split into various strings to pass over the string character limit
            string[] tileData = new string[Mathf.CeilToInt(size.x * size.y * (int)MapLayer.Total / MapLoader.TilesPerString) + 1];
            int i = 0;

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    // Get the tile and encode it's tile types id's
                    int stringIndex = Mathf.FloorToInt(i / MapLoader.TilesPerString);
                    tileData[stringIndex] += tilemap.GetTile(new Vector2Int(x, y)).ToString();
                    i++;
                }
            }

            return tileData;
        }

        public void SetTilemapsFromStringArray(Vector2Int size, string[] tileData) {
            // Load the given encoded string array

            // Initialize vars
            int layers = (int)MapLayer.Total;
            int i = 0;

            for (int x = 0; x < size.x; x++) {
                for (int y = 0; y < size.y; y++) {
                    // Get the current substring index
                    int stringIndex = Mathf.FloorToInt(i / MapLoader.TilesPerString);

                    // Split the substring into smaller string that each contain a single's tile data
                    string[] subTileData = (string[])tileData[stringIndex].Split(layers);

                    // Load each data string into a tile
                    for (int z = 0; z < subTileData.Length; z++) {
                        string data = subTileData[z];
                        tilemap.SetTile(new Vector2Int(x, y), data);
                    }

                    i++;
                }
            }
        }

        public void Save() {
            mapData = new MapData(this);
        }

        public MapData GetMapData() {
            return mapData;
        }

        #region - Tilemaps -

        public bool IsInBounds(Vector2 position) {
            return position.x >= 0 && position.x < size.x && position.y >= 0 && position.y < size.y;
        }

        public TileType GetTileType(string name) {
            // Get a tile type from a tile name
            return (TileType)ContentLoader.GetContentByName(name);
        }

        public TileType GetMapTileTypeAt(MapLayer layer, Vector2 position) {
            // Get a tile on a certain layer
            return tilemap.GetTile(Vector2Int.CeilToInt(position)).Layer(layer);
        }

        public bool CanPlaceBlockAt(Vector2Int position, int size) {
            // Check if a block could be placed at the given position

            // If the block is 1-sized, check only the given pos.
            if (size == 1) return CanPlaceBlockAt(position);

            // If the block is multiple-sized, check each tile it would occupy
            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    Vector2Int sizePosition = position + new Vector2Int(x, y);
                    if (!CanPlaceBlockAt(sizePosition)) return false;
                }
            }

            return true;
        }

        public bool CanPlaceBlockAt(Vector2Int position) {
            // Check if a block could be placed on a tile
            Tile tile = tilemap.GetTile(position);

            if (!tile.Layer(MapLayer.Ground).allowBuildings) return false;
            else if (tile.IsSolid()) return false;

            return true;
        }

        public void PlaceTile(MapLayer layer, Vector2Int position, TileType tile, int size) {
            // Place a square of the given size of tiles (i think i wont use it until a long time)

            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    Vector2Int sizePosition = position + new Vector2Int(x, y);
                    PlaceTile(layer, sizePosition, tile);
                }
            }
        }

        public void PlaceTile(MapLayer layer, Vector2Int position, TileType tile) {
            // Change a tile on the tilemap, this won't update the region mesh as of now
            tilemap.SetTile(tile, position, layer);
        }

        public void PlaceBlock(Block block, Vector2Int position) {
            // Set the given block to all the tiles it occupies
            int size = block.Type.size;

            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    Vector2Int sizePosition = position + new Vector2Int(x, y);

                    // Link position and tile to the given block
                    blockPositions.Add(sizePosition, block);
                    tilemap.SetBlock(sizePosition, block);
                }
            }
        }

        public void RemoveBlock(Block block, Vector2Int position) {
            // Set null the block to all the tiles it occupied
            int size = block.Type.size;

            for (int x = 0; x < size; x++) {
                for (int y = 0; y < size; y++) {
                    Vector2Int sizePosition = position + new Vector2Int(x, y);

                    // Remove the position and tile link of the given block
                    blockPositions.Remove(sizePosition);
                    tilemap.SetBlock(sizePosition, null);
                }
            }
        }

        public string[] BlocksToStringArray() {
            // Store all the blocks into a string array
            // Used for network transfer
            string[] blockArray = new string[Mathf.Max(blocks.Count, 2)];
            for (int i = 0; i < blockArray.Length; i++) blockArray[i] = blocks.Count <= i ? "null" : blocks[i].ToString();
            return blockArray;
        }

        public void SetBlocksFromStringArray(string[] blockData) {
            // Da fuk
            for(int i = 0; i < blockData.Length; i++) {
                string data = blockData[i];
                if (data == "null") continue;

                string[] values = data.Split(':');

                int syncID = int.Parse(values[0]);
                short contentID = short.Parse(values[1]);
                byte teamCode = byte.Parse(values[2]);
                float health = float.Parse(values[3]);
                Vector2 position = new(float.Parse(values[4]), float.Parse(values[5]));
                int orientation = int.Parse(values[6]);

                Block block = MapManager.Instance.InstantiateBlock(position, orientation, contentID, syncID, teamCode);
                block.SetHealth(health);
            }
        }

        #endregion

        public Entity GetClosestEntity(Vector2 position, byte teamCode) {
            Entity closestEntity = null;
            float closestDistance = 99999f;

            foreach (Entity entity in loadedEntities) {
                //If content doesn't match the filter, skip
                if (!(entity.GetTeam() == teamCode)) continue;

                //Get distance to content
                float distance = Vector2.Distance(position, entity.GetPosition());

                //If distance is lower than previous closest distance, set this as the closest content
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestEntity = entity;
                }
            }

            return closestEntity;
        }

        public Entity GetClosestEntity(Vector2 position, Type type, byte teamCode) {
            Entity closestEntity = null;
            float closestDistance = 99999f;

            foreach (Entity entity in loadedEntities) {
                //If content doesn't match the filter, skip
                if (entity.GetTeam() != teamCode || !TypeEquals(entity.GetType(), type)) continue;

                //Get distance to content
                float distance = Vector2.Distance(position, entity.GetPosition());

                //If distance is lower than previous closest distance, set this as the closest content
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestEntity = entity;
                }
            }

            return closestEntity;
        }

        public Entity GetClosestEntityStrict(Vector2 position, Type type, byte teamCode) {
            Entity closestEntity = null;
            float closestDistance = 99999f;

            foreach (Entity entity in loadedEntities) {
                //If content doesn't match the filter, skip
                if (entity.GetTeam() != teamCode || entity.GetType() != type) continue;

                //Get distance to content
                float distance = Vector2.Distance(position, entity.GetPosition());

                //If distance is lower than previous closest distance, set this as the closest content
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestEntity = entity;
                }
            }

            return closestEntity;
        }

        public Entity GetClosestEntityInView(Vector2 position, Vector2 direction, float fov, Type type, byte teamCode) {
            Entity closestEntity = null;
            float closestDistance = 99999f;

            foreach (Entity entity in loadedEntities) {
                //If content doesn't match the filter, skip
                if (entity.GetTeam() != teamCode || !TypeEquals(entity.GetType(), type)) continue;

                //If is not in view range continue to next
                float cosAngle = Vector2.Dot((entity.GetPosition() - position).normalized, direction);
                float angle = Mathf.Acos(cosAngle) * Mathf.Rad2Deg;


                //Get distance to content
                float distance = Vector2.Distance(position, entity.GetPosition());
                if (angle > fov) continue;

                //If distance is lower than previous closest distance, set this as the closest content
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestEntity = entity;
                }
            }

            return closestEntity;
        }

        #region - Blocks -

        public static bool TypeEquals(Type target, Type reference) => target == reference || target.IsSubclassOf(reference);

        public Block GetClosestBlock(Vector2 position, Type type, byte teamCode) {
            Block closestBlock = null;
            float closestDistance = 99999f;

            foreach (Block block in blocks) {
                //If block doesn't match the filter, skip
                if (block.GetTeam() != teamCode || !TypeEquals(block.GetType(), type)) continue;

                //Get distance to block
                float distance = Vector2.Distance(position, block.GetPosition());

                //If distance is lower than previous closest distance, set this as the closest block
                if (distance < closestDistance) {
                    closestDistance = distance;
                    closestBlock = block;
                }
            }

            return closestBlock;
        }

        public LandPadBlock GetBestAvilableLandPad(Unit forUnit) {
            LandPadBlock bestLandPad = null;
            float smallestSize = 999999f;
            float closestDistance = 999999f;

            foreach (Block block in blocks) {
                if (block is LandPadBlock landPad) {

                    //If block doesn't match the filter, skip
                    if (!landPad.CanLand(forUnit)) continue;

                    //Get size to block
                    float size = landPad.Type.unitSize;

                    //If size is lower than previous closest distance, set this as the closest block
                    // If the landpad isn't smaller but is equally sized, check if it's closer
                    if (size < smallestSize) {
                        smallestSize = size;
                        bestLandPad = landPad;

                    } else if (size == smallestSize) {
                        //Get distance to block
                        float distance = Vector2.Distance(forUnit.GetPosition(), landPad.GetPosition());

                        //If distance is lower than previous closest distance, set this as the closest block
                        if (distance < closestDistance) {
                            closestDistance = distance;
                            bestLandPad = landPad;
                        }
                    }
                }
            }

            return bestLandPad;
        }

        public Block GetBlockAt(Vector2Int position) {
            return blockPositions.TryGetValue(position, out Block block) ? block : null;
        }

        public (List<ItemBlock>, List<int>) GetAdjacentBlocks(ItemBlock itemBlock) {
            // Create the adjacent block list
            List<ItemBlock> adjacentBlocks = new();
            List<int> adjacentBlockOrientations = new();

            int size = (int)itemBlock.size;

            // Get the block's position
            Vector2Int position = itemBlock.GetGridPosition();

            // Check for adjacent blocks in all 4 sides
            for (int y = 0; y < size; y++) Handle(-1, y, 2);
            for (int y = 0; y < size; y++) Handle(size, y, 0);
            for (int x = 0; x < size; x++) Handle(x, -1, 3);
            for (int x = 0; x < size; x++) Handle(x, size, 1);
    
            // Check if a block exists in (x, y)
            void Handle(int x, int y, int o) {
                Vector2Int offset = new(x, y);
                ItemBlock block = (ItemBlock)GetBlockAt(offset + position);

                if (block != null && itemBlock != block && !adjacentBlocks.Contains(block)) { 
                    adjacentBlocks.Add(block);
                    adjacentBlockOrientations.Add(o);
                }
            }

            return (adjacentBlocks, adjacentBlockOrientations);
        }

        public void AddBlock(Block block) {
            // Add block to all lists
            blocks.Add(block);
            loadedEntities.Add(block);
            Client.syncObjects.Add(block.SyncID, block);

            // Place the block
            PlaceBlock(block, block.GetGridPosition());
        }

        public void RemoveBlock(Block block) {
            // Remove block from all lists
            blocks.Remove(block);
            loadedEntities.Remove(block);
            Client.syncObjects.Remove(block.SyncID);

            // Remove the block
            RemoveBlock(block, block.GetGridPosition());
        }

        #endregion

        #region - Units -

        public void AddUnit(Unit unit) {
            units.Add(unit);
            loadedEntities.Add(unit);
        }

        public void RemoveUnit(Unit unit) {
            units.Remove(unit);
            loadedEntities.Remove(unit);
        }

        #endregion
    }

    [Serializable]
    public struct MapData {
        public Vector2Int size;
        public TilemapData tilemapData;

        public MapData(Vector2Int size, TilemapData tileData) {
            this.size = size;
            this.tilemapData = tileData;
        }

        public MapData(Map map) {
            size = map.size;
            tilemapData = new TilemapData(map.SaveTilemapData());
        }

        public MapData(Vector2Int size, string[] tileData) {
            this.size = size;
            this.tilemapData = new TilemapData(size, tileData);
        }

        /*
        public BlockArrayData blockData;
        public UnitArrayData unitData;

        public MapData(TileMapData tileData, BlockArrayData blockData, UnitArrayData unitData) {
            this.tileData = tileData;
            this.blockData = blockData;
            this.unitData = unitData;
        }*/

        [Serializable]
        public struct TilemapData {
            public Wrapper2D<string> tileReferenceGrid;

            public TilemapData(string[,] tileReferenceGrid) {
                this.tileReferenceGrid = new Wrapper2D<string>(tileReferenceGrid);
            }

            public TilemapData(string[,,] tileNameGrid) {
                tileReferenceGrid = new Wrapper2D<string>(Encode(tileNameGrid));
            }

            public TilemapData(Vector2Int size, string[] tileData) {
                tileReferenceGrid = new Wrapper2D<string>(ReAssemble(size, tileData));
            }

            public string[,,] DecodeThis() => Decode(tileReferenceGrid.array2D);

            public static string[,] Encode(string[,,] tileNameGrid) {
                Vector3Int size = new(tileNameGrid.GetLength(0), tileNameGrid.GetLength(1), tileNameGrid.GetLength(2));
                string[,] returnGrid = new string[size.x, size.y];

                for(int x = 0; x < size.x; x++) {
                    for (int y = 0; y < size.y; y++) {
                        for (int z = 0; z < size.z; z++) {
                            string name = tileNameGrid[x, y, z];
                            returnGrid[x, y] += name == null ? (char)32 : (char)(ContentLoader.GetContentByName(name).id + 32);
                        }
                    }
                }

                return returnGrid;
            }

            public static string[,] ReAssemble(Vector2Int size, string[] tileData) {
                int layers = (int)Map.MapLayer.Total;
                string[,] returnGrid = new string[size.x, size.y];
                int i = 0;

                for (int z = 0; z < layers; z++) {
                    for (int x = 0; x < size.x; x++) {
                        for (int y = 0; y < size.y; y++) {

                            int stringIndex = Mathf.FloorToInt(i / MapLoader.TilesPerString);
                            int tile = i - (stringIndex * MapLoader.TilesPerString);

                            returnGrid[x, y] += tileData[stringIndex][tile];
                            i++;
                        }
                    }
                }

                return returnGrid;
            }

            public static string[,,] Decode(string[,] tileReferenceGrid) {
                Vector2Int size = new(tileReferenceGrid.GetLength(0), tileReferenceGrid.GetLength(1));

                int layers = (int)Map.MapLayer.Total;
                string[,,] returnGrid = new string[size.x, size.y, layers];

                for (int x = 0; x < size.x; x++) {
                    for (int y = 0; y < size.y; y++) {
                        string tileData = tileReferenceGrid[x, y];

                        for (int z = 0; z < layers; z++) {
                            int value = Convert.ToInt32(tileData[z]) - 32;
                            if (value == 0) continue;
                            returnGrid[x, y, z] = value == 0 ? null : ContentLoader.GetContentById((short)value).name;
                        }
                    }
                }

                return returnGrid;
            }
        }
        /*
        [Serializable]
        public struct BlockArrayData {
            public BlockData[] array;

            public BlockArrayData(BlockData[] array) {
                this.array = array;
            }

            [Serializable]
            public struct BlockData {
                public Vector2Int position;
            }
        }

        [Serializable]
        public struct UnitArrayData {
            public UnitData[] array;

            public UnitArrayData(UnitData[] array) {
                this.array = array;
            }

            [Serializable]
            public struct UnitData {
                public Vector2 position;
            }
        }
        */
    }

    public class BuildPlan {
        public event EventHandler<EventArgs> OnPlanFinished;
        public event EventHandler<EventArgs> OnPlanCanceled;

        public BlockType blockType;
        public Inventory missingItems;
        public Vector2Int position;
        public int orientation;
        public bool breaking;

        public float progress;
        public bool hasStarted, isStuck;

        public BuildPlan(BlockType blockType, Vector2Int position, int orientation) {
            this.blockType = blockType;
            this.position = position;
            this.orientation = orientation;

            missingItems = new Inventory();
            missingItems.Add(blockType.buildCost);
        }

        public void AddItems(ItemStack[] stacks) {
            missingItems.Substract(stacks);
            float progress = BuildProgress();
            if (progress >= 1f) OnPlanFinished?.Invoke(this, EventArgs.Empty);
        }

        public float BuildProgress() {
            float total = 0;

            for(int i = 0; i < missingItems.items.Count; i++) {
                Item item = blockType.buildCost[i].item;
                int neededAmount = blockType.buildCost[i].amount;

                total += (neededAmount - missingItems.items[item]) / neededAmount;
            }

            total /= missingItems.items.Count;

            return total;
        }

        public void Cancel() {
            OnPlanCanceled?.Invoke(this, EventArgs.Empty);
        }
    }
}

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
    public Weapon GetWeaponByID(int ID);
}