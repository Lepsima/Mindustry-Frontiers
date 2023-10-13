using CI.QuickSave;
using Frontiers.Animations;
using Frontiers.Assets;
using Frontiers.Content.Flags;
using Frontiers.Content.Maps;
using Frontiers.Content.SoundEffects;
using Frontiers.Content.Upgrades;
using Frontiers.Content.VisualEffects;
using Frontiers.FluidSystem;
using Frontiers.Pooling;
using Frontiers.Teams;
using Newtonsoft.Json;
using Photon.Pun;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;
using SpriteAnimation = Frontiers.Animations.SpriteAnimation;

namespace Frontiers.Animations {

    public class MovementAnimator {
        readonly MovementAnim[] animations;

        public MovementAnimator(string baseName, string layerName, int layerOrder, Transform parent, MovementAnimation[] animations) {
            this.animations = new MovementAnim[animations.Length];
            for (int i = 0; i < animations.Length; i++) this.animations[i] = new MovementAnim(baseName, layerName, layerOrder, parent, animations[i]);
        }

        public void Update(float deltaTime) {
            for (int i = 0; i < animations.Length; i++) animations[i].Update(deltaTime);
        }

        public void Set(float time) {
            for (int i = 0; i < animations.Length; i++) animations[i].Set(time);
        }
    }

    public class MovementAnim {
        readonly Transform animationTransform;
        readonly MovementAnimation animation;

        float time = 0;
        bool isGoingForward = true;

        public MovementAnim(string baseName, string layerName, int layerOrder, Transform parent, MovementAnimation animation) {
            this.animation = animation;

            // Create a new gameObject to hold the animation
            animationTransform = new GameObject("animation" + animation.name, typeof(SpriteRenderer)).transform;

            // Set the position && rotation to 0
            animationTransform.parent = parent;
            animationTransform.localPosition = Vector3.zero;
            animationTransform.localRotation = Quaternion.identity;

            // Get the sprite renderer component
            SpriteRenderer animationRenderer = animationTransform.GetComponent<SpriteRenderer>();
            animationRenderer.sprite = AssetLoader.GetSprite(baseName + "-" + animation.name);
            animationRenderer.sortingLayerName = layerName;
            animationRenderer.sortingOrder = layerOrder;
        }

        public void Update(float deltaTime) {
            AddTime(deltaTime);

            if (time > animation.time) {
                float extra = animation.time - time;

                switch (animation.repeat) {
                    case MovementAnimation.Repeat.DontRepeat:
                        return;
                    case MovementAnimation.Repeat.Loop:
                        time = 0;
                        break;
                    case MovementAnimation.Repeat.PingPong:
                        time = animation.time;
                        isGoingForward = false;
                        break;
                }

                AddTime(extra);
            } else if (time < 0 && animation.repeat == MovementAnimation.Repeat.PingPong) {
                time = 0;
                isGoingForward = true;
                AddTime(Mathf.Abs(time));
            }

            UpdateTransfrom();

            void AddTime(float time) {
                this.time += (isGoingForward ? 1f : -1f) * time;
            }
        }

        public void Set(float time) {
            this.time = time;
            UpdateTransfrom();
        }

        private void UpdateTransfrom() {
            // Get position and rotation
            Vector2 position = animation.Position(time);
            float rotation = animation.Rotation(time);

            // Apply to transform
            animationTransform.SetLocalPositionAndRotation(position, Quaternion.Euler(0, 0, rotation));
        }
    }

    public struct MovementAnimation {
        public string name;
        public float time;
        public Repeat repeat;

        public Vector2 position;
        public float rotation;

        public (Vector2, float)[] positionTimeline;
        public (float, float)[] rotationTimeline;

        public enum Repeat {
            DontRepeat,
            Loop,
            PingPong
        }

        public MovementAnimation(string name, float time, Repeat repeat, Vector2 position, float rotation, (Vector2, float)[] positionTimeline = null, (float, float)[] rotationTimeline = null) {
            this.name = name;
            this.time = time;
            this.repeat = repeat;
            this.position = position;
            this.rotation = rotation;
            this.positionTimeline = positionTimeline;
            this.rotationTimeline = rotationTimeline;
        }

        public Vector2 Position(float time) {
            //Debug.Log(time);
            if (positionTimeline == null || time == 0f) return position;

            for (int i = 1; i < positionTimeline.Length; i++) {
                (Vector2, float) key = positionTimeline[i];

                if (key.Item2 < time) continue;
                if (key.Item2 == time) return key.Item1;

                // Get a number from 0 to 1 that represents the progress from this key to the next key
                (Vector2, float) prevKey = positionTimeline[i - 1];
                float map = 1f / (key.Item2 - prevKey.Item2) * (time - prevKey.Item2);

                // Interpolate and return
                return Vector2.Lerp(prevKey.Item1, key.Item1, map);
            }

            return position;
        }

        public float Rotation(float time) {
            if (rotationTimeline == null || time == 0f) return rotation;

            for (int i = 1; i < positionTimeline.Length; i++) {
                (float, float) key = rotationTimeline[i];

                if (key.Item2 < time) continue;
                if (key.Item2 == time) return key.Item1;

                // Get a number from 0 to 1 that represents the progress from this key to the next key
                (float, float) prevKey = rotationTimeline[i - 1];
                float map = 1f / (key.Item2 - prevKey.Item2) * (time - prevKey.Item2);

                // Interpolate and return
                return Mathf.Lerp(prevKey.Item1, key.Item1, map);
            }

            return rotation;
        }
    }

    public class SpriteAnimator {
        readonly Dictionary<SpriteAnimation.Case, SpriteAnim> animations = new();

        public void AddAnimation(SpriteAnim anim) {
            if (animations.ContainsKey(anim.GetCase())) return;
            animations.Add(anim.GetCase(), anim);
        }

        public void NextFrame() {
            if (animations.Count == 0) return;
            animations[0].NextFrame();
        }

        public void NextFrame(SpriteAnimation.Case useCase) {
            if (!animations.ContainsKey(useCase)) return;
            animations[useCase].NextFrame();
        }
    }

    public class SpriteAnim {
        readonly SpriteRenderer animationRenderer;
        readonly Sprite[] animationFrames;

        SpriteAnimation animation;
        int frame;

        public SpriteAnim(string baseName, string layerName, int layerOrder, Transform parent, SpriteAnimation animation) {
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

        public SpriteAnimation.Case GetCase() => animation.useCase;
    }

    public struct SpriteAnimation {
        public string name;
        public int frames;
        public Case useCase;

        public enum Case {
            Reload,
            Shoot
        }

        public SpriteAnimation(string name, int frames, Case useCase) {
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
            gameObject.transform.parent = null;

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
        /// The amount of entities to sync every second
        /// </summary>
        public static int Sync_updatesPerSecond = 20;

        /// <summary>
        /// The amount of tiles per each region, x^2 amount of tiles per region
        /// </summary>
        public static int Map_RegionSize = 32;
    }

    public static class NetworkSettings {
        public enum HostSyncQuality {
            Low,        // Low amount of updates
            Standard,   // The standard, recomended even if the host has higher speed
            High,       // Maximum sync quality, for low player count
        }

        /// <summary>
        /// The quality of the multiplayer syncronization
        /// </summary>
        public static HostSyncQuality hostSyncQuality;
        
        /// <summary>
        /// The amount of entities to sync every second
        /// </summary>
        public static int updatesPerSecond;


