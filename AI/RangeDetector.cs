using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RangeDetector : MonoBehaviour
{
    private bool inRange = false;

    public bool InRange { get => inRange; set => inRange = value; }

    void Start()
    {
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "CharacterToken")
            inRange = true;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "CharacterToken")
            inRange = false;
    }
}
