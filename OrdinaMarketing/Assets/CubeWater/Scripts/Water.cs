using UnityEngine;
using System.Collections;

namespace CubeWater
{
	[ExecuteInEditMode]
	public class Water : MonoBehaviour
	{
		//numWaves = 4
		[Range(0, 1)]
		public float Steepness = 1;//value range:[0, 1]
		public float Amplitude = 1;
		public float WaveLength = 1;
		public float Speed = 1;
		public float Direction = 0;

		protected static Vector4 Dx = Vector4.zero;
		protected static Vector4 Dz = Vector4.zero;
		protected static Vector4 W = Vector4.zero;//W=2*PI/waveLength
		protected static Vector4 Q = Vector4.zero;//steepness
		protected static Vector4 S = Vector4.zero;//move speed m/s
		protected static Vector4 A = Vector4.zero;//amplitude

		#region one type of wave param
		protected Vector2[] m_dirArray = new Vector2[] {
		new Vector2(0.615f, 1),
		new Vector2(0.788f, 0.988f),
		new Vector2(0.478f,0.937f),
		new Vector2(0.154f, 0.71f)
	};
		protected Vector4 m_amp = new Vector4(0.023f, 0.013f, 0.017f, 0.019f);
		protected Vector4 m_waveLen = new Vector4(1, 0.76f, 0.89f, 0.93f);
		protected Vector4 m_speed = new Vector4(0.1f, 0.5f, 0.7f, 0.3f);
		protected Vector2[] m_dirTemp = new Vector2[4];
		#endregion

		protected virtual void Start()
		{
			Application.targetFrameRate = 30;
		}

		protected virtual void SetParams()
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
		}

		public static Vector4 Mul(Vector4 a, Vector4 b)
		{
			return new Vector4(a.x * b.x, a.y * b.y, a.z * b.z, a.w * b.w);
		}
		public static Vector4 Sin(Vector4 x)
		{
			return new Vector4(Mathf.Sin(x.x), Mathf.Sin(x.y), Mathf.Sin(x.z), Mathf.Sin(x.w));
		}
		public static Vector4 Cos(Vector4 x)
		{
			return new Vector4(Mathf.Cos(x.x), Mathf.Cos(x.y), Mathf.Cos(x.z), Mathf.Cos(x.w));
		}

		public static Vector3 CalculateWaveDisplacementNormal(Vector3 worldPos, out Vector3 normal)
		{
			Vector3 pos = Vector3.zero;
			Vector4 phase = Dx * worldPos.x + Dz * worldPos.z + S * Time.time;
			Vector4 sinp = Vector4.zero, cosp = Vector4.zero;

			sinp = Sin(Mul(W, phase));
			cosp = Cos(Mul(W, phase));

			pos.x = Vector4.Dot(Mul(Q, Mul(A, Dx)), cosp);
			pos.z = Vector4.Dot(Mul(Q, Mul(A, Dz)), cosp);
			pos.y = Vector4.Dot(A, sinp);

			normal.x = -Vector4.Dot(Mul(W, A), Mul(Dx, cosp));
			normal.z = -Vector4.Dot(Mul(W, A), Mul(Dz, cosp));
			normal.y = 1 - Vector4.Dot(Mul(Q, Mul(A, W)), sinp);

			normal.Normalize();

			return pos;
		}
		public static Vector3 CalculateShipMovement(Vector3 worldPos)
		{
			Vector3 move = Vector3.zero;
			Vector4 phase = Dx * worldPos.x + Dz * worldPos.z + S * Time.time;
			Vector4 sinp = Vector4.zero, cosp = Vector4.zero;

			sinp = Sin(Mul(W, phase));
			cosp = Cos(Mul(W, phase));

			//displacement
			move.y = Vector4.Dot(A, sinp);
			//normal 
			move.x = -Vector4.Dot(Mul(W, A), Mul(Dx, cosp));
			move.z = -Vector4.Dot(Mul(W, A), Mul(Dz, cosp));

			return move;
		}

		public static float ApplyGerstnerWaveH(Vector3 p)
		{
			Vector4 w = W;

			Vector4 q = Q;

			Vector4 t = new Vector4(
					S.x * w.x,
					S.y * w.y,
					S.z * w.z,
					S.w * w.w
				) * Time.time;

			Vector4 phase = new Vector4(
					w.x * Dx[0] * p.x + w.x * Dz[0] * p.z + t.x,
					w.y * Dx[1] * p.x + w.y * Dz[1] * p.z + t.y,
					w.z * Dx[2] * p.x + w.z * Dz[2] * p.z + t.z,
					w.w * Dx[3] * p.x + w.w * Dz[3] * p.z + t.w
				);

			Vector4 x = new Vector4(
					q.x * A.x * Dx[0] * Mathf.Cos(phase.x),
					q.y * A.y * Dx[1] * Mathf.Cos(phase.y),
					q.z * A.z * Dx[2] * Mathf.Cos(phase.z),
					q.w * A.w * Dx[3] * Mathf.Cos(phase.w)
				);

			Vector4 z = new Vector4(
					q.x * A.x * Dz[0] * Mathf.Cos(phase.x),
					q.y * A.y * Dz[1] * Mathf.Cos(phase.y),
					q.z * A.z * Dz[2] * Mathf.Cos(phase.z),
					q.w * A.w * Dz[3] * Mathf.Cos(phase.w)
				);

			p.x -= x.x + x.y + x.z + x.w;
			p.z -= z.x + z.y + z.z + z.w;

			phase = new Vector4(
					w.x * Dx[0] * p.x + w.x * Dz[0] * p.z + t.x,
					w.y * Dx[1] * p.x + w.y * Dz[1] * p.z + t.y,
					w.z * Dx[2] * p.x + w.z * Dz[2] * p.z + t.z,
					w.w * Dx[3] * p.x + w.w * Dz[3] * p.z + t.w
				);
			Vector4 y = new Vector4(
					A.x * Mathf.Sin(phase.x),
					A.y * Mathf.Sin(phase.y),
					A.z * Mathf.Sin(phase.z),
					A.w * Mathf.Sin(phase.w)
				);
			//

			return y.x + y.y + y.z + y.w;
		}


		// Update is called once per frame
		protected virtual void Update()
		{
#if UNITY_EDITOR
			SetParams();

#endif
			Shader.SetGlobalFloat("_T", Time.time);
		}
	}
}