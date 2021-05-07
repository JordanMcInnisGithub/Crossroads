using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class TerrainUtils
{
	// brownian motion currently only used for perlin noise generation
	public static float fBM(float x, float z, int oct, float persistance)
	{
		float total = 0;
		float frequency = 1;
		float amplitude = 1;
		float maxValue = 0;

		for (int i = 0; i < oct; i++)
		{
			total += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
			maxValue += amplitude;
			amplitude *= persistance;
			frequency *= 2; // this should be experminted with , possibly made a param
		}

		return total / maxValue;
	}
}
