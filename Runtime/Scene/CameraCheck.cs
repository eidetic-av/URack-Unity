using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Eidetic.URack;

public class CameraCheck : MonoBehaviour
{
    Camera defaultCamera;
    Camera DefaultCamera => defaultCamera ?? (defaultCamera = GetComponent<Camera>());

    void Update()
    {
        // Only enable the default camera if it's there's no others active
        if (!DefaultCamera.enabled)
            DefaultCamera.enabled = (Camera.allCameras.Length == 0);
        else
            DefaultCamera.enabled = (Camera.allCameras.Length == 1);
    }
}
