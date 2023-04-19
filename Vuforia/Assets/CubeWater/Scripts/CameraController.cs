using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CubeWater
{

    public class CameraController : MonoBehaviour
    {
        [SerializeField]
        private Camera m_camera;
        [SerializeField]
        private GameObject m_rotatePivot;
        [SerializeField]
        private float m_rotateSpeed = 0.1f;
        private Vector3 m_preHitPoint;
        private Vector3 m_curHitPoint;
        // Use this for initialization
        void Start()
        {
            if (m_camera == null)
            {
                m_camera = Camera.main;
            }
            if (m_rotatePivot == null)
            {
                m_rotatePivot = this.gameObject;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (null == m_camera || m_rotatePivot == null)
            {
                return;
            }
            if (Input.GetMouseButtonUp(0))
            {
                m_preHitPoint = Vector3.zero;
                return;
            }
            if (Input.GetMouseButton(0))
            {
                m_curHitPoint = Input.mousePosition;
                if (m_preHitPoint == Vector3.zero)
                {
                    m_preHitPoint = m_curHitPoint;
                }
                float xOffset = m_curHitPoint.x - m_preHitPoint.x;
                float yOffset = m_curHitPoint.y - m_preHitPoint.y;
                if (Mathf.Abs(xOffset) > Mathf.Abs(yOffset))
                {
                    float angle = xOffset * m_rotateSpeed;
                    m_camera.transform.RotateAround(m_rotatePivot.transform.position, Vector3.up, angle);
                    m_preHitPoint = m_curHitPoint;
                }
                else
                {
                    float angle = -yOffset * m_rotateSpeed;
                    float xEuler = m_camera.transform.localEulerAngles.x;
                    xEuler = xEuler > 180 ? xEuler - 360 : xEuler;
                    xEuler = xEuler < -180 ? 360 + xEuler : xEuler;
                    if (xEuler > 75 && angle > 0)
                    {
                        return;
                    }
                    if (xEuler < -75 && angle < 0)
                    {
                        return;
                    }
                    m_camera.transform.RotateAround(m_rotatePivot.transform.position, m_camera.transform.right, angle);
                    m_preHitPoint = m_curHitPoint;
                }
            }
        }
    }
}