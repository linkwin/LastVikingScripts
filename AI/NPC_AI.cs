﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using AC;
using UnityEditor;

public class NPC_AI : MonoBehaviour
{
    public enum AI_State { Patroling, Alerted, Evade, Persue, Attacking, Tracking, Death};

    [Tooltip(" The state to put in NPC in when game/level starts. ")]
    public AI_State initialState;

    /* Internal current state this npc is in. */
    private AI_State curentState;

    public enum AI_Behavior { Normal, Flank };

    public AI_Behavior behaviorType = AI_Behavior.Normal;

    //Player ref 
    private GameObject player;
    private Vector3 lastKnownPlayerLocation;

    /* Navigation mesh generated by AC using polygon collider. this is needed to keep NPC in defined navigation bounds */
    private NavigationMesh navmesh;

    //The AC character being tracked by this NPC_AI instance
    private NPC character;
    private Animator anim;
    private RangeDetector rangeDetector;


    [Tooltip("The delay time in between attack actions. ")]
    public float timeBetweenAttacks = 3f;
    /* Timer handle for timing attack actions */
    TimeStampHandle lastActionTimer;
    /* Timer handle for timng out pursuit of player */
    TimeStampHandle stateTimeOutTimer;

    //SIGHT PARAMS
    public int sightRange = 8;
    public float scanSpeed = 0.5f;

    private Vector2 scanner;
    private Vector2 target;
    private bool active;
    private bool playerInVision;
    //END SIGHT PARAMS

    [Tooltip("Distance AI can hear player make a PlayerNoiseEvent")]
    public float hearingRange = 12f;

    public AI_State CurentState { get => curentState;}

    //If an enemy is currently locked on the player
    private static bool LOCKED = false;
    private bool lockOwner;

    private Spawner owner;

    private void Awake()
    {
        navmesh = AC.KickStarter.sceneSettings.navMesh;
        playerInVision = false;
        curentState = initialState;
    }

    public void setOwner(Spawner s)
    {
        owner = s;
    }
    public Spawner getOwner()
    {
        return owner;
    }

    void Start()
    {
        //curentState = initialState;

        character = this.GetComponentInParent<NPC>();
        anim = this.gameObject.GetComponent<Animator>();
        rangeDetector = GetComponentInChildren<RangeDetector>();
        scanner = Vector2.zero;
        active = false;
        Debug.Log("AI ACTIVE: " + active);
        playerInVision = false;
        StartCoroutine("RunSightScan");//sight sensor, switches AI to alerted state
        //character.MoveToPoint(character.transform.position + new Vector3(0.01f, 0)); //activates npc?

        lastActionTimer = new TimeStampHandle(timeBetweenAttacks);
        stateTimeOutTimer = new TimeStampHandle(3f);

        if (LOCKED)
            LOCKED = false;
        lockOwner = false;
    }

    public void ReadyForRespawn()
    {
        //curentState = initialState;

        //character = this.GetComponentInParent<NPC>();
        //anim = this.gameObject.GetComponent<Animator>();
        //rangeDetector = GetComponentInChildren<RangeDetector>();
        navmesh = AC.KickStarter.sceneSettings.navMesh;
        scanner = Vector2.zero;
        active = false;
        Debug.Log("AI ACTIVE: " + active);
        playerInVision = false;
        //StartCoroutine("RunSightScan");//sight sensor, switches AI to alerted state
        //character.MoveToPoint(character.transform.position + new Vector3(0.01f, 0)); //activates npc?

        lastActionTimer = new TimeStampHandle(timeBetweenAttacks);
        stateTimeOutTimer = new TimeStampHandle(3f);

        rangeDetector = GetComponentInChildren<RangeDetector>();
        rangeDetector.enabled = true;
        GetComponent<Enemy>().IsAlive = true;
        GetComponent<EventListener>().enabled = true;

        if (LOCKED)
            LOCKED = false;
        lockOwner = false;
    }


