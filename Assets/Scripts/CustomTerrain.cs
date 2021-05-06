using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;


[ExecuteInEditMode]
public class CustomTerrain : MonoBehaviour
{
	public Vector2 randomHeightRange = new Vector2(0, 0.1f);

	public bool resetTerrain = true;

	//perlin noise --------
	public float perlinXScale = 0.01f;
	public float perlinZScale = 0.01f;
	public int perlinOffsetX = 0;
	public int perlinOffsetZ = 0;
	public int perlinOctaves = 3;
	public float perlinPersistance = 8;
	public float perlinHeightScale = 0.0f;

	//Multiple Perlin --------
	[System.Serializable]
	public class PerlinParameters
	{
		public float mPerlinXScale = 0.01f;
		public float mPerlinZScale = 0.01f;
		public int mPerlinOctaves = 3;
		public float mPerlinPersistance = 8;
		public float mPerlinHeightScale = 0.09f;
		public int mPerlinOffsetX = 0;
		public int mPerlinOffsetZ = 0;
		public bool remove = false;
	}

	//Voronoi
	public int vpeakCount = 3;
	public float vfalloff = 0.2f;
	public float vdropoff = 0.6f;
	public float vminHeight = 0.25f;
	public float vmaxHeight = 0.412f;
	public enum VoronoiType { Linear = 0, Power = 1, Combined = 2, SinPow = 3 }
	public VoronoiType voronoiType = VoronoiType.Linear;

	public List<PerlinParameters> perlinParameters = new List<PerlinParameters>()
	{
		// table requires one entry;
		new PerlinParameters()
	};

	public Terrain terrain;
	public TerrainData terrainData;
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
	private void OnEnable()
	{
		Debug.Log("Initialising Terrain Data");
		terrain = this.GetComponent<Terrain>();
		terrainData = Terrain.activeTerrain.terrainData;
	}
	float[,] GetHeightMap()
	{
		if (!resetTerrain)
		{
			return terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
		}
		else
		{
			return new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
		}
	}

	public void Perlin()
	{
		float[,] heightMap = GetHeightMap();

		for (int x = 0; x < terrainData.heightmapResolution; x++)
		{
			for (int z = 0; z < terrainData.heightmapResolution; z++)
			{
				heightMap[x, z] += TerrainUtils.fBM((x + perlinOffsetX) * perlinXScale, (z + perlinOffsetZ) * perlinZScale, perlinOctaves, perlinPersistance) * perlinHeightScale;
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
	}

	public void MultiplePerlinTerrain()
	{
		float[,] heightMap = GetHeightMap();
		for (int z = 0; z < terrainData.heightmapResolution; z++)
		{
			for (int x = 0; x < terrainData.heightmapResolution; x++)
			{
				foreach(PerlinParameters p in perlinParameters)
				heightMap[x, z] += TerrainUtils.fBM((x + p.mPerlinOffsetX) * p.mPerlinXScale, (z + p.mPerlinOffsetZ) * p.mPerlinZScale, p.mPerlinOctaves, p.mPerlinPersistance) * p.mPerlinHeightScale;
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
	}

	public void AddNewPerlin()
	{
		perlinParameters.Add(new PerlinParameters());
	}
	public void RemovePerlin()
	{
		List<PerlinParameters> keptPerlinParameters = new List<PerlinParameters>();
		for (int i = 0; i < perlinParameters.Count; i++)
		{
			if (!perlinParameters[i].remove)
			{
				keptPerlinParameters.Add(perlinParameters[i]);
			}
		}
		if (keptPerlinParameters.Count == 0)
		{
			keptPerlinParameters.Add(perlinParameters[0]);
		}
		perlinParameters = keptPerlinParameters;
	}

	public void RandomTerrain()
	{
		float[,] heightMap;
		heightMap = GetHeightMap();

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

	public void Voronoi()
	{
		float[,] heightMap = GetHeightMap();

		for (int p = 0; p < vpeakCount; p++)
		{
			Vector3 peak = new Vector3(UnityEngine.Random.Range(0, terrainData.heightmapResolution), UnityEngine.Random.Range(vminHeight, vmaxHeight), UnityEngine.Random.Range(0, terrainData.heightmapResolution));
			if(heightMap[(int)peak.x, (int)peak.z] < peak.y)
				heightMap[(int)peak.x, (int)peak.z] = peak.y;
			else
			{
				continue;
			}

			Vector2 peakLocation = new Vector2(peak.x, peak.z);

			float maxDistance = Vector2.Distance(new Vector2(0, 0), new Vector2(terrainData.heightmapResolution, terrainData.heightmapResolution));

			for (int z = 0; z < terrainData.heightmapResolution; z++)
			{
				for (int x = 0; x < terrainData.heightmapResolution; x++)
				{
					if (!(x == peak.x && z == peak.z))
					{
						float distanceToPeak = Vector2.Distance(peakLocation, new Vector2(x, z)) / maxDistance;
						float h;

						if (voronoiType == VoronoiType.Combined)
						{
							h = peak.y - distanceToPeak * vfalloff - Mathf.Pow(distanceToPeak, vdropoff);
						}
						else if (voronoiType == VoronoiType.Power)
						{
							h = peak.y - Mathf.Pow(distanceToPeak, vdropoff) * vfalloff;
						}
						else if ( voronoiType == VoronoiType.SinPow)
						{
							h = peak.y - Mathf.Pow(distanceToPeak * 3, vfalloff) - Mathf.Sin(distanceToPeak * 2 * Mathf.PI) / vdropoff;
						}
						else
						{
							h = peak.y - distanceToPeak * vfalloff;
						}
						//float h = peak.y - Mathf.Sin(distanceToPeak * 100)*0.1f; <-- cool effect possibly for water?

						//only set hight if we have a heightmap value less than what we are trying to set
						if (heightMap[x, z] < h) heightMap[x, z] = h;
					}
				}
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
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
