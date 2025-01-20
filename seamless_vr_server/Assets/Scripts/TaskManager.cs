using System.Collections;
using System;
using System.IO;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using UnityEditor;

public class TaskManager : MonoBehaviour
{
    [SerializeField] ServerManager m_Server;
    [SerializeField] SubTaskUIHandler m_SubTaskUIHandler;
    [SerializeField] ShellGameRenderer m_ShellGameRenderer;
    [SerializeField] HighlightAnswerHandler m_HighlightAnsHandler;
    [SerializeField] EngineAnswerHandler m_EngineAnsHandler;
    public TextMeshProUGUI m_TextInfo;
    public TextMeshProUGUI m_TextLog;
    public TextMeshProUGUI m_TextTimer;
    public RawImage m_Image;
    public Texture2D m_BgTex;
    public GameObject m_Button;

    private Task currTask;
    private bool isFinalTask = false;
    private bool registerAnswer = false;
    private float[] taskSpecificInformation;
    private int currConditionIndex = 0;

    // user data persistence
    private string[,] persistenceData;
    private int currTrialIdx = 0; // TODO: add trial if needed
    private int currTaskIdx = 0;
    private int currCondition = 0; // (0:conv, 1:seamless) Attention: diff from currConditionIndex
    private int totalRows = 0;
    private float timePassed = 0f;
    bool correctness = false;
    string userAns = "";

    private void OnEnable()
    {
        m_ShellGameRenderer.OnShellGameMotionStop += SetupShellGameUI;
    }

    private void OnDisable()
    {
        m_ShellGameRenderer.OnShellGameMotionStop -= SetupShellGameUI;
    }

    private void SetupShellGameUI()
    {
        m_Button.GetComponent<Button>().interactable = true;
        m_SubTaskUIHandler.ToggleShellGameTaskUI(true);
    }

    void Start()
    {
        if (!Directory.Exists(Constants.DIR_PATH)) Directory.CreateDirectory(Constants.DIR_PATH);
        //if (!File.Exists(FILE_PATH))
        //{
        //    File.CreateText(FILE_PATH).Close();
        //}

        totalRows = Constants.TOTAL_TASK_NUM * Constants.TOTAL_TRIALS_PER_TASK;
        persistenceData = new string[totalRows, Constants.TOTAL_ANS_TYPE_PER_ROW];  // time, answer, correctness
    }

    void Update()
    {
        UpdateTimePassed();

        //if (Input.GetKeyDown(KeyCode.Space))
        //{
        //    m_Image.texture = m_ShellGameRenderer.GetRenderTexture();
        //    m_ShellGameRenderer.StartShellGame(0, 41f);
        //}
    }

    private void UpdateTimePassed()
    {
        if (!registerAnswer)
            return;
        
        timePassed += Time.deltaTime;
        m_TextTimer.text = $"{(int)timePassed} s";
        //Debug.Log(timePassed);
    }

