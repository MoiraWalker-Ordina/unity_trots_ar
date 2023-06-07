
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

        private void Update()
        {
            var distance = Vector3.Distance(transform.localPosition, Target.localPosition);
            if (distance > 0.02)
            {
                var direction = (Target.localPosition - transform.localPosition).normalized;
                var lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * RotationSpeed);

                var angleLookAt = Quaternion.Angle(transform.rotation, lookRotation);
                var speed = WalkSpeed;
                if (angleLookAt > LookDirectionAngleBeforeWalking)
                    speed /= 2;
                transform.localPosition = transform.localPosition + transform.forward * Time.deltaTime * speed;
            }
            else
            {
                var attempts = 0;
                while (distance <= 0.1 && attempts < 20)
                {
                    MoveTarget();
                    distance = Vector3.Distance(transform.localPosition, Target.localPosition);
                    attempts++;
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
