using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;

public class TrainTrack {
    public Vector2[] points;
    public float[] pointDistance;

    public float length;
    public GameObject rendererGameObject;

    public TrainTrack(Vector2[] points) {
        if (points == null || points.Length < 2) {
            Debug.LogError("Train tracks must contain atleast 2 points (and exist)");
            return;
        }

        length = 0;
        Vector2 previousPoint = points[0];

        this.points = new Vector2[points.Length];
        points.CopyTo(this.points, 0);
        pointDistance = new float[points.Length];

        rendererGameObject = new("Track renderer", typeof(LineRenderer));
        LineRenderer lineRenderer = rendererGameObject.GetComponent<LineRenderer>();

        lineRenderer.useWorldSpace = true;
        lineRenderer.textureMode = LineTextureMode.Tile;
        lineRenderer.sortingLayerName = "Map";
        lineRenderer.sortingOrder = 4;
        lineRenderer.widthCurve = AnimationCurve.Linear(0, 3f, 1, 3f);
        lineRenderer.textureScale = new(0.33f, 1f);

        Material mat = new(AssetLoader.GetAsset<Material>("track material"));
        mat.mainTexture = AssetLoader.GetSprite("track").texture;
        lineRenderer.material = mat;

        lineRenderer.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++) {
            float distance = Vector2.Distance(points[i], previousPoint);
            previousPoint = points[i];
            lineRenderer.SetPosition(i, points[i]);

            length += distance;
            pointDistance[i] = length;
        }
    }

    public Vector2 GetNextPoint(float distance, bool isGoingForward) {
        int index = 0;
        for (int i = 0; i < pointDistance.Length; i++) if (pointDistance[i] < distance) index = Mathf.Clamp(i + (isGoingForward ? 0 : 1), 0, pointDistance.Length - 1);
        return points[index];
    }

    public Vector2 GetPositionAtDistance(float distance) {
        int index = 0;
        for (int i = 0; i < pointDistance.Length; i++) if (pointDistance[i] < distance) index = i;

        if (pointDistance.Length - 1 <= index) return points[pointDistance.Length - 1];

        float a = pointDistance[index];
        float b = pointDistance[index + 1];

        float percent = (distance - a) / (b - a);

        return Vector2.Lerp(points[index], points[index + 1], percent);
    }

    public void Destroy() {
        Object.Destroy(rendererGameObject);
    }
}