using UnityEngine;

[System.Serializable]
public class Country
{
	public string nameOfficial;
	public string name;
	public string abbreviation;

	public string continent;
	public string alpha3Code;
	public int population;

	// sorted by population (highest to lowest)
	public City[] cities;
	public Shape shape;
}

[System.Serializable]
public class City
{
	public string name;
	public int population;
	public Coordinate coordinate;

	public string countryName;
	public string countryAlpha3Code;
	public bool inAmericanState;
	public string americanStateName;
}

[System.Serializable]
public struct Shape
{
	public Polygon[] polygons;
}

[System.Serializable]
public struct Polygon
{
	// First path is the outline of the polygon, any subsequent paths are holes to be cut out
	public CountryPath[] paths;
}
[System.Serializable]
public struct CountryPath
{
	public Coordinate[] points;
}