    void Update()
    {
        if (anim.GetFloat("X_Speed") == 0.0f)
            anim.SetFloat("X_Speed", -0.2f);

        AnimationUpdate();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");

        //if player is in attack range, attack: AnyState --> Attack
        if (rangeDetector.InRange && lastActionTimer.Check(Time.timeSinceLevelLoad) && curentState != AI_State.Death)
        {
            character.Halt();
            anim.SetTrigger("Jab");
            curentState = AI_State.Attacking;
            //TimeStamp();
            lastActionTimer.Set(Time.timeSinceLevelLoad);
        }

        Vector3 offset = lastKnownPlayerLocation - this.transform.position;
        float offsetSign = offset.x / Mathf.Abs(offset.x);
        NavMeshHit hit;

        switch (curentState)
        {
            case AI_State.Patroling:
                CheckLock();
                //Patrolling --> Alerated
                if (playerInVision)
                {
                    curentState = AI_State.Alerted;
                }
                break;
            case AI_State.Attacking:
                //character.EndPath();
                character.SetLookDirection(Vector3.Normalize(lastKnownPlayerLocation - this.transform.position), true);
                //if attack cooldown is not over or player is out of range Attack --> Alerted
                if (lastActionTimer.Check(Time.timeSinceLevelLoad) || Vector3.Distance(this.transform.position, lastKnownPlayerLocation) > 8f);//wait 3 seconds after attack to make a new one
                {
                    //character.FollowAssign(player.GetComponent<Player>(), true, 1f, 10f, 10f);
                    curentState = AI_State.Alerted;
                }
                break;
            case AI_State.Alerted:
                //if player gets away: Alerted --> Patrolling
                CheckLock();
                if (!LOCKED)
                {
                    LOCKED = true;
                    lockOwner = true;
                }

                if (stateTimeOutTimer.Check(Time.timeSinceLevelLoad))
                {
                    curentState = AI_State.Patroling;
                    break;
                }
                //Debug.Log("ALERTED");

                //if set to flank type, change target to behind player.
                if (behaviorType == AI_Behavior.Flank)
                {
                    offsetSign = -offsetSign;
                    behaviorType = AI_Behavior.Normal;
                }

                Vector3 targetLocation = lastKnownPlayerLocation + new Vector3(-offsetSign * 8, -2.5f); //position for attack to land on player.

                //minimize distance, attempt to postion in front of player for attack to trigger
                //if not in attack range
                //TODO possibly just use x value?
                if (!(Vector2.Distance(this.transform.position, player.transform.position) >= 5 && Vector2.Distance(this.transform.position, player.transform.position) <= 8))
                {
                    lastKnownPlayerLocation = player.GetComponent<Transform>().position;
                    //character.SetMoveDirection(lastKnownPlayerLocation - this.GetComponent<Transform>().position);
                    //TODO check around target location, if there is another AI, change target
                    Collider2D[] npc = Physics2D.OverlapCircleAll(this.transform.position + (2 * Vector3.down), 1);
                    Enemy enemyNpc = null;
                    foreach (Collider2D col in npc)
                    {
                        //if colider hit has enemy component attached AND is NOT EQUAL to this npc
                        if (col.GetComponent<Enemy>() && !col.GetComponent<Enemy>().Equals(GetComponentInParent<Enemy>()))
                        {
                            enemyNpc = col.GetComponent<Enemy>();
                            break;
                        }
                    }
                    float scale = 3f; //TODO MAGICNUMBER

                    Vector3[] offsets = {new Vector3(-offsetSign * 6, 0), 
                                         new Vector3(-offsetSign * 8, 0f), 
                                         new Vector3(-offsetSign * 8, -5f), 
                                         new Vector3(offsetSign * 8, 0f), 
                                         new Vector3(offsetSign * 8, -5f), 
                                         new Vector3(offsetSign * 6, 5f), 
                                         new Vector3(-offsetSign * 6, 2.5f), 
                                         new Vector3(-offsetSign * 6, 0)};
                    // If player is being attacked by an enemy currently and it is not this npc
                    if (enemyNpc && LOCKED && !lockOwner)
                    {
                        targetLocation = lastKnownPlayerLocation + offsets[Random.Range(0, offsets.Length)];
                    }
                        

                    character.MoveToPoint(navmesh.gameObject.GetComponent<PolygonCollider2D>().ClosestPoint(targetLocation));
                    
//                    if (NavMesh.SamplePosition(targetLocation, out hit, 10f, NavMesh.AllAreas))
//                        character.MoveToPoint(hit.position);
//                    else
//                        Debug.Log("INVALID POINT INCREASE RADIUS");
                    character.SetLookDirection(Vector3.Normalize(lastKnownPlayerLocation - this.transform.position), true);
                    character.isRunning = true;
                }
                else
                {
                    curentState = AI_State.Attacking;
                }
                break;
            case AI_State.Death:
                break;
            case AI_State.Tracking:
                if (lastActionTimer.Check(Time.timeSinceLevelLoad))//wait 3 seconds after attack to make a new one

                character.FollowAssign(player.GetComponent<Player>(), true, 1f, 5f, 5f);
                break;
        }

    }

    private void AnimationUpdate()
    {

        float angle = anim.GetFloat("Angle");     // The current cardinal direction this character is moving, expressed in degrees by AC.
        float speed = anim.GetFloat("Speed");     // The current speed this character is moving at, 1.0 = walk; 1.0 + run_var = run; defined by AC.
        float xSpeed = anim.GetFloat("X_Speed");  // The current encoded horizontal axis (velocity), direction is indicated by sign, neg = left; pos = right.
        //float ySpeed = anim.GetFloat("Y_Speed");  //TODO define vertical axis velecity

        //if facing left
        if (angle > 5 && angle < 175)
            if (speed >= 0.2f)
                anim.SetFloat("X_Speed", -1 * speed);

        //if facing right
        if (angle > 185 && angle < 355)
            if (speed >= 0.2f)
                anim.SetFloat("X_Speed", speed);

        //facing up
        if (angle > 175 && angle < 185)
            if (speed >= 0.2f)
                anim.SetFloat("X_Speed", (xSpeed / Mathf.Abs(xSpeed)) * speed);

        //facing down
        if (angle < 5 || (angle > 355 && angle <= 360))
            if (speed >= 0.2f)
                anim.SetFloat("X_Speed", (xSpeed / Mathf.Abs(xSpeed)) * speed);
    }

