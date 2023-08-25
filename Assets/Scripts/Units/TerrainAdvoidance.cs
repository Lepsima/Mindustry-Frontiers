using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainAdvoidance {
    public static Vector2 GetDirection(Unit unit, Transform transform) {
        float collisionDistance = 5f;

        float rotation = transform.eulerAngles.z;
        float[] frontRays = MapRaycaster.FovSolid(transform.position, rotation, 45f, 3, collisionDistance * 1.2f);

        for (int i = 0; i < frontRays.Length; i++) {
            if (frontRays[i] > collisionDistance)
                continue;

            float rightClosest = Mathf.Min(MapRaycaster.FovSolid(transform.position, rotation + 67.5f, 90f, 6, collisionDistance));
            float leftClosest = Mathf.Min(MapRaycaster.FovSolid(transform.position, rotation - 67.5f, 90f, 6, collisionDistance));

            // Averages from 0 to 1, 0 = near wall, 1 = max distance
            float rightAvg = 1 - Mathf.Clamp01(rightClosest / 5f);
            float leftAvg = 1 - Mathf.Clamp01(leftClosest / 5f);

            float diff = (leftAvg - rightAvg) * 135f;

            rotation += diff;
            break;
        }

        rotation = (rotation + 90) * Mathf.Deg2Rad; 
        return new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation));
    }

    private static float Average(float[] values) {
        float value = 0;
        for (int i = 0; i < values.Length; i++) value += values[i];
        return value / values.Length;
    }
}
