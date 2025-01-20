using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum Task
{
    CountShape,
    IdentifyHighlightedObject,
    ShellGame,
    EngineGame
}

public enum ExperimentState
{
    NotCalibratedScreen,
    InitialSetupForSpecificUser,
    WaitingForUserToStartCurrentGame,
    BeforeInGame,
    InGame,
    ShellGameMotionAfterTransition,
    WaitingForServerResponseToStartNextGame,
    DoneWithGames,
    WaitingForNextUser,
}

public class SimpleMainManager : MonoBehaviour
{
    [SerializeField] private Camera m_DisplayCamera;
    [SerializeField] private Transform m_CenterEyeAnchor;
    [SerializeField] private ScreenManager m_ScreenManager;
    [SerializeField] private HighlightedObjectTaskSpawner m_HighlightTask;
    [SerializeField] private CountShapeTaskSpawner m_CountShapeTask;
    [SerializeField] private ShellGameTaskSpawner m_ShellGameTask;
    [SerializeField] private EngineGameSpawner m_EngineGameTask;
    [SerializeField] private SeamlessTransitionManager m_SeamlessTransManager;
    [SerializeField] private ConvTransitionManager m_ConvTransManager;
    [SerializeField] private ClientManager m_Server;
    public TMP_Text m_Text;
    //public TMP_Text m_TimerText;
    public AudioSource m_AudioSource;
    public GameObject m_Canvas;

    private Dimensions screenDimensions;
    private int displayWidth = Constants.DISPLAY_SCREEN_WIDTH;
    private int displayHeight = Constants.DISPLAY_SCREEN_HEIGHT;
    private RenderTexture m_RTDisplay;
    private RenderTexture RT_SentToServer;
    /*private bool isCalibrated = false;
    private bool isFirstSetup = true;*/
    private bool serverSentStartNext = false;
    private bool isFinalTask = false;  // final per condition
    private float timeSinceTaskStarted = 0;
    private float timeBeforeTransition = 3f;
    private float timeElapsedInVR = 0f;
    private bool startTimer = false;

    private int completedConditions = 1;
    private int currCondition = 0;  // 0:conv ; 1: seamless-transition
    private int currTrialIdx = 0;
    private int currConditionIndex = 0; // attention: not the sames as the currCondition

    private ExperimentState experimentState = ExperimentState.NotCalibratedScreen;
    private Task task;
    private HashSet<Task> completedTasks;
    private ITaskInterface currentTask;

    // returns true if done with tasks
    private bool SetupNextTaskRandom()
    {
        if (completedTasks.Count == Constants.TOTAL_TASK_NUM)
        {
            return true;
        }
        task = Task.IdentifyHighlightedObject; //todo for testing only
        //task = (Task)UnityEngine.Random.Range(0, numTasks);
        if (completedTasks.Contains(task))
        {
            Task tmpTask = (Task)(((int)task + 1) % Constants.TOTAL_TASK_NUM);
            while (tmpTask != task)
            {
                if (!completedTasks.Contains(tmpTask))
                {
                    task = tmpTask;
                    break;
                }
                tmpTask = (Task)(((int)tmpTask + 1) % Constants.TOTAL_TASK_NUM);
            }
        }

        completedTasks.Add(task);
        if (completedTasks.Count == Constants.TOTAL_TASK_NUM)
        {
            isFinalTask = true;
        }

        currentTask = task switch
        {
            Task.CountShape => m_CountShapeTask,
            Task.IdentifyHighlightedObject => m_HighlightTask,
            //Task.ShellGame => m_ShellGameTask,
            _ => throw new System.NotImplementedException(),
        };
        return false;
    }

