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
	SerializedProperty perlinOffsetX;
	SerializedProperty perlinOffsetZ;
	SerializedProperty perlinOctaves;
	SerializedProperty perlinPersistance;
	SerializedProperty perlinHeightScale;
	SerializedProperty resetTerrain;

	GUITableState perlinParameterTable;
	SerializedProperty perlinParameters;

	SerializedProperty vfalloff;
	SerializedProperty vdropoff;
	SerializedProperty vminHeight;
	SerializedProperty vmaxHeight;
	SerializedProperty vpeakCount;
	SerializedProperty voronoiType;

	SerializedProperty MPDheightMin;
	SerializedProperty MPDheightMax;
	SerializedProperty MPDheightDampenerPower;
	SerializedProperty MPDroughness;








	// fold outs --------
	bool showRandom = false;
	bool showPerlinNoise = false;
	bool showMultiplePerlin = false;
	bool showVoronoi = false;
	bool showMidPoint = false;

	private void OnEnable()
	{
		randomHeightRange = serializedObject.FindProperty("randomHeightRange"); // this links to other property in custom terrain
		perlinXScale = serializedObject.FindProperty("perlinXScale");
		perlinZScale = serializedObject.FindProperty("perlinZScale");
		perlinOffsetX = serializedObject.FindProperty("perlinOffsetX");
		perlinOffsetZ = serializedObject.FindProperty("perlinOffsetZ");
		perlinOctaves = serializedObject.FindProperty("perlinOctaves");
		perlinPersistance = serializedObject.FindProperty("perlinPersistance");
		perlinHeightScale = serializedObject.FindProperty("perlinHeightScale");
		resetTerrain = serializedObject.FindProperty("resetTerrain");
		perlinParameterTable = new GUITableState("perlinParameterTable");
		perlinParameters = serializedObject.FindProperty("perlinParameters");
		vfalloff = serializedObject.FindProperty("vfalloff");
		vdropoff = serializedObject.FindProperty("vdropoff");
		vminHeight = serializedObject.FindProperty("vminHeight");
		vmaxHeight = serializedObject.FindProperty("vmaxHeight");
		vpeakCount = serializedObject.FindProperty("vpeakCount");
		voronoiType = serializedObject.FindProperty("voronoiType");
		MPDheightMin = serializedObject.FindProperty("MPDheightMin");
		MPDheightMax = serializedObject.FindProperty("MPDheightMax");
		MPDheightDampenerPower = serializedObject.FindProperty("MPDheightDampenerPower");
		MPDroughness = serializedObject.FindProperty("MPDroughness");




	}
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		CustomTerrain terrain = (CustomTerrain)target;

		EditorGUILayout.PropertyField(resetTerrain);
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
			EditorGUILayout.IntSlider(perlinOffsetX, 0, 10000, new GUIContent("X Offset"));
			EditorGUILayout.IntSlider(perlinOffsetZ, 0, 10000, new GUIContent("Z Offset"));
			EditorGUILayout.IntSlider(perlinOctaves, 1, 10, new GUIContent("Octaves"));
			EditorGUILayout.Slider(perlinPersistance, 1, 10, new GUIContent("Persistance"));
			EditorGUILayout.Slider(perlinHeightScale, 0, 1, new GUIContent("Height Scale"));

			if (GUILayout.Button("Perlin"))
			{
				terrain.Perlin();
			}

		}
		showMultiplePerlin = EditorGUILayout.Foldout(showMultiplePerlin, "Multiple Perlin");
		if (showMultiplePerlin)
		{
			EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			GUILayout.Label("Multiple Perlin Noise", EditorStyles.boldLabel);
			perlinParameterTable = GUITableLayout.DrawTable(perlinParameterTable, perlinParameters);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("+"))
			{
				terrain.AddNewPerlin();
			}
			if (GUILayout.Button("-"))
			{
				terrain.RemovePerlin();
			}
			EditorGUILayout.EndHorizontal();
			if (GUILayout.Button("Apply Multiple Perlin"))
			{
				terrain.MultiplePerlinTerrain();
			}
		}
		showVoronoi = EditorGUILayout.Foldout(showVoronoi, "Voronoi");
		if (showVoronoi)
		{
			EditorGUILayout.IntSlider(vpeakCount, 1, 10, new GUIContent("Peak Count"));
			EditorGUILayout.Slider(vfalloff, 0, 10, new GUIContent("Falloff"));
			EditorGUILayout.Slider(vdropoff, 0, 10, new GUIContent("Dropoff"));
			EditorGUILayout.Slider(vminHeight, 0, 1, new GUIContent("Min Height"));
			EditorGUILayout.Slider(vmaxHeight, 0, 1, new GUIContent("Max Height"));
			EditorGUILayout.PropertyField(voronoiType);
			if (GUILayout.Button("Voronoi"))
			{
				terrain.Voronoi();
			}
		}

		showMidPoint = EditorGUILayout.Foldout(showMidPoint, "Midpoint");
		if (showMidPoint)
		{
			EditorGUILayout.PropertyField(MPDheightMin);
			EditorGUILayout.PropertyField(MPDheightMax);
			EditorGUILayout.PropertyField(MPDheightDampenerPower);
			EditorGUILayout.PropertyField(MPDroughness);
			if (GUILayout.Button("Midpoint"))
			{
				terrain.MidPointDisplacement();
			}
		}
		EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
		if (GUILayout.Button("Reset Terrain"))
		{
			terrain.ResetTerrain();
		}
		serializedObject.ApplyModifiedProperties();
	}
}
