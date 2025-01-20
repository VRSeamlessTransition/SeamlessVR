using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConvTransitionManager : MonoBehaviour
{
    [SerializeField] private ScreenManager m_ScreenManager;
    public Camera m_ConvCamera;

    private RenderTexture RT_Convention;
    private int displayWidth = Constants.DISPLAY_SCREEN_WIDTH;
    private int displayHeight = Constants.DISPLAY_SCREEN_HEIGHT;
    private GameObject veObjectParent;
    private Bounds veObjectBounds;
    private ITaskInterface currTask;
    private bool isTransitioning = false;
    private Vector3 startScale = Vector3.one;
    private Vector3 endScale = Vector3.one;
    private Vector3 startPos = Vector3.zero;
    private Vector3 endPos = Vector3.zero;
    private float t = 0f;
    private float elapsedTime = 0f;
    private float totalTime = 3f;  // TODO: make sure the convention transition time is the same as the seamless transition time
    private float totalTransitionTime = Constants.TOTAL_TRANSITION_TIME;

    #region Events

    public event Action OnConvTransFinshed;
    
    #endregion

    void Start()
    {
        InitConvCamera();
    }

    void Update()
    {
        SetTimer();
    }

    private void SetTimer()
    {
        if (!isTransitioning)
            return;

        if (elapsedTime < totalTransitionTime)
        {
            elapsedTime += Time.deltaTime;
        }
        else
        {
            elapsedTime = 0f;
            isTransitioning = false;
            OnConvTransFinshed.Invoke();
        }
    }

    // archive: not more uniform transformation
    private void UpdateConvTransition()
    {
        if (!isTransitioning)
            return;
        
        t = elapsedTime / totalTime;
        if (t <= 1f)
        {
            veObjectParent.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            veObjectParent.transform.position = Vector3.Lerp(startPos, endPos, t);

            elapsedTime += Time.deltaTime;
        }
        else
        {
            veObjectParent.transform.localScale = Vector3.Lerp(startScale, endScale, 1f);
            veObjectParent.transform.position = Vector3.Lerp(startPos, endPos, 1f);

            //currTask.SetVEObjectBounds(Utils.ComputeVeObjectBounds(veObjectParent));
            OnConvTransFinshed.Invoke();
            t = 0f;
            elapsedTime = 0f;
            isTransitioning = false;
        }
    }

    private void InitConvCamera()
    {
        m_ConvCamera.fieldOfView = 75;
        m_ConvCamera.enabled = false;

        RT_Convention = new RenderTexture(displayWidth, displayHeight, 24);
        RT_Convention.filterMode = FilterMode.Bilinear;
        RT_Convention.wrapMode = TextureWrapMode.Clamp;
        if (m_ConvCamera.targetTexture != null)
        {
            m_ConvCamera.targetTexture.Release();
        }
        m_ConvCamera.targetTexture = RT_Convention;
    }


    private void ToggleHighlight(bool isShow)
    {
        if (currTask is HighlightedObjectTaskSpawner)
        {
            var task = currTask as HighlightedObjectTaskSpawner;
            task.ToggleHighlightObject(isShow);
        }
        else if (currTask is EngineGameSpawner)
        {
            var task = currTask as EngineGameSpawner;
            task.ToggleHighlightObject(isShow);
        }
    }

    private void RenderToDisplay()
    {
        // set scale
        veObjectBounds = currTask.GetVEObjectBounds();
        float boundWidth = veObjectBounds.size.x;
        float boundHeight = veObjectBounds.size.y;
        float screenWidth = m_ScreenManager.GetScreenWidth();
        float screenHeight = m_ScreenManager.GetScreenHeight();
        float scaleFactor = boundWidth > boundHeight ? screenWidth / boundWidth : screenHeight / boundHeight;
        startScale = veObjectParent.transform.lossyScale;
        endScale = new Vector3(scaleFactor, scaleFactor, scaleFactor);
        Debug.Log("scale factor: " + scaleFactor);

        // set translation
        startPos = veObjectBounds.center;  // veObjectBounds.center
        endPos = m_ScreenManager.GetScreenPosition();

        // render
        veObjectParent.transform.localScale = endScale;
        veObjectParent.transform.position = endPos;
        ToggleHighlight(false);

        m_ConvCamera.Render();

        veObjectParent.transform.localScale = startScale;
        veObjectParent.transform.position = startPos;
        //ToggleHighlight(true);
    }

    public void UpdateConvCameraPose(Vector3 pos, Quaternion rot)
    {
        m_ConvCamera.gameObject.transform.position = pos;
        m_ConvCamera.gameObject.transform.rotation = rot;

        // configure the interior params use the screen as the image plane size
        float f = Vector3.Distance(m_ConvCamera.transform.position, m_ScreenManager.GetScreenPosition());
        float caliScreenHeight = m_ScreenManager.GetScreenHeight();
        float hfov = 2 * Mathf.Rad2Deg * Mathf.Atan(caliScreenHeight / 2 / f);
        m_ConvCamera.fieldOfView = hfov;
        m_ConvCamera.focalLength = f;
    }

    public void SetConvTransition(ITaskInterface task)
    {
        veObjectParent = GameObject.FindGameObjectWithTag("VEObjectParent");
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

        RenderToDisplay();
        isTransitioning = true;
    }

    public RenderTexture GetRenderTexture()
    {
        if (RT_Convention == null)
        {
            Debug.Log("the conv render texture is not initialized!");
            return null;
        }

        //m_ConvCamera.Render();
        return RT_Convention;
    }

    public float GetScaleFactor()
    {
        return endScale.x;
    }
}
