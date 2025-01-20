using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Diagnostics;
using System.IO;

public class HighlightAnswerHandler : MonoBehaviour
{
    public Camera m_DisplayCamera;  // display camera setup by the shell game renderer
    public GameObject m_HighlightSpherePrefab;  
    
    private int displayWidth = Constants.DISPLAY_SCREEN_WIDTH;
    private int displayHeight = Constants.DISPLAY_SCREEN_HEIGHT;
    private GameObject target;
    private LayerMask layerMask;
    private float distFromCenter = 0f;  // in pixels

    public bool ComputeCoordCorrectness(Vector2 uv)
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
            isCorrect = true;
            Debug.Log("raycast true");
        }

        //Utils.SaveRenderTextureToPNGFile(m_DisplayCamera.targetTexture, Application.dataPath + "/Textures/highlight");

        return isCorrect;
    }

    public float ComputePixelDistFromCenter(Vector2 uv)
    {
        // convert the target center to viewpoint
        Vector3 centerScreenUV = m_DisplayCamera.WorldToScreenPoint(target.transform.position);
        distFromCenter = Vector2.Distance(uv, new Vector2(centerScreenUV.x, centerScreenUV.y));
        //Debug.Log(centerScreenUV.ToString());
        //Debug.Log(uv.ToString());
        //Debug.Log("dist off:" + distFromCenter.ToString());

        return distFromCenter;
    }

    public void ConfigureHighlightTask(float vfov, float f, float localScale, Vector3 posInCam)
    {
        // configure display camera
        m_DisplayCamera.gameObject.transform.position = -f * Vector3.forward;
        m_DisplayCamera.gameObject.transform.rotation = Quaternion.identity;
        m_DisplayCamera.fieldOfView = vfov;
        m_DisplayCamera.focalLength = f;

        // configure target position with respect to the camera
        target = Instantiate(m_HighlightSpherePrefab, Vector3.zero, Quaternion.identity);
        target.transform.parent = m_DisplayCamera.transform;
        target.transform.localPosition = posInCam;
        target.transform.localScale = new Vector3(localScale, localScale, localScale);

    }

    public void DestroyObjects()
    {
        Destroy(target);
    }

    void Start()
    {
        layerMask = LayerMask.GetMask("Highlight");
    }

    void Update()
    {
        
    }

    
}
