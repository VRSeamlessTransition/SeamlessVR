using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class RenderPipelineManager : MonoBehaviour
{
    private Camera cam;

    public bool isWireFrame = false;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void OnPreCull()
    {
        cam.cullingMatrix = Matrix4x4.Ortho(float.MinValue, float.MaxValue, float.MinValue, float.MaxValue, 0.001f, float.MaxValue) *
                                   Matrix4x4.Translate(Vector3.forward * float.MinValue / 2f) *
                                   cam.worldToCameraMatrix;
    }
    private void OnDisable()
    {
        cam.ResetCullingMatrix();
    }

    private void OnPreRender()
    {
        //GL.wireframe = isWireFrame;
    }

    private void OnPostRender()
    {
        //GL.wireframe = !isWireFrame;
    }
}