    // shuqi
    private bool SetupNext()
    {
        if (currTrialIdx >= Constants.TOTAL_TRIALS_PER_TASK)
        {
            completedTasks.Add(task);
            currTrialIdx = 0;

            // determine whether to the end of the study
            if (completedTasks.Count == Constants.TOTAL_TASK_NUM)
            {
                if (completedConditions == Constants.TOTAL_CONDITIONS)
                    return true;
                else
                {
                    completedTasks.Clear();
                    task = 0;
                    currCondition = (currCondition == 0) ? 1 : 0;
                    currConditionIndex = 1;
                    completedConditions = Constants.TOTAL_CONDITIONS;
                }
            }

            // get next task
            if (completedTasks.Contains(task))
            {
                task = (Task)(((int)task + 1) % Constants.TOTAL_TASK_NUM);
            }
        }

        if (completedTasks.Count == Constants.TOTAL_TASK_NUM - 1 && currTrialIdx == Constants.TOTAL_TRIALS_PER_TASK - 1)
            isFinalTask = true;
        else
            isFinalTask = false;

        // assign task instance
        currentTask = task switch
        {
            Task.CountShape => m_CountShapeTask,
            Task.IdentifyHighlightedObject => m_HighlightTask,
            Task.ShellGame => m_ShellGameTask,
            Task.EngineGame => m_EngineGameTask,
            _ => throw new System.NotImplementedException(),
        };
        return false;
    }

    void Start()
    {
        DisplayCameraSetup();

        completedTasks = new HashSet<Task>();

        // set up the condition non-randomly
        currCondition = 1;
        //c->0; s->1

        task = Task.CountShape;
        SetupNext();

        StartCoroutine(InitCanvasPos());
    }

    private IEnumerator InitCanvasPos()
    {
        while (m_CenterEyeAnchor.position == Vector3.zero)
            yield return null;

        m_Canvas.transform.position = m_CenterEyeAnchor.position - 0.2f * Vector3.up + Vector3.forward * 0.5f;
    }

    private void OnEnable()
    {
        m_SeamlessTransManager.OnMorphFinished += AfterSeamlessTransition;
        m_ConvTransManager.OnConvTransFinshed += AfterConvTransition;
        m_Server.OnServerMessageReceived += RecvFromServer;
    }

    private void OnDisable()
    {
        m_SeamlessTransManager.OnMorphFinished -= AfterSeamlessTransition;
        m_ConvTransManager.OnConvTransFinshed -= AfterConvTransition;
        m_Server.OnServerMessageReceived -= RecvFromServer;
    }

    private void RecvFromServer()
    {
        Debug.Log("receive from server event trigger");
        serverSentStartNext = true;
    }

    private void DisplayCameraSetup()
    {
        m_DisplayCamera.enabled = false;

        // init render texture for display camera
        m_RTDisplay = new RenderTexture(displayWidth, displayHeight, 24);
        m_RTDisplay.filterMode = FilterMode.Bilinear;
        m_RTDisplay.wrapMode = TextureWrapMode.Clamp;
        if (m_DisplayCamera.targetTexture != null)
            m_DisplayCamera.targetTexture.Release();
        m_DisplayCamera.targetTexture = m_RTDisplay;
    }

    private void ActionBeforeTakeOffHeadset()
    {
        StartCoroutine(BeforeDestroyObjects());
        m_Text.text = "Take off headset.";
        startTimer = false;
        //m_TimerText.gameObject.SetActive(false);
        m_AudioSource.Play();
    }

    private void AfterSeamlessTransition()
    {
        ActionBeforeTakeOffHeadset();

        if (currentTask is ShellGameTaskSpawner)
            return;

        m_DisplayCamera.Render();
        RT_SentToServer = m_RTDisplay;
        SendToServer();
    }

    private void AfterConvTransition()
    {
        ActionBeforeTakeOffHeadset();

        if (currentTask is ShellGameTaskSpawner)
            return;

        RT_SentToServer = m_ConvTransManager.GetRenderTexture();
        SendToServer();
    }

    private IEnumerator BeforeDestroyObjects()
    {
        yield return new WaitForSeconds(Constants.TIME_WAIT_BEFORE_DESTROY_OBJECT);

        if (!(currentTask is ShellGameTaskSpawner))
            currentTask.HideObjects();

        //if (currCondition == 1)  // seamless
        //    m_ScreenManager.ToggleScreenFrame(false);
    }

