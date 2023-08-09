using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class ImageTargetManager : MonoBehaviour
{
    [Tooltip("DefaultObserverEventHandler component on image target.")]
    [SerializeField] DefaultObserverEventHandler defaultObserverEventHandler;

    [Tooltip("GameObject which gets synchronized with transform of image target.")]
    [SerializeField] GameObject pivot;

    [Tooltip("Minimum distance from imageTarget before we consider updating.")]
    [SerializeField] float minDevationDistance = 0.02f;
    [Tooltip("Minimum distance from imageTarget before we begin updating target position and rotation. Too close to image target results in a incorrect position.")]
    [SerializeField] float minValidDistance = 0.1f;

    [Tooltip("Always update with position of found image target. Do not use ARAnchor.")]
    [SerializeField] bool alwaysUpdate = false;

    [SerializeField] ARAnchor anchorPrefab;
    private ARAnchor anchorInstance;

    float deviationPeriod = 0;

    private bool found;
    private bool firstTimeFound;

    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private Camera myCamera;

    void Start()
    {
        pivot.gameObject.SetActive(false);
        found = false;
        firstTimeFound = false;
    }

    void OnEnable()
    {
        defaultObserverEventHandler.OnTargetFound.AddListener(OnTargetFound);
        defaultObserverEventHandler.OnTargetLost.AddListener(OnTargetLost);
    }

    void OnDisable()
    {
        defaultObserverEventHandler.OnTargetFound.RemoveListener(OnTargetFound);
        defaultObserverEventHandler.OnTargetLost.RemoveListener(OnTargetLost);
        DestroyAnchor();
    }

    private void InitAnchor(Vector3 position, Quaternion rotation)
    {
        DestroyAnchor();

        anchorInstance = Instantiate<ARAnchor>(anchorPrefab, position, rotation);
    }

    private void DestroyAnchor()
    {
        if (anchorInstance != null)
        {
            Destroy(anchorInstance.gameObject);
        }
    }

    private void Update()
    {
        if (found)
        {
            // do not update when close to imageTarget
            if (!ValidDistanceToCamera())
            {
                return;
            }

            var newTargetRotation = defaultObserverEventHandler.transform.rotation;
            var newTargetPosition = defaultObserverEventHandler.transform.position;

            bool forceUpdate = false;
            if (!firstTimeFound || alwaysUpdate)
            {
                firstTimeFound = true;

                pivot.gameObject.SetActive(true);
                pivot.transform.position = newTargetPosition;
                pivot.transform.rotation = newTargetRotation;

                if (alwaysUpdate)
                {
                    return;
                }

                forceUpdate = true;
            }

            if ((targetPosition - newTargetPosition).magnitude > minDevationDistance ||
                Quaternion.Angle(targetRotation, newTargetRotation) > 2)
            {
                deviationPeriod += Time.deltaTime;
            }

            if (deviationPeriod > 1.5f || forceUpdate)
            {
                forceUpdate = false;
                deviationPeriod = 0f;
                InitAnchor(newTargetPosition, newTargetRotation);
                targetPosition = newTargetPosition;
                targetRotation = newTargetRotation;
            }

            if (anchorInstance != null)
            {
                targetPosition = anchorInstance.transform.position;
                targetRotation = anchorInstance.transform.rotation;
            }

            float smoothTime = 0.05f;
            Vector3 velocity = new Vector3();
            pivot.transform.position = Vector3.SmoothDamp(pivot.transform.position, targetPosition, ref velocity, smoothTime);

            Quaternion derivative = new Quaternion();
            pivot.transform.rotation = SmoothDamp(pivot.transform.rotation, targetRotation, ref derivative, smoothTime);
        }
    }

    private bool ValidDistanceToCamera()
    {
        if (myCamera == null)
        {
            myCamera = Camera.main;
        }

        var pos1 = defaultObserverEventHandler.transform.position;
        pos1.y = 0;
        var pos2 = myCamera.transform.position;
        pos2.y = 0;

        var distance = (pos1 - pos2).magnitude;
        if (distance >= minValidDistance)
        {
            return true;
        }

        return false;
    }

    private void OnTargetFound()
    {
        found = true;
    }

    private void OnTargetLost()
    {
        found = false;
    }

    private static Quaternion SmoothDamp(Quaternion rot, Quaternion target, ref Quaternion deriv, float time)
    {
        if (Time.deltaTime < Mathf.Epsilon) return rot;
        // account for double-cover
        var Dot = Quaternion.Dot(rot, target);
        var Multi = Dot > 0f ? 1f : -1f;
        target.x *= Multi;
        target.y *= Multi;
        target.z *= Multi;
        target.w *= Multi;
        // smooth damp (nlerp approx)
        var Result = new Vector4(
            Mathf.SmoothDamp(rot.x, target.x, ref deriv.x, time),
            Mathf.SmoothDamp(rot.y, target.y, ref deriv.y, time),
            Mathf.SmoothDamp(rot.z, target.z, ref deriv.z, time),
            Mathf.SmoothDamp(rot.w, target.w, ref deriv.w, time)
        ).normalized;

        // ensure deriv is tangent
        var derivError = Vector4.Project(new Vector4(deriv.x, deriv.y, deriv.z, deriv.w), Result);
        deriv.x -= derivError.x;
        deriv.y -= derivError.y;
        deriv.z -= derivError.z;
        deriv.w -= derivError.w;

        return new Quaternion(Result.x, Result.y, Result.z, Result.w);
    }
}
