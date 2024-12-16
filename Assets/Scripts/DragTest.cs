using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragTest : MonoBehaviour
{

    public void BeginDrag(GameObject gameObject)
    {
        print("BeginDrag:" + gameObject);
    }
    public void OnDrag(GameObject gameObject)
    {
        print("OnDrag:" + gameObject);
    }
    public void EndDrag(GameObject gameObject)
    {
        print("EndDrag:" + gameObject);
    }
    public void OnDrop(GameObject gameObject)
    {
        print("OnDrop:" + gameObject);
    }

}
