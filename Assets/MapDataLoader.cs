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

        Debug.Log(string.Format("cityMapPointsStart : {0}", cityMapPoints.Count)); // 체크

        // List<Vector2> convexHullPoints = GetConvexHull(cityMapPoints);

        // CreateCityMapObject(convexHullPoints);
    }

    IEnumerator LoadMapData()
    {
        string query = @"
            [out:json];
            area[""name""=""대구광역시""]->.a;
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

            // 데이터 필터링 및 처리 함수 호출
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

        // 경계 노드의 위도와 경도를 저장할 리스트를 생성합니다.
        // List<Vector2> boundaryNodes = new List<Vector2>();

        // 각 요소를 순회하면서 경계를 나타내는 way의 노드의 위도와 경도를 추출합니다.
        foreach (JToken element in parsed_data["elements"])
        {
            if (element["type"].ToString() == "node")
            {
                double lat = double.Parse(element["lat"].ToString());
                double lon = double.Parse(element["lon"].ToString());
                cityMapPoints.Add(new Vector2((float)lat, (float)lon));
            }
        }

        // // 결과를 출력합니다.
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

        // 스택에 남은 점들이 볼록 다각형을 이루는 점들
        while (hullStack.Count > 0)
        {
            convexHull.Add(hullStack.Pop());
        }

        Debug.Log(string.Format("convecHull : {0}", convexHull.Count)); // 체크
        return convexHull;
    }

    private int Orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
        if (val == 0) return 0; // 일직선
        return (val > 0) ? 1 : -1; // 시계 방향 또는 반시계 방향
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
        // 다각형의 꼭짓점 배열 생성
        Vector3[] vertices = new Vector3[points.Count];
        for (int i = 0; i < points.Count; i++)
        {
            vertices[i] = new Vector3(points[i].x, 0, points[i].y); // y 좌표는 0으로 설정
        }

        Debug.Log(string.Format("points : {0}", points.Count)); // 체크

        // 메쉬 트라이앵글(삼각형) 생성
        int[] triangles = new int[(points.Count - 2) * 3];
        int index = 0;
        for (int i = 1; i < points.Count - 1; i++)
        {
            triangles[index++] = 0;
            triangles[index++] = i;
            triangles[index++] = i + 1;
        }

        // 메쉬 생성
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }
}

