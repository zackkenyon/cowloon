using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonBehaviors : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void myfun(int i) { Manager.Instance.toPlace = Manager.Instance.prefabs[i - 1]; }
}
