using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AddItemToCart : ITargetReachInteraction
{
    [SerializeField]
    private float DistanceUp = 0.2f;
    [SerializeField]
    private float speed = 0.2f;

    List<Transform> Children = new List<Transform>();

    private void Start()
    {
        foreach (Transform child in transform)
        {
            Children.Add(child);
        }
    }


    public override void OnTargetReach()
    {
        var disabled = Children.Where(c => !c.gameObject.activeSelf).ToList();
        if (disabled.Count() > 0)
        {
            var rand = Random.Range(0, disabled.Count());
            var chosen = disabled[rand];
            chosen.gameObject.SetActive(true);
            StartCoroutine(MoveDownToPosition(chosen));
        }
        else
        {
            foreach (var child in Children)
            {
                child.gameObject.SetActive(false);
            }
        }

    }

    private IEnumerator MoveDownToPosition(Transform obj)
    {
        var endPosition = obj.localPosition;
        obj.localPosition = endPosition + new Vector3(0, DistanceUp, 0);
        while (!Mathf.Approximately(obj.localPosition.y, endPosition.y))
        {
            obj.localPosition -= new Vector3(0, speed, 0 ) * Time.deltaTime;
            if (endPosition.y > obj.localPosition.y)
                obj.localPosition = new Vector3(obj.localPosition.x, endPosition.y, obj.localPosition.z);
            yield return obj;
        }
    }



}
