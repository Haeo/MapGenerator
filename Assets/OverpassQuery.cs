using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class OverpassQuery : MonoBehaviour
{
    // Overpass API URL
    private const string overpassAPIUrl = "http://overpass-api.de/api/interpreter";

    void Start()
    {
        StartCoroutine(ExecuteOverpassQuery());
    }

    IEnumerator ExecuteOverpassQuery()
    {
        // Overpass API로 쿼리 전송
        string query = "[out:json]; area[name=\"대구광역시\"]->.a; ( relation(area.a)[\"boundary\"=\"administrative\"][\"admin_level\"=\"4\"]; ); out body; >; out skel qt;";
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("data", query));

        UnityWebRequest www = UnityWebRequest.Post(overpassAPIUrl, formData);


        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error while requesting Overpass API: " + www.error);
        }
        else
        {
            // GeoJSON 데이터 처리
            ProcessGeoJSON(www.downloadHandler.text);
        }
    }

    void ProcessGeoJSON(string jsonText)
    {
        MapData mapData = JsonConvert.DeserializeObject<MapData>(jsonText);

        if (mapData != null && mapData.features != null)
        {
            foreach (Feature feature in mapData.features)
            {
                if (feature.geometry.type == "Polygon")
                {
                    List<Vector3> points = new List<Vector3>();

                    foreach (List<double> coord in feature.geometry.coordinates[0])
                    {
                        // 좌표를 Unity 좌표계로 변환
                        Vector3 point = new Vector3((float)coord[0], 0f, (float)coord[1]);
                        points.Add(point);
                    }

                    // 지도 경계 그리기
                    DrawMapBoundary(points);
                }
            }
        }
    }

    void DrawMapBoundary(List<Vector3> points)
    {
        GameObject mapBoundary = new GameObject("MapBoundary");
        LineRenderer lineRenderer = mapBoundary.AddComponent<LineRenderer>();

        lineRenderer.positionCount = points.Count;
        lineRenderer.SetPositions(points.ToArray());
        lineRenderer.loop = true;

        // 지도 경계 선 스타일 설정
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.black;
        lineRenderer.endColor = Color.black;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
    }

    [System.Serializable]
    public class MapData
    {
        public List<Feature> features;
    }

    [System.Serializable]
    public class Feature
    {
        public Geometry geometry;
    }

    [System.Serializable]
    public class Geometry
    {
        public string type;
        public List<List<List<double>>> coordinates;
    }
}
