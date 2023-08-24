using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;

public class FireController : MonoBehaviour {
    public static GameObject FirePrefab {
        get {
            if (firePrefab == null) firePrefab = AssetLoader.GetPrefab("FirePrefab");
            return firePrefab;
        }
        set => firePrefab = value;
    }
    private static GameObject firePrefab;

    public static Dictionary<Vector2Int, FireController> activeFires = new Dictionary<Vector2Int, FireController>();

    [SerializeField] private Animator animator;
    private Block affectedBlock;

    private Vector2Int gridPosition;

    [Tooltip("1 = always spread, higher values decrease spread probability to 1 / number")] 
    public int spreadProbability = 3;
    public float spreadDistance = 3f;
    public float spreadTime = 1f;
    public float lifeTime = 5f;
    public float damagePerSecond = 5f;

    private float nextDamageTime;
    private float nextSpreadTime;
    private float endTime;

    #region - Static Methods -
    public static void CreateFire(Vector2Int position) {
        if (activeFires.ContainsKey(position)) return;
        Client.CreateFire(position);
    }

    public static void InstantiateFire(Vector2Int position) {
        GameObject fireGameObject = Instantiate(FirePrefab, (Vector2)position, Quaternion.Euler(0, 0, Random.Range(0, 360)));
        FireController fireController = fireGameObject.GetComponent<FireController>();

        activeFires.Add(position, fireController);
    }

    public static void Spread(Vector2 initialPosition, float distance = 3, int probability = 5, int amount = 1) {
        for (int i = 0; i < amount; i++) {
            Vector3 offset = new Vector3(Random.Range(1f, -1f), Random.Range(1f, -1f), 0) * distance;
            Vector2Int spreadGridPosition = Vector2Int.CeilToInt(initialPosition + (Vector2)offset);

            // Play effect showing the flame spread

            // Should replace with a RPC call to the map manager
            if (Random.Range(0, probability) == 0) CreateFire(spreadGridPosition);
        }
    }
    #endregion

    private void Awake() {   
        gridPosition = Vector2Int.CeilToInt(transform.position);
        transform.position += new Vector3(0.5f, 0.5f, 0f);

        //Check if map tile is flammable, if so, the fire won't end
        affectedBlock = MapManager.Map.GetBlockAt(gridPosition);
        endTime = IsFlammable(affectedBlock) ? -1f : Time.time + lifeTime;

        // Start animator
        animator.SetBool("fire", true);
        nextSpreadTime = Time.time + spreadTime;
    }

    private void Update() {
        if (nextSpreadTime != -1f && nextSpreadTime <= Time.time) TrySpread();
        if (nextDamageTime != -1f && nextDamageTime <= Time.time) DamageBlock(2.5f);
        if (endTime != -1f && endTime <= Time.time) EndFire();
    }

    public void DamageBlock(float interval) {
        if (!affectedBlock) return;
        Client.Damage(affectedBlock, damagePerSecond / interval);
        nextDamageTime = Time.time + interval;
    }

    public void TrySpread() {
        Spread(transform.position, spreadDistance, spreadProbability);
        nextSpreadTime = Time.time + spreadTime;

        // Check if the block is still flammable
        if (endTime == -1f) endTime = affectedBlock && IsFlammable(affectedBlock) ? -1f : Time.time + lifeTime;
    }

    public bool IsFlammable(Block block) {
        if (!block || !block.hasItemInventory) return false;
        return ((ItemBlock)block).IsFlammable();
    }

    public void EndFire() {
        // End animator
        animator.SetBool("fire", false);
        activeFires.Remove(gridPosition);

        // Wait a few seconds to let the animation fade out the fire
        nextSpreadTime = -1f;
        Destroy(gameObject, 2f);
    }
}