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

            // 데이터 필터링 및 처리 함수 호출
            cityMapPoints = FilterAndProcessData(jsonData);

            // 여기에 지도 오브젝트 생성
            // 여러 다각형을 하나의 지도 모양으로 만듭니다.
            List<Vector2> combinedMapPoints = new List<Vector2>();

            foreach (List<Vector2> polygonPoints in cityMapPoints)
            {
                // 다음 다각형을 연결하기 위해 마지막 점과 첫 번째 점을 연결합니다.
                if (combinedMapPoints.Count > 0 && polygonPoints.Count > 0)
                {
                    combinedMapPoints.Add(polygonPoints[0]);
                }

                // 현재 다각형의 점들을 추가합니다.
                combinedMapPoints.AddRange(polygonPoints);
            }

            // 여기에 지도 오브젝트 생성
            // CreateCityMapObject(cityMapPoints);
            CreateCityMapObject02(combinedMapPoints);
        }
    }

    List<List<Vector2>> FilterAndProcessData(string jsonData)
    {
        JObject parsed_data = JObject.Parse(jsonData);

        // JSON 데이터를 파싱합니다.
        JObject parsedData = JObject.Parse(jsonData);

        // node의 id를 키로 갖고 lat과 lon 값을 가지는 딕셔너리를 생성합니다.
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

        // way의 각 항목에 대해 nodes에 있는 노드의 id와 동일한 node의 lat과 lon 값을 추가합니다.
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

        // 결과를 출력합니다.
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
    //     // GameObject를 생성합니다.
    //     GameObject mapObject = new GameObject("CityMapObject");
    //     mapObject.transform.position = Vector3.zero;

    //     // MeshFilter와 MeshRenderer 컴포넌트를 추가합니다.
    //     MeshFilter meshFilter = mapObject.AddComponent<MeshFilter>();
    //     MeshRenderer meshRenderer = mapObject.AddComponent<MeshRenderer>();

    //     // Mesh를 생성합니다.
    //     Mesh mapMesh = new Mesh();

    //     // 정점들을 설정합니다.
    //     List<Vector3> verticesList = new List<Vector3>();
    //     foreach (Vector2 point in mapPoints)
    //     {
    //         verticesList.Add(new Vector3(point.x, 0, point.y));
    //     }
    //     Vector3[] vertices = verticesList.ToArray();
    //     mapMesh.vertices = vertices;

    //     // 다각형의 면을 구성하는 삼각형들을 설정합니다.
    //     List<int> trianglesList = new List<int>();
    //     for (int i = 1; i < mapPoints.Count - 1; i++)
    //     {
    //         // 입력된 각 변의 시작점, 중간 지점, 끝점을 사용하여 삼각형을 생성합니다.
    //         trianglesList.Add(0);
    //         trianglesList.Add(i);
    //         trianglesList.Add(i + 1);
    //     }
    //     int[] triangles = trianglesList.ToArray();
    //     mapMesh.triangles = triangles;

    //     // MeshCollider를 추가하여 충돌을 감지할 수 있도록 합니다.
    //     mapObject.AddComponent<MeshCollider>();

    //     // Mesh를 할당합니다.
    //     meshFilter.mesh = mapMesh;

    //     // 재질을 지정합니다. (여기서는 기본 재질을 사용합니다.)
    //     meshRenderer.material = new Material(Shader.Find("Standard"));
    // }

    void CreateCityMapObject02(List<Vector2> mapPoints)
    {
        // GameObject를 생성합니다.
        GameObject mapObject = new GameObject("CityMapObject");
        mapObject.transform.position = Vector3.zero;

        // 각각의 다각형을 선으로 그리기 위한 LineRenderer 배열을 생성합니다.
        LineRenderer[] lineRenderers = new LineRenderer[cityMapPoints.Count];

        // 각 다각형을 선으로 그립니다.
        for (int i = 0; i < cityMapPoints.Count; i++)
        {
            // 다각형의 점들을 가져옵니다.
            List<Vector2> polygonPoints = cityMapPoints[i];

            // LineRenderer를 생성하여 해당 다각형의 선을 그립니다.
            GameObject lineObject = new GameObject("Polygon" + i);
            lineObject.transform.parent = mapObject.transform; // 부모 설정

            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.black;
            lineRenderer.endColor = Color.black;
            lineRenderer.startWidth = 0.05f;
            lineRenderer.endWidth = 0.05f;
            lineRenderer.positionCount = polygonPoints.Count;

            // 다각형의 각 점을 LineRenderer에 추가합니다.
            for (int j = 0; j < polygonPoints.Count; j++)
            {
                Vector3 position = new Vector3(polygonPoints[j].x, 0, polygonPoints[j].y);
                lineRenderer.SetPosition(j, position);
            }

            // 생성한 LineRenderer를 배열에 저장합니다.
            lineRenderers[i] = lineRenderer;
        }
    }

    void CreateCityMapObject(List<List<Vector2>> cityMapPoints)
    {
        // GameObject를 생성합니다.
        GameObject mapObject = new GameObject("CityMapObject");
        mapObject.transform.position = Vector3.zero;

        // 각각의 다각형을 하나의 메시로 결합하기 위한 리스트를 생성합니다.
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        int vertexOffset = 0; // 메시의 정점 오프셋을 추적합니다.

        // 각 다각형의 선을 그리고 메시에 추가합니다.
        foreach (List<Vector2> polygonPoints in cityMapPoints)
        {
            // 다각형의 각 점을 메시에 추가합니다.
            foreach (Vector2 point in polygonPoints)
            {
                vertices.Add(new Vector3(point.x, 0, point.y));
            }

            // 메시의 삼각형을 설정합니다.
            for (int i = 1; i < polygonPoints.Count - 1; i++)
            {
                triangles.Add(vertexOffset);
                triangles.Add(vertexOffset + i);
                triangles.Add(vertexOffset + i + 1);
            }

            // 정점 오프셋을 증가시킵니다.
            vertexOffset += polygonPoints.Count;
        }

        // Mesh를 생성하고 정점과 삼각형을 할당합니다.
        Mesh cityMapMesh = new Mesh();
        cityMapMesh.vertices = vertices.ToArray();
        cityMapMesh.triangles = triangles.ToArray();

        // MeshCollider를 추가하여 충돌을 감지할 수 있도록 합니다.
        mapObject.AddComponent<MeshCollider>().sharedMesh = cityMapMesh;

        // MeshFilter와 MeshRenderer 컴포넌트를 추가합니다.
        MeshFilter meshFilter = mapObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = mapObject.AddComponent<MeshRenderer>();

        // Mesh를 할당합니다.
        meshFilter.mesh = cityMapMesh;

        // 재질을 지정합니다. (여기서는 기본 재질을 사용합니다.)
        meshRenderer.material = new Material(Shader.Find("Standard"));
    }
}