        public static void ApplyHostSyncQuality() {
            switch(hostSyncQuality) {
                case HostSyncQuality.Low:
                    updatesPerSecond = 5;
                    break;
                case HostSyncQuality.Standard:
                    updatesPerSecond = 20;
                    break;
                case HostSyncQuality.High:
                    updatesPerSecond = 35;
                    break;
            }
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

    public static class BulletLoader {
        public static List<BulletType> loadedBullets = new();

        public static void HandleBullet(BulletType bullet) {
            bullet.id = (short)loadedBullets.Count;
            loadedBullets.Add(bullet);
        }
    }

    public static class ContentLoader {
        public static Dictionary<string, Content> loadedContents;
        public static List<Mod> modList;

        public static void LoadContents() {
            Launcher.SetState("Loading Base Contents...");

            loadedContents = new();
            //modList = new List<Mod>();

            // These three don't inherit from the base content class
            Sounds.Load();
            Effects.Load();
            FlagTypes.Load();

            Items.Load();
            Fluids.Load();
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

            /* Handle Mods
            Launcher.SetState("Loading Mods...");
            LoadMods();
            Launcher.SetState(modList.Count + " Mods Loaded");
            */

            InitializeObjectPools();
        }

        public static void InitializeObjectPools() {
            ConveyorBlock.conveyorItemPool = PoolManager.NewPool(AssetLoader.GetPrefab("ItemPrefab"), 100);
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

        public short id;
        public Sprite sprite;
        public Sprite spriteFull;

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
        public Type type;

        public int tier = 1;

        public ItemStack[] buildCost;

        public float health = 100f, itemMass = -1f;
        public int itemCapacity = 20;
        public bool hasOrientation = false, allowsSingleItem = false;

        public int maximumFires = 0;
        public bool canGetOnFire = false, canSpreadFire = false;

        public float blinkInterval = 0.5f, blinkOffset = 0f, blinkLength = 1f;

        public Effect hitSmokeFX = Effects.hitSmoke, deathFX = Effects.explosion;
        public Sound loopSound = null, deathSound = Sounds.bang;

        public EntityType(string name, Type type, int tier = 1) : base(name) {
            this.type = type;
            this.tier = tier;
        }
    }

    public class BlockType : EntityType {
        public Sprite teamSprite, topSprite, bottomSprite;
        public Sprite localTeamSprite, enemyTeamSprite;
        public Sprite[] glowSprites;

        public Sound destroySound = Sounds.@break;

        public bool updates = false, syncs = true, breakable = true, solid = true;
        public int size = 1;

        public bool usesPower = false, transfersPower = false;
        public float powerUsage = 0f, powerStorage = 0f, powerConnectionRange = 0f;
        public int maxPowerConnections = 0;

        public BlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            teamSprite = AssetLoader.GetSprite(name + "-team", true);
            localTeamSprite = AssetLoader.GetSprite(name + "-team-local", true);
            enemyTeamSprite = AssetLoader.GetSprite(name + "-team-enemy", true);

            topSprite = AssetLoader.GetSprite(name + "-top", true);
            bottomSprite = AssetLoader.GetSprite(name + "-bottom", true);
            this.type = type;

            Sprite glowSprite = AssetLoader.GetSprite(name + "-glow", true);
            if (glowSprite == null) {

                glowSprite = AssetLoader.GetSprite(name + "-glow-0", true); 
                if (glowSprite != null) {
                    List<Sprite> glowSpriteList = new();

                    for (int i = 0; i < 8; i++) {
                        glowSprite = AssetLoader.GetSprite(name + "-glow-" + i, true);
                        if (glowSprite == null) break;

                        glowSpriteList.Add(glowSprite);
                    }

                    glowSprites = glowSpriteList.ToArray();
                }
            }

            canGetOnFire = false;
            maximumFires = 3;
        }

        public void SetTeamRenderer(SpriteRenderer spriteRenderer, byte team) {
            bool localTeam = TeamUtilities.GetLocalTeam() == team;

            if (localTeam && localTeamSprite) {
                spriteRenderer.sprite = localTeamSprite;
                spriteRenderer.color = Color.white;
                return;
            }

            if (!localTeam && enemyTeamSprite) {
                spriteRenderer.sprite = enemyTeamSprite;
                spriteRenderer.color = Color.white;
                return;
            }

            spriteRenderer.sprite = teamSprite;
            spriteRenderer.color = TeamUtilities.GetTeamColor(team);
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
        public Sprite fluidSprite;

        // Max volume per second this block can output/recive from/to each other block
        public float maxInput, maxOutput;

        // Volume = liters at 1 atmosphere, pressure in atm
        public float maxVolume, maxPressure;

        // The minimum percent of health at wich the object is pressurizable
        public float minHealthPressurizable;

        // Whether the block can be pressurized to a custom pressure
        public bool pressurizable;

        // Whether this block can only output or input
        public bool fluidOutputOnly, fluidInputOnly;

        // The max amount of liquids
        public int maxFluids;

        // If true, the block can only contain a fraction of each liquid
        public bool fixedSpace;
        public bool hasItemInventory = true, hasFluidInventory = false;

        public ItemBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            fluidSprite = AssetLoader.GetSprite(name + "-fluid");
        }
    }

    public class DistributionBlockType : ItemBlockType {
        public float itemSpeed = 1f;
        public bool inverted = false;

        public DistributionBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            updates = true;
        }
    }

    public class DrillBlockType : ItemBlockType {
        public Sprite rotorSprite;
        public float drillHardness, drillRate;

        public DrillBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            rotorSprite = AssetLoader.GetSprite(name + "-rotator");
            updates = true;
        }
    }

    public class ConveyorBlockType : ItemBlockType {
        public Sprite[,] allConveyorSprites;
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

    public class BridgeBlockType : DistributionBlockType {
        public Sprite bridgeSprite;
        public int connectionRange = 4; // 3 empty tile + the other's block tile

        public BridgeBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            bridgeSprite = AssetLoader.GetSprite(name + "-bridge");
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
        public CraftPlan[] craftPlans;

        public Effect craftEffect = null, enabledEffect = null;

        public MovementAnimation[] crafterAnimations;
        public ArmData[] arms;

        public CrafterBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            updates = true;
            canGetOnFire = true;
        }
    }

    public class StorageBlockType : ItemBlockType {
        public StorageBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class TurretBlockType : ItemBlockType {
        public Sprite heatSprite, heatEffectSprite;

        public WeaponMount mount;
        public Item ammoItem = Items.copper;
        public float ammoAmount = 10f;

        public TurretBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            updates = true;
        }
    }


    public class CoreBlockType : StorageBlockType {
        public UpgradeType baseUpgrade;

        public CoreBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            breakable = false;
        }
    }

    public class UnitFactoryBlockType : ItemBlockType {
        public UnitPlan unitPlan;

        public UnitFactoryBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class LandPadBlockType : ItemBlockType {
        public Vector2[] landPositions;

        public int unitCapacity = 0;
        public float unitSize = 1.5f;

        public LandPadBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class PowerGeneratorBlockType : ItemBlockType {
        public MaterialList consumption;
        public MovementAnimation[] animations;

        public Effect loopEffect = null, generateEffect = null;

        public PowerGeneratorBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            updates = true;
            canGetOnFire = true;
            usesPower = true;
            transfersPower = true;
        }
    }

    public class PowerBankBlockType : BlockType {
        public PowerBankBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            canGetOnFire = true;
            usesPower = true;
            transfersPower = true;
        }
    }

    public class PowerNodeBlockType : BlockType {
        public PowerNodeBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            canGetOnFire = true;
            usesPower = true;
            transfersPower = true;
        }
    }

