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
		public float minSlope = 0;
		public float maxSlope = 1.5f;
		public Vector2 tileOffset = new Vector2(0, 0);
		public Vector2 tileSize = new Vector2(50, 50);
		public float offset = 0.001f;
		public float noiseX = 0.05f;
		public float noiseY = 0.05f;
		public float noiseScale = 0.1f;
		public bool remove = false;
	}

	//Vegetation --------
	[System.Serializable]
	public class Vegetation
	{
		public GameObject prefab;
		public float minHeight = 0.1f;
		public float maxHeight = 0.2f;
		public float minSlope = 0;
		public float maxSlope = 90;
		public float density = 5;
		public float minRotation = 0;
		public float maxRotation = 180;
		public float minScale = 1;
		public float maxScale = 1;
		public Color colour1 = Color.white;
		public Color colour2 = Color.white;
		public Color lightColour = Color.white;
		public bool remove = false;
	}
	public int maxTrees = 5000;
	public int treeSpacing = 5;

	//Details --------
	[System.Serializable]
	public class Detail
	{
		public GameObject prototype = null;
		public Texture2D prototypeTexture = null;
		public float minHeight = 0.1f;
		public float maxHeight = 0.2f;
		public float minSlope = 0;
		public float maxSlope = 1;
		public float overlap = 0.01f;
		public float feather = 0.05f;
		public float density = 0.5f;
		public Color dryColour = Color.white;
		public Color healthyColour = Color.white;
		public bool remove = false;

	}
	public int maxDetails = 5000;
	public int detailSpacing = 5;

	//Water --------
	public float waterHeight = 0.5f;
	public GameObject waterGO;

	// Erosion --------
	public enum ErosionType { Rain = 0, Thermal = 1, Tidal = 2, River = 3, Wind = 4 }
	public ErosionType erosionType = ErosionType.Rain;
	public float erosionStrength = 0.1f;
	public int springsPerRiver = 5;
	public float solubility = 0.01f;
	public int droplets = 10;
	public int erosionSmoothAmount = 5;
	public float thermalStrength = 0.001f;
	public float windDirection = 0;

	// Generation ------
	public int seed = 0;


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
	public List<Vegetation> vegetation = new List<Vegetation>()
	{
	 new Vegetation()
	};
	public List<Detail> details = new List<Detail>()
	{
		new Detail()
	};

	public Terrain terrain;
	public TerrainData terrainData;
	public enum TagType { Tag = 0, Layer = 1}
	[SerializeField]
	int terrainLayer = -1;
	// Features
	public enum FeatureType { Lake = 0}
	public class terrainFeature
	{
		public FeatureType type;
		public int xSlope;
		public int zSlope;
		public int size;
		public List<Vector2> border;
		public List<Vector2> points;

		public terrainFeature(FeatureType inType, List<Vector2> inPoints)
		{
			type = inType;
			points = inPoints;
			border = TerrainUtils.GrahamScan(points) as List<Vector2>;
			int size = points.Count;
		}
		public void updateData(List<Vector2> newPoints = null)
		{
			if (newPoints != null)
				points = newPoints;
			size = points.Count;
			border = TerrainUtils.GrahamScan(points) as List<Vector2>;
		}
		public bool includedIn(Vector2 point)
		{
			return points.Any(p => p.x == point.x && p.y == point.y);
		}

	}

	//Globals -------
	private Vector2 centrePos;
	private List<terrainFeature> featureList;



	private void Awake()
	{
		SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
		SerializedProperty tagsProp = tagManager.FindProperty("tags");

		AddTag(tagsProp, "Terrain", TagType.Tag);
		AddTag(tagsProp, "Cloud", TagType.Tag);
		AddTag(tagsProp, "Shore", TagType.Tag);
		tagManager.ApplyModifiedProperties();

		SerializedProperty layerProp = tagManager.FindProperty("layers");
		terrainLayer = AddTag(layerProp, "Terrain", TagType.Layer);
		tagManager.ApplyModifiedProperties();
		this.gameObject.tag = "Terrain";
		this.gameObject.layer = terrainLayer;
	}
	private void OnEnable()
	{
		terrain = this.GetComponent<Terrain>();
		//newNoise = new Noise.PerlinNoise(1.002f, 8, 9999);
		//newNoise.Initialize();
		terrainData = Terrain.activeTerrain.terrainData;
		centrePos = new Vector2(terrainData.alphamapWidth / 2, terrainData.alphamapHeight / 2);
		featureList = new List<terrainFeature>();
		Debug.Log("Initialized Terrain Data");
	}
	//globals

	private void OnDrawGizmos()
	{
		Gizmos.DrawSphere(new Vector3(terrainData.alphamapWidth / 2, 0, terrainData.alphamapWidth / 2), 10f);
	}

	// Main generation function
	// TODO have this function take a seed value and gnerate consistent results
	// TODO implement some degree of customization
	public void Generate()
	{
		Debug.Log("Starting Generation");
		//create base terrain
		foreach (PerlinParameters p in perlinParameters)
		{
			p.mPerlinOffsetX = UnityEngine.Random.Range(0, 1000);
			p.mPerlinOffsetZ = UnityEngine.Random.Range(0, 1000);
		}
		//add noise to make terrain look more natural
		//drop edges in natural fashion
		Islandize();
		RemoveInlandLakes(1);
		SplatMaps();
	}

	private void createVoronoi(int points = 3)
	{
		List<Vector2> voronoiPoints = new List<Vector2>();
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);


		while (voronoiPoints.Count < 3)
		{
			Vector2 randomPoint = new Vector2(UnityEngine.Random.Range(0, terrainData.alphamapResolution), UnityEngine.Random.Range(0, terrainData.alphamapResolution));
			if (heightMap[(int)randomPoint.x,(int)randomPoint.y] >= 0.19f)
			{
				voronoiPoints.Add(randomPoint);
			}
		}


	}
	public void Islandize()
	{
		Perlin(true);
		//MultiplePerlinTerrain();
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
		float[,] reductionMap = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
		float reductionScale = UnityEngine.Random.Range(0.001f, 0.0025f);
		float randomSeed = UnityEngine.Random.Range(0f, 10000);
		float maxDistance = Mathf.Sqrt(Mathf.Pow((0 - centrePos.x), 2f) + Mathf.Pow((0 - centrePos.y), 2f));


		//create reductionMap
		for (int x = 0; x <= terrainData.alphamapWidth; x++)
		{
			for (int z = 0; z <= terrainData.alphamapHeight; z++)
			{
				reductionMap[x, z] = TerrainUtils.fBM((x + randomSeed) * reductionScale, (z + randomSeed) * reductionScale, 3, 5) * 0.1f;
			}
		}

		for (int z = 0; z <= terrainData.alphamapHeight; z++)
		{
			for (int x = 0; x <= terrainData.alphamapWidth; x++)
			{
				float distanceFromCentre = DistanceFromCentre(new Vector2(x,z));
				//Debug.Log("Distance: " + distanceFromCentre + " Reduction: " + Mathf.Pow(TerrainUtils.Map(distanceFromCentre, 0, maxDistance, 0, 1) - reductionMap[x, z], 4f));
				heightMap[x, z] -= determineFalloff(TerrainUtils.Map(distanceFromCentre, 0, maxDistance, 0, 1)) + (reductionMap[x,z]);
				if (heightMap[x, z] > 0.1f) heightMap[x, z] = 0.1f;
			}

		}
		terrainData.SetHeights(0, 0, heightMap);

	}

	/// <summary>This method located removes all but r amount of inlandLakes</summary>
	/// <param name="r">Amount of inland lakes to keep</param>
	/// <param name="lakePoints">Can indlue a "short-list" amount of lakePoints to improve effeciency, passing entire heightmap will also work</param>
	private void RemoveInlandLakes(int r)
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
		bool[,] visited = new bool[terrainData.alphamapResolution, terrainData.alphamapResolution];

		List<List<Vector2>> lakes = new List<List<Vector2>>();
		for (int x = 0; x <= terrainData.alphamapWidth; x++)
		{
			for (int z = 0; z <= terrainData.alphamapHeight; z++)
			{
				if (heightMap[x,z] < waterHeight && visited[x,z] != true)
				{
					visited[x,z] = true;
					Vector2 cur = new Vector2(x, z);
					List<Vector2> neighbours = GenerateNeighbours(cur, terrainData.heightmapResolution, terrainData.heightmapResolution);
					List<Vector2> currentFeautre = new List<Vector2>() { cur };

					//get neighbours that are underwater
					foreach(Vector2 n in neighbours)
					{
						visited[(int)n.x, (int)n.y] = true;
						if (heightMap[(int)n.x,(int)n.y] < waterHeight)
						{
							currentFeautre.Add(n);
						}
					}
					lakes = addLake(currentFeautre, lakes);
				}
			}
		}
		Debug.Log(lakes.Count);
	}
	/// <summary>This method will check to see if any of the points from the first param
	/// exist in the any of lists in the second param, if so, marges the lists with overlapping values</summary>
	/// <param name="lakeToAdd">Current list we are wanting to add</param>
	/// <param name="currentLakes">Current double list of features/lakes </param>
	private List<List<Vector2>> addLake(List<Vector2> lakeToAdd, List<List<Vector2>> currentLakes)
	{
		//check if any neighbours already exits in a different lake, if so, merge them
		if (currentLakes.Count == 0)
		{
			currentLakes.Add(lakeToAdd);
			return currentLakes;
		}
		else
		{
			foreach (List<Vector2> lake in currentLakes)
			{
				foreach (Vector2 point in lakeToAdd)
				{
					// do any points in current lake already exist in a lake
					if (lake.Any(p => p.x == point.x && p.y == point.y))
					{
						lake.AddRange(lakeToAdd);
						return currentLakes;
					}
				}
			}
			currentLakes.Add(lakeToAdd);
			return currentLakes;

		}
	}


	private float determineFalloff(float x)
	{
		float a = 2.7f;
		float b = 3f;

		return Mathf.Pow(x, a) / ( Mathf.Pow(x, a) + Mathf.Pow((b - b * x),a));

	}

	//this is currently going to be used to get an accurate and edgecase proof list of shoreline points
	public void Beach()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);


	}


	private float DistanceFromCentre(Vector2 pos)
	{
		return Mathf.Sqrt(Mathf.Pow((pos.x - centrePos.x), 2f) + Mathf.Pow((pos.y - centrePos.y), 2f));
	}

	float[,] GetHeightMap(bool manualReset = false)
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

	public void Erode()
	{
		if (erosionType == ErosionType.Rain) Rain();
		else if (erosionType == ErosionType.Tidal) Tidal();
		else if (erosionType == ErosionType.Thermal) Thermal();
		else if (erosionType == ErosionType.River) River();
		else if (erosionType == ErosionType.Wind) Wind();

		SmoothAmount = erosionSmoothAmount;
		Smooth();
	}

	void Rain()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

		for (int i = 0; i < droplets; i++)
		{
			heightMap[UnityEngine.Random.Range(0, terrainData.alphamapWidth), UnityEngine.Random.Range(0, terrainData.alphamapHeight)] -= erosionStrength;
		}

		terrainData.SetHeights(0, 0, heightMap);
	}


	void Tidal()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

		for (int z = 0; z < terrainData.alphamapHeight; z++)
		{
			for (int x = 0; x < terrainData.alphamapWidth; x++)
			{
				//optimization
				if (heightMap[x, z] < (waterHeight + 0.02))
				{
					Vector2 thisLocation = new Vector2(x, z);
					List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.alphamapWidth, terrainData.alphamapHeight);

					foreach (Vector2 n in neighbours)
					{
						if (heightMap[x, z] < waterHeight && heightMap[(int)n.x, (int)n.y] > waterHeight)
						{
							heightMap[x, z] = waterHeight;
							heightMap[(int)n.x, (int)n.y] = waterHeight;
						}
					}
				}
			}
		}

		terrainData.SetHeights(0, 0, heightMap);
	}

	void Thermal()
	{
		for (int i = 0; i < 5; i++)
		{
			float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);

			for (int z = 0; z < terrainData.alphamapHeight; z++)
			{
				for (int x = 0; x < terrainData.alphamapWidth; x++)
				{
					Vector2 thisLocation = new Vector2(x, z);
					List<Vector2> neighbours = GenerateNeighbours(thisLocation, terrainData.alphamapWidth, terrainData.alphamapHeight);

					foreach (Vector2 n in neighbours)
					{
						if (heightMap[x, z] > heightMap[(int)n.x, (int)n.y] + erosionStrength)
						{
							float currentHeight = heightMap[x, z];
							heightMap[x, z] -= currentHeight * thermalStrength;
							heightMap[(int)n.x, (int)n.y] += currentHeight * thermalStrength;
						}
					}
				}
			}

			terrainData.SetHeights(0, 0, heightMap);
		}
	}

	void River()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
		float[,] erosionMap = new float[terrainData.alphamapWidth, terrainData.alphamapHeight];

		for (int i = 0; i < droplets; i++)
		{
			Vector2 dropletPosition = new Vector2(UnityEngine.Random.Range(0, terrainData.alphamapWidth), UnityEngine.Random.Range(0, terrainData.alphamapHeight));

			erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] = erosionStrength;

			for (int j = 0; j < springsPerRiver; j++)
			{
				erosionMap = RunRiver(dropletPosition, heightMap, erosionMap, terrainData.alphamapWidth, terrainData.alphamapHeight);
			}
		}

		for (int z = 0; z < terrainData.alphamapHeight; z++)
		{
			for (int x = 0; x < terrainData.alphamapWidth; x++)
			{
				if (erosionMap[x,z] > 0)
				{
					heightMap[x, z] -= erosionMap[x, z];
				}
			}
		}
		terrainData.SetHeights(0, 0, heightMap);

	}

	float[,] RunRiver(Vector3 dropletPosition, float[,] heightMap, float[,] erosionMap, int width, int height)
	{
		while (erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] > 0)
		{
			List<Vector2> neighbours = GenerateNeighbours(dropletPosition, width, height);
			neighbours.Shuffle();
			bool foundLower = false;
			foreach(Vector2 n in neighbours)
			{
				if (heightMap[(int)n.x, (int)n.y] < heightMap[(int)dropletPosition.x, (int)dropletPosition.y])
				{
					erosionMap[(int)n.x, (int)n.y] = erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] - solubility;
					dropletPosition = n;
					foundLower = true;
					break;
				}
			}
			if (!foundLower)
			{
				erosionMap[(int)dropletPosition.x, (int)dropletPosition.y] -= solubility;
			}
		}
		return erosionMap;
	}

	void Wind()
	{
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
		int width = terrainData.alphamapWidth;
		int height = terrainData.alphamapHeight;

		float WindDir = windDirection;
		float sinAngle = -Mathf.Sin(Mathf.Deg2Rad * WindDir);
		float cosAngle = Mathf.Cos(Mathf.Deg2Rad * WindDir);

		for (int z = -(height - 1)*2; z <= height*2; z+=10)
		{
			for (int x = -(width - 1)*2; x <= width*2; x+=1)
			{
				float thisNoise = (float)Mathf.PerlinNoise(x * 0.06f, z * 0.06f) * 20 * erosionStrength;
				int nx = (int)x;
				int digz = (int)z + (int)thisNoise;
				int nz = (int)z + 5 + (int)thisNoise; // this increment has to be smaller than the increment in the for loop, should be roughly half

				Vector2 digCoords = new Vector2(x * cosAngle - digz * sinAngle, digz * cosAngle + x * sinAngle);
				Vector2 pileCoords = new Vector2(nx * cosAngle - nz * sinAngle, nz * cosAngle + nx * sinAngle);

				if (!(pileCoords.x < 0 || pileCoords.x > (width - 1) || pileCoords.y < 0 || pileCoords.y > (height - 1) || (int)digCoords.x < 0 || (int)digCoords.x > (width - 1) || (int)digCoords.y < 0 || (int)digCoords.y > (height - 1)))
				{
					heightMap[(int)digCoords.x, (int)digCoords.y] -= 0.001f;
					heightMap[(int)pileCoords.x, (int)pileCoords.y] += 0.001f;
				}
			}
		}
		terrainData.SetHeights(0, 0, heightMap);
	}

	public void AddWater()
	{
		GameObject water = GameObject.Find("water");
		if (!water)
		{
			water = Instantiate(waterGO, this.transform.position, this.transform.rotation);
			water.name = "water";
		}
		water.transform.position = this.transform.position + new Vector3(terrainData.size.x, waterHeight * terrainData.size.y, terrainData.size.z / 2);
		water.transform.localScale = new Vector3(terrainData.size.x / 4, 1, terrainData.size.z / 4);
	}

	public void AddDetails()
	{
		DetailPrototype[] newDetailPrototypes;
		newDetailPrototypes = new DetailPrototype[details.Count];
		int dIndex = 0;
		float[,] heightMap = terrainData.GetHeights(0, 0, terrainData.alphamapWidth,terrainData.alphamapHeight);

		foreach (Detail d in details)
		{
			newDetailPrototypes[dIndex] = new DetailPrototype();
			newDetailPrototypes[dIndex].prototype = d.prototype;
			newDetailPrototypes[dIndex].prototypeTexture = d.prototypeTexture;
			newDetailPrototypes[dIndex].healthyColor = d.healthyColour;
			newDetailPrototypes[dIndex].dryColor = d.dryColour;
			//newDetailPrototypes[dIndex].minHeight = d.minHeight;
			//newDetailPrototypes[dIndex].maxHeight = d.maxHeight;
			newDetailPrototypes[dIndex].noiseSpread = d.feather;

			if (newDetailPrototypes[dIndex].prototype)
			{
				newDetailPrototypes[dIndex].usePrototypeMesh = true;
				newDetailPrototypes[dIndex].renderMode = DetailRenderMode.VertexLit;
			}
			else
			{
				newDetailPrototypes[dIndex].usePrototypeMesh = false;
				newDetailPrototypes[dIndex].renderMode = DetailRenderMode.GrassBillboard;
			}
			dIndex++;
		}
		terrainData.detailPrototypes = newDetailPrototypes;

		for (int i = 0; i < terrainData.detailPrototypes.Length; ++i)
		{
			int[,] detailMap = new int[terrainData.detailWidth, terrainData.detailHeight];
			for (int y = 0; y < terrainData.detailHeight; y += detailSpacing)
			{
				for (int x = 0; x < terrainData.detailWidth; x += detailSpacing)
				{
					if (UnityEngine.Random.Range(0.0f, 1.0f) > details[i].density) continue;

					int xHM = (int)(x / (float)terrainData.detailWidth * terrainData.size.x);
					int yHM = (int)(y / (float)terrainData.detailHeight * terrainData.size.z);

					float thisNoise = TerrainUtils.Map(Mathf.PerlinNoise(x * details[i].feather,
												y * details[i].feather), 0, 1, 0.5f, 1);
					float thisHeightStart = details[i].minHeight * thisNoise -
											details[i].overlap * thisNoise;
					float nextHeightStart = details[i].maxHeight * thisNoise +
											details[i].overlap * thisNoise;

					float thisHeight = heightMap[(int)(yHM / terrainData.size.z * terrainData.alphamapHeight),(int)(xHM / terrainData.size.x * terrainData.alphamapWidth)];
					float steepness = terrainData.GetSteepness(xHM / (float)terrainData.size.x, yHM / (float)terrainData.size.z);
					if ((thisHeight >= thisHeightStart && thisHeight <= nextHeightStart) &&
						(steepness >= details[i].minSlope && steepness <= details[i].maxSlope))
					{
						detailMap[y, x] = 1;
					}
				}
			}
			terrainData.SetDetailLayer(0, 0, i, detailMap);
		}
	}

	public void AddNewDetails()
	{
		details.Add(new Detail());
	}

	public void RemoveDetails()
	{
		List<Detail> keptDetails = new List<Detail>();
		for (int i = 0; i < details.Count; i++)
		{
			if (!details[i].remove)
			{
				keptDetails.Add(details[i]);
			}
		}
		if (keptDetails.Count == 0)
		{
			keptDetails.Add(details[0]);
		}
		details = keptDetails;
	}

	public void PlantVegetation()
	{
		TreePrototype[] newTreePrototypes;
		newTreePrototypes = new TreePrototype[vegetation.Count];
		int tindex = 0;
		foreach (Vegetation t in vegetation)
		{
			newTreePrototypes[tindex] = new TreePrototype();
			newTreePrototypes[tindex].prefab = t.prefab;
			tindex++;
		}
		terrainData.treePrototypes = newTreePrototypes;

		// CREATE TREE INSTANCES
		List<TreeInstance> allVegetation = new List<TreeInstance>();
		for (int z = 0; z < terrainData.size.z; z += treeSpacing)
		{
			for (int x = 0; x < terrainData.size.x; x += treeSpacing)
			{
				for (int tp = 0; tp < terrainData.treePrototypes.Length; tp++)
				{

					if (UnityEngine.Random.Range(0.0f, 1.0f) > vegetation[tp].density) continue;

					float thisHeight = terrainData.GetHeight(

							(int)(x * terrainData.alphamapWidth / terrainData.size.x),

							(int)(z * terrainData.alphamapHeight / terrainData.size.z))

							/ terrainData.size.y;
					//float thisHeight = terrainData.GetHeight(x, z) / terrainData.size.y;
					float thisHeightStart = vegetation[tp].minHeight;
					float thisHeightEnd = vegetation[tp].maxHeight;

					float steepness = terrainData.GetSteepness(x / (float)terrainData.size.x,
						z / (float)terrainData.size.z);


					if ((thisHeight >= thisHeightStart && thisHeight <= thisHeightEnd) &&
						(steepness >= vegetation[tp].minSlope && steepness <= vegetation[tp].maxSlope))
					{
						TreeInstance instance = new TreeInstance();
						instance.position = new Vector3((x + UnityEngine.Random.Range(-5.0f, 5.0f)) / terrainData.size.x,
							thisHeight,
							(z + UnityEngine.Random.Range(-5.0f, 5.0f)) / terrainData.size.z);

						// REAL TREE WORLD POS INSIDE TERRAIN
						Vector3 treeWorldPos = new Vector3(instance.position.x * terrainData.size.x,
							instance.position.y * terrainData.size.y,
							instance.position.z * terrainData.size.z) + transform.position;

						RaycastHit hit;
						int layerMask = 1 << terrainLayer;
						// If yes then we fix height tree position
						if (Physics.Raycast(treeWorldPos + new Vector3(0, 10, 0), -Vector3.up, out hit, 100, layerMask) ||
							Physics.Raycast(treeWorldPos - new Vector3(0, 10, 0), Vector3.up, out hit, 100, layerMask))
						{
							float treeHeight = (hit.point.y - transform.position.y) / terrainData.size.y;
							instance.position = new Vector3(instance.position.x, treeHeight, instance.position.z);
							instance.rotation = UnityEngine.Random.Range(vegetation[tp].minRotation, vegetation[tp].maxRotation);
							instance.prototypeIndex = tp;
							instance.color = Color.Lerp(vegetation[tp].colour1, vegetation[tp].colour2, UnityEngine.Random.Range(0.0f,1.0f));
							instance.lightmapColor = vegetation[tp].lightColour;
							float s = UnityEngine.Random.Range(vegetation[tp].minScale, vegetation[tp].maxScale);
							instance.heightScale = s;
							instance.widthScale = s;



							if ((instance.position.x >= 0 && instance.position.x <= 1) &&
							 (instance.position.z >= 0 && instance.position.z <= 1))
							{
								//Add this tree instance to the list
								allVegetation.Add(instance);
							}
							if (allVegetation.Count >= maxTrees) goto TREESDONE;
						}

					}
				}
			}
		}

	TREESDONE:
		terrainData.treeInstances = allVegetation.ToArray();
	}

	public void AddNewVegetation()
	{
		vegetation.Add(new Vegetation());
	}

	public void RemoveVegetation()
	{
		List<Vegetation> keptVegetation = new List<Vegetation>();
		for (int i = 0; i < vegetation.Count; i++)
		{
			if (!vegetation[i].remove)
			{
				keptVegetation.Add(vegetation[i]);
			}
		}
		if (keptVegetation.Count == 0)
		{
			keptVegetation.Add(vegetation[0]);
		}
		vegetation = keptVegetation;
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

	//uses algo, unity does it better
	// DEPRECATED
	float GetSteepness(float[,] heightmap, int x, int z, int width, int height)
	{
		float h = heightmap[x, z];
		int nx = x + 1;
		int nz = z + 1;

		//if on upper edge find gradient by going backwards
		if (nx > width - 1) nx = x - 1;
		if (nz > height - 1) nz = z - 1;

		float dx = heightmap[nx, z] - h;
		float dz = heightmap[x, nz] - h;
		Vector2 gradient = new Vector2(dx, dz);

		float steep = gradient.magnitude;

		return steep;
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

		for (int z = 0; z < terrainData.alphamapHeight; z++)
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
					float steepness = terrainData.GetSteepness(z / (float)terrainData.alphamapResolution, x / (float)terrainData.alphamapResolution);
					if ((heightMap[x,z] >= thisHeightStart && heightMap[x,z] <= thisHeightStop) && (steepness >= splatHeights[i].minSlope && steepness <= splatHeights[i].maxSlope))
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
	public void Perlin(bool modified = false)
	{
		float[,] heightMap = GetHeightMap();
		float min = float.MinValue;
		float max = float.MaxValue;
		modified = true; //REMOVE
		for (int x = 0; x < terrainData.heightmapResolution; x++)
		{
			for (int z = 0; z < terrainData.heightmapResolution; z++)
			{
				if (modified)
				{
					heightMap[x, z] += TerrainUtils.fBM((x + perlinOffsetX) * perlinXScale, (z + perlinOffsetZ) * perlinZScale, perlinOctaves, perlinPersistance) * perlinHeightScale;
				}
				else
				{
					heightMap[x, z] += TerrainUtils.fBM((x + perlinOffsetX) * perlinXScale, (z + perlinOffsetZ) * perlinZScale, perlinOctaves, perlinPersistance) * perlinHeightScale;
				}
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

	int AddTag(SerializedProperty tagsProp, string newTag, TagType tType)
	{
		bool found = false;
		//make sure the tag doesn't arleady exist
		for (int i = 0; i < tagsProp.arraySize; i++)
		{
			SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
			if (t.stringValue.Equals(newTag)) { found = true; return i; }
		}

		if (!found && tType == TagType.Tag)
		{
			tagsProp.InsertArrayElementAtIndex(0);
			SerializedProperty newTagProp = tagsProp.GetArrayElementAtIndex(0);
			newTagProp.stringValue = newTag;
		}
		else if (!found && tType == TagType.Layer)
		{
			// user layers begin at 8
			for (int j = 8; j < tagsProp.arraySize; j++)
			{
				SerializedProperty newLayer = tagsProp.GetArrayElementAtIndex(j);

				if (newLayer.stringValue == "")
				{
					newLayer.stringValue = newTag;
					return j;
				}
			}
		}
		return -1;
	}

}