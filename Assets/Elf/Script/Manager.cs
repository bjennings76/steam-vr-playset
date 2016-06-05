using UnityEngine;

namespace Elf.Script {
	public class Manager : MonoBehaviour {
		public GameObject CameraObj;
		public Camera cam;

		public bool left;
		public bool right;
		public bool cameraIn;
		public bool cameraOut;
		// Use this for initialization
		void Start() { }

		// Update is called once per frame
		void Update() {
			if (Input.GetMouseButtonUp(0)) {
				left = false;
				right = false;
				cameraIn = false;
				cameraOut = false;
			}
			if (left) { CameraObj.transform.Rotate(Vector3.down, Time.deltaTime*5); }
			if (right) { CameraObj.transform.Rotate(Vector3.up, Time.deltaTime*5); }
			if (cameraIn) { if (cam.fieldOfView >= 20) { cam.fieldOfView--; } }
			if (cameraOut) { if (cam.fieldOfView <= 60) { cam.fieldOfView++; } }
		}

		void OnGUI() {
			//	GUI.skin.button.fontSize = 10;

			if (GUI.RepeatButton(new Rect(Screen.width/2 - 170, 450, 100, 50), "Zoom In")) { cameraIn = true; }
			if (GUI.Button(new Rect(Screen.width/2 - 50, 450, 100, 50), "Zoom Reset")) { cam.fieldOfView = 40; }
			if (GUI.RepeatButton(new Rect(Screen.width/2 + 70, 450, 100, 50), "Zoom Out")) { cameraOut = true; }

			if (GUI.RepeatButton(new Rect(Screen.width/2 - 170, 520, 100, 50), "Camera Left")) { left = true; }
			if (GUI.Button(new Rect(Screen.width/2 - 50, 520, 100, 50), "Camera Reset")) { CameraObj.transform.eulerAngles = new Vector3(0, 0, 0); }
			if (GUI.RepeatButton(new Rect(Screen.width/2 + 70, 520, 100, 50), "Camera Right")) { right = true; }
		}
	}
}