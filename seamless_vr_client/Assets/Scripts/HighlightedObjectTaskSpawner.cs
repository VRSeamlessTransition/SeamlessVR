using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HighlightedObjectTaskSpawner : MonoBehaviour, ITaskInterface
{
    [HideInInspector] public Bounds veObjectBounds;
    [HideInInspector] public GameObject veObjectParent;

    [SerializeField] public ScreenManager m_ScreenManager;
    [SerializeField] private GameObject[] prefabList1;
    [SerializeField] private int[] indicesOfHighlightedObjects1;
    [SerializeField] private GameObject[] prefabList2;
    [SerializeField] private int[] indicesOfHighlightedObjects2;

    public Camera m_DisplayCamera;
    public Texture2D highlightTex;
    public Texture2D normalTex;

    // user study data for count shape task (TODO set up corresponding answer list in server)
    //public List<List<GameObject>> m_PrefabList = new List<List<GameObject>>(2);
    //public List<List<int>> m_IndexOfHighlightedObjects = new List<List<int>>(2);
    //public List<GameObject> m_PrebabList2 = new List<GameObject>(3);
    //public List<int> m_IndexOfHighlightedObjects2 = new List<int>(3);
    private int currTrialIndex = 0;
    private int currConditionIndex = 0;

    [HideInInspector] public int highlightedObjectIndex = -1;
    //const int numberOfValidHighlightedObjects = 5;

    private GameObject[] spawnedObjects;
    private bool spawned = false;

    //private Vector3 startingPoint;
    private float side = 1f;
    private int n_side = 5;  // shuqi: 5->2
    private int n;

    // send data
    private float vfov = 0f;
    private float f = 1f;
    private float scale = 1f;
    private Vector3 posInCam = Vector3.zero;

    // Start is called before the first frame update
    void Start()
    {
        n = n_side * n_side * n_side;
        //startingPoint = startPoint.transform.position;
        //Random.InitState(162);
        //spawnedObjects = new GameObject[n];
        //spawnedObjects = new GameObject[2];  // shuqi
    }

    // Update is called once per frame
    void Update()
    {

    }

    // shuqi
    public void StartTask(ScreenManager screenManager, int conditionIndex, int trialIndex, bool isFinalTask)
    {
        // TODO: based on the trial index to get the prefab list object
        currTrialIndex = trialIndex;
        currConditionIndex = conditionIndex;

        Vector3 pos = screenManager.GetScreenPosition();//dimensions.bottomLeft;
        Quaternion rot = screenManager.GetScreenRotation();//dimensions.bottomLeft;

        if (conditionIndex == 0)
        {
            veObjectParent = Instantiate(prefabList1[trialIndex % prefabList1.Length]);
        }
        else
        {
            veObjectParent = Instantiate(prefabList2[trialIndex % prefabList2.Length]);
        }

        // might need to adjust
        veObjectParent.transform.SetPositionAndRotation(pos, rot * veObjectParent.transform.rotation);
        //veObjectParent.tag = "VEObjectParent";
        //veObjectParent.layer = LayerMask.NameToLayer("VEObjectLayer");

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
        veObjectParent.transform.position = center;

        // set task params
        spawned = true;
        highlightedObjectIndex = (currConditionIndex == 0 ? indicesOfHighlightedObjects1 : indicesOfHighlightedObjects2)[trialIndex % 3];//(Random.Range(n_side / 4, 3 * n_side / 4 + 1) * n_side * n_side) + (Random.Range(n_side / 4, 3 * n_side / 4 + 1) * n_side) + (Random.Range(n_side / 4, 3 * n_side / 4 + 1));

        // compute local scale sent to the server
        float boundWidth = veObjectBounds.size.x;
        float boundHeight = veObjectBounds.size.y;
        float screenWidth = screenManager.GetScreenWidth();
        float screenHeight = screenManager.GetScreenHeight();
        float scaleFactor = boundWidth > boundHeight ? screenWidth / boundWidth : screenHeight / boundHeight;

        ComputeHighlightData(pos, new Vector3(scaleFactor, scaleFactor, scaleFactor));
    }

    private void ComputeHighlightData(Vector3 endPos, Vector3 endScale)
    {
        Vector3 startPos = veObjectParent.transform.position;
        Vector3 startScale = veObjectParent.transform.lossyScale;

        veObjectParent.transform.position = endPos;
        veObjectParent.transform.localScale = endScale;

        posInCam = m_DisplayCamera.transform.InverseTransformPoint(veObjectParent.transform.GetChild(highlightedObjectIndex).position);
        scale = veObjectParent.transform.GetChild(highlightedObjectIndex).lossyScale.x;
        Debug.Log("lossy scale of sphere target: " + scale);

        veObjectParent.transform.position = startPos;
        veObjectParent.transform.localScale = startScale;
    }

    public string GetHighlightData()
    {
        string data = $"{vfov},{f},{scale},{posInCam.x},{posInCam.y},{posInCam.z}";
        return data;
    }

    public void SetSendFovAndFocalLen(float fieldOfView, float focalLength)
    {
        vfov = fieldOfView;
        f = focalLength;
    }

    public void ToggleHighlightObject(bool setHighlight)
    {
        if (highlightedObjectIndex == -1)
            return;

        if (setHighlight)
            veObjectParent.transform.GetChild(highlightedObjectIndex).GetChild(0).gameObject.GetComponent<MeshRenderer>().material.mainTexture = highlightTex;
        else
            veObjectParent.transform.GetChild(highlightedObjectIndex).GetChild(0).gameObject.GetComponent<MeshRenderer>().material.mainTexture = normalTex;
    }

    public bool TaskStarted()
    {
        return spawned;
    }

    public void SetSpawned(bool value)
    {
        spawned = value;
    }

    public void DestroyObjects()
    {
        foreach (Transform obj in veObjectParent.transform)
        {
            Destroy(obj.gameObject);
        }
        Destroy(veObjectParent);
        spawned = false;
    }

    public void HideObjects()
    {
        veObjectParent.SetActive(false);
    }

    public int GetRandom09()
    {
        return Random.Range(0, 9);
    }

    void ITaskInterface.ActionBeforeTranstion()
    {
        ToggleHighlightObject(false);
    }

    public void SetTaskText(TMP_Text textMeshProText, int conditionIndex, int trialIndex)
    {
        textMeshProText.text = "Identify and remember highlighted sphere";
    }

    public Answer GetAnswer()
    {
        return Answer.Pending; //todo
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