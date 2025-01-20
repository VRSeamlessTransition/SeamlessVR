using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class SeamlessTransitionManager : MonoBehaviour
{
    public Camera m_FlattenCamera;
    [SerializeField] private ScreenManager m_ScreenManager;
    [HideInInspector] public GameObject m_VEObjectParent;

    private RenderTexture RT_Flatten;
    private RenderTexture RT_Depth;
    private float elapsedTime = 0f;
    private float iStep = 1f;
    private float nStep = 100f;
    private float epsilon = 0.05f;
    private bool isMorphing = false;
    private Vector3 startScale = Vector3.one;
    private Vector3 endScale = Vector3.one;
    private Vector3 startPos = Vector3.zero;
    private Vector3 endPos = Vector3.zero;
    private ITaskInterface currTask;
    private Bounds veObjectBounds;
    private float totalTransitionTime = Constants.TOTAL_TRANSITION_TIME;

    #region Events

    public event Action OnMorphFinished;

    #endregion

    void Start()
    {
        InitCameras();
    }

    void Update()
    {
        UpdateMorph();
    }

    /** ATTENTION: need to make sure in the inspector, two camera has the same interior paramters */
    private void InitCameras()
    {
        m_FlattenCamera.enabled = false;
    }

    public void UpdateFlattenCameraPose(Vector3 pos, Quaternion rot)
    {
        m_FlattenCamera.gameObject.transform.position = pos;
        m_FlattenCamera.gameObject.transform.rotation = rot;
    }

    public void SetSeamlessTransition(ITaskInterface task)
    {
        m_VEObjectParent = GameObject.FindGameObjectWithTag("VEObjectParent");
        currTask = task;

        // set diff transition time for diff tasks
        if (currTask is CountShapeTaskSpawner)
        {
            totalTransitionTime = Constants.TOTAL_TRANSITION_TIME_COUNT_SHAPE;
        }
        else if (currTask is ShellGameTaskSpawner)
        {
            totalTransitionTime = Constants.TOTAL_TRANSITION_TIME_SHELL_GAME;
        }
        else if (currTask is EngineGameSpawner)
        {
            totalTransitionTime = Constants.TOTAL_TRANSITION_TIME_ENGINE_GAME;
        }
        else
        {
            totalTransitionTime = Constants.TOTAL_TRANSITION_TIME;
        }

        ConfigureBeforeMorph();
        isMorphing = true;
    }

    private void ConfigureBeforeMorph()
    {
        //m_VEObjectParent = GameObject.FindGameObjectWithTag("VEObjectParent");
        //m_FlattenCamera.RenderWithShader(m_UnlitTexShader, "");
        //m_DepthCamera.RenderWithShader(m_UnlitTexShader, "");
        //m_FlattenCamera.SetReplacementShader(m_UnlitTexShader, "");
        //m_DepthCamera.SetReplacementShader(m_UnlitTexShader, "");

        Matrix4x4 V = m_FlattenCamera.worldToCameraMatrix;
        Matrix4x4 P = m_FlattenCamera.projectionMatrix;
        float z_dist_in_cam = Mathf.Abs(m_FlattenCamera.gameObject.transform.InverseTransformPoint(m_ScreenManager.GetScreenPosition()).z);

        // set scale
        veObjectBounds = currTask.GetVEObjectBounds(); //Get bounds using interface
        float boundWidth = veObjectBounds.size.x;
        float boundHeight = veObjectBounds.size.y;
        float screenWidth = m_ScreenManager.GetScreenWidth();
        float screenHeight = m_ScreenManager.GetScreenHeight();
        float scaleFactor = boundWidth > boundHeight ? screenWidth / boundWidth : screenHeight / boundHeight;
        startScale = m_VEObjectParent.transform.lossyScale;
        endScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);

        // set translation (screen position is the target position)
        startPos = veObjectBounds.center;  // veObjectBounds.center
        endPos = m_ScreenManager.GetScreenPosition();
        Vector3 offsetPos = endPos - startPos;

        // set uniform transform matrix
        Matrix4x4 uniformTransMat = Matrix4x4.Translate(endPos - startPos);
        Matrix4x4 transformToOrigin = Matrix4x4.Translate(-endPos);
        Matrix4x4 transformBack = Matrix4x4.Translate(endPos);
        Matrix4x4 uniformScaleMat = transformBack * Matrix4x4.Scale(endScale) * transformToOrigin;

        foreach (Transform obj in m_VEObjectParent.transform)
        {
            var currObj = obj.childCount > 0 ? obj.GetChild(0) : obj;
            Material mat = currObj.GetComponent<MeshRenderer>().material;
            mat.SetTexture("_ProjectTex", RT_Flatten);
            mat.SetTexture("_ShadowMap", RT_Depth);
            mat.SetFloat("_IStep", iStep);
            mat.SetInteger("_Mode", 2);
            mat.SetVector("_CamPos", m_FlattenCamera.transform.position);
            mat.SetVector("_CamNormal", m_FlattenCamera.transform.forward);
            mat.SetFloat("_F", z_dist_in_cam);
            mat.SetMatrix("_WorldToCamMat", V);
            mat.SetMatrix("_BlendCamProjectionMat", P);
            mat.SetMatrix("_UniScaleMat", uniformScaleMat);
            mat.SetMatrix("_UniTransMat", uniformTransMat);
            mat.SetFloat("_NStep", nStep);
        }
    }

    private void UpdateMorph()
    {
        if (!isMorphing)
            return;

        iStep = nStep * elapsedTime / totalTransitionTime;
        
        // end morph
        if (iStep > nStep - epsilon)
        {
            foreach (Transform obj in m_VEObjectParent.transform)
            {
                var currObj = obj.childCount > 0 ? obj.GetChild(0) : obj;
                currObj.GetComponent<MeshRenderer>().material.SetFloat("_IStep", nStep - epsilon);
            }

            OnMorphFinished.Invoke();
            iStep = 1f;
            elapsedTime = 0f;
            isMorphing = false;
            return;
        }

        foreach (Transform obj in m_VEObjectParent.transform)
        {
            var currObj = obj.childCount > 0 ? obj.GetChild(0) : obj;
            currObj.GetComponent<MeshRenderer>().material.SetFloat("_IStep", iStep);
        }

        elapsedTime += Time.deltaTime;

        //Debug.Log("istep " + iStep.ToString());
    }

    public float GetScaleFactor()
    {
        return endScale.x;
    }
}
