using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorkObject : MonoBehaviour
{
    protected ActiveCheck activeCheck;

    public virtual void Awake()
    {
        //activeCheck = FindObjectOfType<ActiveCheck>();
        //activeCheck.AddWorkObject(this.name);
    }

    public virtual bool IsActive()
    {
        return true;
    }
}
