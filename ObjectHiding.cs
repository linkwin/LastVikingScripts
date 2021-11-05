using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectHiding : MonoBehaviour
{
    [Tooltip("The value to change the opactiy to when hiding behind")]
    public float intensity = 0.5f;
    public float overlapRadius = 2f;

    private Collider2D lastHit;
    private Color originColor;

    private SpriteRenderer sprite;
    private bool revert;

    private List<GameObject> overlappedObjects;

    private void Start()
    {
        originColor = GetComponentInChildren<SpriteRenderer>().color;
        overlappedObjects = new List<GameObject>();
    }

    private void Update()
    {
        //if overlap is not null
//        if (lastHit)
//        {
//            sprite = lastHit.gameObject.GetComponent<SpriteRenderer>();
//
//            originColor = sprite.color;
//            sprite.color = new Color(originColor.r, originColor.g, originColor.b, intesity);
//        } else
//        {
//            sprite.color = new Color(originColor.r, originColor.g, originColor.b, intesity);
//            revert = false;
//            sprite = null;
//        }
        Collider2D[] hits = Physics2D.OverlapCircleAll(this.transform.position + 3f*Vector3.down, overlapRadius);
        List<GameObject> nextOverlappedObjects = new List<GameObject>();

        for (int i = 0; i < hits.Length; i++)//build list of gameobjects that are overlaping this dt
        {
            if (hits[i].gameObject.tag == "SetPiece")
                nextOverlappedObjects.Add(hits[i].gameObject);
        }

        //add any new objects to the list
        foreach (GameObject obj in nextOverlappedObjects)
        {
            if (!overlappedObjects.Contains(obj))
            {
                ObjectOverlapped(obj, true);
                overlappedObjects.Add(obj);
            }
        }
        //purge list and revert opacity
        List<GameObject> toRemove = new List<GameObject>();
        foreach (GameObject obj in overlappedObjects)
        {
            if (!nextOverlappedObjects.Contains(obj))//if the new list does not contain an object from the current list, revert it and purge from list
            {
                ObjectOverlapped(obj, false);
                toRemove.Add(obj);
            }
        }
        foreach (GameObject obj in toRemove)
        {
            overlappedObjects.Remove(obj);
        }

        // Switch layer back only if there are no overlapped objects
        if (overlappedObjects.Count == 0)
            GetComponentsInChildren<SpriteRenderer>()[0].gameObject.layer = LayerMask.NameToLayer("Characters");
    }

    private void ObjectOverlapped(GameObject obj, bool overlapped)
    {
        if (overlapped)
        {
            obj.GetComponent<SpriteRenderer>().color = new Color(originColor.r, originColor.g, originColor.b, intensity);
            GetComponentsInChildren<SpriteRenderer>()[0].gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
        else
        {
            if (obj)
                obj.GetComponent<SpriteRenderer>().color = originColor;
        }
    }

}
