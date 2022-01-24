using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Bounds2D
{
	public Vector2 min;
	public Vector2 max;

	public Bounds2D(Vector2[] points)
	{
		float minX = float.MaxValue;
		float minY = float.MaxValue;
		float maxX = float.MinValue;
		float maxY = float.MinValue;

		for (int i = 0; i < points.Length; i++)
		{
			Vector2 p = points[i];
			minX = Mathf.Min(minX, p.x);
			minY = Mathf.Min(minY, p.y);
			maxX = Mathf.Max(maxX, p.x);
			maxY = Mathf.Max(maxY, p.y);
		}

		min = new Vector2(minX, minY);
		max = new Vector2(maxX, maxY);
	}
}
