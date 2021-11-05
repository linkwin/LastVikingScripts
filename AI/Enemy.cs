using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public EventRaiser DeathEvent;
    private bool isAlive;

    public bool IsAlive { get => isAlive; set => isAlive = value; }

    // Start is called before the first frame update
    void Start()
    {
        isAlive = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        NPC_AI npc = GetComponent<NPC_AI>();
        if (collision.tag == "PlayerAttack")
        {
            if (isAlive)
            {
                //GetComponent<BoxCollider2D>().enabled = false;
                GetComponentInParent<CircleCollider2D>().enabled = false;
                if (DeathEvent)
                    DeathEvent.RaiseEvent();
                if (npc.CurentState != NPC_AI.AI_State.Death)
                    npc.Death();
                isAlive = false;
            }
        }
    }
}
