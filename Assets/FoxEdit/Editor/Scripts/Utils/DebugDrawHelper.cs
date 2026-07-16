using UnityEngine;

public static class DebugDrawHelper
{
    public static void DebugDrawSphere(Vector3 position, float radius, Color color, float duration = 1.0f, int segements = 8)
    {
        float offset = (2 * Mathf.PI) / segements;

        for (int x = 0; x < segements; x++)
        {
            for (int y = 0; y < segements; y++)
            {
                float Ax = Mathf.Cos(offset * x) * Mathf.Sin(offset * y) * radius;
                float Ay = Mathf.Cos(offset * y) * radius;
                float Az = Mathf.Sin(offset * x) * Mathf.Sin(offset * y) * radius;

                int nextY = (y + 1) % segements;
                float Bx = Mathf.Cos(offset * x) * Mathf.Sin(offset * nextY) * radius;
                float By = Mathf.Cos(offset * nextY) * radius;
                float Bz = Mathf.Sin(offset * x) * Mathf.Sin(offset * nextY) * radius;

                int nextX = (x + 1) % segements;
                float Cx = Mathf.Cos(offset * nextX) * Mathf.Sin(offset * y) * radius;
                float Cy = Mathf.Cos(offset * y) * radius;
                float Cz = Mathf.Sin(offset * nextX) * Mathf.Sin(offset * y) * radius;

                Debug.DrawLine(new Vector3(Ax, Ay, Az) + position, new Vector3(Bx, By, Bz) + position, color, duration);
                Debug.DrawLine(new Vector3(Ax, Ay, Az) + position, new Vector3(Cx, Cy, Cz) + position, color, duration);
            }
        }
    }

    public static void DebugDrawGrid(Vector3 position, Vector3Int size, float cubeSize, Color color, float duration = 1.0f)
    {
        Debug.Log(size);
        Vector3 linesLength = new Vector3(size.x, size.y, size.z) * cubeSize;

        for (int x = 0; x < size.x + 1; x++)
        {
            for (int y = 0; y < size.y + 1; y++)
            {
                Vector3 start = position + new Vector3(x * cubeSize, y * cubeSize, 0.0f);
                Debug.DrawLine(start, start + new Vector3(0.0f, 0.0f, linesLength.z), color, duration);
            }
        }

        for (int x = 0; x < size.x + 1; x++)
        {
            for (int z = 0; z < size.z + 1; z++)
            {
                Vector3 start = position + new Vector3(x * cubeSize, 0.0f, z * cubeSize);
                Debug.DrawLine(start, start + new Vector3(0.0f, linesLength.y, 0.0f), color, duration);
            }
        }

        for (int z = 0; z < size.z + 1; z++)
        {
            for (int y = 0; y < size.y + 1; y++)
            {
                Vector3 start = position + new Vector3(0.0f, y * cubeSize, z * cubeSize);
                Debug.DrawLine(start, start + new Vector3(linesLength.x, 0.0f, 0.0f), color, duration);
            }
        }
    }
}
