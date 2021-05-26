using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

	public static IList<Vector2> GrahamScan (IList<Vector2> initialPoints)
	{
		if (initialPoints.Count < 2)
			return initialPoints;

		// find the smallest y, minimizing for x also when tied
		int iMin = Enumerable.Range(0, initialPoints.Count).Aggregate((jMin, jCur) =>
		{
			if (initialPoints[jCur].y < initialPoints[jMin].y)
				return jCur;
			if (initialPoints[jCur].y > initialPoints[jMin].y)
				return jMin;
			if (initialPoints[jCur].x < initialPoints[jMin].x)
				return jCur;
			return jMin;
		});

		// sort by polar angles from iMin
		var sortQuery = Enumerable.Range(0, initialPoints.Count)
			.Where((i) => (i != iMin))
			.Select((i) => new KeyValuePair<double, Vector2>(Mathf.Atan2(initialPoints[i].y - initialPoints[iMin].y, initialPoints[i].x - initialPoints[iMin].x), initialPoints[i]))
			.OrderBy((pair) => pair.Key)
			.Select((pair) => pair.Value);

		List<Vector2> points = new List<Vector2>(initialPoints.Count);
		points.Add(initialPoints[iMin]); // add initial point
		points.AddRange(sortQuery); //add sorted points

		int M = 0;

		for (int i = 1, N = points.Count; i < N; i++)
		{
			bool keepNewPoint = true;
			if (M == 0)
			{
				// Find at least one point not coincident with points[0]
				keepNewPoint = !NearlyEqual(points[0], points[i]);
			}
			else
			{
				while (true)
				{
					var flag = WhichToRemoveFromBoundary(points[M - 1], points[M], points[i]);
					if (flag == RemovalFlag.None)
						break;
					else if (flag == RemovalFlag.MidPoint)
					{
						if (M > 0)
							M--;
						if (M == 0)
							break;
					}
					else if (flag == RemovalFlag.EndPoint)
					{
						keepNewPoint = false;
						break;
					}
					else
						throw new Exception("Unknown RemovalFlag");
				}
			}
			if (keepNewPoint)
			{
				M++;
				Swap(points, M, i);
			}
		}
		// points[M] is now the last point in the boundary.  Remove the remainder.
		points.RemoveRange(M + 1, points.Count - M - 1);
		return points;
	}
	static void Swap<T>(IList<T> list, int i, int j)
	{
		if (i != j)
		{
			T temp = list[i];
			list[i] = list[j];
			list[j] = temp;
		}
	}

	public static double RelativeTolerance { get { return 1e-10; } }

	public static bool NearlyEqual(Vector3 a, Vector3 b)
	{
		return NearlyEqual(a.x, b.x) && NearlyEqual(a.y, b.y);
	}

	public static bool NearlyEqual(double a, double b)
	{
		return NearlyEqual(a, b, RelativeTolerance);
	}

	public static bool NearlyEqual(double a, double b, double epsilon)
	{
		// See here: http://floating-point-gui.de/errors/comparison/
		if (a == b)
		{ // shortcut, handles infinities
			return true;
		}

		double absA = Math.Abs(a);
		double absB = Math.Abs(b);
		double diff = Math.Abs(a - b);
		double sum = absA + absB;
		if (diff < 4 * double.Epsilon || sum < 4 * double.Epsilon)
			// a or b is zero or both are extremely close to it
			// relative error is less meaningful here
			return true;

		// use relative error
		return diff / (absA + absB) < epsilon;
	}

	static double CCW(Vector3 p1, Vector3 p2, Vector3 p3)
	{
		// Compute (p2 - p1) X (p3 - p1)
		double cross1 = (p2.x - p1.x) * (p3.y - p1.y);
		double cross2 = (p2.y - p1.y) * (p3.x - p1.x);
		if (NearlyEqual(cross1, cross2))
			return 0;
		return cross1 - cross2;
	}

	enum RemovalFlag
	{
		None,
		MidPoint,
		EndPoint
	};

	static RemovalFlag WhichToRemoveFromBoundary(Vector3 p1, Vector3 p2, Vector3 p3)
	{
		var cross = CCW(p1, p2, p3);
		if (cross < 0)
			// Remove p2
			return RemovalFlag.MidPoint;
		if (cross > 0)
			// Remove none.
			return RemovalFlag.None;
		// Check for being reversed using the dot product off the difference vectors.
		var dotp = (p3.x - p2.x) * (p2.x - p1.x) + (p3.y - p2.y) * (p2.y - p1.y);
		if (NearlyEqual(dotp, 0.0))
			// Remove p2
			return RemovalFlag.MidPoint;
		if (dotp < 0)
			// Remove p3
			return RemovalFlag.EndPoint;
		else
			// Remove p2
			return RemovalFlag.MidPoint;
	}
}
