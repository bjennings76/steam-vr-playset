﻿// ---------------------------------------------------------------------
// Copyright (c) 2016 Magic Leap. All Rights Reserved.
// Magic Leap Confidential and Proprietary
// ---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using DG.DemiEditor;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Invaders.Editor {
	public class AssetUsageWindow : EditorWindow {
		private static AssetUsageWindow Instance {
			get { return s_Instance ? s_Instance : (s_Instance = GetWindow<AssetUsageWindow>("Asset Usage")); }
		}
		private static AssetUsageWindow s_Instance;

		private static readonly Dictionary<string, UsageInfo> s_ReferenceInfos = new Dictionary<string, UsageInfo>();
		private static bool s_Scanned;
		private static bool s_ShowIndirectReferences;
		private static bool s_LockSelection;
		private static bool s_LastShowIndirectReferences;

		private const string kShowIndirectReferencesPrefKey = "AssetUsageWindow_HideLowerLevelReferences";

		private static readonly List<Object> s_GoBackStack = new List<Object>();
		private static readonly List<Object> s_GoForwardStack = new List<Object>();

		private static GUIStyle ButtonStyle {
			get {
				if (s_ButtonStyle != null) { return s_ButtonStyle; }
				s_ButtonStyle = GUI.skin.button.Clone();
				s_ButtonStyle.alignment = TextAnchor.MiddleLeft;
				return s_ButtonStyle;
			}
		}
		private static GUIStyle s_ButtonStyle;

		private static GUIStyle FileNameStyle {
			get {
				if (s_TitleStyle != null) { return s_TitleStyle; }
				s_TitleStyle = GUI.skin.label.Clone();
				s_TitleStyle.alignment = TextAnchor.LowerLeft;
				s_TitleStyle.fontStyle = FontStyle.Bold;
				s_TitleStyle.fontSize = 16;
				s_TitleStyle.margin = new RectOffset();
				return s_TitleStyle;
			}
		}
		private static GUIStyle s_TitleStyle;

		private static GUIStyle DirectoryStyle {
			get {
				if (s_SmallTitleStyle != null) { return s_SmallTitleStyle; }
				s_SmallTitleStyle = GUI.skin.label.Clone();
				s_SmallTitleStyle.alignment = TextAnchor.LowerRight;
				s_SmallTitleStyle.margin = new RectOffset();
				return s_SmallTitleStyle;
			}
		}
		private static GUIStyle s_SmallTitleStyle;

		[MenuItem("Window/Asset Usages")]
		public static void OpenWindow() {
			Instance.Show();
		}

		private static UsageInfo GetUsageInfo(Object obj) {
			if (!obj) { return null; }
			string path = AssetDatabase.GetAssetPath(obj);
			return GetUsageInfo(path, obj);
		}

		private static UsageInfo GetUsageInfo(string path) { return GetUsageInfo(path, null); }

		private static UsageInfo GetUsageInfo(string path, Object obj) {
			if (!obj && path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) {
				obj = AssetDatabase.LoadAssetAtPath<Object>(path);
			}
			if (!s_ReferenceInfos.ContainsKey(path)) { s_ReferenceInfos[path] = new UsageInfo(obj); }
			return s_ReferenceInfos[path];
		}

		[UsedImplicitly]
		private void OnEnable() {
			EditorSelectionTracker.SelectionChanged -= SelectionChanged;
			EditorSelectionTracker.SelectionChanged += SelectionChanged;
			s_ShowIndirectReferences = EditorPrefs.GetBool(kShowIndirectReferencesPrefKey, false);
		}

		private void SelectionChanged(Object selection) { Repaint(); }

		private UsageInfo m_LastInfo;

		[UsedImplicitly]
		private void OnGUI() {
			GUILayout.BeginHorizontal();
			GUI.enabled = s_GoBackStack.Any();
			if (GUILayout.Button("<")) { GoBack(); }
			GUI.enabled = s_GoForwardStack.Any();
			if (GUILayout.Button(">")) { GoForward(); }
			GUI.enabled = true;

			s_ShowIndirectReferences = GUILayout.Toggle(s_ShowIndirectReferences, "Show Indirect References");
			s_LockSelection = GUILayout.Toggle(s_LockSelection, "Lock Current Selection");

			if (s_LastShowIndirectReferences != s_ShowIndirectReferences) {
				s_LastShowIndirectReferences = s_ShowIndirectReferences;
				EditorPrefs.SetBool(kShowIndirectReferencesPrefKey, s_ShowIndirectReferences);
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			try {
				Object obj = Selection.activeObject;
				if (!obj) { return; }
				UsageInfo info = GetUsageInfo(obj);

				if (info != m_LastInfo && !s_LockSelection) {
					info.Refresh();
					Go(obj);
					m_LastInfo = info;
				}
				m_LastInfo.OnGUI();
			}
			catch (Exception e) {
				Debug.LogException(e);
			}
		}

		//[MenuItem("Window/Asset Usages - Refresh", priority = (int) BDJMenuPriority.BDJ)]
		private static void PopulateAllReferences() {
			string[] all = AssetDatabase.GetAllAssetPaths();
			for (int i = 0; i < all.Length; i++) {
				string path = all[i];
				if (!path.StartsWith("Assets")) {
					continue;
				}
				try {
					if (GetUsageInfo(path).UpdateReferencees(i, all.Length)) {
						Debug.LogWarning("Populate All Cancelled at " + (i*1f/all.Length).ToString("P0"));
						EditorUtility.ClearProgressBar();
						return;
					}
				}
				catch (Exception e) {
					Debug.LogError("Populate All couldn't get references for " + path + " because " + e.Message);
				}
			}
			EditorUtility.ClearProgressBar();
			Instance.Repaint();
			s_Scanned = true;
		}

		private static T Pop<T>(List<T> list) where T : class {
			if (list.Count == 0) { return null; }
			T result = list.Last();
			list.RemoveAt(list.Count - 1);
			return result;
		}

		private static void GoForward() {
			s_GoBackStack.Add(Selection.activeObject);
			Selection.activeObject = Pop(s_GoForwardStack);
		}

		private static void GoBack() {
			s_GoForwardStack.Add(Selection.activeObject);
			Selection.activeObject = Pop(s_GoBackStack);
		}

		private static void Go(Object obj) { Go(AssetDatabase.GetAssetPath(obj), obj); }

		private static void Go(string path) { Go(path, AssetDatabase.LoadAssetAtPath<Object>(path)); }

		private static void Go(string path, Object obj) {
			Object prev = s_GoBackStack.LastOrDefault();
			Object next = s_GoForwardStack.LastOrDefault();

			if (prev && prev == obj) {
				GoBack();
				return;
			}

			if (next && next == obj) {
				GoForward();
				return;
			}

			Object oldObj = Selection.activeObject;
			UsageInfo oldInfo = GetUsageInfo(oldObj);

			if (oldInfo.Path == path) { return; }

			if (oldInfo.HasUsing(path)) {
				s_GoBackStack.Add(oldObj);
				s_GoForwardStack.Clear();
			}
			else if (oldInfo.HasUsedBy(path)) {
				s_GoForwardStack.Add(oldObj);
				s_GoBackStack.Clear();
			}
			else {
				s_GoForwardStack.Clear();
				s_GoBackStack.Clear();
			}

			Selection.activeObject = obj;
		}

		private class UsageInfo {
			private readonly Object m_Obj;

			private Object[] RawUsing {
				get { return m_RawUsing ?? (m_RawUsing = EditorUtility.CollectDependencies(new[] {m_Obj})); }
			}
			private Object[] m_RawUsing;

			private List<string> Using { get { return m_Using ?? (m_Using = GetUsing()); } }
			private List<string> m_Using;

			private List<string> FilteredUsing { get { return m_FilteredUsing ?? (m_FilteredUsing = GetFilteredUsing(false)); } }
			private List<string> m_FilteredUsing;

			private List<string> UsedBy { get { return m_UsedBy; } }
			private readonly List<string> m_UsedBy = new List<string>();

			private List<string> FilteredUsedBy { get { return m_FilteredUsedBy ?? (m_FilteredUsedBy = GetFilteredUsedBy(false)); } }
			private List<string> m_FilteredUsedBy;

			public string Path { get { return m_Path ?? (m_Path = AssetDatabase.GetAssetPath(m_Obj)); } }
			private string m_Path;

			private string FileName { get { return m_FileName ?? (m_FileName = System.IO.Path.GetFileName(Path)); } }
			private string m_FileName;

			private string Directory {
				get { return m_Directory ?? (m_Directory = System.IO.Path.GetDirectoryName(Path) + "/"); }
			}
			private string m_Directory;

			private string Summary { get { return m_Summary ?? (m_Summary = Path); } }
			private string m_Summary;

			private Vector2 m_UsesScroll = Vector2.zero;
			private Vector2 m_UsedByScroll = Vector2.zero;

			private List<string> GetUsing() {
				List<string> results =
					RawUsing.Select<Object, UsageInfo>(GetUsageInfo)
						.Where(i => i != null && i != this)
						.Select(i => i.Path)
						.Distinct()
						.Where(CanShow)
						.ToList();
				if (!Path.IsNullOrEmpty()) {
					results.Select<string, UsageInfo>(GetUsageInfo).ForEach(info => {
						if (!info.UsedBy.Contains(Path)) info.AddUsedBy(Path);
					});
				}
				m_FilteredUsing = null;
				return results;
			}

			private void AddUsedBy(string path) {
				if (UsedBy.Contains(path)) { return; }
				UsedBy.Add(path);
				m_FilteredUsedBy = null;
			}

			private List<string> GetFilteredUsingWithProgress() {
				return m_FilteredUsing ?? (m_FilteredUsing = GetFilteredUsing(true));
			}

			private List<string> GetFilteredUsing(bool showProgress) {
				// Grab all the references.
				HashSet<string> childPaths = new HashSet<string>(Using);

				for (int i = 0; i < Using.Count; i++) {
					string path = Using[i];

					// Avoid infinite loops where assets reference each other.
					if (UsedBy.Contains(path)) {
						if (childPaths.Contains(path)) { childPaths.Remove(path); }
						continue;
					}

					UsageInfo info = GetUsageInfo(path);

					// Only show progress if there is a significant number of sub-paths to search through.
					if (showProgress && info.Using.Count > 20 && EditorUtility.DisplayCancelableProgressBar("Gathering Child File References for " + FileName, path, i*1f/Using.Count)) {
						EditorUtility.ClearProgressBar();
						return new List<string>();
					}

					// Go through each sub path and remove any found in childPaths.
					foreach (string subPath in info.Using) {
						if (childPaths.Contains(subPath)) { childPaths.Remove(subPath); }
					}
				}

				if (showProgress) EditorUtility.ClearProgressBar();

				// Return final child paths as an ordered list.
				return childPaths.ToList();
			}

			private List<string> GetFilteredUsedByWithProgress() {
				return m_FilteredUsedBy ?? (m_FilteredUsedBy = GetFilteredUsedBy(true));
			}

			private List<string> GetFilteredUsedBy(bool showProgress) {
				HashSet<string> filteredUsedBy = new HashSet<string>();

				for (int i = 0; i < UsedBy.Count; i++) {
					string path = UsedBy[i];
					UsageInfo info = GetUsageInfo(path);
					if (showProgress &&
							EditorUtility.DisplayCancelableProgressBar("Gathering Parent File References for " + FileName, path,
								i*1f/UsedBy.Count)) {
						EditorUtility.ClearProgressBar();
						return new List<string>();
					}

					if (info.FilteredUsing.Any(subPath => subPath == Path)) { filteredUsedBy.Add(path); }
				}

				if (showProgress) EditorUtility.ClearProgressBar();
				return filteredUsedBy.ToList();
			}

			private bool CanShow(string path) { return !path.IsNullOrEmpty() && path != Path && !UsedBy.Contains(path); }

			public UsageInfo(Object obj) { m_Obj = obj; }

			public override string ToString() { return Summary; }

			public void OnGUI() {
				if (Path.IsNullOrEmpty()) { return; }

				GUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.BeginVertical();
				GUILayout.Space(5);
				GUILayout.Label(Directory, DirectoryStyle);
				GUILayout.EndVertical();
				GUILayout.Label(FileName, FileNameStyle);
				if (GUILayout.Button("Refresh")) { Refresh(); }
				if (GUILayout.Button("Reset All")) { ResetAll(); }
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();

				float width = Instance.position.width - 10;
				GUILayout.BeginVertical(GUILayout.Width(width/2));

				GUILayout.Label("Used by:");
				m_UsedByScroll = GUILayout.BeginScrollView(m_UsedByScroll, GUIStyle.none);
				if (s_ShowIndirectReferences) { UsedBy.OrderBy(p => p).ForEach(OnButtonGUI); }
				else { GetFilteredUsedByWithProgress().OrderBy(p => p).ForEach(OnButtonGUI); }
				if (GUILayout.Button(s_Scanned ? "Refresh All..." : "Find All...")) { PopulateAllReferences(); }
				GUILayout.EndScrollView();
				GUILayout.EndVertical();

				GUILayout.BeginVertical(GUILayout.Width(width/2));
				GUILayout.Label("Using:");
				m_UsesScroll = GUILayout.BeginScrollView(m_UsesScroll, GUIStyle.none);
				if (s_ShowIndirectReferences) { Using.OrderBy(p => p).ForEach(OnButtonGUI); }
				else { GetFilteredUsingWithProgress().OrderBy(p => p).ForEach(OnButtonGUI); }
				GUILayout.EndScrollView();
				GUILayout.EndVertical();

				GUILayout.EndHorizontal();
			}

			private static void OnButtonGUI(string path) {
				GUI.enabled = path.StartsWith("Assets");
				if (!path.IsNullOrEmpty() && GUILayout.Button(path, ButtonStyle)) { Go(path); }
				GUI.enabled = true;
			}

			public bool UpdateReferencees(int current, int total) {
				if (EditorUtility.DisplayCancelableProgressBar("Cataloging References for EVERYTHING", Path, current*1f/total)) {
					EditorUtility.ClearProgressBar();
					return true;
				}
				m_Using = GetUsing();
				return false;
			}

			public void Refresh() {
				//Object[] updatedDependencies = EditorUtility.CollectDependencies(new[] {m_Obj});
				//if (m_RawUsing != null && updatedDependencies.SequenceEqual(m_RawUsing)) { return; }
				m_RawUsing = EditorUtility.CollectDependencies(new[] { m_Obj });
				m_Using = null;
				m_FilteredUsing = null;
				m_FilteredUsedBy = null;
				m_Path = null;
				//if (refreshChildren) {
				//	Instance.Repaint();
				//}
				Instance.SelectionChanged(m_Obj);
			}

			public void ResetAll() {
				s_ReferenceInfos.Clear();
				Instance.SelectionChanged(m_Obj);
			}

			public bool HasUsedBy(string path) { return UsedBy.Contains(path); }
			public bool HasUsing(string path) { return Using.Contains(path); }
		}
	}
}