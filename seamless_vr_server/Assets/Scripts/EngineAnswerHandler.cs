using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineAnswerHandler : MonoBehaviour
{
    public Camera m_DisplayCamera;
    public GameObject m_EnginePrefab;

    private GameObject engine;
    private LayerMask layerMask;

    public bool ComputeCoordCorrectness(Vector2 uv, int currConditionIndex, int trialIndex)
    {
        bool isCorrect = false;

        Debug.Log("uv: " + uv);

        // convert to viewport coordinate
        float s = uv.x / (float)Screen.width;
        float t = uv.y / (float)Screen.height;

        Ray ray = m_DisplayCamera.ViewportPointToRay(new Vector3(s, t, 0f));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            string ans = Constants.EngineGameHighlightIndices[currConditionIndex, trialIndex].ToString();
            Debug.Log("curr ans " + ans);
            Debug.Log("hit object name" + hit.collider.gameObject.name);
            if (hit.collider.gameObject.name.Contains(ans))
            {
                Debug.Log("raycast true");
                isCorrect = true;
            }
        }

        //Utils.SaveRenderTextureToPNGFile(m_DisplayCamera.targetTexture, Application.dataPath + "/Textures/engine_display");

        return isCorrect;
    }

    public void ConfigureEngineTask(float vfov, float f, float scale, Quaternion rotInCam)
    {
        // configure display camera
        m_DisplayCamera.gameObject.transform.position = -f * Vector3.forward;
        m_DisplayCamera.gameObject.transform.rotation = Quaternion.identity;
        m_DisplayCamera.fieldOfView = vfov;
        m_DisplayCamera.focalLength = f;

        engine = Instantiate(m_EnginePrefab, Vector3.zero, Quaternion.identity);

        engine.transform.parent = m_DisplayCamera.transform;
        engine.transform.localRotation = rotInCam;
        engine.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void DestroyObjects()
    {
        foreach (Transform obj in engine.transform)
        {
            Destroy(obj.gameObject);
        }
        Destroy(engine);
    }

    void Start()
    {
        layerMask = LayerMask.GetMask("Highlight");
    }

    void Update()
    {

    }
}
