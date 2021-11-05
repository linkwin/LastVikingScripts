using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AC;
using System;

public class ActionController : MonoBehaviour
{

    public GameObject triggerbox;
    public GameObject triggerboxright;

    public int health = 1;

    public EventRaiser PlayerDeathEvent;

    private Animator anim;

    private bool isBlocking;

    private int blockDirection;

    private bool isCrouching;

    /* AC player ref*/
    private Player player;

    //Reset health on respawm
    private void Awake()
    {
        player = GetComponentInParent<Player>();
        health = 1;
        isBlocking = false;
        isCrouching = false;
        StartCoroutine(IgnoreRaycastOnLoad());//TODO temp patch?
    }

    /*
     * Switches to ignore raycast layer when spawned in to prevent trggering 
     * any NPC_AI sight scans prematurely. Restores original layer after 3 second.
     */
    private IEnumerator IgnoreRaycastOnLoad()
    {
        this.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        yield return new WaitForSeconds(3f);
        this.gameObject.layer = LayerMask.NameToLayer("Characters");
    }

    // Start is called before the first frame update
    void Start()
    {
        anim = this.GetComponent<Animator>();
    }


    private void OnEnable()
    {
        EventManager.OnFinishLoading += Respawn;
    }
    private void OnDisable()
    {
        EventManager.OnFinishLoading -= Respawn;
    }

    void Respawn()
    {
        health = 1;
        anim.SetBool("Blocking", true);
        anim.SetFloat("X_Speed", 0.0f);
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


    // Update is called once per frame
    void Update()
    {
        if (anim.GetFloat("X_Speed") == 0.0f)
            anim.SetFloat("X_Speed", 0.2f);
        AnimationUpdate();
        if (health != 0)
        {
            // Register inputs only if this player is active
            if (player.IsActivePlayer())
            {
                //SLASH
                if (Input.GetButtonDown("Fire1"))
                    anim.SetTrigger("slash");

                //CROUCH
                if (Input.GetButtonDown("Crouch"))
                    isCrouching = !isCrouching;

                //BLOCK
                if (Input.GetButtonDown("Fire2"))
                {
                    blockDirection = anim.GetInteger("Direction");
                    isBlocking = true;
                }
                if (Input.GetButtonUp("Fire2"))
                {
                    blockDirection = -1;
                    isBlocking = false;
                }

            }
            anim.SetBool("Crouching", isCrouching);
            anim.SetFloat("Move_State", isCrouching ? 1.0f : 0.0f);
            anim.SetBool("Blocking", isBlocking);
            //if we are overrided direction changes because blocking
            if (blockDirection != -1)
            {
                anim.SetInteger("Direction", blockDirection);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        //DEATH
//        if (health > 0 && collision.tag == "EnemyAttack")
        if (health > 0 && collision.tag == "EnemyAttack")
        {
            if (isBlocking)
            {
                anim.SetTrigger("Hit");
            }
            else
            {
//                Debug.Log("DEAD");
                PlayerDeathEvent.RaiseEvent();
                health = 0;
//                anim.SetInteger("Health", health);
            }
        }
    }

    void EnableTriggerBox()
    {
        if (triggerbox)
            triggerbox.SetActive(true);
    }

    void DisableTriggerBox()
    {
        if (triggerbox)
            triggerbox.SetActive(false);
    }
    void EnableTriggerBoxRight()
    {
        if (triggerboxright)
            triggerboxright.SetActive(true);
    }

    void DisableTriggerBoxRight()
    {
        if (triggerboxright)
            triggerboxright.SetActive(false);
    }

    public void SetCrouching(bool c)
    {
        isCrouching = c;
    }
}
