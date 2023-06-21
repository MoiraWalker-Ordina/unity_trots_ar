
using UnityEngine;

namespace Assets.Scripts
{
    public class WanderRandom : MonoBehaviour
    {
        [SerializeField]
        private float AreaWidth = 0.35f;
        [SerializeField]
        private float AreaHeight = 0.35f;
        [SerializeField]
        private Transform StartLocation;
        [SerializeField]
        private Transform Target;
        [SerializeField]
        private float RotationSpeed = 0.35f;
        [SerializeField]
        private float WalkSpeed = 0.01f;
        [SerializeField]
        private float LookDirectionAngleBeforeWalking = 40f;
        [SerializeField]
        private float SpeedDevisionWhenTurning = 3;
        [SerializeField]
        private float DetectionRange = 0.03f;
        [SerializeField]
        private ITargetReachInteraction CartItems;


        private void Update()
        {

            var distance = Vector3.Distance(transform.localPosition, Target.localPosition);
            if (distance > DetectionRange)
            {
                var direction = (Target.localPosition - transform.localPosition).normalized;
                var lookRotation = Quaternion.LookRotation(direction);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, lookRotation, Time.deltaTime * RotationSpeed);

                var angleLookAt = Quaternion.Angle(transform.localRotation, lookRotation);
                var speed = WalkSpeed;
                if (angleLookAt > LookDirectionAngleBeforeWalking)
                    speed /= SpeedDevisionWhenTurning;

                var newPos = transform.localPosition + transform.TransformDirection(Vector3.left) * Time.deltaTime * speed; ;
                newPos.y = 0;
                transform.localPosition = newPos;


            }
            else
            {
                var attempts = 0;
                while (distance <= DetectionRange && attempts < 20)
                {
                    MoveTarget();
                    distance = Vector3.Distance(transform.localPosition, Target.localPosition);
                    attempts++;
                }
                if (CartItems != null)
                {
                    CartItems.OnTargetReach();
                }
            }
        }

        private void MoveTarget()
        {
            var x = Random.Range(StartLocation.localPosition.x - AreaWidth / 2, StartLocation.localPosition.x + AreaWidth / 2);
            var z = Random.Range(StartLocation.localPosition.z - AreaWidth / 2, StartLocation.localPosition.z + AreaWidth / 2);
            Target.localPosition = new Vector3(x, Target.localPosition.y, z);
        }
    }
}
