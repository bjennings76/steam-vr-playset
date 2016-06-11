// ---------------------------------------------------------------------
// Copyright (c) 2016 Magic Leap. All Rights Reserved.
// Magic Leap Confidential and Proprietary
// ---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Invaders.Util.Editor {
	public static partial class EditorUtils {
		[MenuItem("GameObject/Sort Siblings", true)]
		public static bool CanSortSiblings() {
			return Selection.transforms.Length > 0;
		}

		[MenuItem("GameObject/Sort Siblings")]
		public static void SortSiblings() {
			if (Selection.transforms.Length == 0) { return; }
			List<Transform> siblings =
				Selection.transforms.SelectMany<Transform, Transform>(GetSiblings).Distinct().OrderBy(t => t.name).ToList();
			for (int i = 0; i < siblings.Count; i++) { siblings[i].SetSiblingIndex(i); }
		}

		private static IEnumerable<Transform> GetSiblings(Transform transform) {
			return !transform.parent ? GetRootSceneObjects().Select(go => go.transform) : transform.parent.GetChildren();
		}

		[MenuItem("GameObject/Group Selected %g", true)]
		public static bool CanGroupSelected() {
			return Selection.gameObjects.Length > 0;
		}

		[MenuItem("GameObject/Group Selected %g")]
		public static void GroupSelected() {
			if (!Selection.activeTransform) { return; }
			GameObject go = new GameObject(Selection.activeTransform.name + " Group");
			Undo.RegisterCreatedObjectUndo(go, "Group Selected");
			go.transform.SetParent(Selection.activeTransform.parent, false);
			go.transform.SetSiblingIndex(Selection.activeTransform.GetSiblingIndex());
			go.transform.position = UnityUtils.GetCenter(Selection.transforms);
			foreach (Transform transform in Selection.transforms) {
				Undo.SetTransformParent(transform, go.transform, "Group Selected");
			}
			Selection.activeGameObject = go;
		}

		[MenuItem("GameObject/Ungroup Selected %#g", true)]
		public static bool CanUngroupSelected() {
			return Selection.transforms.Any();
		}

		[MenuItem("GameObject/Ungroup Selected %#g")]
		public static void UngroupSelected() {
			if (!Selection.transforms.Any()) { return; }

			List<Object> deletables = new List<Object>();
			List<Object> selectables = new List<Object>();

			Selection.gameObjects.ForEach(go => {
				if (!go) { return; }
				if (go.transform.childCount == 0) {
					selectables.Add(go);
					return;
				}
				Transform t = go.transform;
				int index = t.GetSiblingIndex();
				Transform parent = t.parent;
				t.GetChildren().ForEach(c => {
					if (!c) { return; }
					Undo.SetTransformParent(c, parent, "Ungroup Selected");
					c.SetSiblingIndex(index);
					index++;
					selectables.Add(c.gameObject);
				});
				deletables.Add(go);
			});

			deletables.Distinct().ForEach(Undo.DestroyObjectImmediate);
			Selection.objects = selectables.ToArray();
		}

		public delegate void ApplyHandler(GameObject instance);

		public static ApplyHandler OnApply;
		public static ApplyHandler OnApplied;

		[MenuItem("GameObject/Apply Selected %#a", true)]
		public static bool CanApplySelected() {
			return Selection.gameObjects.Any(IsInstance);
		}

		[MenuItem("GameObject/Zero Selected %#z", true)]
		public static bool CanZeroSelected() {
			return Selection.gameObjects.Any();
		}

		[MenuItem("GameObject/Zero Selected %#z")]
		public static void ZeroSelected() {
			Selection.transforms.ForEach(EditorExtensions.Zero);
		}

		[MenuItem("GameObject/Zero Selected To Children Center", true)]
		public static bool CanZeroSelectedToCenter() {
			return Selection.gameObjects.Any();
		}

		[MenuItem("GameObject/Zero Selected To Children Center")]
		public static void ZeroSelectedToCenter() {
			Selection.transforms.ForEach(EditorExtensions.ZeroToCenter);
		}

		[MenuItem("GameObject/Nest Selected %#a", true)]
		public static bool CanNestSelected() {
			return Selection.gameObjects.Any(IsInstance);
		}

		[MenuItem("GameObject/Revert Selected", true)]
		public static bool CanRevertSelected() {
			return Selection.gameObjects.Any(IsInstance);
		}

		[MenuItem("GameObject/Revert Selected")]
		public static void RevertSelected() {
			foreach (GameObject instance in Selection.gameObjects) {
				Undo.RegisterCompleteObjectUndo(instance, "Revert Selected");
				Vector3 position = instance.transform.localPosition;
				Quaternion rotation = instance.transform.localRotation;
				Vector3 scale = instance.transform.localScale;
				PrefabUtility.RevertPrefabInstance(instance);
				instance.transform.localPosition = position;
				instance.transform.localRotation = rotation;
				instance.transform.localScale = scale;
			}
			AssetDatabase.SaveAssets();
		}

		public static GameObject GetPrefab(Object instance) {
			GameObject go = UnityUtils.GetGameObject(instance);
			if (!go) { return null; }
			GameObject root = PrefabUtility.FindRootGameObjectWithSameParentPrefab(go);
			if (!root) { return null; }
			return PrefabUtility.GetPrefabParent(root) as GameObject;
		}

		public static Transform FindTransformByPath(string path) {
			string[] pieces = path.Split(new[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
			Transform result = null;
			List<Transform> children = GetRootTransforms().ToList();

			foreach (string piece in pieces) {
				int i = 0;
				bool found = false;
				foreach (Transform child in children) {
					if (child.name == piece || child.name + "[" + i + "]" == piece) {
						children = child.transform.GetChildren();
						result = child;
						found = true;
						break;
					}
					i++;
				}

				if (found) { continue; }

				Debug.Log("Couldn't find " + path + "." +
									(result
										? " Could only find " +
											result.FullName(Extensions.FullNameFlags.FullScenePath | Extensions.FullNameFlags.SiblingIndex)
										: ""));
				return null;
			}

			return result;
		}

		public static GameObject FindGameObjectByPath(string path) {
			Transform transform = FindTransformByPath(path);
			return transform ? transform.gameObject : null;
		}

		public static bool IsInstance(GameObject go) {
			GameObject prefab = GetPrefab(go);
			return prefab && prefab != go;
		}

		public static IEnumerable<GameObject> GetRootPrefabInstances(IEnumerable<GameObject> instances) {
			return
				instances.Select<GameObject, GameObject>(PrefabUtility.FindRootGameObjectWithSameParentPrefab)
					.Where(p => p)
					.Distinct();
		}

		public static bool IsLocked(GameObject go) { return (go.hideFlags & HideFlags.NotEditable) == HideFlags.NotEditable; }

		public static void Lock(GameObject go, bool recursive = true, bool disableSelection = true, bool allowUndo = true) {
			if (allowUndo) { Undo.RecordObject(go, "Lock Object"); }

			go.hideFlags |= HideFlags.NotEditable;

			if (disableSelection) {
				foreach (Component comp in go.GetComponents<Component>().Where(comp => !(comp is Transform))) {
					comp.hideFlags |= HideFlags.NotEditable;
					comp.hideFlags |= HideFlags.HideInHierarchy;
				}
			}

			EditorUtility.SetDirty(go);

			if (recursive) { for (int i = 0; i < go.transform.childCount; i++) { Lock(go.transform.GetChild(i).gameObject); } }
		}

		public static void Unlock(GameObject go, bool recursive = true, bool allowUndo = true) {
			if (allowUndo) { Undo.RecordObject(go, "Unlock Object"); }
			go.hideFlags &= ~HideFlags.NotEditable;
			foreach (Component comp in go.GetComponents<Component>().Where(c => !(c is Transform))) {
				// Don't check pref key; no harm in removing flags that aren't there
				comp.hideFlags &= ~HideFlags.NotEditable;
				comp.hideFlags &= ~HideFlags.HideInHierarchy;
			}

			EditorUtility.SetDirty(go);

			if (recursive) { for (int i = 0; i < go.transform.childCount; i++) { Unlock(go.transform.GetChild(i).gameObject); } }
		}

		public static IEnumerable<GameObject> GetRootSceneObjects() {
			// TODO: Replace with 'SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()' in Unity 5.3

			HierarchyProperty prop = new HierarchyProperty(HierarchyType.GameObjects);
			int[] expanded = new int[0];
			while (prop.Next(expanded)) { yield return prop.pptrValue as GameObject; }
		}

		public static IEnumerable<Transform> GetRootTransforms() { return GetRootSceneObjects().Select(go => go.transform); }
	}
}