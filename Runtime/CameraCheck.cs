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
        foreach(var module in UModule.Instances.Values)
            {
                if (module.ModuleType != "Drone") continue;
                if (module.Active) {
                    DefaultCamera.enabled = false;
                    return;
                }
            }
        DefaultCamera.enabled = true;
    }
}
