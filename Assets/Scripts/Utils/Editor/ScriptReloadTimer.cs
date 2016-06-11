using System;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ScriptReloadTimer : ScriptableObject {
	private static ScriptReloadTimer m_Instance;

	[SerializeField] private bool m_SerialisedField;
	[SerializeField] private float m_CompilationStartTime;
	[SerializeField] private bool m_AutoPlay;

	[NonSerialized] private bool m_EphemeralField;
	[NonSerialized] private bool m_IsCompiling;

	static ScriptReloadTimer() {
		EditorApplication.update -= OneTimeUpdate;
		EditorApplication.update += OneTimeUpdate; 
	}

	private static void OneTimeUpdate() {
		EditorApplication.update -= OneTimeUpdate;
		if (m_Instance == null) {
			m_Instance = FindObjectOfType<ScriptReloadTimer>();
			if (m_Instance == null) {
				m_Instance = CreateInstance<ScriptReloadTimer>();
			}
		}
	}

	[UsedImplicitly]
	private void OnEnable() {
		hideFlags = HideFlags.HideAndDontSave;
		m_Instance = this;
		m_IsCompiling = false;
		if (m_AutoPlay) {
			Debug.Log("Restarting play.");
			EditorApplication.isPlaying = true;
		}
		m_AutoPlay = false;
	}

	public ScriptReloadTimer() {
		EditorApplication.update -= Update;
		EditorApplication.update += Update;
	}

	private void Update() {
		if (m_Instance != this) {
			EditorApplication.update -= Update;
			if (Application.isPlaying) { Destroy(this); }
			else { DestroyImmediate(this); }
		}
		else {
			if (m_SerialisedField && !m_EphemeralField) {
				if (m_CompilationStartTime > 0.0f) {
					var elapsedTime = Time.realtimeSinceStartup - m_CompilationStartTime;
					if (elapsedTime > 0.0f) { Debug.Log(string.Format("Script Reload detected, duration {0} secs", elapsedTime)); }
					m_CompilationStartTime = float.NegativeInfinity;
				}
			}

			m_SerialisedField = true;
			m_EphemeralField = true;

			if (!m_IsCompiling && EditorApplication.isCompiling) {
				m_IsCompiling = true;
				m_CompilationStartTime = Time.realtimeSinceStartup;
			}

			if (m_IsCompiling && !EditorApplication.isCompiling) {
				m_IsCompiling = false;
				m_CompilationStartTime = float.NegativeInfinity;
			}

			if (EditorApplication.isPlaying && EditorApplication.isCompiling) {
				Debug.Log("Exiting play mode due to script recompilation.");
				EditorApplication.isPlaying = false;
				m_AutoPlay = true;
			}
		}
	}
}