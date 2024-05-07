using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System;
using JetBrains.Annotations;
using System.Linq;

public class MapDataLoader : MonoBehaviour
{
    private string overpassAPIURL = "https://overpass-api.de/api/interpreter?data=";
    private string jsonData;

    [SerializeField]
    public List<List<Vector2>> cityMapPoints = new List<List<Vector2>>();

    void Start()
    {
        StartCoroutine(LoadMapData());

        Debug.Log(string.Format("cityMapPointsStart : {0}", cityMapPoints.Count)); // üũ

        // List<Vector2> convexHullPoints = GetConvexHull(cityMapPoints);

        // CreateCityMapObject(convexHullPoints);
    }

    IEnumerator LoadMapData()
    {
        string query = @"
            [out:json];
            area[""name""=""�뱸������""]->.a;
            (
              relation(area.a)[""boundary""=""administrative""][""admin_level""=""6""];
            );
            out body;
            >;
            out skel qt;";
        overpassAPIURL += UnityWebRequest.EscapeURL(query);

        UnityWebRequest request = UnityWebRequest.Get(overpassAPIURL);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to load map data: " + request.error);
        }
        else
        {
            jsonData = request.downloadHandler.text;
            Debug.Log("Map data loaded successfully:\n" + jsonData);

            // ������ ���͸� �� ó�� �Լ� ȣ��
            cityMapPoints = FilterAndProcessData(jsonData);

            // ���⿡ ���� ������Ʈ ����
            // ���� �ٰ����� �ϳ��� ���� ������� ����ϴ�.
            List<Vector2> combinedMapPoints = new List<Vector2>();

            foreach (List<Vector2> polygonPoints in cityMapPoints)
            {
                // ���� �ٰ����� �����ϱ� ���� ������ ���� ù ��° ���� �����մϴ�.
                if (combinedMapPoints.Count > 0 && polygonPoints.Count > 0)
                {
                    combinedMapPoints.Add(polygonPoints[0]);
                }

                // ���� �ٰ����� ������ �߰��մϴ�.
                combinedMapPoints.AddRange(polygonPoints);
            }

            // ���⿡ ���� ������Ʈ ����
            // CreateCityMapObject(cityMapPoints);
            CreateCityMapObject02(combinedMapPoints);
        }
    }

    List<List<Vector2>> FilterAndProcessData(string jsonData)
    {
        JObject parsed_data = JObject.Parse(jsonData);

        // JSON �����͸� �Ľ��մϴ�.
        JObject parsedData = JObject.Parse(jsonData);

        // node�� id�� Ű�� ���� lat�� lon ���� ������ ��ųʸ��� �����մϴ�.
        Dictionary<long, Vector2> nodeDict = new Dictionary<long, Vector2>();
        foreach (JToken element in parsedData["elements"])
        {
            if (element["type"].ToString() == "node")
            {
                long id = long.Parse(element["id"].ToString());
                float lat = float.Parse(element["lat"].ToString());
                float lon = float.Parse(element["lon"].ToString());
                nodeDict.Add(id, new Vector2(lat * 10, lon * 10));
            }
        }

        // way�� �� �׸� ���� nodes�� �ִ� ����� id�� ������ node�� lat�� lon ���� �߰��մϴ�.
        List<List<Vector2>> wayNodesList = new List<List<Vector2>>();
        foreach (JToken element in parsedData["elements"])
        {
            if (element["type"].ToString() == "way")
            {
                List<Vector2> latLonList = new List<Vector2>();
                foreach (long nodeId in element["nodes"])
                {
                    if (nodeDict.ContainsKey(nodeId))
                    {
                        latLonList.Add(nodeDict[nodeId]);
                    }
                }
                wayNodesList.Add(latLonList);
            }
        }

        // ����� ����մϴ�.
        foreach (List<Vector2> latLonList in wayNodesList)
        {
            string output = "[";
            foreach (Vector2 latLon in latLonList)
            {
                output += "[" + latLon.x + ", " + latLon.y + "], ";
            }
            output = output.TrimEnd(' ', ',') + "]";
            Debug.Log(output);
        }

        return wayNodesList;
    }

    // void CreateCityMapObject(List<Vector2> mapPoints)
    // {
    //     // GameObject�� �����մϴ�.
    //     GameObject mapObject = new GameObject("CityMapObject");
    //     mapObject.transform.position = Vector3.zero;

    //     // MeshFilter�� MeshRenderer ������Ʈ�� �߰��մϴ�.
    //     MeshFilter meshFilter = mapObject.AddComponent<MeshFilter>();
    //     MeshRenderer meshRenderer = mapObject.AddComponent<MeshRenderer>();

    //     // Mesh�� �����մϴ�.
    //     Mesh mapMesh = new Mesh();

    //     // �������� �����մϴ�.
    //     List<Vector3> verticesList = new List<Vector3>();
    //     foreach (Vector2 point in mapPoints)
    //     {
    //         verticesList.Add(new Vector3(point.x, 0, point.y));
    //     }
    //     Vector3[] vertices = verticesList.ToArray();
    //     mapMesh.vertices = vertices;

    //     // �ٰ����� ���� �����ϴ� �ﰢ������ �����մϴ�.
    //     List<int> trianglesList = new List<int>();
    //     for (int i = 1; i < mapPoints.Count - 1; i++)
    //     {
    //         // �Էµ� �� ���� ������, �߰� ����, ������ ����Ͽ� �ﰢ���� �����մϴ�.
    //         trianglesList.Add(0);
    //         trianglesList.Add(i);
    //         trianglesList.Add(i + 1);
    //     }
    //     int[] triangles = trianglesList.ToArray();
    //     mapMesh.triangles = triangles;

    //     // MeshCollider�� �߰��Ͽ� �浹�� ������ �� �ֵ��� �մϴ�.
    //     mapObject.AddComponent<MeshCollider>();

    //     // Mesh�� �Ҵ��մϴ�.
    //     meshFilter.mesh = mapMesh;

    //     // ������ �����մϴ�. (���⼭�� �⺻ ������ ����մϴ�.)
    //     meshRenderer.material = new Material(Shader.Find("Standard"));
    // }

    void CreateCityMapObject02(List<Vector2> mapPoints)
    {
        // GameObject�� �����մϴ�.
        GameObject mapObject = new GameObject("CityMapObject");
        mapObject.transform.position = Vector3.zero;

        // ������ �ٰ����� ������ �׸��� ���� LineRenderer �迭�� �����մϴ�.
        LineRenderer[] lineRenderers = new LineRenderer[cityMapPoints.Count];

        // �� �ٰ����� ������ �׸��ϴ�.
        for (int i = 0; i < cityMapPoints.Count; i++)
        {
            // �ٰ����� ������ �����ɴϴ�.
            List<Vector2> polygonPoints = cityMapPoints[i];

            // LineRenderer�� �����Ͽ� �ش� �ٰ����� ���� �׸��ϴ�.
            GameObject lineObject = new GameObject("Polygon" + i);
            lineObject.transform.parent = mapObject.transform; // �θ� ����

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.black;
            lineRenderer.endColor = Color.black;
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.positionCount = polygonPoints.Count;

            // �ٰ����� �� ���� LineRenderer�� �߰��մϴ�.
            for (int j = 0; j < polygonPoints.Count; j++)
            {
                Vector3 position = new Vector3(polygonPoints[j].x, 0, polygonPoints[j].y);
                lineRenderer.SetPosition(j, position);
            }

            // ������ LineRenderer�� �迭�� �����մϴ�.
            lineRenderers[i] = lineRenderer;
        }
    }

    void CreateCityMapObject(List<List<Vector2>> cityMapPoints)
    {
        // GameObject�� �����մϴ�.
        GameObject mapObject = new GameObject("CityMapObject");
        mapObject.transform.position = Vector3.zero;

        // ������ �ٰ����� �ϳ��� �޽÷� �����ϱ� ���� ����Ʈ�� �����մϴ�.
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int vertexOffset = 0; // �޽��� ���� �������� �����մϴ�.

        // �� �ٰ����� ���� �׸��� �޽ÿ� �߰��մϴ�.
        foreach (List<Vector2> polygonPoints in cityMapPoints)
        {
            // �ٰ����� �� ���� �޽ÿ� �߰��մϴ�.
            foreach (Vector2 point in polygonPoints)
            {
                vertices.Add(new Vector3(point.x, 0, point.y));
            }

            // �޽��� �ﰢ���� �����մϴ�.
            for (int i = 1; i < polygonPoints.Count - 1; i++)
            {
                triangles.Add(vertexOffset);
                triangles.Add(vertexOffset + i);
                triangles.Add(vertexOffset + i + 1);
            }

            // ���� �������� ������ŵ�ϴ�.
            vertexOffset += polygonPoints.Count;
        }

        // Mesh�� �����ϰ� ������ �ﰢ���� �Ҵ��մϴ�.
        Mesh cityMapMesh = new Mesh();
        cityMapMesh.vertices = vertices.ToArray();
        cityMapMesh.triangles = triangles.ToArray();

        // MeshCollider�� �߰��Ͽ� �浹�� ������ �� �ֵ��� �մϴ�.
        mapObject.AddComponent<MeshCollider>().sharedMesh = cityMapMesh;

        // MeshFilter�� MeshRenderer ������Ʈ�� �߰��մϴ�.
        MeshFilter meshFilter = mapObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = mapObject.AddComponent<MeshRenderer>();

        // Mesh�� �Ҵ��մϴ�.
        meshFilter.mesh = cityMapMesh;

        // ������ �����մϴ�. (���⼭�� �⺻ ������ ����մϴ�.)
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }
}

