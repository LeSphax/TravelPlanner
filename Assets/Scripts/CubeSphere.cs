using UnityEngine;

public static class CubeSphere
{

	public static MeshData[] GenerateMeshes(int resolution, int numSubdivisions, float minLatitude, float maxLatitude, float minLongitude, float maxLongitude)
	{
		MeshData[] meshes = new MeshData[6 * numSubdivisions * numSubdivisions];
		Vector3[] faceNormals = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
		float faceCoveragePerSubFace = 1f / numSubdivisions;
		int meshIndex = 0;

		foreach (Vector3 faceNormal in faceNormals)
		{
			for (int y = 0; y < numSubdivisions; y++)
			{
				for (int x = 0; x < numSubdivisions; x++)
				{
					Vector2 startT = new Vector2(x, y) * faceCoveragePerSubFace;
					Vector2 endT = startT + Vector2.one * faceCoveragePerSubFace;

					meshes[meshIndex] = CreateFace(resolution, faceNormal, startT, endT, minLatitude, maxLatitude, minLongitude, maxLongitude);
					meshIndex++;
				}
			}
		}

		return meshes;
	}

	static MeshData CreateFace(int resolution, Vector3 normal, Vector2 startT, Vector2 endT, float minLatitude, float maxLatitude, float minLongitude, float maxLongitude)
	{
		int numVerts = resolution * resolution;
		int numTris = (resolution - 1) * (resolution - 1) * 6;
		int triIndex = 0;

		MeshData meshData = new MeshData(numVerts, numTris);

		Vector3 axisA = new Vector3(normal.y, normal.z, normal.x);
		Vector3 axisB = Vector3.Cross(normal, axisA);


		float ty = startT.y;
		float dx = (endT.x - startT.x) / (resolution - 1);
		float dy = (endT.y - startT.y) / (resolution - 1);

		for (int y = 0; y < resolution; y++)
		{
			float tx = startT.x;

			for (int x = 0; x < resolution; x++)
			{
				int i = x + y * resolution;
				Vector3 pointOnUnitCube = normal + (tx - 0.5f) * 2 * axisA + (ty - 0.5f) * 2 * axisB;
				Vector3 pointOnUnitSphere = PointOnCubeToPointOnSphere(pointOnUnitCube);

				float longitude_rad = Mathf.Atan2(pointOnUnitSphere.x, -pointOnUnitSphere.z);
				float latitude_rad = Mathf.Asin(pointOnUnitSphere.y);

				if (latitude_rad < maxLatitude && latitude_rad > minLatitude && longitude_rad < maxLongitude && longitude_rad > minLongitude) {

					meshData.vertices[i] = pointOnUnitSphere;

					if (x != resolution - 1 && y != resolution - 1)
					{
						meshData.triangles[triIndex] = i;
						meshData.triangles[triIndex + 1] = i + resolution + 1;
						meshData.triangles[triIndex + 2] = i + resolution;

						meshData.triangles[triIndex + 3] = i;
						meshData.triangles[triIndex + 4] = i + 1;
						meshData.triangles[triIndex + 5] = i + resolution + 1;
						triIndex += 6;
					}
				}
				tx += dx;
			}
			ty += dy;
		}
		return meshData;
	}

	// From http://mathproofs.blogspot.com/2005/07/mapping-cube-to-sphere.html
	static Vector3 PointOnCubeToPointOnSphere(Vector3 p)
	{
		float x2 = p.x * p.x / 2;
		float y2 = p.y * p.y / 2;
		float z2 = p.z * p.z / 2;
		float x = p.x * Mathf.Sqrt(1 - y2 - z2 + (p.y * p.y * p.z * p.z) / 3);
		float y = p.y * Mathf.Sqrt(1 - z2 - x2 + (p.x * p.x * p.z * p.z) / 3);
		float z = p.z * Mathf.Sqrt(1 - x2 - y2 + (p.x * p.x * p.y * p.y) / 3);
		return new Vector3(x, y, z);

	}

	public struct MeshData
	{
		public readonly Vector3[] vertices;
		public readonly int[] triangles;

		public MeshData(int numVerts, int numTris)
		{
			vertices = new Vector3[numVerts];
			triangles = new int[numTris];
		}
	}
}
