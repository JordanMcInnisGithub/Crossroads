using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;


[ExecuteInEditMode]
public class CustomTerrain : MonoBehaviour
{
	public Vector2 randomHeightRange = new Vector2(0, 0.1f);
	public Terrain terrain;
	public TerrainData terrainData;

	//perlin noise --------
	public float perlinXScale = 0.01f;
	public float perlinZScale = 0.01f;


	public void Perlin()
	{
		ResetTerrain(); // rest the terrain
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

		for (int x = 0; x < terrainData.heightmapResolution; x++)
		{
			for (int z = 0; z < terrainData.heightmapResolution; z++)
			{
				heightMap[x, z] += Mathf.PerlinNoise(x * perlinXScale, z * perlinZScale);
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
	}

	private void OnEnable()
	{
		Debug.Log("Initialising Terrain Data");
		terrain = this.GetComponent<Terrain>();
		terrainData = Terrain.activeTerrain.terrainData;


	}
	public void RandomTerrain()
	{
		float[,] heightMap;
		heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

		for (int x = 0; x < terrainData.heightmapResolution; x++)
		{
			for (int z = 0; z < terrainData.heightmapResolution; z++)
			{
				heightMap[x, z] += UnityEngine.Random.Range(randomHeightRange.x, randomHeightRange.y);
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
	}

	public void ResetTerrain()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
		for (int x = 0; x < terrainData.heightmapResolution; x++)
		{
			for (int z = 0; z < terrainData.heightmapResolution; z++)
			{
				heightMap[x, z] = 0;
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
	}

	private void Awake()
	{
		SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
		SerializedProperty tagsProp = tagManager.FindProperty("tags");

		AddTag(tagsProp, "Terrain");
		AddTag(tagsProp, "Cloud");
		AddTag(tagsProp, "Shore");

		tagManager.ApplyModifiedProperties();

		this.gameObject.tag = "Terrain";
	}
	
	void AddTag(SerializedProperty tagsProp, string newTag)
	{
		bool found = false;
		//make sure the tag doesn't arleady exist
		for (int i = 0; i < tagsProp.arraySize; i++)
		{
			SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
			if (t.stringValue.Equals(newTag)) { found = true; break; }
		}

		if (!found)
		{
			tagsProp.InsertArrayElementAtIndex(0);
			SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
			newTagProp.stringValue = newTag;
		}
	}

}
