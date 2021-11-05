using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/**
 *
 * NOTE: Use only 1 EventRaiser per TriggerEvent component.
 * 
 * @author linkwin.itch.io
 * 
 */
public class TriggerEvent : MonoBehaviour
{

    public string tagFilter;
    public bool multipleTrigger = false;
    public bool sceneChange;
    public string sceneToLoad;
    private EventRaiser eventRaiser;
    private bool hasBeenTriggered;

    private void Start()
    {
        eventRaiser = GetComponent<EventRaiser>();
        hasBeenTriggered = false;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == tagFilter)
        {
            if (multipleTrigger)
                ManualTrigger();
            else if (!hasBeenTriggered)
            {
                ManualTrigger();
                hasBeenTriggered = true;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log("YES");
        if (collision.gameObject.tag == tagFilter)
        {
            this.GetComponent<EventRaiser>().RaiseEvent();
        }
    }


    public void ManualTrigger()
    {
        if (eventRaiser)
            eventRaiser.RaiseEvent();
        if (sceneChange)
            SceneManager.LoadScene(sceneToLoad);
    }
}