    private void SendToServer()
    {
        if (RT_SentToServer == null)
        {
            Debug.LogError("render texture is null! Please check!");
            return;
        }

        Texture2D sendTex = Utils.RenderTextureToTexture2D(RT_SentToServer);
        int isFinalToInt = isFinalTask ? 1 : 0;
        string data = $"{(int)task},{currTrialIdx},{isFinalToInt},{timeElapsedInVR},{currCondition},";  // !! make sure the first elem is task type

        switch (task)
        {
            case Task.CountShape:
                var countTask = currentTask as CountShapeTaskSpawner;
                if (countTask != null)
                {
                    data += $"{countTask.idxOfTaskShape},{countTask.countByObjectType[countTask.idxOfTaskShape]}";
                    m_Server.SendData(sendTex, data);
                }
                else
                {
                    Debug.Log("Task is count shape but current task is " + currentTask.GetType());
                }
                break;

            case Task.IdentifyHighlightedObject:
                var highlightTask = currentTask as HighlightedObjectTaskSpawner;
                if (highlightTask != null)
                {
                    string taskData = highlightTask.GetHighlightData();
                    data += taskData;
                    m_Server.SendData(sendTex, data);
                }
                else
                {
                    Debug.Log("Task is highlight object but current task is " + currentTask.GetType());
                }
                break;

            case Task.EngineGame:
                var engineTask = currentTask as EngineGameSpawner;
                if (engineTask != null)
                {
                    string taskData = engineTask.GetEngineData();
                    data += taskData;
                    m_Server.SendData(sendTex, data);
                }
                else
                {
                    Debug.Log("Task is engine game but current task is " + currentTask.GetType());
                }
                break;
        }
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
        m_SeamlessTransManager.UpdateFlattenCameraPose(camPos, camRot);
        m_ConvTransManager.UpdateConvCameraPose(camPos, camRot);

        // setup display camera to output rendered result to physical display
        m_DisplayCamera.gameObject.transform.position = camPos;
        m_DisplayCamera.gameObject.transform.rotation = camRot;
        float caliScreenHeight = m_ScreenManager.GetScreenHeight();
        float hfov = 2 * Mathf.Rad2Deg * Mathf.Atan(caliScreenHeight / 2 / zDistFromScreen);
        m_DisplayCamera.fieldOfView = hfov;
        m_DisplayCamera.focalLength = zDistFromScreen;

        // set sent server parameters
        m_ShellGameTask.SetSendFovAndFocalLen(hfov, zDistFromScreen);
        m_ShellGameTask.SetConditionType(currCondition);
        m_HighlightTask.SetSendFovAndFocalLen(hfov, zDistFromScreen);
        m_EngineGameTask.SetSendFovAndFocalLen(hfov, zDistFromScreen);
        Debug.Log(hfov);
    }

    private void UpdateTimer()
    {
        if (!startTimer)
            return;

        timeElapsedInVR += Time.deltaTime;
        //m_TimerText.text = $"{(int)timeElapsedInVR} s";
    }

    private IEnumerator StartNext()
    {
        currentTask.SetTaskText(m_Text, currConditionIndex, currTrialIdx);

        yield return new WaitForSeconds(3f);

        m_Canvas.gameObject.SetActive(false);
        //m_TimerText.gameObject.SetActive(true);
        currentTask.StartTask(m_ScreenManager, currConditionIndex, currTrialIdx, isFinalTask);
        experimentState = ExperimentState.InGame;
        timeElapsedInVR = 0f;
        startTimer = true;
    }

