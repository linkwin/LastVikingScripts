using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Spawner : MonoBehaviour
{
    /*Time to delay in between spawns*/
    private const float DELAYTIME = 0.75f;

    /* Transform where object pool stores objects while 'unspawned' */
    private Transform PoolLoc;

    /* Total number of enemies to initialize pool size to. */
    const int POOLSIZE = 20;
    
    /* Static ref to object pool to use for spawning */
    protected static ObjectPool EnemyPool = new ObjectPool(POOLSIZE);

    /* Current number of spawned enemy NPC's */
    private int numSpawned;

    [Tooltip(" The prefab of the object to spawn. ")]
    public GameObject objToSpawn;

    [Tooltip(" Number of objects to spawn per 'wave'. ")]
    public int numToSpawn;

    [Tooltip(" If true, spawner will continue to spawn after first round. ")]
    public bool reloadSpawner = false;

    public bool spawnOnStart = false;

    //TODO number of waves to spawn.

    [Tooltip(" If reloadSpawner is enabled, total spawns will not excede this number. ")]
    public int maxSpawnNum = 20;

    /* List of currently spawned enemies this spawner has control over. */
    private List<Transform> enemies = new List<Transform>();
//    private void Start()
//    {
//        numSpawned = 0;
//        if (spawnOnStart)
//            Spawn();
//    }

    private AC.EventManager.Delegate_AfterSceneChange OnSceneLoad()
    {
        go = new GameObject();
        PoolLoc = go.transform;

        // initialize object pool
        if (!EnemyPool.Initialized)
            EnemyPool.Initialize(objToSpawn, PoolLoc);

        Debug.Log("RESETING POOL");
        // Build a list of all transforms
        List<Transform> objsTransforms = new List<Transform>(POOLSIZE);
        foreach (KeyValuePair<Transform, bool> t in EnemyPool.Objects)
            objsTransforms.Add(t.Key);

        // Call despawn on all transforms except on 1st level.
        if (SceneManager.GetActiveScene().name != "Level 1")
            foreach (Transform t in objsTransforms)
                Despawn(t, true);

        // Gather any manually placed enemies and add them to the pool.
        GameObject[] es = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject go in es)
        {
            NPC_AI npcai = go.GetComponentInChildren<NPC_AI>();
            if (npcai.getOwner() == null)
            {
                EnemyPool.AddToPool(go.transform);
                go.GetComponentInChildren<NPC_AI>().setOwner(this);
            }
        }

        numSpawned = 0;
        if (spawnOnStart)
            Spawn();
        return null;
    }

    private void OnEnable()
    {
        //AC.EventManager.OnInitialiseScene += OnSceneLoad();
        AC.EventManager.OnAfterChangeScene += OnSceneLoad();
        
    }

    GameObject go;
    private void Awake()
    {
        // Transform where unspawned enemies reside
        //PoolLoc = GameObject.FindGameObjectWithTag("PoolLoc").transform;//TODO


        //AC.EventManager.OnInitialiseScene += OnSceneLoad();
        //AC.EventManager.OnEnterGameState += OnSceneLoad();

    }

    public void Spawn()
    {
        //filter out null (destroyed) enemies from list
        //enemies = enemies.Where(e => e != null).ToList();//TODO is thie doing anything with object pooling?

        //if reload enabled and all have spawned at least once
        if (reloadSpawner && numSpawned == numToSpawn)
            numSpawned = 0;
        
        //if total number of spawns has not exceded maxspawnnum
        if (enemies.Count < 20)
            StartCoroutine(DoSpawn());
    }

    IEnumerator DoSpawn()
    {
        Debug.Log("Spawn coroutine start!!");
        while (numSpawned < numToSpawn)
        {
            Transform spawn = EnemyPool.GetSpawnRef();
            if (spawn)
            {
                Debug.Log("Spawn #" + numSpawned);
                //spawn.gameObject.SetActive(true);
                // Set params
                //spawn.position = this.transform.position;//TODO defined starting location 
                spawn.Translate(this.transform.position - spawn.position, Space.World);
                spawn.position = new Vector3(spawn.position.x, spawn.position.y, 0);

                spawn.gameObject.GetComponentInChildren<NPC_AI>().Spawn();

                spawn.gameObject.GetComponentInChildren<NPC_AI>().setOwner(this);


                // Add to local spawner list.
                enemies.Add(spawn);
                numSpawned++;
            }
            //enemies.Add(Instantiate(objToSpawn, this.transform.position, this.transform.rotation).transform);

            yield return new WaitForSeconds(DELAYTIME);
        }

    }

    /**
     * Used as an interface to the object pool. Removes an object from play and places back into the pool for
     * later use.
     * 
     * <param name="force"> If true, will not verify existence in this spawners cache before despawning. </param>
     * <param name="t"> The Transform to be despawned. </param>
     * <returns> If the despawn was sucessful. </returns>
     */
    public bool Despawn(Transform t, bool force)
    {
        bool ret = false;

        // If speciied transform is found in this spawner cache.
        if (force || enemies.Remove(t))
        {

            t.Translate(new Vector3(0,0,-500) - t.position);
            t.gameObject.GetComponentInChildren<NPC_AI>().ReadyForRespawn();
            EnemyPool.Despawn(t);
            ret = true;
            numSpawned--;
        }

        return ret;
    }
}
