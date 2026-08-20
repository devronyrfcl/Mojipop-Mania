using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    private static ObjectPoolManager _instance;
    public static ObjectPoolManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("ObjectPoolManager");
                _instance = obj.AddComponent<ObjectPoolManager>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    private readonly Dictionary<int, Queue<GameObject>> poolDictionary = new Dictionary<int, Queue<GameObject>>();
    private readonly Dictionary<int, int> instanceToPrefabMap = new Dictionary<int, int>();

    public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;
        return Instance.SpawnInternal(prefab, position, rotation);
    }

    public static void Despawn(GameObject instance, float delay = 0f)
    {
        if (instance == null) return;
        if (delay > 0f)
        {
            Instance.StartCoroutine(Instance.DespawnCoroutine(instance, delay));
        }
        else
        {
            Instance.DespawnInternal(instance);
        }
    }

    private GameObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int prefabKey = prefab.GetInstanceID();

        if (!poolDictionary.ContainsKey(prefabKey))
        {
            poolDictionary[prefabKey] = new Queue<GameObject>();
        }

        GameObject obj;
        Queue<GameObject> queue = poolDictionary[prefabKey];

        while (queue.Count > 0)
        {
            obj = queue.Dequeue();
            if (obj != null)
            {
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.transform.localScale = Vector3.one;
                obj.SetActive(true);

                // Restart particle systems if any
                ParticleSystem ps = obj.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    ps.Clear();
                    ps.Play();
                }

                return obj;
            }
        }

        // Pool empty -> instantiate new
        obj = Instantiate(prefab, position, rotation);
        obj.transform.SetParent(transform);
        instanceToPrefabMap[obj.GetInstanceID()] = prefabKey;
        return obj;
    }

    private void DespawnInternal(GameObject instance)
    {
        if (instance == null) return;
        int instanceKey = instance.GetInstanceID();

        if (instanceToPrefabMap.TryGetValue(instanceKey, out int prefabKey))
        {
            instance.SetActive(false);
            if (!poolDictionary.ContainsKey(prefabKey))
            {
                poolDictionary[prefabKey] = new Queue<GameObject>();
            }
            poolDictionary[prefabKey].Enqueue(instance);
        }
        else
        {
            Destroy(instance);
        }
    }

    private IEnumerator DespawnCoroutine(GameObject instance, float delay)
    {
        yield return new WaitForSeconds(delay);
        DespawnInternal(instance);
    }
}