    void Update()
    {
        UpdateTimer();

        switch (experimentState)
        {
            case ExperimentState.NotCalibratedScreen:
                if (!m_ScreenManager.GetCalibrated())
                {
                    m_ScreenManager.SetCanCalibrate(true);
                    return;
                }
                else
                {
                    //screenDimensions = m_ScreenManager.GetDimensions();
                    experimentState = ExperimentState.InitialSetupForSpecificUser;
                }
                break;

            case ExperimentState.InitialSetupForSpecificUser:
                ConfigureCameras();
                experimentState = ExperimentState.WaitingForUserToStartCurrentGame;
                break;

            case ExperimentState.WaitingForUserToStartCurrentGame:
                if (OVRInput.GetDown(m_ScreenManager.triggerButton))
                {
                    m_ScreenManager.SetScreenVisible(false);
                    experimentState = ExperimentState.BeforeInGame;
                    StartCoroutine(StartNext());
                }
                else
                {
                    string str = (completedConditions == Constants.TOTAL_CONDITIONS) ? "Method 2\n" : "Method 1\n";
                    str += $"Press the {m_ScreenManager.triggerHandName} trigger to start the next task.";
                    m_Text.text = str;
                }
                break;

            case ExperimentState.InGame:
                timeSinceTaskStarted += Time.deltaTime;
                if (timeSinceTaskStarted > timeBeforeTransition)
                {
                    timeSinceTaskStarted = 0f;
                    currentTask.ActionBeforeTranstion();

                    // two conditions
                    if (currCondition == 1)
                    {
                        m_SeamlessTransManager.SetSeamlessTransition(currentTask);
                        //m_ScreenManager.ToggleScreenFrame(true);
                    }
                    else
                    {
                        m_ConvTransManager.SetConvTransition(currentTask);
                    }

                    experimentState = ExperimentState.WaitingForServerResponseToStartNextGame;
                }
                break;

            case ExperimentState.WaitingForServerResponseToStartNextGame:
                if (serverSentStartNext)
                {
                    currentTask.DestroyObjects();
                    timeSinceTaskStarted = 0f;
                    serverSentStartNext = false;
                    m_Canvas.gameObject.SetActive(true);
                    currTrialIdx++;

                    bool doneWithGames = SetupNext();
                    if (doneWithGames)
                    {
                        experimentState = ExperimentState.DoneWithGames;
                    }
                    else
                    {
                        experimentState = ExperimentState.WaitingForUserToStartCurrentGame;
                    }
                }
                break;

            case ExperimentState.DoneWithGames:
                m_Text.text = "Thank you for participating in this experiment! Please take off your headset";
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    experimentState = ExperimentState.WaitingForNextUser;
                }
                break;

            case ExperimentState.WaitingForNextUser:
                if (OVRManager.instance.isUserPresent)
                {
                    experimentState = ExperimentState.InitialSetupForSpecificUser;
                }
                break;
        }
    }
}


//using System.Collections;
//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;

//public enum Task
//{
//    CountShape,
//    IdentifyHighlightedObject,
//    ShellGame,
//    EngineGame
//}

//public enum ExperimentState
//{
//    NotCalibratedScreen,
//    InitialSetupForSpecificUser,
//    WaitingForUserToStartCurrentGame,
//    BeforeInGame,
//    InGame,
//    ShellGameMotionAfterTransition,
//    WaitingForServerResponseToStartNextGame,
//    DoneWithGames,
//    WaitingForNextUser,
//}

//public class SimpleMainManager : MonoBehaviour
//{
//    [SerializeField] private Camera m_DisplayCamera;
//    [SerializeField] private Transform m_CenterEyeAnchor;
//    [SerializeField] private ScreenManager m_ScreenManager;
//    [SerializeField] private HighlightedObjectTaskSpawner m_HighlightTask;
//    [SerializeField] private CountShapeTaskSpawner m_CountShapeTask;
//    [SerializeField] private ShellGameTaskSpawner m_ShellGameTask;
//    [SerializeField] private EngineGameSpawner m_EngineGameTask;
//    [SerializeField] private SeamlessTransitionManager m_SeamlessTransManager;
//    [SerializeField] private ConvTransitionManager m_ConvTransManager;
//    public TMP_Text m_Text;
//    public GameObject m_Canvas;

//    private Dimensions screenDimensions;
//    private int displayWidth = Constants.DISPLAY_SCREEN_WIDTH;
//    private int displayHeight = Constants.DISPLAY_SCREEN_HEIGHT;
//    private RenderTexture m_RTDisplay;
//    private RenderTexture RT_SentToServer;
//    /*private bool isCalibrated = false;
//    private bool isFirstSetup = true;*/
//    private bool serverSentStartNext = false;
//    private bool isFinalTask = false;  // final per condition
//    private float timeSinceTaskStarted = 0;
//    private float timeBeforeTransition = 3f;
//    private float timeElapsedInVR = 0f;
//    private bool startTimer = false;

//    private int completedConditions = 1;
//    private int currCondition = 0;  // 0:conv ; 1: seamless-transition
//    private int currTrialIdx = 0;
//    private int currConditionIndex = 0; // attention: not the sames as the currCondition

//    private ExperimentState experimentState = ExperimentState.NotCalibratedScreen;
//    private Task task;
//    private HashSet<Task> completedTasks;
//    private ITaskInterface currentTask;

