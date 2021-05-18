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

	public static float Map(float value, float origonalMin, float origonalMax, float targetMin, float targetMax)
	{
		return (value - origonalMin) * (targetMax - targetMin) / (origonalMax - origonalMin) + targetMin;
	}

	//Fisher-Yates Shuffle
	public static System.Random r = new System.Random();
	public static void Shuffle<T>(this IList<T> list)
	{
		int n = list.Count;
		while (n > 1)
		{
			n--;
			int k = r.Next(n + 1);
			T value = list[k];
			list[k] = list[n];
			list[n] = value;
		}
	}
}
