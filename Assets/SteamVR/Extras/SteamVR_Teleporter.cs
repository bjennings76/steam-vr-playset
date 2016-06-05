using SteamVR.Scripts;
using UnityEngine;

namespace SteamVR.Extras {
	public class SteamVR_Teleporter : MonoBehaviour {
		public enum TeleportType {
			TeleportTypeUseTerrain,
			TeleportTypeUseCollider,
			TeleportTypeUseZeroY
		}

		public bool teleportOnClick;
		public TeleportType teleportType = TeleportType.TeleportTypeUseZeroY;

		private Transform reference {
			get {
				var top = SteamVR_Render.Top();
				return top != null ? top.origin : null;
			}
		}

		private void Start() {
			var trackedController = GetComponent<SteamVR_TrackedController>();
			if (trackedController == null) { trackedController = gameObject.AddComponent<SteamVR_TrackedController>(); }

			trackedController.TriggerClicked += DoClick;

			if (teleportType == TeleportType.TeleportTypeUseTerrain) {
				// Start the player at the level of the terrain
				var t = reference;
				if (t != null) { t.position = new Vector3(t.position.x, Terrain.activeTerrain.SampleHeight(t.position), t.position.z); }
			}
		}

		private void DoClick(object sender, ClickedEventArgs e) {
			if (teleportOnClick) {
				var t = reference;
				if (t == null) { return; }

				var refY = t.position.y;

				var plane = new Plane(Vector3.up, -refY);
				var ray = new Ray(transform.position, transform.forward);

				var hasGroundTarget = false;
				var dist = 0f;
				if (teleportType == TeleportType.TeleportTypeUseTerrain) {
					RaycastHit hitInfo;
					var tc = Terrain.activeTerrain.GetComponent<TerrainCollider>();
					hasGroundTarget = tc.Raycast(ray, out hitInfo, 1000f);
					dist = hitInfo.distance;
				} else if (teleportType == TeleportType.TeleportTypeUseCollider) {
					RaycastHit hitInfo;
					Physics.Raycast(ray, out hitInfo);
					dist = hitInfo.distance;
				} else { hasGroundTarget = plane.Raycast(ray, out dist); }

				if (hasGroundTarget) {
					var headPosOnGround = new Vector3(SteamVR_Render.Top().head.localPosition.x, 0.0f, SteamVR_Render.Top().head.localPosition.z);
					t.position = ray.origin + ray.direction*dist - new Vector3(t.GetChild(0).localPosition.x, 0f, t.GetChild(0).localPosition.z) - headPosOnGround;
				}
			}
		}
	}
}