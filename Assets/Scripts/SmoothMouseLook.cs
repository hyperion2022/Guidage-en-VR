using UnityEngine;
using System.Collections.Generic;

namespace UserOnboarding
{
	// Online sources for pieces of code:
	// https://answers.unity.com/questions/548794/how-to-move-a-camera-only-using-the-arrow-keys.html
	// http://wiki.unity3d.com/index.php/SmoothMouseLook?_gl=1*yrp5bs*_ga*MTM0MDA5NTg0My4xNjEzNjYzMzMw*_ga_1S78EFL1W5*MTYyNDYyOTQ3Ny4xNy4xLjE2MjQ2MzMxNzAuNTM.&_ga=2.42927589.1520312072.1624620879-1340095843.1613663330

	[AddComponentMenu("Camera-Control/Smooth Mouse Look")]
	public class SmoothMouseLook : MonoBehaviour
	{

		public enum RotationAxes { MouseXAndY = 0, MouseX = 1, MouseY = 2 }
		public RotationAxes axes = RotationAxes.MouseXAndY;
		public float sensitivityX = 15F;
		public float sensitivityY = 15F;

		public float minimumX = -360F;
		public float maximumX = 360F;

		public float minimumY = -60F;
		public float maximumY = 60F;

		float rotationX = 0F;
		float rotationY = 0F;

		private List<float> rotArrayX = new List<float>();
		float rotAverageX = 0F;

		private List<float> rotArrayY = new List<float>();
		float rotAverageY = 0F;

		public float frameCounter = 20;

		Quaternion originalRotation;

		[SerializeField] float speed = 5f;

		void Update()
		{
			// Rotation only when Right Mouse Button is held
			if (Input.GetMouseButton(1))
			{
				// Rotation using mouse axes
				if (axes == RotationAxes.MouseXAndY)
				{
					rotAverageY = 0f;
					rotAverageX = 0f;

					rotationY += Input.GetAxis("Mouse Y") * sensitivityY;
					rotationX += Input.GetAxis("Mouse X") * sensitivityX;

					rotArrayY.Add(rotationY);
					rotArrayX.Add(rotationX);

					if (rotArrayY.Count >= frameCounter)
					{
						rotArrayY.RemoveAt(0);
					}
					if (rotArrayX.Count >= frameCounter)
					{
						rotArrayX.RemoveAt(0);
					}

					for (int j = 0; j < rotArrayY.Count; j++)
					{
						rotAverageY += rotArrayY[j];
					}
					for (int i = 0; i < rotArrayX.Count; i++)
					{
						rotAverageX += rotArrayX[i];
					}

					rotAverageY /= rotArrayY.Count;
					rotAverageX /= rotArrayX.Count;

					rotAverageY = ClampAngle(rotAverageY, minimumY, maximumY);
					rotAverageX = ClampAngle(rotAverageX, minimumX, maximumX);

					Quaternion yQuaternion = Quaternion.AngleAxis(rotAverageY, Vector3.left);
					Quaternion xQuaternion = Quaternion.AngleAxis(rotAverageX, Vector3.up);

					transform.localRotation = originalRotation * xQuaternion * yQuaternion;
				}
				else if (axes == RotationAxes.MouseX)
				{
					rotAverageX = 0f;

					rotationX += Input.GetAxis("Mouse X") * sensitivityX;

					rotArrayX.Add(rotationX);

					if (rotArrayX.Count >= frameCounter)
					{
						rotArrayX.RemoveAt(0);
					}
					for (int i = 0; i < rotArrayX.Count; i++)
					{
						rotAverageX += rotArrayX[i];
					}
					rotAverageX /= rotArrayX.Count;

					rotAverageX = ClampAngle(rotAverageX, minimumX, maximumX);

					Quaternion xQuaternion = Quaternion.AngleAxis(rotAverageX, Vector3.up);
					transform.localRotation = originalRotation * xQuaternion;
				}
				else
				{
					rotAverageY = 0f;

					rotationY += Input.GetAxis("Mouse Y") * sensitivityY;

					rotArrayY.Add(rotationY);

					if (rotArrayY.Count >= frameCounter)
					{
						rotArrayY.RemoveAt(0);
					}
					for (int j = 0; j < rotArrayY.Count; j++)
					{
						rotAverageY += rotArrayY[j];
					}
					rotAverageY /= rotArrayY.Count;

					rotAverageY = ClampAngle(rotAverageY, minimumY, maximumY);

					Quaternion yQuaternion = Quaternion.AngleAxis(rotAverageY, Vector3.left);
					transform.localRotation = originalRotation * yQuaternion;
				}
			}

			// Movement using ZQSD keys
			if (Input.GetKey(KeyCode.Z))
			{
				transform.Translate(new Vector3(0, 0, speed * Time.deltaTime));
			}
			else if (Input.GetKey(KeyCode.Q))
			{
				transform.Translate(new Vector3(-speed * Time.deltaTime, 0, 0));
			}
			else if (Input.GetKey(KeyCode.S))
			{
				transform.Translate(new Vector3(0, 0, -speed * Time.deltaTime));
			}
			else if (Input.GetKey(KeyCode.D))
			{
				transform.Translate(new Vector3(speed * Time.deltaTime, 0, 0));
			}

		}

		void Start()
		{
			Rigidbody rb = GetComponent<Rigidbody>();
			if (rb)
				rb.freezeRotation = true;
			originalRotation = transform.localRotation;
			Cursor.visible = false;
		}

		public static float ClampAngle(float angle, float min, float max)
		{
			angle = angle % 360;
			if ((angle >= -360F) && (angle <= 360F))
			{
				if (angle < -360F)
				{
					angle += 360F;
				}
				if (angle > 360F)
				{
					angle -= 360F;
				}
			}
			return Mathf.Clamp(angle, min, max);
		}
	}
}
