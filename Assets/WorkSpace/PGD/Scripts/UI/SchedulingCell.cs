using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SchedulingCell : MonoBehaviour
{
    public TMP_Text text_name;
    public TMP_Text text_count;

    public void OnClickDownBtn()
    {
        int currentIndexNum = transform.GetSiblingIndex();
        
        if(currentIndexNum < transform.parent.childCount - 1)
        {
            transform.SetSiblingIndex(currentIndexNum + 1);
        }
    }

    public void OnClickUpBtn()
    {
        int currentIndexNum = transform.GetSiblingIndex();

        if (currentIndexNum > 0)
        {
            transform.SetSiblingIndex(currentIndexNum - 1);
        }
    }

    public void OnClickDeleteBtn()
    {
        Destroy(gameObject);
    }
}
