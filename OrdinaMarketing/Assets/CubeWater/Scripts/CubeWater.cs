using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class CubeWater : MonoBehaviour // Water
{
	public Color WaterColor = new(.5f, .8f, 1f);

	public Vector3 CubeWaterSize = new(10, 2, 10);
	public int SubdivisionX = 40;
	public int SubdivisionY = 10;
	public int SubdivisionZ = 40;

	public Vector3[] WaveSettings = { new Vector3(30, .25f, .2f), new Vector3(150, .25f, .11f), new Vector3(270, .25f, .06f) };

	public float Speed = .2f;

	public Material UpWater;
	public Material WallWater;

    void Start()
    {
		Application.targetFrameRate = 30;
		if (Application.isPlaying)
		{
			UpWater.color = WaterColor;
			WallWater.color = WaterColor;

			CreateCube(CubeWaterSize, SubdivisionX, SubdivisionY, SubdivisionZ);
		}
	}

    private void Update()
    {
		SetParams();
    }

    void CreateCube(Vector3 size, int subdivisionX, int subdivisionY, int subdivisionZ) 
	{
		var upMesh = CreateGridMesh(subdivisionX, subdivisionZ, new Vector3(size.x, -size.y, size.z), 0);
		GameObject upWater = new("UpWater");
		upWater.transform.position = transform.position;
		upWater.transform.parent = transform;
		var upFilter = upWater.AddComponent<MeshFilter>();

		var upRd = upWater.AddComponent<MeshRenderer>();
		upRd.sharedMaterial = UpWater;
		upFilter.mesh = upMesh;
		
		CombineInstance[] ci = new CombineInstance[4];

		var wallMesh1 = CreateGridMesh(subdivisionX, 1, size, 1);
		var wallMesh2 = CreateGridMesh(subdivisionX, 1, new Vector3(size.x, size.y, -size.z), 1, true);
		var wallMesh3 = CreateGridMesh(1, subdivisionZ, size, 2);
		var wallMesh4 = CreateGridMesh(1, subdivisionZ, new Vector3(-size.x, size.y, size.z), 2, true);

		ci[0].mesh = wallMesh1;
		ci[0].transform = Matrix4x4.identity;//Matrix4x4.TRS(new Vector3(0, -size.y, 0), Quaternion.identity, size);

		ci[1].mesh = wallMesh2;
		ci[1].transform = Matrix4x4.identity;//Matrix4x4.TRS(new Vector3(0, 0, size.z), Quaternion.identity, Vector3.one);

		ci[2].mesh = wallMesh3;
		ci[2].transform = Matrix4x4.identity;//Matrix4x4.TRS(new Vector3(0, -size.y, 0), Quaternion.identity, size);

		ci[3].mesh = wallMesh4;
		ci[3].transform = Matrix4x4.identity;//Matrix4x4.TRS(new Vector3(size.x, 0, 0), Quaternion.identity, Vector3.one);

		Mesh wallMesh = new();
		wallMesh.CombineMeshes(ci);

		GameObject cube = new("WallWater");
		var wallFilter = cube.AddComponent<MeshFilter>();
		
		var renderer = cube.AddComponent<MeshRenderer>();
		renderer.sharedMaterial = WallWater;
		wallFilter.mesh = wallMesh;

		upWater.layer = LayerMask.NameToLayer("Water");
		cube.layer = LayerMask.NameToLayer("Water");
		cube.transform.position = transform.position;
		cube.transform.parent = transform;
	}
	void SetParams()
	{
		Shader.SetGlobalFloat("_GameTime", Time.timeSinceLevelLoad);

		SetWaveData("_WaveA", WaveSettings[0]);
		SetWaveData("_WaveB", WaveSettings[1]);
		SetWaveData("_WaveC", WaveSettings[2]);
		UpWater.SetFloat("_SizeX", CubeWaterSize.x);
		UpWater.SetFloat("_SizeZ", CubeWaterSize.z);
		UpWater.SetFloat("_Speed", Speed);
		WallWater.SetInt("_Wall", 1);
		WallWater.SetFloat("_Speed", Speed);
	}

	void SetWaveData(string parameterName, Vector3 waveData)
    {
		float direction = waveData.x;
		float steepness = waveData.y;
		float wavelength = waveData.z;

		float dx = Mathf.Cos(Mathf.Deg2Rad * direction);
		float dy = Mathf.Sin(Mathf.Deg2Rad * direction);

		UpWater.SetVector(parameterName, new Vector4(dx, dy, steepness, wavelength));
		WallWater.SetVector(parameterName, new Vector4(dx, dy, steepness, wavelength));
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="subdivision"></param>
	/// <param name="type">0:xz 1:xy 2:yz</param>
	/// <returns></returns>
	public Mesh CreateGridMesh(int subdivision1, int subdivision2, Vector3 size, int type, bool reverse = false)
	{
		Mesh mesh = new();

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
		Vector3[] nm = new Vector3[totalVertices];

		//Vector2[] uv = new Vector2[totalVertices];
		int[] ids = new int[totalIndices];

		float direction = -1;
		if (reverse) direction = 1;

		for (int y = 0; y < vertDim2; ++y)
		{
			for (int x = 0; x < vertDim1; ++x)
			{
				int idx = x + vertDim1 * y;
				if (type == 0)//xz
				{
					vs[idx] = new Vector3(size.x * x / (float)subdivision1, 0, size.z * y / (float)(subdivision2));
					nm[idx] = new Vector3(0, -direction, 0);
				}
				else if (type == 1)//xy
				{
					vs[idx] = new Vector3(size.x * x / (float)subdivision1, size.y * y / (float)(subdivision2), 0);
					nm[idx] = new Vector3(0, 0, -direction);
				}
				else if (type == 2)//yz
				{
					vs[idx] = new Vector3(0, size.y * x / (float)subdivision1, size.z * y / (float)(subdivision2));
					nm[idx] = new Vector3(direction, 0, 0);
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
		mesh.normals = nm;
		//mesh.uv = uv;
		mesh.SetIndices(ids, MeshTopology.Triangles, 0);
		//mesh.UploadMeshData(true);

		return mesh;
	}
}
