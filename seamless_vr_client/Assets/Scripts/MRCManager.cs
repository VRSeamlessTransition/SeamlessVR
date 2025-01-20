using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MRCManager : MonoBehaviour
{
    [SerializeField] private ScreenManager m_ScreenManager;
    [SerializeField] private Transform m_CenterEyeAnchor;
    public GameObject m_SpawnObjectPrefab;
    public Camera m_FlattenCamera;
    public Camera m_DisplayCamera;
    public GameObject m_Canvas;

    private GameObject spawnObject;
    private RenderTexture RT_Flatten;
    private RenderTexture RT_Display;

    private float elapsedTime = 0f;
    private float iStep = 1f;
    private float nStep = 100f;
    private float epsilon = 0.05f;
    private bool isMorphing = false;
    private Vector3 startScale = Vector3.one;
    private Vector3 endScale = Vector3.one;
    private Vector3 startPos = Vector3.zero;
    private Vector3 endPos = Vector3.zero;
    private Bounds veObjectBounds;
    private float totalTransitionTime = 10;
    private int step = 0;

    private int displayWidth = 3840;
    private int displayHeight = 2160;


    void Start()
    {
        DisplayCameraSetup();

        m_ScreenManager.SetCanCalibrate(true);
    }

    void Update()
    {
        if (m_ScreenManager.GetCalibrated() && OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
        {
            if (step == 0)
            {
                ConfigureCameras();
                ConfigureBeforeMorph();
                m_Canvas.SetActive(false);
                //m_ScreenManager.SetScreenVisible(false);
                m_ScreenManager.MoveScreenBackward();
                step++;
            }
            else if (step == 1)
            {
                isMorphing = true;
                step++;
            }
            else if (isMorphing == false && step == 2)
            {
                Utils.SaveRenderTextureToPNGFile(RT_Display, Application.dataPath + $"/Textures/morph_output");
            }
        }

        UpdateMorph();
    }

    private void DisplayCameraSetup()
    {
        m_DisplayCamera.enabled = false;

        // init render texture for display camera
        RT_Display = new RenderTexture(displayWidth, displayHeight, 24);
        RT_Display.filterMode = FilterMode.Bilinear;
        RT_Display.wrapMode = TextureWrapMode.Clamp;
        if (m_DisplayCamera.targetTexture != null)
            m_DisplayCamera.targetTexture.Release();
        m_DisplayCamera.targetTexture = RT_Display;
    }

    private void ConfigureCameras()
    {
        // based on the screen pose and approximate head position
        Vector3 screenCenter = m_ScreenManager.GetScreenPosition();
        Vector3 forward = m_ScreenManager.GetScreenForward();
        Vector3 up = m_ScreenManager.GetScreenUp();
        Vector3 right = m_ScreenManager.GetScreenRight();
        Vector3 headPos = m_CenterEyeAnchor.position;
        float zDistFromScreen = Mathf.Abs(Utils.PosFromWorldToLocal(screenCenter, right, forward, up, headPos).z);
        Vector3 camPos = screenCenter - forward * zDistFromScreen;
        Quaternion camRot = m_ScreenManager.GetScreenRotation();

        // make sure the morph camera pose is the same as the conventional camera
        m_FlattenCamera.gameObject.transform.position = camPos;
        m_FlattenCamera.gameObject.transform.rotation = camRot;

        // setup display camera to output rendered result to physical display
        m_DisplayCamera.gameObject.transform.position = camPos;
        m_DisplayCamera.gameObject.transform.rotation = camRot;
        float caliScreenHeight = m_ScreenManager.GetScreenHeight();
        float hfov = 2 * Mathf.Rad2Deg * Mathf.Atan(caliScreenHeight / 2 / zDistFromScreen);
        m_DisplayCamera.fieldOfView = hfov;
        m_DisplayCamera.focalLength = zDistFromScreen;
    }

    private void ConfigureBeforeMorph()
    {
        Matrix4x4 V = m_FlattenCamera.worldToCameraMatrix;
        Matrix4x4 P = m_FlattenCamera.projectionMatrix;
        float z_dist_in_cam = Mathf.Abs(m_FlattenCamera.gameObject.transform.InverseTransformPoint(m_ScreenManager.GetScreenPosition()).z);

        // init object
        Vector3 pos = m_ScreenManager.GetScreenPosition() - m_ScreenManager.GetScreenForward() * 0.15f + m_ScreenManager.GetScreenUp() * 0.05f;
        Quaternion rot = m_ScreenManager.GetScreenRotation();
        rot = Quaternion.Euler(0f, 112f, 0f) * rot;
        spawnObject = Instantiate(m_SpawnObjectPrefab, pos, rot);

        // calc bounding box
        veObjectBounds = new Bounds();
        Vector3 center = Vector3.zero;
        int cnt = 0;
        foreach (Transform obj in spawnObject.transform)
        {
            var currObj = obj.childCount > 0 ? obj.GetChild(0) : obj;
            center += currObj.GetComponent<Renderer>().bounds.center;
            veObjectBounds.Encapsulate(currObj.GetComponent<Renderer>().bounds);
            cnt++;
        }
        center /= cnt;
        veObjectBounds.center = center;
        spawnObject.transform.position = center;


        // set scale
        float boundWidth = veObjectBounds.size.x;
        float boundHeight = veObjectBounds.size.y;
        float screenWidth = m_ScreenManager.GetScreenWidth();
        float screenHeight = m_ScreenManager.GetScreenHeight();
        float scaleFactor = boundWidth > boundHeight ? screenWidth / boundWidth : screenHeight / boundHeight;
        startScale = spawnObject.transform.lossyScale;
        scaleFactor *= 2.1f;
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

        foreach (Transform obj in spawnObject.transform)
        {
            var currObj = obj.childCount > 0 ? obj.GetChild(0) : obj;
            Material mat = currObj.GetComponent<MeshRenderer>().material;
            //mat.SetTexture("_ProjectTex", RT_Flatten);
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
            foreach (Transform obj in spawnObject.transform)
            {
                var currObj = obj.childCount > 0 ? obj.GetChild(0) : obj;
                currObj.GetComponent<MeshRenderer>().material.SetFloat("_IStep", nStep - epsilon);
            }

            iStep = 1f;
            elapsedTime = 0f;
            m_DisplayCamera.Render();
            isMorphing = false;
            return;
        }

        foreach (Transform obj in spawnObject.transform)
        {
            var currObj = obj.childCount > 0 ? obj.GetChild(0) : obj;
            currObj.GetComponent<MeshRenderer>().material.SetFloat("_IStep", iStep);
            //currObj.GetComponent<MeshRenderer>().material.SetTexture("_ProjectTex", RT_Flatten);
        }

        elapsedTime += Time.deltaTime;
    }
}
