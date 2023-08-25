using Frontiers.Content.Maps;
using UnityEngine;

public static class MapRaycaster {
    public static Map map;

    public static Vector2 Solid(Vector2 point, Vector2 direction, float maxDistance = 20f) {
        int x = -1;
        int y = -1;

        float distance = 0;
        float step = 0.1f;
        direction = direction.normalized * step;

        while (distance < maxDistance) {
            distance += step;
            point += direction;

            int oldX = x;
            int oldY = y;

            x = Mathf.FloorToInt(point.x);
            y = Mathf.FloorToInt(point.y);

            bool xChange = oldX != x;
            bool yChange = oldY != y;

            if (xChange || yChange) {
                if (!map.InBounds(point)) {
                    return new Vector2(Mathf.Clamp(point.x, 0, map.size.x), Mathf.Clamp(point.y, 0, map.size.y));
                }

                if (map.tilemap.GetTile(new Vector2Int(x, y)).IsSolid()) {
                    if (xChange) point.x -= direction.x;
                    if (yChange) point.y -= direction.y;

                    return point;
                }
            }
        }

        return point + direction.normalized * maxDistance;
    }

    public static float Solid(Vector2 point, float rotation, float maxDistance = 20f) {
        int x = -1;
        int y = -1;

        float distance = 0;
        float step = 0.1f;

        Vector2 direction = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation)) * step;

        while (distance < maxDistance) {
            distance += step;
            point += direction;

            int oldX = x;
            int oldY = y;

            x = Mathf.FloorToInt(point.x);
            y = Mathf.FloorToInt(point.y);

            bool xChange = oldX != x;
            bool yChange = oldY != y;

            if ((xChange || yChange) && (!map.InBounds(point) || map.tilemap.GetTile(new Vector2Int(x, y)).IsSolid())) {
                break;
            }
        }

        return distance;
    }

    public static float[] FovSolid(Vector2 point, float rotation, float fov, int rays, float maxDistance = 20) {
        rotation += 90;

        float halfFov = fov / 2f;
        float degOffset = fov / (rays - 1);

        float[] rayArray = new float[rays];

        for (int i = 0; i < rays; i++) {
            float rot = rotation + (i * degOffset - halfFov);
            rayArray[i] = Solid(point, Mathf.Deg2Rad * rot, maxDistance);

            Debug.DrawLine(point, new Vector2(Mathf.Cos(Mathf.Deg2Rad * rot), Mathf.Sin(Mathf.Deg2Rad * rot)) * rayArray[i] + point);
        }

        return rayArray;
    }
}