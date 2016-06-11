// ---------------------------------------------------------------------
// Copyright (c) 2016 Magic Leap. All Rights Reserved.
// Magic Leap Confidential and Proprietary
// ---------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

public static class UnityExtensions {
	/// <summary>
	///   Destroys all the children of the game object.
	/// </summary>
	/// <param name="obj">The game object containng the children to destroy.</param>
	public static void DestroyAllChildren(this MonoBehaviour obj) {
		if (obj) obj.transform.DestroyAllChildren();
	}

	/// <summary>
	///   Destroys all the children of a transform.
	/// </summary>
	/// <param name="transform">The transform containing the children to destroy.</param>
	public static void DestroyAllChildren(this Transform transform) {
		if (!transform) return;
		for (int i = transform.childCount - 1; i >= 0; i--) {
			Transform t = transform.GetChild(i);
			if (!t) continue;
			GameObject go = t.gameObject;
			if (!go) continue;
			if (Application.isEditor) Object.DestroyImmediate(go);
			else Object.Destroy(go);
		}
	}

	/// <summary>
	///   Moves an object to a parent and optionally will keep the original local transforms.
	/// </summary>
	/// <param name="source">The transform to move.</param>
	/// <param name="parent">The transform to target.</param>
	/// <param name="keepLocalTransforms">
	///   If true, saves the local position and scale and re-assigns them. Good for objects
	///   saved at a certain size
	/// </param>
	public static void MoveTo(this Transform source, Transform parent, bool keepLocalTransforms = false) {
		if (keepLocalTransforms) {
			Vector3 position = source.localPosition;
			Vector3 scale = source.localScale;
			//var rotation = source.localRotation;
			source.parent = parent;
			source.localPosition = position;
			source.localScale = scale;
			//source.localRotation = rotation;
		}
		else { source.parent = parent; }
	}

	/// <summary>
	///   If found, returns an existing component of the given type. If not found, adds and returns a new component of the given type.
	/// </summary>
	/// <typeparam name="T">Type of component to get.</typeparam>
	/// <param name="component">The component of the game object to search.</param>
	/// <returns>The existing or new component of given type.</returns>
	public static T GetOrAddComponent<T>(this Component component) where T : Component { return component.gameObject.GetOrAddComponent<T>(); }

	/// <summary>
	///   Returns a component of given type. If not found, adds the component and then returns it.
	/// </summary>
	/// <typeparam name="T">Type of component to get.</typeparam>
	/// <param name="go">The game object to search.</param>
	/// <returns>The existing or new component of given type.</returns>
	public static T GetOrAddComponent<T>(this GameObject go) where T : Component {
		T result = go.GetComponent<T>();
		return result ? result : go.AddComponent<T>();
	}

	/// <summary>
	///   Returns a list of children for the given transform.
	/// </summary>
	/// <param name="t">The transform to search for children.</param>
	/// <returns>A list of children of the given transform.</returns>
	public static List<Transform> GetChildren(this Transform t) {
		List<Transform> children = new List<Transform>();
		for (int i = 0; i < t.childCount; i++) { children.Add(t.GetChild(i)); }
		return children;
	}
}