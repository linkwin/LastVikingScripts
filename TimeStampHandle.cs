using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeStampHandle
{

    private float timeStamp;
    private float time;

    /**
     * <param name="time"> The ammount of time until this timer is complete. </param>
     */
    public TimeStampHandle(float time)
    {
        this.time = time;
    }

    public void Set(float timeStamp)
    {
        this.timeStamp = timeStamp;
    }

    public bool Check(float currentTime)
    {
        bool ret = false;

        if (currentTime - timeStamp > time)
            ret = true;

        return ret;
    }
}