    public void SetupCurrentTask(int taskId, int trialIdx, int isFinalInt, float timeElapsedInVR, int conditionType, float[] taskSpecificInfo)
    {
        currTrialIdx = trialIdx;
        currTaskIdx = taskId;
        currCondition = conditionType;
        taskSpecificInformation = taskSpecificInfo;
        
        // set up question text
        string taskInfo = $"Trial {trialIdx + 1}:\n\n";
        float vfov, f, scale;
        switch (taskId)
        {
            case 0:
                currTask = Task.CountShape;
                taskInfo += Constants.QUESTION_COUNT_SHAPE + Constants.GetCountShapeType(((int)taskSpecificInfo[0]));
                m_SubTaskUIHandler.ToggleCountShapeTaskUI(true);
                m_Button.GetComponent<Button>().interactable = true;
                break;

            case 1:
                currTask = Task.IdentifyHighlightedObject;
                vfov = taskSpecificInformation[0];
                f = taskSpecificInformation[1];
                scale = taskSpecificInformation[2];
                float x = taskSpecificInformation[3];
                float y = taskSpecificInformation[4];
                float z = taskSpecificInformation[5];
                m_HighlightAnsHandler.ConfigureHighlightTask(vfov, f, scale, new Vector3(x, y, z));

                taskInfo += Constants.QUESTION_HIGHLIGHT_OBJECTS;
                m_SubTaskUIHandler.ToggleHighlightTaskUI(true);
                m_Button.GetComponent<Button>().interactable = true;
                break;

            case 2:
                currTask = Task.ShellGame;
                vfov = taskSpecificInformation[0];
                f = taskSpecificInformation[1];
                scale = taskSpecificInformation[2];
                m_Image.texture = m_ShellGameRenderer.GetRenderTexture();
                m_ShellGameRenderer.StartShellGame(currConditionIndex, trialIdx, vfov, f, scale); // TODO: add event in the coroutine when finish??
                taskInfo += Constants.QUESTION_SHELLGAME;
                m_Button.GetComponent<Button>().interactable = false;
                break;

            case 3:
                currTask = Task.EngineGame;
                vfov = taskSpecificInformation[0];
                f = taskSpecificInformation[1];
                scale = taskSpecificInformation[2];
                float a = taskSpecificInformation[3];
                float b = taskSpecificInformation[4];
                float c = taskSpecificInformation[5];
                float d = taskSpecificInformation[6];
                m_EngineAnsHandler.ConfigureEngineTask(vfov, f, scale, new Quaternion(a, b, c, d));

                taskInfo += Constants.QUESTION_ENGINE_GAME;
                m_SubTaskUIHandler.ToggleEngineTaskUI(true);
                m_Button.GetComponent<Button>().interactable = true;
                break;

            default:
                m_TextLog.SetText("Error task msg!"); 
                break;
        }
        m_TextInfo.SetText(taskInfo);

        // other settings
        isFinalTask = (isFinalInt == 1) ? true : false;
        timePassed = timeElapsedInVR;  // TODO: need to justify this
        registerAnswer = true;
    }
  
    public void OnNextButtonClick()
    {
        if (!registerAnswer)
        {
            return;
        }

        if (!m_SubTaskUIHandler.IsAnswerValidate())
        {
            m_TextInfo.text += "\nPlease enter a valid answer!";
            return;
        }

        // record answers (must before set sub ui)
        int rowIdx = currTaskIdx * Constants.TOTAL_TRIALS_PER_TASK + currTrialIdx;
        persistenceData[rowIdx, 0] = timePassed.ToString("0.00");
        persistenceData[rowIdx, 1] = m_SubTaskUIHandler.GetUserAnswerStr();
        if (currTask is Task.IdentifyHighlightedObject)
        {
            // add pixel distance from the center
            var uv = ParseSelectedCoordinate();
            float dist = m_HighlightAnsHandler.ComputePixelDistFromCenter(uv);
            persistenceData[rowIdx, 1] += $";{dist.ToString("0.00")}";
        }
        persistenceData[rowIdx, 2] = IsUserAnswerCorrect() ? "True" : "False";
        //Debug.Log(persistenceData[rowIdx, 2]);

        // set sub ui
        switch (currTask)
        {
            case Task.CountShape:
                m_SubTaskUIHandler.ToggleCountShapeTaskUI(false);
                break;

            case Task.IdentifyHighlightedObject:
                //Utils.SaveTextureToPNGFile(m_Image.texture as Texture2D, Application.dataPath + "/Textures/sent_tex");
                m_SubTaskUIHandler.ToggleHighlightTaskUI(false);
                m_HighlightAnsHandler.DestroyObjects();
                break;

            case Task.ShellGame:
                m_SubTaskUIHandler.ToggleShellGameTaskUI(false);
                m_ShellGameRenderer.DestroyObjects();
                break;

            case Task.EngineGame:
                //Utils.SaveTextureToPNGFile(m_Image.texture as Texture2D, Application.dataPath + "/Textures/engine_sent_tex");
                m_SubTaskUIHandler.ToggleEngineTaskUI(false);
                m_EngineAnsHandler.DestroyObjects();
                break;

        }

        m_Image.texture = m_BgTex;
        registerAnswer = false;
        m_TextTimer.text = $"{0} s";
        m_Button.GetComponent<Button>().interactable = false;

        // send back to server
        m_Server.SendBackToClient();

        if (isFinalTask)
        {
            if (currConditionIndex == 0)
            {
                currConditionIndex = 1;
                m_TextInfo.SetText("Finish all the tasks for method 1. Please put on the headset to start the next method.");
            }
            else
            {
                currConditionIndex = 0;
                m_TextInfo.SetText("Finish all the tasks! Thank you!");
            }

            // output the user data to disk
            WriteToDisk();
        }
        else
        {
            m_TextInfo.SetText("Please put on the headset for the next task");
        }
    }