    public void Spawn()
    {
        curentState = AI_State.Patroling;
    }

    public void Death()
    {
        if (curentState != AI_State.Death)
        {
            GetComponent<EventListener>().enabled = false;
            character.Halt();
            Debug.Log("DEAD");
            rangeDetector.enabled = false;
            anim.SetTrigger("Death");
            curentState = AI_State.Death;
            CheckLock();
        }
    }

    private void CheckLock()
    {
        if (!LOCKED)
        {
            LOCKED = true;
            lockOwner = true;
        }
        if (LOCKED && lockOwner)
        {
            LOCKED = false;
            lockOwner = false;
        }
    }

    /**
     * Called from animation event.
     */
    public void DestroyNPC()
    {
        if (curentState == AI_State.Death && owner)
            owner.Despawn(transform.parent, false);
    }

    public void Playerspoted()
    {
        if (Mathf.Abs(Vector3.Distance(player.transform.position, this.transform.position)) < hearingRange)
        {
            Debug.Log(Mathf.Abs(Vector3.Distance(player.transform.position, this.transform.position)));
            curentState = AI_State.Alerted;
        }
    }
    
    public void AlertedByPlayer()
    {
        if (Mathf.Abs(Vector3.Distance(player.transform.position, this.transform.position)) < hearingRange)
        {
            Debug.Log(Mathf.Abs(Vector3.Distance(player.transform.position, this.transform.position)));
            curentState = AI_State.Alerted;
        }
    }

    /**
     * Coroutine to run vision/sight sensor logic.
     **/
    IEnumerator RunSightScan()
    {
        for (; ; )
        {
            playerInVision = SightScan();
            yield return new WaitForSeconds(0.04f);
        }
    }

    /**
     * Vision sensor logic.
     * 
     * Raycast scans in semi circle in front of character with radius 'sightRange'.
     * 
     * <returns> if player was hit durring scan.</returns>
     **/
    private bool SightScan()
    {
        Vector2 sightMax = new Vector2(character.GetMoveDirection().x, 0);
        Vector2 sightMin = new Vector2(character.GetMoveDirection().x, 0);
        //float singleStep = scanSpeed * Time.deltaTime;
        float singleStep = scanSpeed;

        //Facing Left
        //if (character.GetMoveDirection().x < 0)
        if (this.transform.localScale.x > 0)
        {
            sightMax = sightRange * new Vector2(-1,1);
            sightMin = sightRange * new Vector2(-1,-1);
            target = new Vector2(-Mathf.Abs(target.x), target.y);
            scanner = new Vector2(-Mathf.Abs(scanner.x), scanner.y);
        }
        //Facing Right
        //else if (character.GetMoveDirection().x > 0)
        else if (this.transform.localScale.x < 0)
        {
            sightMax = sightRange * new Vector2(1,1);
            sightMin = sightRange * new Vector2(1,-1);
            target = new Vector2(Mathf.Abs(target.x), target.y);
            scanner = new Vector2(Mathf.Abs(scanner.x), scanner.y);
        }

        //initial setup
        if (scanner == Vector2.zero)
        {
            scanner = sightMin;
            target = sightMax;
        }


        //keep rotating until scanner == target, then toggle target. (between sightMax and sightMin)
        if (scanner != target)
        {
            scanner = Vector3.RotateTowards(scanner, target, singleStep, 1f);
        } 
        else //toggle target
        {
            if (target == sightMax)
                target = sightMin;
            else if (target == sightMin)
                target = sightMax;
        }

        Debug.DrawRay(this.transform.position, sightMax, Color.red);
        Debug.DrawRay(this.transform.position, sightMin, Color.red);
        Debug.DrawRay(this.transform.position, scanner, Color.cyan);
        Debug.DrawRay(this.transform.position, target, Color.green);

        int layerMask = LayerMask.GetMask(new string[] { "Characters" });
        RaycastHit2D hit = Physics2D.Raycast(this.transform.position, scanner, Mathf.Sqrt(Vector2.SqrMagnitude(scanner)), layerMask);

        bool returnVal = false;
        if (hit && hit.transform.gameObject.tag == "Player")
        {
            if (stateTimeOutTimer != null)
                stateTimeOutTimer.Set(Time.timeSinceLevelLoad);
            returnVal = true;
        }

        playerInVision = returnVal;

        return returnVal;
    }

    private void OnDrawGizmos()
    {
        Handles.Label(transform.position, "" + curentState);
    }
}