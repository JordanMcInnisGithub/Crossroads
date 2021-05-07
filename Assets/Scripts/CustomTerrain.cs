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

	//Voronoi --------
	public int vpeakCount = 3;
	public float vfalloff = 0.2f;
	public float vdropoff = 0.6f;
	public float vminHeight = 0.25f;
	public float vmaxHeight = 0.412f;
	public enum VoronoiType { Linear = 0, Power = 1, Combined = 2, SinPow = 3 }
	public VoronoiType voronoiType = VoronoiType.Linear;

	//Midpoint displacement --------
	public float MPDheightMin = -2f;
	public float MPDheightMax = 2f;
	public float MPDheightDampenerPower = 2f;
	public float MPDroughness = 2.0f;

	//Smoothing --------
	public int SmoothAmount = 1;

	//Splatmaps --------

	[System.Serializable]
	public class SplatHeights
	{
		public Texture2D texture = null;
		public float minHeight = 0.1f;
		public float maxHeight = 0.2f;
		public Vector2 tileOffset = new Vector2(0, 0);
		public Vector2 tileSize = new Vector2(50, 50);
		public float offset = 0.001f;
		public float noiseX = 0.05f;
		public float noiseY = 0.05f;
		public float noiseScale = 0.1f;
		public bool remove = false;
	}

	//Tables --------
	public List<SplatHeights> splatHeights = new List<SplatHeights>()
	{
		//table requires one entry
		new SplatHeights()
	};
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

	public void AddNewSplatHeight()
	{
		splatHeights.Add(new SplatHeights());
	}
	public void RemoveSplatHeight()
	{
		List<SplatHeights> keptSplatHeights = new List<SplatHeights>();
		for (int i = 0; i < splatHeights.Count; i++)
		{
			if (!splatHeights[i].remove)
			{
				keptSplatHeights.Add(splatHeights[i]);
			}
		}
		if (keptSplatHeights.Count == 0)
		{
			keptSplatHeights.Add(splatHeights[0]);
		}
		splatHeights = keptSplatHeights;
	}

	public void SplatMaps()
	{
		TerrainLayer[] newSplatPrototypes;
		newSplatPrototypes = new TerrainLayer[splatHeights.Count];
		int spindex = 0;
		foreach (SplatHeights sh in splatHeights)
		{
			newSplatPrototypes[spindex] = new TerrainLayer();
			newSplatPrototypes[spindex].diffuseTexture = sh.texture;
			newSplatPrototypes[spindex].tileOffset = sh.tileOffset;
			newSplatPrototypes[spindex].tileSize = sh.tileSize;
			newSplatPrototypes[spindex].diffuseTexture.Apply(true);
			spindex++;
		}
		terrainData.terrainLayers = newSplatPrototypes;

		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

		float[,,] splatMapdata = new float[terrainData.alphamapResolution, terrainData.alphamapResolution, terrainData.alphamapLayers];

		for (int z = 0; z < terrainData.alphamapResolution; z++)
		{
			for (int x = 0; x < terrainData.alphamapWidth; x++)
			{
				float[] splat = new float[terrainData.alphamapLayers];
				for (int i = 0; i < splatHeights.Count; i++)
				{
					float noise = Mathf.PerlinNoise(x * splatHeights[i].noiseX, z * splatHeights[i].noiseY) * splatHeights[i].noiseScale;
					float offset = splatHeights[i].offset + noise;
					float thisHeightStart = splatHeights[i].minHeight - offset;
					float thisHeightStop = splatHeights[i].maxHeight + offset;
					if ((heightMap[x,z] >= thisHeightStart && heightMap[x,z] <= thisHeightStop))
					{
						splat[i] = 1;
					}
				}
				NormalizeVector(splat);
				for (int j = 0; j < splatHeights.Count; j++)
				{
					splatMapdata[x, z, j] = splat[j];
				}
			}
		}
		terrainData.SetAlphamaps(0, 0, splatMapdata);
	}

	public void NormalizeVector(float[] v)
	{
		float total = 0;
		for (int i = 0; i < v.Length; i++)
		{
			total += v[i];
		}

		for (int i = 0; i < v.Length; i++)
		{
			v[i] /= total;
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

	public void MidPointDisplacement()
	{
		float[,] heightMap = GetHeightMap();
		int width = terrainData.heightmapResolution - 1;
		int squareSize = width;
		float heightMin = MPDheightMin;
		float heightMax = MPDheightMax;
		float heightDampener = (float)Mathf.Pow(MPDheightDampenerPower, -1 * MPDroughness);

		int cornerX, cornerZ;
		int midX, midZ;
		int pmidXL, pmidXR, pmidZU, pmidZD;

		while (squareSize > 0)
		{
			for (int x = 0; x < width; x += squareSize)
			{
				for (int z = 0; z < width; z += squareSize)
				{
					cornerX = (x + squareSize);
					cornerZ = (z + squareSize);

					midX = (int)(x + squareSize / 2.0f);
					midZ = (int)(z + squareSize / 2.0f);

					heightMap[midX, midZ] = (float)((heightMap[x, z] + heightMap[cornerX, z] + heightMap[x, cornerZ] + heightMap[cornerX, cornerZ]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));
				}
			}

			for (int x = 0; x < width; x += squareSize)
			{
				for (int z = 0; z < width; z += squareSize)
				{
					cornerX = (x + squareSize);
					cornerZ = (z + squareSize);

					midX = (int)(x + squareSize / 2.0f);
					midZ = (int)(z + squareSize / 2.0f);

					pmidXR = (int)(midX + squareSize);
					pmidXL = (int)(midX - squareSize);
					pmidZD = (int)(midZ - squareSize);
					pmidZU = (int)(midZ + squareSize);

					if (pmidXL <= 0 || pmidZD <= 0 || pmidXR >= width - 1 || pmidZU >= width - 1) continue;
					//Calculate the square value for the bottom side

					//bot mid
					heightMap[midX, z] = (float)((heightMap[x, z] + heightMap[cornerX, z] + heightMap[midX, pmidZD] + heightMap[midX, midZ]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));

					//top mid
					heightMap[midX, cornerZ] = (float)((heightMap[x, cornerZ] + heightMap[midX, midZ] + heightMap[cornerX, cornerZ] + heightMap[midX, pmidZU]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));

					//mid left
					heightMap[x, midZ] = (float)((heightMap[x, z] + heightMap[pmidXL, midZ] + heightMap[x, cornerZ] + heightMap[midX, midZ]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));

					//mid right
					heightMap[cornerX, midZ] = (float)((heightMap[cornerX, z] + heightMap[midX, midZ] + heightMap[cornerX, cornerZ] + heightMap[pmidXR, midZ]) / 4.0f + UnityEngine.Random.Range(heightMin, heightMax));

				}
			}
			squareSize = (int)(squareSize / 2.0f);

			//this is the magic, this is what reduces terrain and prevents everything being max height
			heightMax *= heightDampener;
			heightMin *= heightDampener;

		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	public void Smooth()
	{
		float[,] heightMap = GetHeightMap();
		for (int i = 0; i < SmoothAmount; i++)
		{
			for (int z = 0; z < terrainData.heightmapResolution; z++)
			{
				for (int x = 0; x < terrainData.heightmapResolution; x++)
				{
					float avgHeight = heightMap[x, z];
					List<Vector2> neighbours = GenerateNeighbours(new Vector2(x, z), terrainData.heightmapResolution, terrainData.heightmapResolution);

					foreach (Vector2 n in neighbours)
					{
						avgHeight += heightMap[(int)n.x, (int)n.y];
					}

					heightMap[x, z] = avgHeight / ((float)neighbours.Count + 1);
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	List<Vector2> GenerateNeighbours(Vector2 pos, int width, int height)
	{
		List<Vector2> neighbours = new List<Vector2>();

		for (int z = -1; z < 2; z++)
		{
			for (int x = -1; x < 2; x++)
			{
				if (!(x == 0 && z == 0))
				{
					Vector2 nPos = new Vector2(Mathf.Clamp(pos.x + x, 0, width - 1), Mathf.Clamp(pos.y + z, 0, height - 1));

					if (!neighbours.Contains(nPos)) neighbours.Add(nPos);
				}
			}
		}

		return neighbours;
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