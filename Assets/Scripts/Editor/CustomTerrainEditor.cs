using UnityEngine;
using UnityEditor;
using EditorGUITable;


[CustomEditor(typeof(CustomTerrain))]
[CanEditMultipleObjects]
public class CustomTerrainEditor : Editor
{
	//properties --------
	SerializedProperty randomHeightRange;
	SerializedProperty perlinXScale;
	SerializedProperty perlinZScale;


	// fold outs --------
	bool showRandom = false;
	bool showPerlinNoise = false;
	private void OnEnable()
	{
		randomHeightRange = serializedObject.FindProperty("randomHeightRange"); // this links to other property in custom terrain
		perlinXScale = serializedObject.FindProperty("perlinXScale");
		perlinZScale = serializedObject.FindProperty("perlinZScale");
	}
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		CustomTerrain terrain = (CustomTerrain)target;

		showRandom = EditorGUILayout.Foldout(showRandom, "Random");
		if (showRandom)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Set Heights Between Random Value", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(randomHeightRange);

			if (GUILayout.Button("Random Heights"))
			{
				terrain.RandomTerrain();
			}
		}
		showPerlinNoise = EditorGUILayout.Foldout(showPerlinNoise, "Single Perlin Noise");
		if (showPerlinNoise)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Perlin Noise", EditorStyles.boldLabel);
			EditorGUILayout.Slider(perlinXScale, 0, 1, new GUIContent("X Scale"));
			EditorGUILayout.Slider(perlinZScale, 0, 1, new GUIContent("Z Scale"));

			if (GUILayout.Button("Perlin"))
			{
				terrain.Perlin();
			}

		}

		if (GUILayout.Button("Reset Terrain"))
		{
			terrain.ResetTerrain();
		}
		serializedObject.ApplyModifiedProperties();
	}
}
