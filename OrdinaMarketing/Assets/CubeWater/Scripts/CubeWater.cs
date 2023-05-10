using UnityEngine;
using System.Collections;

namespace CubeWater
{
	[ExecuteInEditMode]
	public class CubeWater : Water
	{

		public Vector3 CubeWaterSize = new Vector3(10, 2, 10);
		public int SubdivisionX = 40;
		public int SubdivisionY = 10;
		public int SubdivisionZ = 40;
		public Material UpWater;
		public Material DownWater;
		public Material WallWater;

		protected override void Start()
		{
			Application.targetFrameRate = 30;
			if (Application.isPlaying)
			{
				CreateCube(CubeWaterSize, SubdivisionX, SubdivisionY, SubdivisionZ);
			}
		}
		void CreateCube(Vector3 size, int subdivisionX, int subdivisionY, int subdivisionZ)
		{
			//
			var upMesh = CreateGridMesh(subdivisionX, subdivisionZ, new Vector3(size.x, -size.y, size.z), 0);
			GameObject upWater = new GameObject("UpWater");
			upWater.transform.SetParent(this.transform, false);
			var upFilter = upWater.AddComponent<MeshFilter>();
			var upRd = upWater.AddComponent<MeshRenderer>();
			upRd.sharedMaterial = UpWater;
			upFilter.mesh = upMesh;


			//
			var downMesh = CreateGridMesh(subdivisionX, subdivisionZ, new Vector3(size.x, -size.y, size.z), 0);
			GameObject downWater = new GameObject("DownWater");
			downWater.transform.SetParent(this.transform, false);
			var downFilter = downWater.AddComponent<MeshFilter>();

			var downRd = downWater.AddComponent<MeshRenderer>();
			downRd.sharedMaterial = DownWater;
			downFilter.mesh = downMesh;

			//
			CombineInstance[] ci = new CombineInstance[5];

			var wallMesh1 = CreateGridMesh(subdivisionX, subdivisionY, size, 1);
			var wallMesh2 = CreateGridMesh(subdivisionX, subdivisionY, new Vector3(size.x, size.y, -size.z), 1, true);
			var wallMesh3 = CreateGridMesh(subdivisionY, subdivisionZ, size, 2);
			var wallMesh4 = CreateGridMesh(subdivisionY, subdivisionZ, new Vector3(-size.x, size.y, size.z), 2, true);
			var wallMesh5 = CreateGridMesh(subdivisionX, subdivisionZ, size, 0, true);

			ci[0].mesh = wallMesh1;
			ci[0].transform = Matrix4x4.identity;//Matrix4x4.TRS(new Vector3(0, -size.y, 0), Quaternion.identity, size);

			ci[1].mesh = wallMesh2;
			ci[1].transform = Matrix4x4.identity;//Matrix4x4.TRS(new Vector3(0, 0, size.z), Quaternion.identity, Vector3.one);

			ci[2].mesh = wallMesh3;
			ci[2].transform = Matrix4x4.identity;//Matrix4x4.TRS(new Vector3(0, -size.y, 0), Quaternion.identity, size);

			ci[3].mesh = wallMesh4;
			ci[3].transform = Matrix4x4.identity;//Matrix4x4.TRS(new Vector3(size.x, 0, 0), Quaternion.identity, Vector3.one);

			ci[4].mesh = wallMesh5;
			ci[4].transform = Matrix4x4.identity;//Matrix4x4.TRS(new Vector3(0, -size.y, 0), Quaternion.identity, size);

			Mesh wallMesh = new Mesh();
			wallMesh.CombineMeshes(ci);

			GameObject cube = new GameObject("WallWater");
			var wallFilter = cube.AddComponent<MeshFilter>();
			wallFilter.transform.SetParent(this.transform, false);
			var renderer = cube.AddComponent<MeshRenderer>();
			renderer.sharedMaterial = WallWater;
			wallFilter.mesh = wallMesh;
		}
		protected override void SetParams()
		{
			W = new Vector4(
				2 * Mathf.PI / (m_waveLen.x * WaveLength),
				2 * Mathf.PI / (m_waveLen.y * WaveLength),
				2 * Mathf.PI / (m_waveLen.z * WaveLength),
				2 * Mathf.PI / (m_waveLen.w * WaveLength));

			//rotation matrix
			float rad = Mathf.Deg2Rad * Direction;
			float m00 = Mathf.Cos(rad);
			float m01 = -Mathf.Sin(rad);
			float m10 = -m01;
			float m11 = m00;

			for (int i = 0; i < m_dirArray.Length; ++i)
			{
				m_dirArray[i].Normalize();
				m_dirTemp[i].x = m00 * m_dirArray[i].x + m10 * m_dirArray[i].x;
				m_dirTemp[i].y = m01 * m_dirArray[i].y + m11 * m_dirArray[i].y;
			}

			Dx = new Vector4(m_dirTemp[0].x, m_dirTemp[1].x, m_dirTemp[2].x, m_dirTemp[3].x);
			Dz = new Vector4(m_dirTemp[0].y, m_dirTemp[1].y, m_dirTemp[2].y, m_dirTemp[3].y);
			S = m_speed * Speed;
			A = m_amp * Amplitude;

			Q = new Vector4(Steepness / (W.x * A.x * 4), Steepness / (W.y * A.y * 4), Steepness / (W.z * A.z * 4), Steepness / (W.w * A.w * 4));

			Shader.SetGlobalVector("_QA", Mul(Q, A));
			Shader.SetGlobalVector("_A", A);
			Shader.SetGlobalVector("_Dx", Dx);
			Shader.SetGlobalVector("_Dz", Dz);
			Shader.SetGlobalVector("_S", S);
			Shader.SetGlobalVector("_L", W);
			Shader.SetGlobalVector("_BoxSize", CubeWaterSize * 0.5f);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="subdivision"></param>
		/// <param name="type">0:xz 1:xy 2:yz</param>
		/// <returns></returns>
		public Mesh CreateGridMesh(int subdivision1, int subdivision2, Vector3 size, int type, bool reverse = false)
		{
			Mesh mesh = new Mesh();

			if (subdivision1 <= 0 || subdivision2 <= 0)
			{
				return mesh;
			}
			int vertDim1 = subdivision1 + 1;
			int vertDim2 = subdivision2 + 1;
			int totalVertices = vertDim1 * vertDim2;

			Debug.Assert(totalVertices <= 65535);

			int totalIndices = subdivision1 * subdivision2 * 2 * 3;

			Vector3[] vs = new Vector3[totalVertices];
			//Vector2[] uv = new Vector2[totalVertices];
			int[] ids = new int[totalIndices];

			for (int y = 0; y < vertDim2; ++y)
			{
				for (int x = 0; x < vertDim1; ++x)
				{
					int idx = x + vertDim1 * y;
					if (type == 0)//xz
					{
						vs[idx] = new Vector3(size.x * x / (float)subdivision1, 0, size.z * y / (float)(subdivision2));
					}
					else if (type == 1)//xy
					{
						vs[idx] = new Vector3(size.x * x / (float)subdivision1, size.y * y / (float)(subdivision2), 0);

					}
					else if (type == 2)//yz
					{
						vs[idx] = new Vector3(0, size.y * x / (float)subdivision1, size.z * y / (float)(subdivision2));
					}
					vs[idx] += -new Vector3(size.x, size.y, size.z) * 0.5f;
				}
			}

			int index = 0;

			for (int y = 0; y < subdivision2; y++)
			{
				for (int x = 0; x < subdivision1; x++)
				{
					if (!reverse)
					{
						ids[index++] = x + vertDim1 * y;
						ids[index++] = x + vertDim1 * (y + 1);
						ids[index++] = (x + 1) + vertDim1 * y;

						ids[index++] = (x + 1) + vertDim1 * y;
						ids[index++] = x + vertDim1 * (y + 1);
						ids[index++] = (x + 1) + vertDim1 * (y + 1);
					}
					else
					{
						ids[index++] = x + vertDim1 * y;
						ids[index++] = (x + 1) + vertDim1 * y;
						ids[index++] = x + vertDim1 * (y + 1);

						ids[index++] = (x + 1) + vertDim1 * y;
						ids[index++] = (x + 1) + vertDim1 * (y + 1);
						ids[index++] = x + vertDim1 * (y + 1);
					}
				}
			}

			mesh.vertices = vs;
			//mesh.uv = uv;
			mesh.SetIndices(ids, MeshTopology.Triangles, 0);
			//mesh.UploadMeshData(true);

			return mesh;
		}
	}
}