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
    public List<Vector2> cityMapPoints = new List<Vector2>();

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
              relation(area.a)[""boundary""=""administrative""][""admin_level""=""4""];
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

            //List<Vector2> convexHullPoints = GetConvexHull(cityMapPoints);
            List<Vector2> convexHullPoints = GetConvexHull(cityMapPoints);
            Debug.Log(convexHullPoints);

            CreateCityMapObject(convexHullPoints);
        }
        //Debug.Log(string.Format("cityMapPoints(LoadMapData) : {0}", cityMapPoints.Count));
    }

    List<Vector2> FilterAndProcessData(string jsonData)
    {
        JObject parsed_data = JObject.Parse(jsonData);

        // ��� ����� ������ �浵�� ������ ����Ʈ�� �����մϴ�.
        // List<Vector2> boundaryNodes = new List<Vector2>();

        // �� ��Ҹ� ��ȸ�ϸ鼭 ��踦 ��Ÿ���� way�� ����� ������ �浵�� �����մϴ�.
        foreach (JToken element in parsed_data["elements"])
        {
            if (element["type"].ToString() == "node")
            {
                double lat = double.Parse(element["lat"].ToString());
                double lon = double.Parse(element["lon"].ToString());
                cityMapPoints.Add(new Vector2((float)lat, (float)lon));
            }
        }

        // // ����� ����մϴ�.
        // foreach (Vector2 node in cityMapPoints)
        // {
        //     Debug.Log("Latitude: " + node.x + ", Longitude: " + node.y);
        // }

        Debug.Log(string.Format("cityMapPoints(Filter) : {0}", cityMapPoints.Count));

        return cityMapPoints;
    }

    List<Vector2> GetConvexHull(List<Vector2> points)
    {
        List<Vector2> convexHull = new List<Vector2>();

        points.Sort((a, b) =>
        {
            if (a.y != b.y) return a.y.CompareTo(b.y);
            return a.x.CompareTo(b.x);
        });

        Stack<Vector2> hullStack = new Stack<Vector2>();

        foreach (Vector2 point in points)
        {
            while (hullStack.Count >= 2 && Orientation(hullStack.Peek(), hullStack.ElementAt(1), point) <= 0)
            {
                hullStack.Pop();
            }
            hullStack.Push(point);
        }

        // ���ÿ� ���� ������ ���� �ٰ����� �̷�� ����
        while (hullStack.Count > 0)
        {
            convexHull.Add(hullStack.Pop());
        }

        Debug.Log(string.Format("convecHull : {0}", convexHull.Count)); // üũ
        return convexHull;
    }

    private int Orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
        if (val == 0) return 0; // ������
        return (val > 0) ? 1 : -1; // �ð� ���� �Ǵ� �ݽð� ����
    }

    void CreateCityMapObject(List<Vector2> convexHullPoints)
    {
        //Debug.Log(string.Format("convexHullPoints : {0}", convexHullPoints.Count));
        Mesh mesh = CreatePolygonMesh(convexHullPoints);

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }

    Mesh CreatePolygonMesh(List<Vector2> points)
    {
        // �ٰ����� ������ �迭 ����
        Vector3[] vertices = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            vertices[i] = new Vector3(points[i].x, 0, points[i].y); // y ��ǥ�� 0���� ����
        }

        Debug.Log(string.Format("points : {0}", points.Count)); // üũ

        // �޽� Ʈ���̾ޱ�(�ﰢ��) ����
        int[] triangles = new int[(points.Count - 2) * 3];
        int index = 0;
        for (int i = 1; i < points.Count - 1; i++)
        {
            triangles[index++] = 0;
            triangles[index++] = i;
            triangles[index++] = i + 1;
        }

        // �޽� ����
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}

