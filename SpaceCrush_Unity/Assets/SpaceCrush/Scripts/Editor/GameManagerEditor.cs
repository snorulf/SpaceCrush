using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GameManager))]
[CanEditMultipleObjects]
public class GameManagerEditor : Editor {
    public override void OnInspectorGUI() {
        DrawDefaultInspector();

        GameManager myTarget = (GameManager)target;

        if (GUILayout.Button("Reset")) {
            myTarget.ResetGame();
        }
    }
}