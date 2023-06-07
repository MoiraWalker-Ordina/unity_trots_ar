using UnityEngine;

public class ShipController : MonoBehaviour
{
	public Transform[] JointAttachments = new Transform[3];
	public CubeWater water;
	public float LerpT = 1;

	[SerializeField]
	private Transform Start;


    void Update()
    {
		UpdateJoints();
    }

	void UpdateJoints()
    {
		Vector3[] vs = new Vector3[3];

		for (int i = 0; i < 3; ++i)
		{
			Vector3 pos = JointAttachments[i].position - water.transform.position;
			float h = ApplyGerstnerWaveH(pos, water.WaveSettings) + Start.position.y;

			vs[i] = new Vector3(JointAttachments[i].position.x, h, JointAttachments[i].position.z);
		}

   //     for (int y = 0; y < 10; y++)
   //     {
   //         for (int x = 0; x < 10; x++)
   //         {
			//	Vector3 pos = new((float)(x - 4.5f) * (.5f / 9f), 0, (float)(y - 4.5f) * (.5f / 9f));
			//	float h = ApplyGerstnerWaveH(pos, water.WaveSettings) + .05f;
			//	testPos[x, y] = new(pos.x, h, pos.z);
			//}
   //     }

        Plane p = new();
        p.Set3Points(vs[0], vs[1], vs[2]);

        transform.position = Vector3.Lerp(transform.position, p.ClosestPointOnPlane(transform.position), LerpT);
		var rotation = Quaternion.FromToRotation(transform.up, p.normal);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, LerpT);
    }

	private float ApplyGerstnerWaveH(Vector3 pos, Vector3[] waveData)
	{
		Vector2 p = new(pos.x, pos.z);

		float[] k = new float[3];
		float[] c = new float[3];
		float[] a = new float[3];
		float[] t = new float[3];
		Vector2[] d = new Vector2[3];

		for (int i = 0; i < 3; i++)
		{
			float steepness = waveData[i].y;
			float wavelength = waveData[i].z;
			k[i] = Mathf.PI * 2 / wavelength;
			c[i] = Mathf.Sqrt(9.8f / k[i]);
			a[i] = steepness / k[i];

			float angle = Mathf.Deg2Rad * waveData[i].x;
			d[i] = new(Mathf.Cos(angle), Mathf.Sin(angle));

			t[i] = c[i] * Time.timeSinceLevelLoad * water.Speed;
		}

		Vector2 offset = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
			float f = k[i] * (Vector2.Dot(d[i], p) - t[i]);
			offset -= (a[i] * Mathf.Cos(f) * d[i]);
		}

		p += offset;

		float y = 0;
		for (int i = 0; i < 3; i++)
		{
			float f = k[i] * (Vector2.Dot(d[i], p) - t[i]);
			y += a[i] * Mathf.Sin(f);
		}

		return y;
	}

	private void OnDrawGizmos()
	{
        for (int i = 0; i < JointAttachments.Length; ++i)
        {
            if (JointAttachments[i] != null)
            {
                Gizmos.DrawWireSphere(JointAttachments[i].position, 0.1f);
            }
        }

        //for (int y = 0; y < 10; y++)
        //{
        //    for (int x = 0; x < 10; x++)
        //    {
        //        Vector3 pos = testPos[x, y];
        //        DrawAxes(pos);
        //    }
        //}

        //     for (int i = 1; i < 9; i++)
        //     {
        //DrawAxes(testPos[i, 1]);
        //DrawAxes(testPos[i, 8]);
        //DrawAxes(testPos[8, i]);
        //DrawAxes(testPos[1, i]);
        //     }

        //      DrawAxes(testPos[0, 0]);
        //DrawAxes(testPos[9, 0]);
        //DrawAxes(testPos[0, 9]);
        //DrawAxes(testPos[9, 9]);
    }

	private void DrawAxes(Vector3 pos)
    {
		Gizmos.DrawLine(pos, pos + Vector3.right * .02f);
		Gizmos.DrawLine(pos, pos + Vector3.forward * .02f);
		Gizmos.DrawLine(pos, pos + Vector3.up * .02f);
	}
}
