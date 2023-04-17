using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logger : MonoBehaviour
{

    public GameObject sphere;

    // Start is called before the first frame update
    void Start()
    {
        sphere.SetActive(false);    
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TargetFound()
    {
        sphere.SetActive(true);
        Debug.Log("TargetFound");
    }

    public void TargetLost()
    {
        sphere.SetActive(false);
        Debug.Log("TargetLost");
    }
    
}