//    // shuqi
//    private bool SetupNext()
//    {
//        if (currTrialIdx >= Constants.TOTAL_TRIALS_PER_TASK)
//        {
//            completedTasks.Add(task);
//            currTrialIdx = 0;

//            // determine whether to the end of the study
//            if (completedTasks.Count == Constants.TOTAL_TASK_NUM)
//            {
//                if (completedConditions == Constants.TOTAL_CONDITIONS)
//                    return true;
//                else
//                {
//                    completedTasks.Clear();
//                    task = 0;
//                    currCondition = (currCondition == 0) ? 1 : 0;
//                    currConditionIndex = 1;
//                    completedConditions = Constants.TOTAL_CONDITIONS;
//                }
//            }

//            // get next task
//            if (completedTasks.Contains(task))
//            {
//                task = (Task)(((int)task + 1) % Constants.TOTAL_TASK_NUM);
//            }
//        }

//        if (completedTasks.Count == Constants.TOTAL_TASK_NUM - 1 && currTrialIdx == Constants.TOTAL_TRIALS_PER_TASK - 1)
//            isFinalTask = true;
//        else
//            isFinalTask = false;

//        // assign task instance
//        currentTask = task switch
//        {
//            Task.CountShape => m_CountShapeTask,
//            Task.IdentifyHighlightedObject => m_HighlightTask,
//            Task.ShellGame => m_ShellGameTask,
//            Task.EngineGame => m_EngineGameTask,
//            _ => throw new System.NotImplementedException(),
//        };
//        return false;
//    }

//    void Start()
//    {
//        DisplayCameraSetup();

//        completedTasks = new HashSet<Task>();

//        // set up the condition non-randomly
//        currCondition = 1;
//        //c->0; s->1

//        task = Task.CountShape;
//        SetupNext();

//        StartCoroutine(InitCanvasPos());
//    }

//    private IEnumerator InitCanvasPos()
//    {
//        while (m_CenterEyeAnchor.position == Vector3.zero)
//            yield return null;

//        m_Canvas.transform.position = m_CenterEyeAnchor.position - 0.2f * Vector3.up + Vector3.forward * 0.5f;
//    }

//    private void OnEnable()
//    {
//        m_SeamlessTransManager.OnMorphFinished += AfterSeamlessTransition;
//        m_ConvTransManager.OnConvTransFinshed += AfterConvTransition;
//    }

//    private void OnDisable()
//    {
//        m_SeamlessTransManager.OnMorphFinished -= AfterSeamlessTransition;
//        m_ConvTransManager.OnConvTransFinshed -= AfterConvTransition;
//    }

//    private void DisplayCameraSetup()
//    {
//        m_DisplayCamera.enabled = false;

//        // init render texture for display camera
//        m_RTDisplay = new RenderTexture(displayWidth, displayHeight, 24);
//        m_RTDisplay.filterMode = FilterMode.Bilinear;
//        m_RTDisplay.wrapMode = TextureWrapMode.Clamp;
//        if (m_DisplayCamera.targetTexture != null)
//            m_DisplayCamera.targetTexture.Release();
//        m_DisplayCamera.targetTexture = m_RTDisplay;
//    }

//    private void ActionBeforeTakeOffHeadset()
//    {
//        StartCoroutine(BeforeDestroyObjects());
//        m_Text.text = "Take off headset.";
//    }

//    private void AfterSeamlessTransition()
//    {
//        ActionBeforeTakeOffHeadset();
//    }

//    private void AfterConvTransition()
//    {
//        ActionBeforeTakeOffHeadset();
//    }

//    private IEnumerator BeforeDestroyObjects()
//    {
//        yield return new WaitForSeconds(Constants.TIME_WAIT_BEFORE_DESTROY_OBJECT);

//        currentTask.HideObjects();

//        //if (!(currentTask is ShellGameTaskSpawner))
//        //    currentTask.HideObjects();

//        //if (currCondition == 1)  // seamless
//        //    m_ScreenManager.ToggleScreenFrame(false);
//    }


