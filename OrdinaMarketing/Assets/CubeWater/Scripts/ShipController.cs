using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CubeWater
{
	public class ShipController : MonoBehaviour
	{
		public Transform[] JointAttachments = new Transform[3];
		[Range(0, 1)]
		public float LerpT = 0.1f;
        [SerializeField]
		private Transform HeightLine;

		// Update is called once per frame
		void Update()
		{
			if (JointAttachments != null)
			{
				Plane p;
				Vector3[] vs;
				vs = new Vector3[JointAttachments.Length];
				for (int i = 0; i < JointAttachments.Length; ++i)
				{
					if (JointAttachments[i] == null)
					{
						continue;
					}

					float h = Water.ApplyGerstnerWaveH(JointAttachments[i].position) + HeightLine.position.y;
					vs[i] = new Vector3(JointAttachments[i].position.x, h, JointAttachments[i].position.z);
				}

				p = new Plane();
				p.Set3Points(vs[0], vs[1], vs[2]);

				//
				transform.position = Vector3.Lerp(transform.position, p.ClosestPointOnPlane(transform.position), LerpT);

				var rotation = Quaternion.LookRotation(transform.forward, p.normal);
				transform.rotation = Quaternion.Lerp(transform.rotation, rotation, LerpT);
			}
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
		}
	}
}