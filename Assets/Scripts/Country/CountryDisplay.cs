using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using System.Linq;

public class CountryDisplay : MonoBehaviour
{
	const int outlineKernel = 0;
	const int countryFillKernel = 1;
	const int countryDisplayUpdateKernel = 2;

	public int width;
	public ComputeShader countryDisplayCompute;
	public float lineThickness;
	public float lineSmoothingSize;
	public Transform player;
	public float countryFadeSpeed = 3;
	public FilterMode filterMode = FilterMode.Bilinear;

	public RenderTexture countryDataTexture;
	public event System.Action<RenderTexture> OnTextureCreated;
	public MeshRenderer display;

	public bool update = true;
	public bool saveToFile;

	ComputeBuffer countryDisplayStrengths;
	Country[] countries;

	void Start()
	{
		var format = RenderTextureFormat.ARGBFloat;
		ComputeHelper.CreateRenderTexture(ref countryDataTexture, width, width / 2, filterMode, format, "Country Data");

		countries = FindObjectOfType<CountryLoader>().countries;
		Run();
		OnTextureCreated?.Invoke(countryDataTexture);

		if (display != null)
		{
			display.material.mainTexture = countryDataTexture;
		}

		if (saveToFile)
		{
			SaveToFile();
		}

		countryDisplayStrengths = new ComputeBuffer(countries.Length, sizeof(float));
		float[] testData = new float[countries.Length];
		for (int i = 0; i < testData.Length; i++)
		{
			testData[i] = 0;
		}
		countryDisplayStrengths.SetData(testData);
		countryDisplayCompute.SetBuffer(countryDisplayUpdateKernel, "CountryDisplayStrength", countryDisplayStrengths);
		countryDisplayCompute.SetTexture(countryDisplayUpdateKernel, "CountryData", countryDataTexture);


	}
	void Update()
	{
		if (update)
		{
			float lookaheadDst = 1;
			Vector3 p = (player.position + player.forward * lookaheadDst).normalized;
			Coordinate longLat = CoordinateSystem.PointToCoordinate(p);
			countryDisplayCompute.SetInt("numCountries", countries.Length);
			countryDisplayCompute.SetFloat("deltaTime", Time.deltaTime * countryFadeSpeed);
			countryDisplayCompute.SetVector("playerLongitudeLatitude", longLat.ToVector2());
			ComputeHelper.Dispatch(countryDisplayCompute, countries.Length, 1, 1, countryDisplayUpdateKernel);


			FindObjectOfType<WorldGenerator>().material.SetBuffer("CountryDisplayStrengths", countryDisplayStrengths);
		}
	}

	void OnDestroy()
	{
		countryDisplayStrengths.Release();
	}

	public void Run()
	{

		var metaData = new List<PolygonMetaData>();
		var points = new List<Vector2>();

		for (int i = 0; i < countries.Length; i++)
		{
			Country country = countries[i];
			foreach (Polygon polygon in country.shape.polygons)
			{
				PolygonMetaData meta = new PolygonMetaData();

				meta.countryIndex = i;
				meta.bufferOffset = points.Count;
				Coordinate[] coordPath = polygon.paths[0].points;
				Vector2[] path = new Vector2[coordPath.Length];
				for (int j = 0; j < path.Length; j++)
				{
					path[j] = new Vector2(coordPath[j].longitude * Mathf.Rad2Deg, coordPath[j].latitude * Mathf.Rad2Deg);
				}
				Bounds2D bounds = new Bounds2D(path);
				meta.boundsMax = bounds.max + Vector2.one * lineThickness;
				meta.boundsMin = bounds.min - Vector2.one * lineThickness;

				points.AddRange(path);
				meta.length = path.Length;
				metaData.Add(meta);
			}
		}

		ComputeBuffer pointBuffer = new ComputeBuffer(points.Count, sizeof(float) * 2);
		pointBuffer.SetData(points);


		ComputeBuffer metadataBuffer = null;
		ComputeHelper.CreateStructuredBuffer(ref metadataBuffer, metaData.ToArray());


		countryDisplayCompute.SetInt("width", countryDataTexture.width);
		countryDisplayCompute.SetInt("height", countryDataTexture.height);
		countryDisplayCompute.SetInt("numPoints", pointBuffer.count);
		countryDisplayCompute.SetInt("numMeta", metadataBuffer.count);

		// Line
		countryDisplayCompute.SetBuffer(outlineKernel, "Meta", metadataBuffer);
		countryDisplayCompute.SetBuffer(outlineKernel, "Points", pointBuffer);
		countryDisplayCompute.SetTexture(outlineKernel, "CountryData", countryDataTexture);
		countryDisplayCompute.SetFloat("lineThickness", lineThickness);
		countryDisplayCompute.SetFloat("lineSmoothingSize", lineSmoothingSize);
		ComputeHelper.Dispatch(countryDisplayCompute, countryDataTexture.width, countryDataTexture.height, 1, outlineKernel);

		// Fill
		countryDisplayCompute.SetBuffer(countryFillKernel, "Points", pointBuffer);
		countryDisplayCompute.SetTexture(countryFillKernel, "CountryData", countryDataTexture);
		countryDisplayCompute.SetBuffer(countryFillKernel, "Meta", metadataBuffer);
		ComputeHelper.Dispatch(countryDisplayCompute, countryDataTexture.width, countryDataTexture.height, 1, countryFillKernel);

		pointBuffer.Release();
		metadataBuffer.Release();
	}

	void SaveToFile()
	{
		Texture2D tex = new Texture2D(countryDataTexture.width, countryDataTexture.height, GraphicsFormat.R32G32B32A32_SFloat, 0);
		tex.filterMode = countryDataTexture.filterMode;
		var prev = RenderTexture.active;
		RenderTexture.active = countryDataTexture;
		tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
		tex.Apply();
		RenderTexture.active = prev;

		byte[] bytes = tex.EncodeToPNG();
		System.IO.File.WriteAllBytes("C:\\Users\\sebas\\Desktop\\Test.png", bytes);
		Destroy(tex);
	}


	struct PolygonMetaData
	{
		public int countryIndex;
		public Vector2 boundsMin;
		public Vector2 boundsMax;
		public int bufferOffset;
		public int length;
	}
}
