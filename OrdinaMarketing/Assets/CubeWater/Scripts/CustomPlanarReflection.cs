using System;
using System.Collections.Generic;
using UnityEngine;

namespace CubeWater
{
	[RequireComponent(typeof(Camera))]
	[ExecuteInEditMode]
	public class CustomPlanarReflection : MonoBehaviour
	{
		public LayerMask ReflectionMask;
		public float ClipPlaneOffset = 0.07F;
		public float SeaLevel = 0;
		public String ReflectionSampler = "_ReflectionTex";
		protected Camera m_reflectionCamera;


		Camera CreateReflectionCameraFrom(Camera cam)
		{
			String reflName = gameObject.name + "Reflection" + cam.name;
			GameObject go = GameObject.Find(reflName);

			if (!go)
			{
				go = new GameObject(reflName, typeof(Camera));
			}
			if (!go.GetComponent(typeof(Camera)))
			{
				go.AddComponent(typeof(Camera));
			}
			go.hideFlags = HideFlags.DontSave;
			Camera reflectCamera = go.GetComponent<Camera>();
			reflectCamera.CopyFrom(cam);

			reflectCamera.backgroundColor = Color.black;
			reflectCamera.clearFlags = CameraClearFlags.Skybox;
			reflectCamera.cullingMask = ReflectionMask & ~(1 << LayerMask.NameToLayer("Water"));
			reflectCamera.enabled = false;
			reflectCamera.depthTextureMode = DepthTextureMode.None;

			if (reflectCamera.targetTexture == null)
			{
				RenderTexture rt = new RenderTexture(cam.pixelWidth, cam.pixelHeight, 24);
				rt.hideFlags = HideFlags.DontSave;
				rt.wrapMode = TextureWrapMode.Repeat;
				reflectCamera.targetTexture = rt;
			}

			return reflectCamera;
		}

		void OnPreRender()
		{
			if (m_reflectionCamera == null)
			{
				m_reflectionCamera = CreateReflectionCameraFrom(Camera.main);
			}

			//if (Application.isPlaying)
			{
				RenderReflection(Camera.current, m_reflectionCamera);
			}

			if (m_reflectionCamera)
			{
				Shader.SetGlobalTexture(ReflectionSampler, m_reflectionCamera.targetTexture);
			}

		}

		private void OnDisable()
		{
			DeleteReflectionCamera();
		}

		void OnDestroy()
		{
			DeleteReflectionCamera();
		}

		private void DeleteReflectionCamera()
		{
			if (m_reflectionCamera != null)
			{
				if (m_reflectionCamera.targetTexture != null)
				{
					m_reflectionCamera.targetTexture.Release();
					DestroyImmediate(m_reflectionCamera.targetTexture);
					m_reflectionCamera.targetTexture = null;
				}

				GameObject.DestroyImmediate(m_reflectionCamera);
			}
		}

		void RenderReflection(Camera cam, Camera reflectCamera)
		{
			if (!reflectCamera)
			{
				return;
			}

			Vector3 pos = new Vector3(0, SeaLevel, 0);
			Vector3 normal = Vector3.up;// * (int)isUnderWater;
			float d = -Vector3.Dot(normal, pos) - ClipPlaneOffset;
			Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

			Matrix4x4 reflection = Matrix4x4.zero;
			reflection = CalculateReflectionMatrix(reflection, reflectionPlane);

			reflectCamera.worldToCameraMatrix = cam.worldToCameraMatrix * reflection;

			Vector4 clipPlane = CameraSpacePlane(reflectCamera, pos, normal, 1.0f);

			Matrix4x4 projection = cam.projectionMatrix;
			projection = CalculateObliqueMatrix(projection, clipPlane);

			reflectCamera.projectionMatrix = projection;

			GL.invertCulling = true;

			reflectCamera.Render();

			GL.invertCulling = false;
		}


		static Matrix4x4 CalculateObliqueMatrix(Matrix4x4 projection, Vector4 clipPlane)
		{
			Vector4 q = projection.inverse * new Vector4(
				Mathf.Sign(clipPlane.x),
				Mathf.Sign(clipPlane.y),
				1.0F,
				1.0F
				);
			Vector4 c = clipPlane * (2.0F / (Vector4.Dot(clipPlane, q)));
			// third row = clip plane - fourth row
			projection[2] = c.x - projection[3];
			projection[6] = c.y - projection[7];
			projection[10] = c.z - projection[11];
			projection[14] = c.w - projection[15];

			return projection;
		}


		static Matrix4x4 CalculateReflectionMatrix(Matrix4x4 reflectionMat, Vector4 plane)
		{
			reflectionMat.m00 = (1.0F - 2.0F * plane[0] * plane[0]);
			reflectionMat.m01 = (-2.0F * plane[0] * plane[1]);
			reflectionMat.m02 = (-2.0F * plane[0] * plane[2]);
			reflectionMat.m03 = (-2.0F * plane[3] * plane[0]);

			reflectionMat.m10 = (-2.0F * plane[1] * plane[0]);
			reflectionMat.m11 = (1.0F - 2.0F * plane[1] * plane[1]);
			reflectionMat.m12 = (-2.0F * plane[1] * plane[2]);
			reflectionMat.m13 = (-2.0F * plane[3] * plane[1]);

			reflectionMat.m20 = (-2.0F * plane[2] * plane[0]);
			reflectionMat.m21 = (-2.0F * plane[2] * plane[1]);
			reflectionMat.m22 = (1.0F - 2.0F * plane[2] * plane[2]);
			reflectionMat.m23 = (-2.0F * plane[3] * plane[2]);

			reflectionMat.m30 = 0.0F;
			reflectionMat.m31 = 0.0F;
			reflectionMat.m32 = 0.0F;
			reflectionMat.m33 = 1.0F;

			return reflectionMat;
		}


		Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
		{
			Vector3 offsetPos = pos + normal * ClipPlaneOffset;
			Matrix4x4 m = cam.worldToCameraMatrix;
			Vector3 cpos = m.MultiplyPoint(offsetPos);
			Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;

			return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
		}
	}


}