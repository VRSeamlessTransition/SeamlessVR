using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EngineGameSpawner : MonoBehaviour, ITaskInterface
{
    public GameObject m_EnginePrefab;
    public Camera m_DisplayCamera;
    public Texture2D normalTex;
    public Texture2D highlightTex;

    [HideInInspector] public Bounds veObjectBounds;
    [HideInInspector] public GameObject veObjectParent;

    private int currTrialIndex = 0;
    private int currConditionIndex = 0;
    private int highlightIndex = -1;

    // send data
    private float vfov = 0f;
    private float f = 1f;
    private float scale = 1f;
    private Quaternion rotInCam = Quaternion.identity;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    public void StartTask(ScreenManager screenManager, int conditionIndex, int trialIndex, bool isFinalTask = false)
    {
        currTrialIndex = trialIndex;
        currConditionIndex = conditionIndex;

        Vector3 pos = screenManager.GetScreenPosition();
        Quaternion rot = screenManager.GetScreenRotation();
        rot = Constants.EngineGameExtraRotations[currConditionIndex, currTrialIndex] * rot;
        veObjectParent = Instantiate(m_EnginePrefab, pos, rot);

        veObjectBounds = new Bounds();
        Vector3 center = Vector3.zero;
        int cnt = 0;
        foreach (Transform obj in veObjectParent.transform)
        {
            center += obj.GetComponent<Renderer>().bounds.center;
            veObjectBounds.Encapsulate(obj.GetComponent<Renderer>().bounds);
            cnt++;
        }
        center /= cnt;
        veObjectBounds.center = center;
        veObjectParent.transform.position = center;

        // set task params
        highlightIndex = Constants.EngineGameHighlightIndices[currConditionIndex, currTrialIndex];
        ToggleHighlightObject(true);

        // compute local scale sent to the server
        float boundWidth = veObjectBounds.size.x;
        float boundHeight = veObjectBounds.size.y;
        float screenWidth = screenManager.GetScreenWidth();
        float screenHeight = screenManager.GetScreenHeight();
        scale = boundWidth > boundHeight ? screenWidth / boundWidth : screenHeight / boundHeight;

        // compute rotation in camera sent to the server
        rotInCam = Quaternion.Inverse(m_DisplayCamera.transform.rotation) * veObjectParent.transform.rotation;
    }

    public string GetEngineData()
    {
        string data = $"{vfov},{f},{scale},{rotInCam.x},{rotInCam.y},{rotInCam.z},{rotInCam.w}";
        return data;
    }

    public void SetTaskText(TMP_Text textMeshProText, int conditionIndex, int trialIndex)
    {
        textMeshProText.text = "Identify and remember highlighted element";
    }

    public void SetSendFovAndFocalLen(float fieldOfView, float focalLength)
    {
        vfov = fieldOfView;
        f = focalLength;
    }

    public void ToggleHighlightObject(bool setHighlight)
    {
        if (highlightIndex == -1)
            return;

        if (setHighlight)
            veObjectParent.transform.GetChild(highlightIndex).gameObject.GetComponent<MeshRenderer>().material.mainTexture = highlightTex;
        else
            veObjectParent.transform.GetChild(highlightIndex).gameObject.GetComponent<MeshRenderer>().material.mainTexture = normalTex;
    }


    public void DestroyObjects()
    {
        foreach (Transform obj in veObjectParent.transform)
        {
            Destroy(obj.gameObject);
        }
        Destroy(veObjectParent);
    }

    public void HideObjects()
    {
        veObjectParent.SetActive(false);
    }

    public void ActionBeforeTranstion()
    {
        ToggleHighlightObject(false);
    }

    public bool TaskStarted()
    {
        return false;
    }
    public void SetSpawned(bool spawned)
    {

    }

    void ITaskInterface.SetVEObjectBounds(Bounds bounds)
    {
        veObjectBounds = bounds;
    }

    Bounds ITaskInterface.GetVEObjectBounds()
    {
        return veObjectBounds;
    }
}