//    private void ConfigureCameras()
//    {
//        // based on the screen pose and approximate head position
//        Vector3 screenCenter = m_ScreenManager.GetScreenPosition();
//        Vector3 forward = m_ScreenManager.GetScreenForward();
//        Vector3 up = m_ScreenManager.GetScreenUp();
//        Vector3 right = m_ScreenManager.GetScreenRight();
//        Vector3 headPos = m_CenterEyeAnchor.position;
//        float zDistFromScreen = Mathf.Abs(Utils.PosFromWorldToLocal(screenCenter, right, forward, up, headPos).z);
//        Vector3 camPos = screenCenter - forward * zDistFromScreen;
//        Quaternion camRot = m_ScreenManager.GetScreenRotation();

//        // make sure the morph camera pose is the same as the conventional camera
//        m_SeamlessTransManager.UpdateFlattenCameraPose(camPos, camRot);
//        //m_ConvTransManager.UpdateConvCameraPose(camPos, camRot);
//    }

//    private void UpdateTimer()
//    {
//        if (!startTimer)
//            return;

//        timeElapsedInVR += Time.deltaTime;
//        //m_TimerText.text = $"{(int)timeElapsedInVR} s";
//    }

//    private IEnumerator StartNext()
//    {
//        currentTask.SetTaskText(m_Text, currConditionIndex, currTrialIdx);

//        yield return new WaitForSeconds(3f);

//        m_Canvas.gameObject.SetActive(false);
//        //m_TimerText.gameObject.SetActive(true);
//        currentTask.StartTask(m_ScreenManager, currConditionIndex, currTrialIdx, isFinalTask);
//        experimentState = ExperimentState.InGame;
//        timeElapsedInVR = 0f;
//        startTimer = true;
//    }

//    void Update()
//    {
//        UpdateTimer();

//        switch (experimentState)
//        {
//            case ExperimentState.NotCalibratedScreen:
//                if (!m_ScreenManager.GetCalibrated())
//                {
//                    m_ScreenManager.SetCanCalibrate(true);
//                    return;
//                }
//                else
//                {
//                    //screenDimensions = m_ScreenManager.GetDimensions();
//                    experimentState = ExperimentState.InitialSetupForSpecificUser;
//                }
//                break;

//            case ExperimentState.InitialSetupForSpecificUser:
//                ConfigureCameras();
//                experimentState = ExperimentState.WaitingForUserToStartCurrentGame;
//                break;

//            case ExperimentState.WaitingForUserToStartCurrentGame:
//                if (OVRInput.GetDown(m_ScreenManager.triggerButton))
//                {
//                    m_ScreenManager.SetScreenVisible(false);
//                    experimentState = ExperimentState.BeforeInGame;
//                    StartCoroutine(StartNext());
//                }
//                else
//                {
//                    string str = (completedConditions == Constants.TOTAL_CONDITIONS) ? "Method 2\n" : "Method 1\n";
//                    str += $"Press the {m_ScreenManager.triggerHandName} trigger to start the next task.";
//                    m_Text.text = str;
//                }
//                break;

//            case ExperimentState.InGame:
//                timeSinceTaskStarted += Time.deltaTime;
//                if (timeSinceTaskStarted > timeBeforeTransition)
//                {
//                    timeSinceTaskStarted = 0f;
//                    currentTask.ActionBeforeTranstion();

//                    // two conditions
//                    if (currCondition == 1)
//                    {
//                        m_SeamlessTransManager.SetSeamlessTransition(currentTask);
//                        //m_ScreenManager.ToggleScreenFrame(true);
//                    }
//                    else
//                    {
//                        m_ConvTransManager.SetConvTransition(currentTask);
//                    }

//                    experimentState = ExperimentState.WaitingForServerResponseToStartNextGame;
//                }
//                break;

//            case ExperimentState.WaitingForServerResponseToStartNextGame:
//                if (OVRInput.GetDown(OVRInput.RawButton.A))
//                {
//                    currentTask.DestroyObjects();
//                    timeSinceTaskStarted = 0f;
//                    m_Canvas.gameObject.SetActive(true);
//                    currTrialIdx++;

//                    bool doneWithGames = SetupNext();
//                    if (doneWithGames)
//                    {
//                        experimentState = ExperimentState.DoneWithGames;
//                    }
//                    else
//                    {
//                        experimentState = ExperimentState.WaitingForUserToStartCurrentGame;
//                    }
//                }
//                break;

//            case ExperimentState.DoneWithGames:
//                m_Text.text = "Thank you for participating in this experiment! Please take off your headset";
//                break;
//        }
//    }
//}
