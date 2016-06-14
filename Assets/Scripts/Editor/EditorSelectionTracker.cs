using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class EditorSelectionTracker {
	private static Object s_LastSelection;
	private static int s_LastSelectionCount;

	public delegate void SelectionChangedCallback(Object selection);
	public static SelectionChangedCallback SelectionChanged;

	static EditorSelectionTracker() {
		EditorApplication.update -= Update;
		EditorApplication.update += Update;
	}

	private static void Update() {
		if (s_LastSelection == Selection.activeObject && s_LastSelectionCount == Selection.gameObjects.Length) {
			return;
		}

		s_LastSelection = Selection.activeObject;
		s_LastSelectionCount = Selection.gameObjects.Length;

		if (SelectionChanged != null) {
			SelectionChanged(Selection.activeObject);
		}
	}
}