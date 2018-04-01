using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SurfaceCreator))]
public class SurfaceCreatorInspector : Editor {

	private SurfaceCreator creator;

	private void OnEnable () {
		creator = target as SurfaceCreator;
		Undo.undoRedoPerformed += RefreshCreator;
	}

	private void OnDisable () {
		Undo.undoRedoPerformed -= RefreshCreator;
	}

	private void RefreshCreator () {
#if UNITY_EDITOR
		if (Application.isPlaying || creator.simulateInEditor) {
#else
		if (Application.isPlaying) {
#endif
			creator.Refresh();
		}
	}

	public override void OnInspectorGUI () {
		EditorGUI.BeginChangeCheck();
		DrawDefaultInspector();
		if (EditorGUI.EndChangeCheck()) {
			RefreshCreator();
		}
	}
}