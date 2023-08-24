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

            float[] rightRays = MapRaycaster.FovSolid(transform.position, rotation + 90f, 90f, 6, collisionDistance);
            float[] leftRays = MapRaycaster.FovSolid(transform.position, rotation - 90f, 90f, 6, collisionDistance);

            // Averages from 0 to 1, 0 = near wall, 1 = max distance
            float rightAvg = 1 - Mathf.Clamp01(Average(rightRays) / 2f);
            float leftAvg = 1 - Mathf.Clamp01(Average(leftRays) / 2f);

            float diff = (leftAvg - rightAvg) * 135f;

            rotation += diff;
            break;
        }

        rotation = (rotation + 90) * Mathf.Deg2Rad;
        Vector3 dir = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation));

        Debug.DrawLine(transform.position, transform.position + dir, Color.green);
        Debug.DrawLine(transform.position, transform.position + transform.up, Color.red);

        return dir;
    }

    private static float Average(float[] values) {
        float value = 0;
        for (int i = 0; i < values.Length; i++) value += values[i];
        return value / values.Length;
    }
}
