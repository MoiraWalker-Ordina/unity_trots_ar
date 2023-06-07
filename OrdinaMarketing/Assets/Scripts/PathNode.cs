using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    public PathNode[] Connected = new PathNode[0];


    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, 0.003f);

        foreach(var node in Connected)
        {
            Gizmos.DrawLine(transform.position, node.transform.position);
        }
    }
}