    public class AtmosphericCollectorBlockType : ItemBlockType {
        public AtmosphericCollectorBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            hasFluidInventory = true;
            fluidOutputOnly = true;
        }
    }

    public class FluidPipeBlockType : ItemBlockType {
        public Sprite[] allPipeSprites;

        public FluidPipeBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            hasFluidInventory = true;

            allPipeSprites = new Sprite[16];
            for (int i = 0; i < 16; i++) allPipeSprites[i] = AssetLoader.GetSprite($"{name}-{i}");
        }
    }

    public class FluidExhaustBlockType : ItemBlockType {
        public FluidExhaustBlockType(string name, Type type, int tier) : base(name, type, tier) {
            fluidInputOnly = true;
        }
    }

    public class FluidPumpBlockType : ItemBlockType {
        public Sprite rotorSprite;

        public FluidPumpBlockType(string name, Type type, int tier = 1) : base(name, type, tier) {
            rotorSprite = AssetLoader.GetSprite(name + "-rotator");
            updates = true;
            fluidOutputOnly = true;
        }
    }

    public class Blocks {
        public const BlockType none = null;

        public static BlockType // Walls
            copperWall, copperWallLarge, graphiteWall, graphiteWallLarge, 
            heavyWall, heavyWallLarge, lightWall, lightWallLarge, reflectiveWall, 
            reflectiveWallLarge, siliconWall, siliconWallLarge, thoriumWall, thoriumWallLarge;

        public static BlockType // Turrets
            // Anti tank turrets
            cyclone, windstorm, //2x2, 4x4

            // Anti air turrets
            spread, deviation, //3x3

            // Multi purpose turrets
            tempest; //2x2, 3x3

        public static BlockType // Item factories
            siliconSmelter, graphitePress, crystalizer, componentAssembler, superconductorAssembler,
            reflectiveFabricWeaver, alloyForge, thoriumCrusher, thoriumCentrifuge, thoriumReprocessor;

        public static BlockType // Fluid processing
            oilDestillator, oilCompressor, coalLiquidator, plastaniumPress, waterElectrolyzer,
            carbonElectrolyzer, bitumenMixer;

        public static BlockType // Power generators
            combustionGenerator, combustionGeneratorLarge, turbineGenerator, turbineGeneratorLarge,
            pistonGeneratorSmall, pistonGenerator, fissionReactor, fissionReactorLarge;

        public static BlockType // Distribution
            conveyor, router, junction, sorter, overflowGate;

        public static BlockType // Power blocks
            powerNode, powerNodeLarge, battery, batteryLarge;

        public static BlockType // Unit blocks, S = small units, M = medium units, L = large units
            landingPadS, landingPadSLarge, landingPadM, landingPadL, landingPadLLarge;

        public static BlockType // Item drills / Fluid collectors
            mechanicalDrill, pneumaticDrill, geodeMiner,
            atmosphericCollector, rotatoryPump, fuelMixer;

        public static BlockType // Cores / storage
            coreShard, container, coreFoundation;

        public static BlockType // Fluid distribution
            lowPressurePipe, highPressurePipe, liquidContainer, fluidFilter, fluidExhaust;     

        public static void Load() {
            // Walls
            copperWall = new BlockType("copper-wall", typeof(Block), 1) {
                buildCost = ItemStack.With(Items.copper, 6),
                flags = new Flag[] { FlagTypes.wall },

                health = 100,
            };

            copperWallLarge = new BlockType("copper-wall-large", typeof(Block), 1) {
                buildCost = ItemStack.With(Items.copper, 24),
                flags = new Flag[] { FlagTypes.wall },

                health = 400,
                size = 2
            };

            graphiteWall = new BlockType("graphite-wall", typeof(Block), 1) {
                buildCost = ItemStack.With(Items.graphite, 6),
                flags = new Flag[] { FlagTypes.wall },

                health = 150,
            };

            graphiteWallLarge = new BlockType("graphite-wall-large", typeof(Block), 1) {
                buildCost = ItemStack.With(Items.graphite, 24),
                flags = new Flag[] { FlagTypes.wall },

                health = 600,
                size = 2
            };

            heavyWall = new BlockType("heavy-wall", typeof(Block), 2) {
                buildCost = ItemStack.With(Items.heavyAlloy, 6),
                flags = new Flag[] { FlagTypes.wall },

                health = 250,
            };

            heavyWallLarge = new BlockType("heavy-wall-large", typeof(Block), 2) {
                buildCost = ItemStack.With(Items.heavyAlloy, 24),
                flags = new Flag[] { FlagTypes.wall },

                health = 1000,
                size = 2
            };

            lightWall = new BlockType("light-wall", typeof(Block), 2) {
                buildCost = ItemStack.With(Items.lightAlloy, 6),
                flags = new Flag[] { FlagTypes.wall },

                health = 130,
            };

            lightWallLarge = new BlockType("light-wall-large", typeof(Block), 2) {
                buildCost = ItemStack.With(Items.lightAlloy, 24),
                flags = new Flag[] { FlagTypes.wall },

                health = 520,
                size = 2
            };

            reflectiveWall = new BlockType("reflective-wall", typeof(Block), 3) {
                buildCost = ItemStack.With(Items.reflectiveFabric, 6),
                flags = new Flag[] { FlagTypes.wall },

                health = 175,
            };

            reflectiveWallLarge = new BlockType("reflective-wall-large", typeof(Block), 3) {
                buildCost = ItemStack.With(Items.reflectiveFabric, 24),
                flags = new Flag[] { FlagTypes.wall },

                health = 700,
                size = 2
            };

            siliconWall = new BlockType("silicon-wall", typeof(Block), 1) {
                buildCost = ItemStack.With(Items.silicon, 6),
                flags = new Flag[] { FlagTypes.wall },

                health = 175,
            };

            siliconWallLarge = new BlockType("silicon-wall-large", typeof(Block), 1) {
                buildCost = ItemStack.With(Items.silicon, 24),
                flags = new Flag[] { FlagTypes.wall },

                health = 700,
                size = 2
            };

            thoriumWall = new BlockType("thorium-wall", typeof(Block), 3) {
                buildCost = ItemStack.With(Items.thorium, 6),
                flags = new Flag[] { FlagTypes.wall },

                health = 300,
            };

            thoriumWallLarge = new BlockType("thorium-wall-large", typeof(Block), 3) {
                buildCost = ItemStack.With(Items.thorium, 24),
                flags = new Flag[] { FlagTypes.wall },

                health = 1200,
                size = 2
            };



            // Cores
            coreShard = new CoreBlockType("core-shard", typeof(CoreBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 530, Items.graphite, 300, Items.heavyAlloy, 240, Items.silicon, 275),
                flags = new Flag[] { FlagTypes.core},

                hidden = true,
                breakable = false,
                health = 1600,
                size = 3,

                itemCapacity = 2000,

                canGetOnFire = true,
            };
      
            coreFoundation = new BlockType("core-foundation", typeof(Block), 2) {
                buildCost = ItemStack.With(Items.copper, 950, Items.silicon, 560, Items.iron, 250, Items.nickel, 340, Items.thorium, 250),
                flags = new Flag[] { FlagTypes.core },

                hidden = true,
                breakable = false,
                health = 5600,
                size = 4,

                itemCapacity = 5000,

                canGetOnFire = true,
            };

            container = new StorageBlockType("container", typeof(StorageBlock), 2) {
                buildCost = ItemStack.With(Items.heavyAlloy, 120, Items.graphite, 40),
                health = 150,
                size = 2,
                itemCapacity = 120,

                canGetOnFire = true,
            };



            // Landing pads
            landingPadS = new LandPadBlockType("landingPad", typeof(LandPadBlock), 2) {
                health = 250,
                size = 3,
                solid = false,
                updates = true,
                unitCapacity = 4,
                unitSize = 2.5f,

                hasItemInventory = false,
                hasFluidInventory = true,

                maxInput = 50f,
                maxOutput = 0f,
                maxVolume = 300f,

                fluidInputOnly = true,

                landPositions = new Vector2[] {
                    new Vector2(0.8f, 0.8f),
                    new Vector2(0.8f, 2.2f),
                    new Vector2(2.2f, 0.8f),
                    new Vector2(2.2f, 2.2f)
                }
            };

            landingPadSLarge = new LandPadBlockType("landingPad-large", typeof(LandPadBlock), 3) {
                health = 300,
                size = 3,
                solid = false,
                updates = true,
                unitCapacity = 1,
                unitSize = 5f,

                hasItemInventory = false,
                hasFluidInventory = true,

                maxInput = 50f,
                maxOutput = 0f,
                maxVolume = 500f,

                fluidInputOnly = true,

                landPositions = new Vector2[] {
                    new Vector2(1.5f, 1.5f)
                }
            };



            // Turrets
            tempest = new TurretBlockType("tempest", typeof(TurretBlock), 1) {
                mount = new WeaponMount(Weapons.tempestWeapon, Vector2.zero),

                health = 230f,
                size = 2,

                canGetOnFire = true,
            };

            windstorm = new TurretBlockType("windstorm", typeof(TurretBlock), 2) {
                mount = new WeaponMount(Weapons.windstormWeapon, Vector2.zero),

                health = 540f,
                size = 3,

                canGetOnFire = true,
                maximumFires = 2,
            };

            spread = new TurretBlockType("spread", typeof(TurretBlock), 3) {
                mount = new WeaponMount(Weapons.spreadWeapon, Vector2.zero),

                health = 245f,
                size = 2,

                canGetOnFire = true,
            };

            cyclone = new TurretBlockType("cyclone", typeof(TurretBlock), 3) {
                buildCost = ItemStack.With(Items.copper, 125, Items.graphite, 55, Items.silicon, 35),
                mount = new WeaponMount(Weapons.cycloneWeapon, Vector2.zero),

                health = 512f,
                size = 3,

                canGetOnFire = true,
            };

            /*airFactory = new UnitFactoryBlockType("air-factory", typeof(UnitFactoryBlock), 2) {
                unitPlan = new UnitPlan(Units.flare, 4f, new ItemStack[1] {
                    new ItemStack(Items.silicon, 20)
                }),

                health = 250f,
                size = 3,
                itemCapacity = 50,

                canGetOnFire = true,
            };*/

            // Item Factories
            siliconSmelter = new CrafterBlockType("silicon-smelter", typeof(CrafterBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 45, Items.graphite, 25),
                craftPlan = new CraftPlan() {
                    production = new MaterialList(ItemStack.With(Items.silicon, 2), null),
                    consumption = new MaterialList(ItemStack.With(Items.sand, 3, Items.coal, 2), null),
                    craftTime = 1f
                },

                loopSound = Sounds.smelter,

                health = 95,
                size = 2,
                itemCapacity = 30,
            };

            graphitePress = new CrafterBlockType("graphite-press", typeof(CrafterBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 55),
                craftPlan = new CraftPlan() {
                    production = new MaterialList(ItemStack.With(Items.graphite, 1), null),
                    consumption = new MaterialList(ItemStack.With(Items.coal, 2), null),
                    craftTime = 1.5f
                },

                health = 95,
                size = 2,
                itemCapacity = 20,
            };

            alloyForge = new CrafterBlockType("alloy-forge", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.iron, 95, Items.nickel, 50, Items.graphite, 25),

                craftPlans = new CraftPlan[] {
                    // Light alloy
                    new CraftPlan() {
                        production = new MaterialList(ItemStack.With(Items.lightAlloy, 3), null),
                        consumption = new MaterialList(ItemStack.With(Items.copper, 3, Items.nickel, 2), null),
                        craftTime = 2.5f
                    },

                    // Heavy alloy
                    new CraftPlan() {
                        production = new MaterialList(ItemStack.With(Items.heavyAlloy, 4), null),
                        consumption = new MaterialList(ItemStack.With(Items.copper, 3, Items.graphite, 2, Items.iron, 4), null),
                        craftTime = 4f
                    },
                },

                health = 210,
                size = 3,
                itemCapacity = 30,
            };

            componentAssembler = new CrafterBlockType("component-assembler", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.iron, 125, Items.silicon, 65, Items.lithium, 25),
                craftPlans = new CraftPlan[] {
                    new CraftPlan() {
                        production = new MaterialList(ItemStack.With(Items.resistor, 3), null),
                        consumption = new MaterialList(ItemStack.With(Items.copper, 2, Items.silicon, 3, Items.lithium, 4), null),
                        craftTime = 3.5f
                    },
                    new CraftPlan() {
                        production = new MaterialList(ItemStack.With(Items.resistor, 2), null),
                        consumption = new MaterialList(ItemStack.With(Items.lithium, 1, Items.gold, 2), null),
                        craftTime = 1.5f
                    },
                },

                arms = new ArmData[2] {
                    new ArmData("assembler") {
                        idlePosition = new Vector2(0.34375f, 0),
                        minPosition = new Vector2(0.34375f, -0.34375f),
                        maxPosition = new Vector2(0.34375f, 0.34375f),

                        middleArmOffset = new Vector2(0.4f, 0f),
                        maxTargetOffset = new Vector2(0.25f, 0.25f),

                        idleAngle = 0f,
                        minBaseAngle = -90f,
                        maxBaseAngle = 90f,
                        minTime = 1.5f,
                        maxTime = 3.5f,

                        effect = Effects.weldSparks
                    },
                    new ArmData("assembler") {
                        idlePosition = new Vector2(-0.34375f, 0),
                        minPosition = new Vector2(-0.34375f, -0.34375f),
                        maxPosition = new Vector2(-0.34375f, 0.34375f),

                        middleArmOffset = new Vector2(0.4f, 0f),
                        maxTargetOffset = new Vector2(0.25f, 0.25f),

                        idleAngle = 180f,
                        minBaseAngle = 90f,
                        maxBaseAngle = 270f,
                        minTime = 1.5f,
                        maxTime = 3.5f,

                        effect = Effects.weldSparks
                    }
                },

                health = 255,
                size = 2,
                itemCapacity = 30,
            };

            thoriumCrusher = new CrafterBlockType("thorium-crusher", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.heavyAlloy, 65, Items.thorium, 25, Items.lithium, 35),
                craftPlan = new CraftPlan() {
                    production = new MaterialList(ItemStack.With(Items.thoriumDust, 1), null),
                    consumption = new MaterialList(ItemStack.With(Items.thorium, 2), null),
                    craftTime = 1.75f
                },

                health = 250,
                size = 2,
                itemCapacity = 10,
            };

            thoriumCentrifuge = new CrafterBlockType("thorium-centrifuge", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.iron, 45, Items.graphite, 65, Items.thorium, 55, Items.lithium, 25),
                craftPlan = new CraftPlan() {
                    production = new MaterialList(ItemStack.With(Items.thoriumFuel, 1), null),
                    consumption = new MaterialList(ItemStack.With(Items.thoriumDust, 5), FluidStack.With(Fluids.nitrogen, 6f)),
                    craftTime = 1.5f
                },

                health = 375,
                size = 3,
                itemCapacity = 50,

                hasFluidInventory = true,
                fluidInputOnly = true,

                maxInput = 10f,
                maxVolume = 60f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 1,
            };

            thoriumReprocessor = new CrafterBlockType("thorium-reprocessor", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.iron, 65, Items.thorium, 25, Items.resistor, 30),
                craftPlan = new CraftPlan() {
                    production = new MaterialList(ItemStack.With(Items.thoriumDust, 4), null),
                    consumption = new MaterialList(ItemStack.With(Items.depletedThorium, 2), FluidStack.With(Fluids.water, 12f)),
                    craftTime = 2f
                },

                health = 275,
                size = 2,
                itemCapacity = 20,

                hasFluidInventory = true,
                fluidInputOnly = true,

                maxInput = 15f,
                maxVolume = 120f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 1,
            };

            crystalizer = new CrafterBlockType("crystalizer", typeof(CrafterBlock), 3) {
                buildCost = ItemStack.With(Items.copper, 125, Items.nickel, 65, Items.magnesium, 40, Items.superconductor, 25, Items.resistor, 30),
                craftPlan = new CraftPlan() {
                    production = new MaterialList(ItemStack.With(Items.quartz, 2), null),
                    consumption = new MaterialList(ItemStack.With(Items.salt, 4, Items.silicon, 2), FluidStack.With(Fluids.oxigen, 8f)),
                    craftTime = 3f
                },

                health = 405,
                size = 3,
                itemCapacity = 40,

                hasFluidInventory = true,
                fluidInputOnly = true,

                maxInput = 12f,
                maxVolume = 80f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 1,
            };

            reflectiveFabricWeaver = new CrafterBlockType("reflective-fabric-weaver", typeof(CrafterBlock), 3) {
                buildCost = ItemStack.With(Items.heavyAlloy, 200, Items.lightAlloy, 125, Items.nickel, 80, Items.superconductor, 55, Items.resistor, 15),
                craftPlan = new CraftPlan() {
                    production = new MaterialList(ItemStack.With(Items.reflectiveFabric, 3), null),
                    consumption = new MaterialList(ItemStack.With(Items.resistor, 2, Items.gold, 1), FluidStack.With(Fluids.oxigen, 4f)),
                    craftTime = 5f
                },

                health = 580,
                size = 4,
                itemCapacity = 20,

                hasFluidInventory = true,
                fluidInputOnly = true,

                maxInput = 4f,
                maxVolume = 120f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 1,
            };



            // Fluid factories
            coalLiquidator = new CrafterBlockType("coal-liquidator", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.copper, 60, Items.iron, 25),

                craftPlan = new CraftPlan() {
                    production = new MaterialList(null, FluidStack.With(Fluids.petroleum, 6f)),
                    consumption = new MaterialList(ItemStack.With(Items.coal, 2), FluidStack.With(Fluids.co2, 0.5f)),
                    craftTime = 1f
                },

                health = 175,
                size = 2,

                hasFluidInventory = true,
                hasItemInventory = true,

                itemCapacity = 20,

                maxInput = 1f,
                maxOutput = 10f,
                maxVolume = 100f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 2,
                fixedSpace = true,
            };

            oilCompressor = new CrafterBlockType("oil-compressor", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.copper, 70, Items.nickel, 15),

                craftPlan = new CraftPlan() {
                    production = new MaterialList(ItemStack.With(Items.coal, 4), null),
                    consumption = new MaterialList(null, FluidStack.With(Fluids.petroleum, 12f)),
                    craftTime = 2.5f
                },

                health = 225,
                size = 2,

                hasFluidInventory = true,
                hasItemInventory = true,
                fluidInputOnly = true,

                itemCapacity = 40,

                maxInput = 8f,
                maxVolume = 100f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 1,
            };

            plastaniumPress = new CrafterBlockType("plastanium-press", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.copper, 25, Items.iron, 65, Items.lithium, 25),

                craftPlan = new CraftPlan() {
                    production = new MaterialList(ItemStack.With(Items.plastanium, 6), null),
                    consumption = new MaterialList(ItemStack.With(Items.sand, 12), FluidStack.With(Fluids.petroleum, 24f)),
                    craftTime = 9f
                },

                health = 225,
                size = 2,

                hasFluidInventory = true,
                hasItemInventory = true,
                fluidInputOnly = true,

                itemCapacity = 48,

                maxInput = 10f,
                maxVolume = 120f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 1,
            };

            oilDestillator = new CrafterBlockType("oil-destillator", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.graphite, 60, Items.iron, 35, Items.nickel, 40),

                craftPlan = new CraftPlan() {
                    production = new MaterialList(null, FluidStack.With(Fluids.kerosene, 4f)),
                    consumption = new MaterialList(null, FluidStack.With(Fluids.petroleum, 3f)),
                    craftTime = 0.75f
                },

                health = 355,
                size = 3,

                hasFluidInventory = true,
                hasItemInventory = false,

                maxInput = 6f,
                maxOutput = 10f,
                maxVolume = 200f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 2,
                fixedSpace = true,
            };

            waterElectrolyzer = new CrafterBlockType("water-electrolyzer", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.copper, 60, Items.silicon, 25, Items.graphite, 20),

                craftPlan = new CraftPlan() {
                    production = new MaterialList(null, FluidStack.With(Fluids.hydrogen, 30f, Fluids.oxigen, 15f)),
                    consumption = new MaterialList(ItemStack.With(Items.salt, 1), FluidStack.With(Fluids.water, 15f)),
                    craftTime = 3f
                },

                health = 310,
                size = 3,

                hasFluidInventory = true,
                hasItemInventory = true,

                itemCapacity = 10,

                maxInput = 15f,
                maxOutput = 20f,
                maxVolume = 300f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 3,
                fixedSpace = true,
            };

            fuelMixer = new CrafterBlockType("fuel-mixer", typeof(CrafterBlock), 3) {
                buildCost = ItemStack.With(Items.copper, 60, Items.silicon, 25, Items.graphite, 20),

                craftPlan = new CraftPlan() {
                    production = new MaterialList(null, FluidStack.With(Fluids.fuel, 3.5f)),
                    consumption = new MaterialList(null, FluidStack.With(Fluids.nitrogen, 1f, Fluids.kerosene, 3f)),
                    craftTime = 1f
                },

                health = 290,
                size = 2,

                hasFluidInventory = true,
                hasItemInventory = false,

                itemCapacity = 10,

                maxInput = 5f,
                maxOutput = 5f,
                maxVolume = 140f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 3,
                fixedSpace = true,
            };

            // TODO, also the bitumen mixer
            /*carbonElectrolyzer = new CrafterBlockType("carbon-electrolyzer", typeof(CrafterBlock), 2) {
                buildCost = ItemStack.With(Items.copper, 60, Items.lightAlloy, 35, Items.graphite, 20),

                craftPlan = new CraftPlan() {
                    production = new MaterialList(null, FluidStack.With(Fluids.oxigen, 10)),
                    consumption = new MaterialList(null, FluidStack.With(Fluids.co2, 5)),
                    craftTime = 2f
                },

                health = 210,
                size = 2,

                hasFluidInventory = true,
                hasItemInventory = false,

                maxInput = 10f,
                maxOutput = 20f,
                maxVolume = 100f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 2,
                fixedSpace = true,
            };*/

            // Distribution
            conveyor = new ConveyorBlockType("conveyor", typeof(ConveyorBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 2),

                loopSound = Sounds.conveyor,

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



            // Drills
            mechanicalDrill = new DrillBlockType("mechanical-drill", typeof(DrillBlock), 1) {
                buildCost = ItemStack.With(Items.copper, 16),

                loopSound = Sounds.drill,

                health = 100f,
                size = 2,
                itemCapacity = 10,

                drillHardness = 2.5f,
                drillRate = 1f,

                canGetOnFire = true,
            };

            pneumaticDrill = new DrillBlockType("pneumatic-drill", typeof(DrillBlock), 2) {
                buildCost = ItemStack.With(Items.copper, 24, Items.graphite, 10),

                loopSound = Sounds.drill,

                health = 175f,
                size = 2,
                itemCapacity = 12,

                drillHardness = 3.5f,
                drillRate = 1.75f,

                canGetOnFire = true,
            };

            geodeMiner = new ExtractorBlockType("geode-miner", typeof(ExtractorBlock), 3) {
                //-1.175
                size = 4,
                health = 750f,

                drillFX = Effects.smallExplosion,
                drillAnimations = new MovementAnimation[] {
                    new MovementAnimation(
                        // Stats
                        "hammer", 1f, MovementAnimation.Repeat.Loop, new Vector2(-1.175f, 0f), 0f,

                        // Animation keys
                        new (Vector2, float)[] {
                            new(new(-1.175f, 0f), 0f),
                            new(new(-0.7f, 0f), 0.1f),
                            new(new(-1.175f, 0f), 1f),
                        }),

                    new MovementAnimation(
                        // Stats
                        "hammer", 1f, MovementAnimation.Repeat.Loop, new Vector2(0f, -1.175f), 90f,

                        // Animation keys
                        new (Vector2, float)[] {
                            new(new(0f, -1.175f), 0f),
                            new(new(0f, -0.7f), 0.1f),
                            new(new(0f, -1.175f), 1f),
                        }),

                    new MovementAnimation(
                        // Stats
                        "hammer", 1f, MovementAnimation.Repeat.Loop, new Vector2(1.175f, 0f), 180f,

                        // Animation keys
                        new (Vector2, float)[] {
                            new(new(1.175f, 0f), 0f),
                            new(new(0.7f, 0f), 0.1f),
                            new(new(1.175f, 0f), 1f),
                        }),

                    new MovementAnimation(
                        // Stats
                        "hammer", 1f, MovementAnimation.Repeat.Loop, new Vector2(0f, 1.175f), 270f,

                        // Animation keys
                        new (Vector2, float)[] {
                            new(new(0f, 1.175f), 0f),
                            new(new(0f, 0.7f), 0.1f),
                            new(new(0f, 1.175f), 1f),
                        }),
                },

                itemCapacity = 50,
                updates = true,
            };

            lowPressurePipe = new FluidPipeBlockType("pipe", typeof(FluidPipeBlock), 1) {
                health = 100,
                size = 1,
                updates = true,

                hasFluidInventory = true,
                hasItemInventory = false,

                maxInput = 10f,
                maxOutput = 10f,
                maxVolume = 10f,

                maxPressure = -1f,
                minHealthPressurizable = 0.5f,
                pressurizable = false,

                maxFluids = 2,
                fixedSpace = true,
            };

            highPressurePipe = new FluidPipeBlockType("high-pressure-pipe", typeof(FluidPipeBlock), 1) {
                health = 100,
                size = 1,
                updates = true,

                hasFluidInventory = true,
                hasItemInventory = false,

                maxInput = 25f,
                maxOutput = 25f,
                maxVolume = 10f,

                maxPressure = 3f,
                minHealthPressurizable = 0.7f,
                pressurizable = true,

                maxFluids = 2,
                fixedSpace = true,
            };

            liquidContainer = new StorageBlockType("liquid-container", typeof(StorageBlock), 1) {
                health = 400,
                size = 2,
                updates = true,

                hasFluidInventory = true,
                hasItemInventory = false,

                maxInput = 50f,
                maxOutput = 50f,
                maxVolume = 1000f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = -1,
            };

            rotatoryPump = new FluidPumpBlockType("rotary-pump", typeof(FluidPumpBlock), 2) {
                health = 400,
                size = 2,
                updates = true,

                hasFluidInventory = true,
                hasItemInventory = false,

                maxInput = 240f,
                maxOutput = 360f,
                maxVolume = 860f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = -1,
            };

            atmosphericCollector = new AtmosphericCollectorBlockType("atmospheric-collector", typeof(AtmosphericCollectorBlock), 2) {
                health = 1600,
                size = 4,
                updates = true,

                hasFluidInventory = true,
                hasItemInventory = false,

                maxInput = 240f,
                maxOutput = 360f,
                maxVolume = 2400f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = -1,
            };

            fluidExhaust = new FluidExhaustBlockType("fluid-exhaust", typeof(FluidExhaustBlock), 1) {
                health = 100,
                size = 1,
                updates = true,

                hasFluidInventory = true,
                hasItemInventory = false,

                maxInput = 80f,
                maxOutput = 75f,
                maxVolume = 100f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = -1,
            };

            fluidFilter = new StorageBlockType("filter", typeof(FluidFilterBlock), 1) {
                health = 100,
                size = 1,
                updates = true,

                hasFluidInventory = true,
                hasItemInventory = false,

                maxInput = 25f,
                maxOutput = 25f,
                maxVolume = 250f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 1,
            };

            /*oilRefinery = new CrafterBlockType("oil-refinery", typeof(CrafterBlock)) {
                buildCost = ItemStack.With(Items.copper, 45),

                health = 325,
                size = 3,

                canGetOnFire = true,
                hasFluidInventory = true,
                hasItemInventory = false,

                craftPlan = new CraftPlan() {
                    production = MaterialList.Multiply(new MaterialList(null, new FluidStack[] { Fluids.kerosene.ReturnStack() }), 10f),
                    consumption = MaterialList.Multiply(new MaterialList(Fluids.kerosene.CompositionToStacks()), 10f),
                    craftTime = 0.33f
                },

                maxInput = 20f,
                maxOutput = 20f,
                maxVolume = 300f,

                maxPressure = -1f,
                minHealthPressurizable = 0.7f,
                pressurizable = false,

                maxFluids = 2,
                fixedSpace = true,

                loopSound = Sounds.smelter,
            };*/
        }
    }

    #endregion

    #region - Units -
    public class UnitType : EntityType {
        public Sprite cellSprite, outlineSprite;
        public Type[] priorityList = null;

        public float size = 1.5f;
        public float maxVelocity = 2f, rotationSpeed = 90f;

        public float itemPickupDistance = 3f, buildSpeedMultiplier = 1f;

        public float range = 10f, searchRange = 15f, fov = 95;
        public float groundHeight = 18f;

        public float fuelCapacity = 60f, fuelConsumption = 1.5f, fuelRefillRate = 7.5f, fuelLeftToReturn = 10f;
        public float emptyMass = 10f, fuelDensity = 0.0275f;

        public WeaponMount[] weapons = new WeaponMount[0];
        public Item ammoItem = Items.copper;
        public float ammoAmount = 1f;

        public UnitType(string name, Type type, int tier = 1) : base(name, type, tier) {
            cellSprite = AssetLoader.GetSprite(name + "-cell");
            outlineSprite = AssetLoader.GetSprite(name + "-outline");
            this.type = type;
            hasOrientation = true;

            canGetOnFire = true;
            maximumFires = 2;
        }
    }

    public class MechUnitType : UnitType {
        public Sprite legSprite, baseSprite;
        public float turretRotationSpeed = 90f, legStepDistance = 0.2f, sideSway = 0.075f, frontSway = 0.01f;

        public MechUnitType(string name, Type type, int tier = 1) : base(name, type, tier) {
            legSprite = AssetLoader.GetSprite(name + "-leg");
            baseSprite = AssetLoader.GetSprite(name + "-base");
        }
    }

    public class AircraftUnitType : UnitType {
        public float drag = 1f, force = 500f;
        public float bankAmount = 25f, bankSpeed = 5f;
        public bool useAerodynamics = true, hasDragTrails = true;

        public float engineSize = 0.2f, engineOffset = -0.35f, engineLength = 3.5f;

        public float takeoffTime = 3f, takeoffHeight = 0.5f; // Takeoff height is measured in a percentage of ground height
        public float maxLiftVelocity = 3f;

        public bool hasWreck = false;
        public float wreckHealth = 0f;

        public Vector2 trailOffset = Vector2.zero;
        public float trailSize = 1f;

        public AircraftUnitType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }

    public class CopterUnitType : AircraftUnitType {
        public UnitRotor[] rotors;

        // Degrees / second
        public float wreckSpinAccel = 50f;
        public float wreckSpinMax = 270f;

        public CopterUnitType(string name, Type type, int tier = 1) : base(name, type, tier) {
            useAerodynamics = hasDragTrails = false; // Copters don't do that
        }
    }

    public class Units {
        public const UnitType none = null;
        public static UnitType
            flare, horizon, zenith,  // Assault - air
            poly,                    // Support - air
            sonar, foton,            // Copter - air
            dagger, fortress;        // Assault - ground       

        public static EntityType train, turretTrain;

        public static void Load() {
            train = new TrainType("train", typeof(TrainSegment)) {
                connectionPinOffset = 3.75f,
            };

            turretTrain = new TrainType("trainTurret", typeof(TrainSegment)) {
                weapons = new WeaponMount[2] {
                    new WeaponMount(Weapons.trainTurret, new(-1.135f, 0.9f), true, true),
                    new WeaponMount(Weapons.trainTurret, new(-1.135f, -0.9f), true, true),
                },

                connectionPinOffset = 2.6f,
            };

            flare = new AircraftUnitType("flare", typeof(AircraftUnit), 1) {
                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.flareWeapon, new(-0.375f, 0.45f), true),
                },

                flags = new Flag[] { FlagTypes.aircraft, FlagTypes.fighter, FlagTypes.light, FlagTypes.fast, FlagTypes.lightArmored },
                priorityList = new Type[5] { typeof(Unit), typeof(TurretBlock), typeof(CoreBlock), typeof(ItemBlock), typeof(Block) },
                useAerodynamics = true,

                trailOffset = new(0.5625f, -0.675f),

                health = 75f,
                size = 1.5f,
                maxVelocity = 20f,
                drag = 0.1f,

                engineSize = 0.4f,
                engineOffset = -0.6f,
                engineLength = 1.2f,

                rotationSpeed = 160f,
                bankAmount = 20f,

                range = 10f,
                searchRange = 15f,
                fov = 100f,
                groundHeight = 18f,

                fuelCapacity = 120f,
                fuelConsumption = 1.25f,
                fuelRefillRate = 8.25f,

                force = 350f,
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

                trailOffset = new(0.8f, -0.7f),
                trailSize = 1.75f,

                health = 215f,
                size = 2.25f,
                maxVelocity = 10f,
                itemCapacity = 25,
                drag = 0.2f,

                engineSize = 0.4f,
                engineOffset = -0.8f,
                engineLength = 1f,
                
                rotationSpeed = 100f,
                bankAmount = 25f,

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
                    new WeaponMount(Weapons.zenithMissiles, new(0.875f, 0f), true, true),
                },

                flags = new Flag[] { FlagTypes.aircraft, FlagTypes.slow, FlagTypes.heavy, FlagTypes.heavyArmored },
                priorityList = new Type[5] { typeof(TurretBlock), typeof(Unit), typeof(ItemBlock), typeof(Block), typeof(CoreBlock) },
                useAerodynamics = false,

                health = 825f,
                size = 3.5f,
                maxVelocity = 7.5f,
                itemCapacity = 50,
                drag = 2f,

                engineSize = 0.8f,
                engineOffset = -1.3f,
                engineLength = 0.8f,

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
                    new UnitRotor("sonar-rotor", new(0f, 0.2f), 3f, 0.5f, 0.667f, 1f),
                },

                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.zenithMissiles, new Vector2(0.9f, 0.35145f), true),
                },

                flags = new Flag[] { FlagTypes.copter, FlagTypes.slow, FlagTypes.heavyArmored },
                priorityList = new Type[5] { typeof(MechUnit), typeof(Unit), typeof(TurretBlock), typeof(CoreBlock), typeof(Block) },

                loopSound = Sounds.copterBladeLoop,

                takeoffTime = 6f,

                hasWreck = true,
                wreckHealth = 125f,

                health = 395f,
                size = 2.25f,
                maxVelocity = 7f,
                itemCapacity = 35,
                drag = 3f,

                engineSize = 0f,

                rotationSpeed = 80f,
                bankAmount = 0f,

                range = 12f,
                searchRange = 17.5f,
                fov = 160f,
                groundHeight = 8f,

                fuelCapacity = 260f,
                fuelConsumption = 3.65f,
                fuelRefillRate = 18.25f,

                force = 600f,
                emptyMass = 13.5f,
                itemMass = 12.25f,
                //fuelDensity = 0.0275f,
            };

            foton = new CopterUnitType("foton", typeof(CopterUnit), 3) {
                rotors = new UnitRotor[] {
                    new UnitRotor("foton-rotor", new(0f, 0.5625f), 6f, 1.5f, 1.5f, 2.25f, new UnitRotorBlade[2] {
                        new UnitRotorBlade(0f, false),
                        new UnitRotorBlade(0f, true)
                    }),
                },

                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.fotonWeapon, new Vector2(0.975f, 0.54375f), true),
                },

                flags = new Flag[] { FlagTypes.copter, FlagTypes.slow, FlagTypes.heavy, FlagTypes.heavyArmored },
                priorityList = new Type[5] { typeof(MechUnit), typeof(Unit), typeof(TurretBlock), typeof(CoreBlock), typeof(Block) },

                loopSound = Sounds.copterBladeLoop,

                hasWreck = true,
                wreckHealth = 225f,

                health = 750f,
                size = 3.75f,
                maxVelocity = 9f,
                itemCapacity = 50,
                drag = 3.5f,

                engineSize = 0f,

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
                //fuelDensity = 0.0275f,
            };

            dagger = new MechUnitType("dagger", typeof(MechUnit), 1) {
                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.daggerWeapon, new Vector2(0.437805f, 0.2343f), true, false),
                },

                flags = new Flag[] { FlagTypes.mech, FlagTypes.slow, FlagTypes.light, FlagTypes.moderateArmored },
                priorityList = new Type[5] { typeof(Unit), typeof(TurretBlock), typeof(CoreBlock), typeof(ItemBlock), typeof(Block) },

                health = 140f,
                size = 1.5f,
                maxVelocity = 1f,

                turretRotationSpeed = 180f,
                legStepDistance = 0.65f,
                sideSway = 0.075f,
                frontSway = 0.01f,

                rotationSpeed = 120f,

                range = 15f,
                searchRange = 20f,
                fov = 80f,
                groundHeight = 0.2f,

                fuelCapacity = 525f,
                fuelConsumption = 2.5f,
                fuelRefillRate = 24.5f,

                emptyMass = 7.05f,
                itemMass = 2.2f,
                //fuelDensity = 12.25f,
            };

            fortress = new MechUnitType("fortress", typeof(MechUnit), 3) {
                weapons = new WeaponMount[1] {
                    new WeaponMount(Weapons.fortressWeapon, new Vector2(1f, 0.125f), true, false),
                },

                flags = new Flag[] { FlagTypes.mech, FlagTypes.slow, FlagTypes.heavy, FlagTypes.heavyArmored },
                priorityList = new Type[5] { typeof(MechUnit), typeof(TurretBlock), typeof(CoreBlock), typeof(ItemBlock), typeof(Block) },

                health = 140f,
                size = 3.125f,
                maxVelocity = 0.8f,

                turretRotationSpeed = 50f,
                legStepDistance = 1.1f,
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
                //fuelDensity = 23.5f,
            };
        }
    }

    #endregion

    #region - Weapons -
    public class WeaponType : Content {
        public Sprite outlineSprite;
        public SpriteAnimation[] animations;
        public WeaponBarrel[] barrels;

        public Vector2 shootOffset = Vector2.zero;
        public BulletType bulletType;

        public Sound shootSound = Sounds.pew, reloadSound = Sounds.noAmmo;
        public Effect shootFX = Effects.muzzle, casingFX = Effects.casing;

        public float shootFXSize = 1f, casingFXSize = 1f, casingFXOffset = -0.5f;
        public float ammoPerShot = 0.25f; // The amount of ammo units consumed per shot, 1 ammo == 1 item

        public bool independent = false; // If enbled, can gather it's own target
        public bool predictTarget = true; // If enabled, the weapon can predict the bullet collision of moving targets

        public bool chargesUp = false; // Special behabiour, if enabled, the fire rate increases per each shot
        public float chargedShootTime = 1f; // The firerate after the weapon is fully charged
        public float chargeShotCooldown = 1f; // The rate at wich each charge dissapears
        public int shotsToChargeUp = 1; // The amount of shots needed to fully charge

        public int clipSize = 10; // The amount of shots per magazine, keep to 1 if this behaviour isn't wanted
        public float shootTime = 1f; // The time between shots
        public float reloadTime = 1f; // The time for each magazine to reload

        public float rotateSpeed = 90f; // The degrees per second this weapon rotates at
        public float maxTargetDeviation = 15f; // The angle of inacurracy that the weapon sees as accepable to shoot at
        public float spread = 5f;  // The max deviation of the bullets when shot

        // The higher both values, the "snappier" it looks
        public float recoil = 0.75f; // The recoil of the weapon after each shot
        public float returnSpeed = 1f; // The return speed to compensate recoil, keep high for fast shooting weapons

        public WeaponType(string name) : base(name) {
            outlineSprite = AssetLoader.GetSprite(name + "-outline", true);
        }

        public float Range { get => bulletType.GetRange(); }
    }

    public class Weapons {
        public const Weapon none = null;

        // Base weapons
        public static WeaponType smallAutoWeapon, tempestWeapon, windstormWeapon, stingerWeapon, pathWeapon, spreadWeapon, cycloneWeapon, lasertWeapon;

        //Unit weapons
        public static WeaponType 
            flareWeapon, horizonBombBay, zenithMissiles,
            sonarWeapon, fotonWeapon,
            daggerWeapon, fortressWeapon;

        // Item related weapons 
        public static WeaponType missileRack;

        // Train weapon
        public static WeaponType trainTurret;

        public static void Load() {
            trainTurret = new WeaponType("trainTurret-weapon") {
                bulletType = Bullets.basicBullet,
                shootOffset = new Vector2(0, 0.37f),

                independent = true,
                rotateSpeed = 90f,

                recoil = 0f,
                clipSize = 12,
                shootTime = 0.15f,
                reloadTime = 3.5f,
            };

            smallAutoWeapon = new WeaponType("small-auto-weapon") {
                bulletType = Bullets.basicBullet,
                shootOffset = new Vector2(0, 0.37f),

                recoil = 0f,
                clipSize = 25,
                shootTime = 0.15f,
                reloadTime = 5f,
            };

            flareWeapon = new WeaponType("flare-weapon") {
                bulletType = Bullets.basicBullet,
                shootOffset = new Vector2(0, 0.37f),

                recoil = 0.075f,
                returnSpeed = 3f,
                clipSize = 25,
                shootTime = 0.15f,
                reloadTime = 5f,
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
            };

            fortressWeapon = new WeaponType("fortress-weapon") {
                bulletType = Bullets.bigBullet,
                shootOffset = new Vector2(0.1f, 0.64f),

                recoil = 0.1f,
                returnSpeed = 2f,
                clipSize = 3,
                shootTime = 0.33f,
                reloadTime = 1.5f,
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

                shootFX = null,
            };

            zenithMissiles = new WeaponType("zenith-missiles") {
                bulletType = Bullets.missileBullet,

                shootOffset = new Vector2(0, 0.25f),

                independent = true,
                recoil = 0.05f,
                returnSpeed = 2f,
                clipSize = 2,
                shootTime = 0.5f,
                reloadTime = 1.5f,
                rotateSpeed = 115f,

                shootFX = Effects.rcs
            };

            tempestWeapon = new WeaponType("tempest-weapon") {
                bulletType = Bullets.basicBullet,
                shootOffset = new Vector2(0, 1f),

                independent = true,
                recoil = 0.1f,
                returnSpeed = 2f,
                clipSize = 12,
                shootTime = 0.075f,
                reloadTime = 2f,
                rotateSpeed = 90f,
            };

            windstormWeapon = new WeaponType("windstorm-weapon") {
                bulletType = Bullets.bigBullet,
                shootOffset = Vector2.zero,

                barrels = new WeaponBarrel[2] {
                    new WeaponBarrel("windstorm-weapon", 1, new Vector2(-0.28125f, 1.65f)),
                    new WeaponBarrel("windstorm-weapon", 2, new Vector2(0.28125f, 1.65f)),
                },

                casingFX = Effects.casing,
                
                shootFXSize = 2f,
                casingFXSize = 2f,

                independent = true,
                recoil = 0.4f,
                clipSize = 2,
                shootTime = 0.25f,
                reloadTime = 0.5f,
                rotateSpeed = 50f,
            };

            stingerWeapon = new WeaponType("stinger-weapon") {
                bulletType = new BulletType() {
                    damage = 15f,
                    lifeTime = 0.5f,
                    velocity = 200f
                },
                shootOffset = new Vector2(0, 0.5f),

                independent = true,
                recoil = 0.2f,
                clipSize = 5,
                shootTime = 0.3f,
                reloadTime = 4f,
                rotateSpeed = 60f,
            };

            pathWeapon = new WeaponType("path-weapon") {
                bulletType = new BulletType() {
                    damage = 3f,
                    lifeTime = 0.35f,
                    velocity = 150f
                },
                shootOffset = new Vector2(0, 0.75f),

                independent = true,
                animations = new SpriteAnimation[1] { new SpriteAnimation("-belt", 3, SpriteAnimation.Case.Shoot) },
                recoil = 0.02f,
                clipSize = 50,
                shootTime = 0.03f,
                reloadTime = 6f,
                rotateSpeed = 120f,
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

                independent = true,
                recoil = 0.1f,
                clipSize = 20,
                shootTime = 0.085f,
                reloadTime = 6f,
                rotateSpeed = 100f,
            };

            cycloneWeapon = new WeaponType("cyclone-weapon") {
                bulletType = new BulletType() {
                    damage = 15f,
                    lifeTime = 1.5f,
                    velocity = 100f
                },

                shootOffset = Vector2.zero,
                barrels = new WeaponBarrel[2] {
                    new WeaponBarrel("cyclone-weapon", 1, new Vector2(-0.3125f, 1.375f)),
                    new WeaponBarrel("cyclone-weapon", 2, new Vector2(0.3125f, 1.375f)),
                },

                independent = true,
                animations = new SpriteAnimation[1] { new SpriteAnimation("-belt", 3, SpriteAnimation.Case.Shoot) },

                recoil = 0.1f,
                clipSize = 24,
                shootTime = 0.55f,
                reloadTime = 4f,
                rotateSpeed = 80f,

                chargesUp = true,
                shotsToChargeUp = 8,
                chargedShootTime = 0.25f,
                chargeShotCooldown = 0.25f,

                casingFXOffset = -1.5f,
            };

            lasertWeapon = new WeaponType("lasert-weapon") {
                bulletType = new BeamBulletType() {
                    damage = 200f,
                    buildingDamageMultiplier = 2f,
                    velocity = 5f,
                    lifeTime = 3f,
                    size = 1f,
                    beamLength = 10f,
                    burns = true,
                },

                shootOffset = new Vector2(0, 1f),

                independent = true,
                recoil = 0f,
                returnSpeed = 1f,
                clipSize = 1,
                shootTime = 4.9f,
                reloadTime = 10f,
                rotateSpeed = 45f,
            };

            missileRack = new WeaponType("missileRack") {
                bulletType = Bullets.missileBullet,
                shootOffset = new Vector2(0, 0.5f),

                independent = true,
                maxTargetDeviation = 360f,

                clipSize = 1,
                reloadTime = 5f,
                rotateSpeed = 0f
            };
        }
    }

    public class BulletType {
        public GameObjectPool pool;

        public string name;

        public short id;
        public Sprite sprite;

        public float damage = 10f, buildingDamageMultiplier = 1f, velocity = 100f, lifeTime = 1f, size = 0.05f;
        public float blastRadius = -1f, blastRadiusFalloff = -1f, minBlastDamage = 0f;

        public bool explodeOnDespawn = false;

        public Effect hitFX = Effects.bulletHit, despawnFX = Effects.despawn;

        public BulletType(string name = null) {
            this.name = name;
            BulletLoader.HandleBullet(this);

            sprite = AssetLoader.GetSprite(name, true);

            pool = GetPool();
            if (pool != null) pool.OnGameObjectCreated += OnPoolObjectCreated;        
        }

        public virtual float GetRange() {
            return velocity * lifeTime;
        }

        public virtual GameObjectPool GetPool() {
            return PoolManager.GetOrCreatePool(AssetLoader.GetPrefab("tracer-prefab"), 100);
        }

        public virtual Bullet NewBullet(Weapon weapon, Transform transform) {
            return new Bullet(weapon, transform);
        }

        public float Multiplier(IDamageable damageable) {
            return damageable.IsBuilding() ? buildingDamageMultiplier : 1f;
        }

        public float Damage(IDamageable damageable, float distance) {
            float mult = Multiplier(damageable);
            return Explodes() ? Mathf.Lerp(damage * mult, minBlastDamage * mult, distance / blastRadius) : damage * mult;
        }

        public bool Explodes() {
            return blastRadius > 0;
        }

        public float Value(IDamageable damageable, float distance) {
            float percent = (distance - blastRadiusFalloff) / (blastRadius - blastRadiusFalloff);
            float raw = distance > blastRadiusFalloff ? Mathf.Lerp(damage, minBlastDamage, percent) : damage;

            float mult = damageable.IsBuilding() ? buildingDamageMultiplier : 1f;
            return raw * mult;
        }

        public bool IsValid() {
            return blastRadius > 0f;
        }

        public virtual void OnPoolObjectCreated(object sender, GameObjectPool.PoolEventArgs e) {
            if (e.target && sprite != null) e.target.transform.GetComponent<SpriteRenderer>().sprite = sprite;
        }
    }

    public class MissileBulletType : BulletType {
        public float homingStrength = 30f;
        public bool canUpdateTarget = true;
        
        public MissileBulletType(string name = null) : base(name) {
            despawnFX = Effects.smallExplosion;
            explodeOnDespawn = true;
        }

        public override Bullet NewBullet(Weapon weapon, Transform transform) {
            return new MissileBullet(weapon, transform);
        }

        public override GameObjectPool GetPool() {
            return PoolManager.GetOrCreatePool(AssetLoader.GetPrefab("missile-prefab"), 100);
        }
    }

    public class BombBulletType : BulletType {
        public float fallVelocity = 3f, initialSize = 1f, finalSize = 0.5f;

        public BombBulletType(string name = null) : base(name) {
            hitFX = Effects.explosion;
            despawnFX = Effects.explosion;
        }

        public override Bullet NewBullet(Weapon weapon, Transform transform) {
            return new BombBullet(weapon, transform);
        }

        public override GameObjectPool GetPool() {
            return PoolManager.GetOrCreatePool(AssetLoader.GetPrefab("bomb-prefab"), 100);
        }

        public override void OnPoolObjectCreated(object sender, GameObjectPool.PoolEventArgs e) {
            base.OnPoolObjectCreated(sender, e);
            if (!e.target) return;

            Transform shadow = e.target.transform.GetChild(0);

            SpriteRenderer renderer = shadow.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0, 0, 0, 0.5f);
        }
    }

    public class BeamBulletType : BulletType {
        /* Beam bullet info:
         * 
         * Velocity is used as the beam's expand/retract velocity
         * The lifetime is the total time the beam stays active
         * 
         * The blast damage is calculated from the closest point to the beam line
         * Size is the beam's width
         */
        public float beamLength = 10f;
        public bool burns = false;

        public BeamBulletType(string name = null) : base(name) {

        }

        public override float GetRange() {
            return beamLength;
        }

        public override Bullet NewBullet(Weapon weapon, Transform transform) {
            return new BeamBullet(weapon, transform);
        }

        public override GameObjectPool GetPool() {
            return PoolManager.GetOrCreatePool(AssetLoader.GetPrefab("beam-prefab"), 100);
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

            bombBullet = new BombBulletType("bomb") {
                damage = 25f,
                minBlastDamage = 5f,
                blastRadius = 3f,
                buildingDamageMultiplier = 5f,
                fallVelocity = 4f
            };

            missileBullet = new MissileBulletType("homing-missile") {
                damage = 20f,
                minBlastDamage = 5f,
                blastRadius = 1.25f,
                buildingDamageMultiplier = 2f,
                velocity = 30f,
                lifeTime = 2.5f,
                homingStrength = 75f,
            };
        }
    }

    #endregion

    #region - Map -

    public class MapType : Content {
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

    public class Element : Content {
        public float density = 1f;
        public float explosiveness = 0f, flammability = 0f, radioactivity = 0f, charge = 0f;

        // If this is a molecule ex: water = 2 hidrogen and 1 oxigen
        public float returnAmount = 1f;
        public (Element, float)[] composition;

        public Element(string name) : base(name) {

        }

        public Element(string name, (Element, float)[] composition) : base(name) {
            this.composition = composition;

            density = 0;
            float sum = composition.Sum(x => x.Item2);

            for (int i = 0; i < composition.Length; i++) {
                // Calculate total density
                density += composition[i].Item1.density * composition[i].Item2 / sum;
            }
        }

        public static (Element, float)[] With(params object[] items) {
            (Element, float)[] composite = new (Element, float)[items.Length / 2];
            for (int i = 0; i < items.Length; i += 2) composite[i / 2] = ((Element)items[i], (float)items[i + 1]); 
            return composite;
        }

        public (ItemStack[], FluidStack[]) CompositionToStacks() {
            if (composition == null) return (null, null);

            List<ItemStack> itemStacks = new();
            List<FluidStack> fluidStacks = new();

            for (int i = 0; i < composition.Length; i++) {
                Element element = composition[i].Item1;

                switch (element) {
                    case Item item:
                        itemStacks.Add(new ItemStack(item, Mathf.RoundToInt(composition[i].Item2)));
                        break;
                    case Fluid fluid:
                        fluidStacks.Add(new FluidStack(fluid, composition[i].Item2));
                        break;
                }
            }

            ItemStack[] itemStackArray = itemStacks.Count == 0 ? null : itemStacks.ToArray();
            FluidStack[] fluidStackArray = fluidStacks.Count == 0 ? null : fluidStacks.ToArray();
            return (itemStackArray, fluidStackArray);
        }
    }

    #endregion

    #region - Structures - 

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

    public struct MaterialList {
        public ItemStack[] items;
        public FluidStack[] fluids;

        public MaterialList(ItemStack[] items, FluidStack[] fluids) {
            this.items = items;
            this.fluids = fluids;
        }

        public MaterialList((ItemStack[], FluidStack[]) elementStacks, float mult = 1f) {
            items = elementStacks.Item1;
            fluids = elementStacks.Item2;

            if (mult != 1f) {
                items = ItemStack.Multiply(items, mult);
                fluids = FluidStack.Multiply(fluids, mult);
            }
        }

        public static MaterialList Multiply(MaterialList materials, float mult) {
            return new(ItemStack.Multiply(materials.items, mult), FluidStack.Multiply(materials.fluids, mult));
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
        public MaterialList production;
        public MaterialList consumption;
        public float craftTime;

        public CraftPlan(MaterialList product, MaterialList cost, float craftTime) {
            this.production = product;
            this.consumption = cost;
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