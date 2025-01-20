using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShellGameTaskSpawner : MonoBehaviour, ITaskInterface
{
    [HideInInspector] public Bounds veObjectBounds;
    [HideInInspector] public GameObject veObjectParent;
    public GameObject m_ShellGamePrefab;
    public GameObject m_TargetPrefab;
    //public Camera m_DisplayCamera;

    //private int targetIndexInCup;  // get index based on the trial
    private int[] targetIndexPairs = new int[2];
    private Answer answer = Answer.Pending;
    private GameObject target0;
    private GameObject target1;
    private float timeElapsed = 0f;
    private float duration = 0f;
    private List<GameObject> cupList = new List<GameObject>(5);

    //camera data sent
    private float vfov = 0f;
    private float f = 1f;
    private float scale = 1f;
    private int currTrialIndex = 0;
    private int currCondition = 0;  // 0:conv, 1:seamless
    private int currConditionIndex = 0;
    private bool isFinalTask = false;

    #region

    public event Action OnShellGameObjectSpawned;

    #endregion

    void Start()
    {

    }

    IEnumerator StartMotion()
    {
        GameObject targetCup0 = cupList[targetIndexPairs[0]];
        GameObject targetCup1 = cupList[targetIndexPairs[1]];
        target0 = Instantiate(m_TargetPrefab, targetCup0.transform.position + Vector3.up * 0.01f, Quaternion.identity);
        target1 = Instantiate(m_TargetPrefab, targetCup1.transform.position + Vector3.up * 0.01f, Quaternion.identity);
        target0.transform.parent = veObjectParent.transform;
        target1.transform.parent = veObjectParent.transform;

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

        //int sequenceIdx = currConditionIndex * 3 + currTrialIndex;
        //Debug.Log($"sequenceIdx: {sequenceIdx}");

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
    }


    public void StartTask(ScreenManager screenManager, int conditionIndex, int trialIndex, bool isFinal)
    {
        // init pose of the objects
        Vector3 pos = screenManager.GetScreenPosition();
        Quaternion rot = screenManager.GetScreenRotation();
        Vector3 forward = screenManager.GetScreenForward();
        pos += -forward * 0.2f;
        veObjectParent = Instantiate(m_ShellGamePrefab, pos, rot);
        veObjectBounds = new Bounds();
        Vector3 center = Vector3.zero;
        int cnt = 0;
        foreach (Transform obj in veObjectParent.transform)
        {
            center += obj.GetChild(0).GetComponent<Renderer>().bounds.center;
            veObjectBounds.Encapsulate(obj.transform.GetChild(0).GetComponent<Renderer>().bounds);
            cnt++;
        }
        center /= cnt;
        veObjectBounds.center = center;
        veObjectParent.transform.position = center;  // important for the pivot palcement

        // set up task params
        currTrialIndex = trialIndex;
        currConditionIndex = conditionIndex;
        UpdateTargetIndex(trialIndex);
        isFinalTask = isFinal;

        //  compute scale sent to the server
        float boundWidth = veObjectBounds.size.x;
        float boundHeight = veObjectBounds.size.y;
        float screenWidth = screenManager.GetScreenWidth();
        float screenHeight = screenManager.GetScreenHeight();
        scale = boundWidth > boundHeight ? screenWidth / boundWidth : screenHeight / boundHeight;

        OnShellGameObjectSpawned.Invoke();

        for (int i = 0; i < 5; i++)
            cupList.Add(veObjectParent.transform.GetChild(i).gameObject);

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

    public void SetSendFovAndFocalLen(float fieldOfView, float focalLength)
    {
        vfov = fieldOfView;
        f = focalLength;
    }

    public void SetConditionType(int conditionType)
    {
        currCondition = conditionType;
    }
   
    public string GetShellGameData()
    {
        int isFinalToInt = isFinalTask ? 1 : 0;
        string data = $"{(int)Task.ShellGame},{currTrialIndex},{isFinalToInt},{0},{currCondition},{vfov},{f},{scale}"; // !!make sure the first elem is task type
        return data;
    }

    // archive
    public bool TaskStarted()
    {
        return true;
    }
   
    // archive
    public void SetSpawned(bool value)
    {
        
    }

    public void HideObjects()
    {
        veObjectParent.SetActive(false);
    }

    public void DestroyObjects()
    {
        foreach (Transform obj in veObjectParent.transform)
        {
            Destroy(obj.gameObject);
        }
        
        if (target0 != null)
            Destroy(target0);

        if (target1 != null)
            Destroy(target1);

        if (cupList.Count > 0)
            cupList.Clear();
        
        Destroy(veObjectParent);
    }

    public void SetTaskText(TMP_Text textMeshProText, int conditionIndex, int trialIndex)
    {
        textMeshProText.text = "Keep an eye on the shell";
    }

    public Answer GetAnswer()
    {
        return answer;
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