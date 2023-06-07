using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wander : MonoBehaviour
{
    [SerializeField]
    private float TargetMargine = 0.2f;
    [SerializeField]
    private PathNode Target;
    private PathNode PreviousTarget = null;

    [SerializeField]
    private float RotationSpeed = 0.35f;

    [SerializeField]
    private float WalkSpeed = 0.01f;

    [SerializeField]
    private float LookDirectionAngleBeforeWalking = 40f;

    [SerializeField]
    private float SpeedDecreaseWhileTurning = 3;
    

    private void Update()
    {
        var distance = Vector3.Distance(transform.localPosition, Target.transform.localPosition);
        if (distance > TargetMargine)
        {
            var direction = (Target.transform.localPosition - transform.localPosition).normalized;
            var lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * RotationSpeed);

            var angleLookAt = Quaternion.Angle(transform.rotation, lookRotation);
            var speed = WalkSpeed;
            if (angleLookAt > LookDirectionAngleBeforeWalking)
                speed /= SpeedDecreaseWhileTurning;
            transform.localPosition = transform.localPosition + transform.forward * Time.deltaTime * speed;
        }
        else
        {
            PathNode nextTarget = null;
            while (nextTarget == null || nextTarget == PreviousTarget)
            {
                var nextValue = Random.Range(0, Target.Connected.Length);
                nextTarget = Target.Connected[nextValue];
            }
            PreviousTarget = Target;
            Target = nextTarget;
        }
    }
}
