using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MouseUtil 
{
    private static Camera camera = Camera.main;
    public static Vector3 GetMousePositionInWorldSpace(float zValue = 0f)
    {
        CheckCamera();
        Plane dragPlane = new(Camera.main.transform.forward, new Vector3(0, 0, zValue));
        Ray ray = camera.ScreenPointToRay(Input.mousePosition);
        if(dragPlane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }
        return Vector3.zero;
    }

    private static void CheckCamera() // since static, check again where camera at 
    {
        if (camera == null)
        {
            camera = Camera.main;
        }
    }
}
