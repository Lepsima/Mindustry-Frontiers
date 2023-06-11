using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*public class PoolManager : MonoBehaviour {
    public static List<GameObjectPool> allPools = new List<GameObjectPool>();

    public static GameObjectPool NewPool(GameObject prefab, int targetAmount) {
        GameObjectPool newPool = new GameObjectPool(prefab, targetAmount);
        allPools.Add(newPool);
        return newPool;
    }

    public static GameObject Pool_CreateGameObject(GameObject prefab, Transform parent = null) {
        return Instantiate(prefab, parent);
    }

    public static void Pool_DestroyGameObject(GameObject gameObject) {
        Destroy(gameObject);
    }
}

public class GameObjectPool {
    // The hard limit of gameobjects in the pool, only used if the pool gets too big where creating/destroying a gameobject is better than storing it
    public int targetAmount;

    public GameObject prefab;
    public Queue<GameObject> pooledGameObjects;

    public GameObjectPool(GameObject prefab, int targetAmount) {
        this.targetAmount = targetAmount;
        pooledGameObjects = new Queue<GameObject>();
    }

    public bool CanTake() => pooledGameObjects.Count > 0;

    public bool CanReturn() => targetAmount == -1 || pooledGameObjects.Count < targetAmount;

    public GameObject Take() {
        return !CanTake() ? PoolManager.Pool_CreateGameObject(prefab) : pooledGameObjects.Dequeue();
    }

    public void Return(GameObject gameObject) {
        if (CanReturn())  pooledGameObjects.Enqueue(gameObject);
        else PoolManager.Pool_DestroyGameObject(gameObject);
    }
}
*/