    private Vector2 ParseSelectedCoordinate()
    {
        if (currTask != Task.IdentifyHighlightedObject && currTask != Task.EngineGame)
            return Vector2.zero;

        Vector2 uv;
        string ans = m_SubTaskUIHandler.GetUserAnswerStr();
        string[] splitStrs = ans.Split(';');
        uv = new Vector2(int.Parse(splitStrs[0]), int.Parse(splitStrs[1]));
        return uv;
    }

    private bool IsUserAnswerCorrect()
    {
        bool isCorrect = false;
        string ans = "";
        Vector2 uv = Vector2.zero;

        switch (currTask)
        {
            case Task.IdentifyHighlightedObject:
                uv = ParseSelectedCoordinate();
                isCorrect = m_HighlightAnsHandler.ComputeCoordCorrectness(uv);
                break;

            case Task.CountShape:
                ans = m_SubTaskUIHandler.GetUserAnswerStr();
                int ansNum = int.Parse(ans);
                int correctNum = Constants.CountShapeAnswers[currConditionIndex, currTrialIdx];
                if (ansNum == correctNum)
                    isCorrect = true;
                break;

            case Task.ShellGame:
                ans = m_SubTaskUIHandler.GetUserAnswerStr();
                string[] splits = ans.Split(';');
                int selectIdx0 = int.Parse(splits[0]);
                int selectIdx1 = int.Parse(splits[1]);
                int correctIdx0 = Constants.ShellGameAnswers[currConditionIndex, currTrialIdx, 0];
                int correctIdx1 = Constants.ShellGameAnswers[currConditionIndex, currTrialIdx, 1];
                if (selectIdx0 == correctIdx0 && selectIdx1 == correctIdx1)
                    return true;
                break;

            case Task.EngineGame:
                uv = ParseSelectedCoordinate();
                isCorrect = m_EngineAnsHandler.ComputeCoordCorrectness(uv, currConditionIndex, currTrialIdx);
                break;

            default:
                break;
        }

        return isCorrect;
    }

    private void WriteToDisk()
    {
        string path = "";

        for (int i = 0; i < totalRows; i++)
        {
            Debug.Log("--------- " + i + " -----------");
            Debug.Log("time: " + persistenceData[i, 0]);
            Debug.Log("ans: " + persistenceData[i, 1]);
            Debug.Log("correctness: " + persistenceData[i, 2]);
        }

        path = Constants.DIR_PATH;

        string fname = currCondition == 1 ? "seamless_" : "convention_";
        fname += System.DateTime.Now.ToString("MMddHHmm") + ".csv";

        using (StreamWriter sw = new StreamWriter(Path.Combine(path, fname)))
        {
            for (int i = 0; i < totalRows; i++)
                sw.WriteLine(string.Join(",", GetRow(persistenceData, i)));
        }
    }

    // https://www.codegrepper.com/code-examples/csharp/select+a+whole+row+out+of+a+2d+array+C%23
    private string[] GetRow(string[,] matrix, int rowNumber)
    {
        return Enumerable.Range(0, matrix.GetLength(1))
                .Select(x => matrix[rowNumber, x])
                .ToArray();
    }
}
