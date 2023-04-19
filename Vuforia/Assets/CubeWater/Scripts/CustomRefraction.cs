using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CubeWater
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class CustomRefraction : MonoBehaviour
    {
        public string RefractionSampler = "_RefractionTex";
        private Dictionary<Camera, CommandBuffer> m_cameras = new Dictionary<Camera, CommandBuffer>();

        void Clearup()
        {
            foreach (var cam in m_cameras)
            {
                if (cam.Key != null)
                {
                    cam.Key.RemoveCommandBuffer(CameraEvent.AfterForwardOpaque, cam.Value);
                }
            }
            m_cameras.Clear();
        }

        private void OnEnable()
        {
            Clearup();
        }

        private void OnDisable()
        {
            Clearup();
        }

        private void OnPreRender()
        {
            bool active = gameObject.activeInHierarchy && enabled;
            if (!active)
            {
                return;
            }

            Camera cam = Camera.current;
            if (cam == null)
            {
                return;
            }

            if (m_cameras.ContainsKey(cam))
            {
                return;
            }

            CommandBuffer buffer = new CommandBuffer();
            buffer.name = "Grab Screen";
            m_cameras[cam] = buffer;

            int id = Shader.PropertyToID(RefractionSampler);
            buffer.GetTemporaryRT(id, -1, -1, 0, FilterMode.Bilinear);
            buffer.Blit(BuiltinRenderTextureType.CurrentActive, id);

            buffer.ReleaseTemporaryRT(id);

            cam.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, buffer);
        }
    }
}