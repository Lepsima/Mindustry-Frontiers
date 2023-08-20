using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Assets;
using Frontiers.Content;

public class TrackPlacerTool {
    public LineRenderer previewRenderer;
    public Vector3[] points;

    public Vector2 startPos = Vector2.zero;
    public Vector2 startDir = Vector3.up;

    public Vector2 endDir;

    public TrainTrack editingTrack;

    public void Init() {
        previewRenderer = new GameObject("Track preview", typeof(LineRenderer)).GetComponent<LineRenderer>();

        previewRenderer.useWorldSpace = true;
        previewRenderer.textureMode = LineTextureMode.Tile;
        previewRenderer.sortingLayerName = "Map";
        previewRenderer.sortingOrder = 4;
        previewRenderer.widthCurve = AnimationCurve.Linear(0, 3f, 1, 3f);
        previewRenderer.textureScale = new(0.33f, 1f);

        Material mat = new(AssetLoader.GetAsset<Material>("track material"));
        mat.mainTexture = AssetLoader.GetSprite("track").texture;
        previewRenderer.material = mat;
        previewRenderer.startColor = previewRenderer.endColor = new Color(0, 1, 1, 0.5f);
    }

    public void Update() {
        Vector2 mousePos = PlayerManager.mousePos;
        Vector2 mouseDirection = (mousePos - startPos).normalized;

        float straightTrackPrecision = 0.9f;
        float anglePercent = 1 - Vector2.Angle(mouseDirection, startDir.normalized) / 180f;
        float distance = Vector2.Distance(mousePos, startPos);

        if (anglePercent > straightTrackPrecision) {
            previewRenderer.positionCount = 2;

            Vector2 endPos = Vector3.Project(mouseDirection * distance, startDir) + (Vector3)startPos;

            previewRenderer.SetPosition(0, startPos);
            previewRenderer.SetPosition(1, endPos);

            points = new Vector3[2] { startPos, endPos };

            // Caluclate the end direction
            endDir = (endPos - startPos).normalized;
        } else {
            // Create the guide point for the start pos
            Vector2 startPosBezier = startPos + (distance * 0.4f * startDir);

            // Get the angle of the curve
            float offsetAngle = Mathf.Deg2Rad * Vector2.SignedAngle(Vector3.up, startDir);
            float angle = offsetAngle + Mathf.Deg2Rad * (Vector2.SignedAngle(startDir, mousePos - startPos) * 2f - 90f);

            // Create the guide pos for the end position
            Vector2 mousePosBezier = mousePos + (distance * 0.4f * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));

            // Calculate the amount of points
            int points = Mathf.RoundToInt(Mathf.Abs(angle) * 10f + distance / 1.5f + 6);

            // Initialize arrays
            previewRenderer.positionCount = points + 1;
            this.points = new Vector3[points + 1];

            // Create bezier curve
            for (int i = 0; i < points; i++) {
                this.points[i] = Bezier(startPos, startPosBezier, mousePosBezier, mousePos, i / (float)points);
                previewRenderer.SetPosition(i, this.points[i]);
            }

            // Set last point
            Vector3 lastPoint = Bezier(startPos, startPosBezier, mousePosBezier, mousePos, 1);
            previewRenderer.SetPosition(points, lastPoint);
            this.points[points] = lastPoint;

            // Caluclate the end direction
            endDir = (this.points[points] - this.points[points - 1]).normalized;
        }      
    }

    public void Place() {
        editingTrack.AddPoints(points, true);
        startPos = points[^1];
        startDir = endDir;
    }

    public Vector3[] GetPoints() => points;

    public Vector2 GetEndDirection() => endDir;

    Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t) {
        return Vector2.Lerp(Bezier(a, b, c, t), Bezier(b, c, d, t), t);
    }

    Vector2 Bezier(Vector2 a, Vector2 b, Vector2 c, float t) {
        return Vector2.Lerp(Vector2.Lerp(a, b, t), Vector2.Lerp(b, c, t), t);
    }
}