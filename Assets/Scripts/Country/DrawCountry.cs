using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DrawCountry : MonoBehaviour
{
	public bool enableDraw;
	public bool drawAll;
	public string countryToDraw;
	public Country lastDrawn;
	public bool drawOnSphere;
	public bool drawCities;

	public void StartDraw(Country[] countries)
	{

		if (enableDraw)
		{
			foreach (Country country in countries)
			{
				if (drawAll || country.nameOfficial.ToLower() == countryToDraw.ToLower())
				{

					Draw(country);
					lastDrawn = country;
				}
			}
		}

	}



	void Draw(Country country)
	{
		foreach (Polygon polygon in country.shape.polygons)
		{
			DrawPolygon(polygon);
			if (polygon.paths.Length > 1)
			{
				//	Debug.Log(country.name);
			}
		}

		
		if (drawCities)
		{
			foreach (City city in country.cities)
			{
				var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
				g.transform.position = new Vector2(city.coordinate.longitude, city.coordinate.latitude);
				g.transform.localScale = Vector3.one * 0.5f;
				g.name = city.name;
			}
		}

	}

	void DrawPolygon(Polygon polygon)
	{
		bool isHole = polygon.paths.Length > 1;
		//Color col = new Color(Random.value, Random.value, Random.value);
		Color col = Color.green;
		foreach (CountryPath path in polygon.paths)
		{
			DrawPath(path, col);

		}
	}

	void DrawPath(CountryPath path, Color colour)
	{
		for (int i = 0; i < path.points.Length - 1; i++)
		{
			Coordinate coordA = path.points[i];
			Coordinate coordB = path.points[i + 1];
			float radius = 100.05f;

			Vector3 a = (drawOnSphere) ? CoordinateSystem.CoordinateToPoint(coordA, radius) : (Vector3)coordA.ToVector2();
			Vector3 b = (drawOnSphere) ? CoordinateSystem.CoordinateToPoint(coordB, radius) : (Vector3)coordB.ToVector2();
			Debug.DrawLine(a, b, colour, 1000);
		}
	}


}
