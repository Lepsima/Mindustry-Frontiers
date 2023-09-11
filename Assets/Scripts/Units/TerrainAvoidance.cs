using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainAvoidance {
    public static Vector2 GetDirection(Unit unit, Vector2 position, Vector2 target) {
        float collisionDistance = 5f;
        int rays = 10;

        float rotation = Quaternion.LookRotation(target - position, Vector2.up).eulerAngles.z;
        float[] frontRays = MapRaycaster.FovSolid(position, rotation, 35f, 3, collisionDistance * 1.2f);

        for (int i = 0; i < frontRays.Length; i++) {
            if (frontRays[i] > collisionDistance)
                continue;

            float[] rightRays = MapRaycaster.FovSolid(position, rotation + 90f, 170f, rays, collisionDistance);
            float[] leftRays = MapRaycaster.FovSolid(position, rotation - 90f, 170f, rays, collisionDistance);

            
            for (int j = 0; j < rays; j++) {

            }

            //float rightClosest = Mathf.Min();
            float leftClosest = Mathf.Min(MapRaycaster.FovSolid(position, rotation - 90f, 170f, 10, collisionDistance));

            // Averages from 0 to 1, 0 = near wall, 1 = max distance
            //float rightAvg = 1 - Mathf.Clamp01(rightClosest / 5f);
            float leftAvg = 1 - Mathf.Clamp01(leftClosest / 5f);

            //float diff = (leftAvg - rightAvg) * 135f;

            // rotation += diff;
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

    public static Vector2 ClosestDir(Vector2 position, Vector2 avoidDir, Vector2 dir, int rays, float collisionDistance) {
        return ClosestRot2(position, avoidDir, dir, rays, collisionDistance);
    }

    // Returns the direction of the closest non collision point
    public static float ClosestRot(Vector2 position, Vector2 target, int rays, float collisionDistance) {
        float rotation = Quaternion.LookRotation(target - position, Vector2.up).eulerAngles.z + 90;

        int sectionRays = rays / 2;
        float degOffset = 360 / sectionRays;
        float differenceFactor = collisionDistance / 10f;

        float posRotation = rotation;
        float negRotation = rotation;

        float distancePos = -1;
        float distanceNeg = -1;

        for (int i = 0; i < sectionRays; i++) {
            float rot = rotation + i * degOffset;
            float value = MapRaycaster.Distance(position, Mathf.Deg2Rad * rot, collisionDistance + 5f);

            Debug.DrawLine(position, new Vector2(Mathf.Cos(Mathf.Deg2Rad * rot), Mathf.Sin(Mathf.Deg2Rad * rot)) * value + position, value > collisionDistance ? Color.green : Color.red);

            if (distancePos != -1 && value <= distancePos) return posRotation;
            else if (value > collisionDistance) {
                posRotation = rot;

                if (distancePos != -1) return posRotation;
                else distancePos = value + differenceFactor;
            }

            rot = rotation - degOffset - i * degOffset;
            value = MapRaycaster.Distance(position, Mathf.Deg2Rad * rot, collisionDistance + 5f);

            Debug.DrawLine(position, new Vector2(Mathf.Cos(Mathf.Deg2Rad * rot), Mathf.Sin(Mathf.Deg2Rad * rot)) * value + position, value > collisionDistance ? Color.green : Color.red);

            if (distanceNeg != -1 && value <= distanceNeg) return negRotation;
            else if (value > collisionDistance) {
                negRotation = rot;

                if (distanceNeg != -1) return negRotation;
                else distanceNeg = value + differenceFactor;
            }
        }

        return rotation;
    }

    public static float[] angles = new float[] { 30f, -30f, 45f, -45f, 60f, -60f, 75f, -75f, 90f, -90f, 105f, -105f, 120f, -120f, 135f, -135f, 150f, -150f, 165f, -165f, 180f };

    public static Vector2 From(Vector2 position, Vector2 target, Vector2 unitDir) {
        // Fixed advoid distance
        float advoidDistance = 8f;

        // Get direction vector and angle
        Vector2 direction = (target - position).normalized;

        // If can walk straight, dont try to advoid anything
        if (!MapRaycaster.Collides(position, direction * advoidDistance)) return direction;

        if (direction != unitDir) {
            float angleToDir = Vector2.SignedAngle(direction, unitDir);

            for (int a = 0; a < Mathf.Abs(angleToDir); a += 15 * (int)Mathf.Sign(angleToDir)) {
                // Get angle and direction
                Vector2 offsetDirection = Quaternion.Euler(0, 0, a) * direction;

                // If this angle has no collision, return it's direction
                if (!MapRaycaster.Collides(position, offsetDirection * advoidDistance)) return offsetDirection;
            }
        }

        Vector2 oDir = From2(position, unitDir);
        return oDir;
    }

    public static Vector2 From2(Vector2 position, Vector2 direction) {
        // Fixed advoid distance
        float advoidDistance = 10f;

        // If can walk straight, dont try to advoid anything
        if (!MapRaycaster.Collides(position, direction * advoidDistance)) return direction;

        for (int i = 0; i < angles.Length; i++) {
            // Get angle and direction
            float angle = angles[i];
            Vector2 offsetDirection = Quaternion.Euler(0, 0, angle) * direction;

            // If this angle has no collision, return it's direction
            if (!MapRaycaster.Collides(position, offsetDirection * advoidDistance)) return offsetDirection;
        }

        // If all angles collide, rotate slightly to the last rotation in the array
        return Quaternion.Euler(0, 0, angles[^1]) * direction;
    }

    public static Vector2 ClosestRot2(Vector2 position, Vector2 avoidDir, Vector2 dir, int rays, float collisionDistance) {
        float rotation = Vector2.Angle(avoidDir, Vector2.up) + 90;

        int sectionRays = rays / 2;
        float degOffset = 360 / rays;
        //float differenceFactor = collisionDistance / 10f;

        float[] rayValues = new float[rays];

        for (int i = 0; i < rays; i++) {
            float rot = rotation + i * degOffset;
            rayValues[i] = MapRaycaster.Distance(position, Mathf.Deg2Rad * rot, collisionDistance + 5f);
            Debug.DrawLine(position, RotationToDir(rot) * rayValues[i] + position, rayValues[i] > collisionDistance ? Color.green : Color.red);
        }

        float longestVal = 0;
        bool avoid = false;

        for (int i = 0; i < rays; i++) {
            if (rayValues[i] < collisionDistance) continue;
            avoid = true;

            rayValues[i] = (rayValues[i] + rayValues[(i + rays - 1) % rays] + rayValues[(i + 1) % rays]) / 3f;
            longestVal = Mathf.Max(longestVal, rayValues[i]);
        }

        if (!avoid) {
            return dir;
        }

        int index = 0;
        float maxScore = 0;

        for (int i = 0; i < rays; i++) {
            float angleScore = (float)(i >= sectionRays ? i : 2 * sectionRays - i) / sectionRays;
            float distanceScore = rayValues[i] / longestVal;

            float score = angleScore + distanceScore;

            if (maxScore < score) {
                maxScore = score;
                index = i;
            }
        }

        return RotationToDir(rotation + index * degOffset);
    }

    private static Vector2 RotationToDir(float rotation) {
        return new Vector2(Mathf.Cos(Mathf.Deg2Rad * rotation), Mathf.Sin(Mathf.Deg2Rad * rotation));
    }

    private int Index(int index, int maxIndex) {
        return index % maxIndex;
    }
}
