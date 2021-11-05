using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool
{
    /* The total number of objects to keep in the pool */
    private int totalCount;

    /* A list of refs to the transforms of each object currently in the pool, and whether they are spawned. */
    private Dictionary<Transform, bool> objects;

    private bool initialized;

    public bool Initialized { get => initialized; }
    public Dictionary<Transform, bool> Objects { get => objects; set => objects = value; }

    public ObjectPool(int totalCount)
    {
        this.totalCount = totalCount;
        objects = new Dictionary<Transform, bool>(totalCount);
        initialized = false;
    }

    /**
     * Instantiates <em>totalCount</em> number of <em>objToSpawn</em>. This seeds the object pool with the
     * specified amount and type of object.
     * 
     * <param name="loc"> The transform to use as a template for the instatiated object pool. </param>
     * <param name="objToSpawn"> The prefab reference to be instantiated and seeded in the object pool. </param>
     */
    internal void Initialize(GameObject objToSpawn, Transform loc)
    {
        lock (objects)
        {
            for (int i = 0; i < totalCount; i++)
            {
                GameObject obj = GameObject.Instantiate(objToSpawn, new Vector3(0,0,-500f), Quaternion.identity);
                UnityEngine.Object.DontDestroyOnLoad(obj);
                objects.Add(obj.transform, false);
            }
        }
        initialized = true;
    }

    /**
     * Attempts to spawn an object from the pool. If there are none left, this 
     * method will return null.
     * 
     * <returns> The first transorm ref to spawn. </returns>
     */
    public Transform GetSpawnRef()
    {
        Transform ret = null;
        //TODO optimize
        lock (objects)
        {
            foreach (KeyValuePair<Transform, bool> t in objects)
                if (t.Value == false)
                {
                    ret = t.Key;
                    break;
                }
        }

        if (ret)
            objects[ret] = true;
        return ret;
    }

    /**
     * Attempts to despawn the specified transform ref. if the transform is not found in the
     * object pool dictionary, this method will return false.
     * 
     * <param name="transform"> The transform ref being despawned. </param>
     * <returns> If the tranform ref was found in the pool, and thus, despawned. </returns>
     */
    public bool Despawn(Transform transform)
    {
        bool ret = false;

        if (objects.ContainsKey(transform))
        {
            Debug.Log("Transform, " + transform + " successfully despawned.");
            ret = true;
            objects[transform] = false;
        }

        return ret;
    }

    /**
     * Adds a new Transform to the object pool. Used for adding manually placed
     * objects to the pool.
     */
    public bool AddToPool(Transform transform)
    {
        bool ret = false;

        if (!objects.ContainsKey(transform))
        {
            objects.Add(transform, true);
            ret = true;
        }

        return ret;
    }

}
