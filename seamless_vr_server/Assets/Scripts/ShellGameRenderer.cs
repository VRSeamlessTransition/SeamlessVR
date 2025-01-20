using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class ShellGameRenderer : MonoBehaviour
{
    public Camera m_DisplayCamera;
    public GameObject m_ShellGamePrefab;
    public GameObject m_TargetPrefab;

    private GameObject shellGameParent;
    private Bounds veObjectBounds;
    private int displayWidth = Constants.DISPLAY_SCREEN_WIDTH;
    private int displayHeight = Constants.DISPLAY_SCREEN_HEIGHT;
    private RenderTexture m_RTDisplay;
    private int[] targetIndexPairs = new int[2];  // get index based on the trial
    private GameObject target0;
    private GameObject target1;

    private float timeElapsed = 0f;
    private float duration = 0f;
    private List<GameObject> cupList = new List<GameObject>(5);

    private int currConditionIndex = 0;  // Attention: not the same as the condition type
    private int currTrialIndex = 0;
    
    #region

    public event Action OnShellGameMotionStop;

    #endregion

    void Start()
    {
        DisplayCameraSetup();
    }

    void Update()
    {
        
    }

    IEnumerator StartMotion()
    {
        GameObject targetCup0 = cupList[targetIndexPairs[0]];
        GameObject targetCup1 = cupList[targetIndexPairs[1]];
        target0 = Instantiate(m_TargetPrefab, targetCup0.transform.position + Vector3.up * 0.01f, Quaternion.identity);
        target1 = Instantiate(m_TargetPrefab, targetCup1.transform.position + Vector3.up * 0.01f, Quaternion.identity);
        target0.transform.parent = shellGameParent.transform;
        target1.transform.parent = shellGameParent.transform;

        timeElapsed = 0f;
        duration = 0.5f;
        Vector3 startPos0 = targetCup0.transform.position;
        Vector3 endPos0 = targetCup0.transform.position + Vector3.up * 0.1f;
        Vector3 startPos1 = targetCup1.transform.position;
        Vector3 endPos1 = targetCup1.transform.position + Vector3.up * 0.1f;
        while (timeElapsed < duration)
        {
            targetCup0.transform.position = Vector3.Lerp(startPos0, endPos0, timeElapsed / duration);
            targetCup1.transform.position = Vector3.Lerp(startPos1, endPos1, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        targetCup0.transform.position = endPos0;
        targetCup1.transform.position = endPos1;

        yield return new WaitForSeconds(1f);

        timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            targetCup0.transform.position = Vector3.Lerp(endPos0, startPos0, timeElapsed / duration);
            targetCup1.transform.position = Vector3.Lerp(endPos1, startPos1, timeElapsed / duration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        targetCup0.transform.position = startPos0;
        targetCup1.transform.position = startPos1;

        target0.SetActive(false);
        target1.SetActive(false);

        yield return new WaitForSeconds(0.5f);

        int cnt = 0;
        while (cnt < Constants.SHELL_GAME_TOTAL_ANIM_COUNT)
        {
            float angle = 0f;
            int exchangeIndex = cnt;
            int rotationDirection = exchangeIndex % 2;
            //Debug.Log(exchangeIndex);

            int g1Index = Constants.ShellGameAnimSequences[currConditionIndex * 3 + currTrialIndex, exchangeIndex, 0];
            int g2Index = Constants.ShellGameAnimSequences[currConditionIndex * 3 + currTrialIndex, exchangeIndex, 1];
            GameObject g1 = cupList[g1Index];
            GameObject g2 = cupList[g2Index];

            float ellipseEccentricity = 0.8f;
            Vector3 ellipseCenter = (g1.transform.localPosition + g2.transform.localPosition) / 2f;
            Vector3 ellipseX = g1.transform.localPosition - ellipseCenter;
            Vector3 ellipseY = Vector3.forward * ellipseX.magnitude * ellipseEccentricity;

            float angleSin, angleCos, angleInRadians;
            Vector3 offsetFromCenter;
            float endAngle = (rotationDirection == 1) ? 180f : -180f;
            timeElapsed = 0f;
            duration = 1f;

            while (timeElapsed < Constants.SHELL_GAME_DELTA_TIME)
            {
                angleInRadians = angle * Mathf.Deg2Rad;
                angleSin = Mathf.Sin(angleInRadians);
                angleCos = Mathf.Cos(angleInRadians);
                offsetFromCenter = ellipseX * angleCos + ellipseY * angleSin;
                g1.transform.localPosition = ellipseCenter + offsetFromCenter;
                g2.transform.localPosition = ellipseCenter - offsetFromCenter;
                angle = Mathf.Lerp(0f, endAngle, timeElapsed / duration);

                timeElapsed += Time.deltaTime;
                yield return null;
            }
            angle = endAngle;
            angleInRadians = angle * Mathf.Deg2Rad;
            angleSin = Mathf.Sin(angleInRadians);
            angleCos = Mathf.Cos(angleInRadians);
            offsetFromCenter = ellipseX * angleCos + ellipseY * angleSin;
            g1.transform.localPosition = ellipseCenter + offsetFromCenter;
            g2.transform.localPosition = ellipseCenter - offsetFromCenter;

            cnt++;
        }

        OnShellGameMotionStop.Invoke();
    }

    public void StartTask(int conditionIndex, int trialIndex, float scale)  // todo need to change to condition_idx + trial_idx
    {
        shellGameParent = Instantiate(m_ShellGamePrefab, Vector3.zero, Quaternion.identity);
        shellGameParent.transform.localScale = new Vector3(scale, scale, scale);
        veObjectBounds = new Bounds();
        Vector3 center = Vector3.zero;
        int cnt = 0;
        foreach (Transform obj in shellGameParent.transform)
        {
            center += obj.GetChild(0).GetComponent<Renderer>().bounds.center;
            veObjectBounds.Encapsulate(obj.transform.GetChild(0).GetComponent<Renderer>().bounds);
            cnt++;
        }

        // set parent
        center /= cnt;
        veObjectBounds.center = center;
        shellGameParent.transform.position = center;  // important for the pivot palcement

        currConditionIndex = conditionIndex;
        currTrialIndex = trialIndex;
        UpdateTargetIndex(trialIndex);

        for (int i = 0; i < 5; i++)
            cupList.Add(shellGameParent.transform.GetChild(i).gameObject);

        StartCoroutine(StartMotion());
    }

    private void UpdateTargetIndex(int trialIdx)
    {
        if (trialIdx < 0 || trialIdx > Constants.TOTAL_TRIALS_PER_TASK)
        {
            Debug.LogError("please assign a correct trial!");
        }
        targetIndexPairs[0] = Constants.ShellGameTargetIndices[currConditionIndex, trialIdx, 0];
        targetIndexPairs[1] = Constants.ShellGameTargetIndices[currConditionIndex, trialIdx, 1];
        Debug.Log("target index of shell: " + targetIndexPairs[0] + ", " + targetIndexPairs[1]);
    }

    public void StartShellGame(int conditionIndex, int trialIdx, float vfov, float f, float scale)
    {
        // based on the trial to get data (TODO) should transfer the screen pose from client.
        ConfigureDisplayCamera(vfov, f);

        StartTask(conditionIndex, trialIdx, scale);
    }

    private void ConfigureDisplayCamera(float vfov, float f)
    {
        m_DisplayCamera.gameObject.transform.position = -f * Vector3.forward;
        m_DisplayCamera.gameObject.transform.rotation = Quaternion.identity;

        // setup camera to output rendered result to physical display
        m_DisplayCamera.fieldOfView = vfov;
        m_DisplayCamera.focalLength = f;

        Debug.Log(vfov);
    }

    private void DisplayCameraSetup()
    {
        // init render texture for display camera
        m_RTDisplay = new RenderTexture(displayWidth, displayHeight, 24);
        m_RTDisplay.filterMode = FilterMode.Bilinear;
        m_RTDisplay.wrapMode = TextureWrapMode.Clamp;
        if (m_DisplayCamera.targetTexture != null)
            m_DisplayCamera.targetTexture.Release();
        m_DisplayCamera.targetTexture = m_RTDisplay;
    }

    public RenderTexture GetRenderTexture()
    {
        if (m_RTDisplay == null)
        {
            Debug.LogError("the display render texture is null!");
            return null;
        }

        return m_RTDisplay;
    }

    public void DestroyObjects()
    {
        foreach (Transform obj in shellGameParent.transform)
        {
            Destroy(obj.gameObject);
        }

        if (target0 != null)
            Destroy(target0);

        if (target1 != null)
            Destroy(target1);

        if (cupList.Count > 0)
            cupList.Clear();

        Destroy(shellGameParent);
    }
}
