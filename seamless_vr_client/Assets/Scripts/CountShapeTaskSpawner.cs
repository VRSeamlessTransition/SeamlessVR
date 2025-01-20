using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum CountTaskType
{ 
     redCubes,
     greenCubes,
     blueCubes,
     redSpheres,
     greenSpheres,
     blueSpheres,
     redPyramids,
     greenPyramids,
     bluePyramids,
}
public class CountShapeTaskSpawner : MonoBehaviour, ITaskInterface
{
    [HideInInspector] public Bounds veObjectBounds;
    [HideInInspector] public GameObject veObjectParent;

    [SerializeField] public ScreenManager m_ScreenManager;

    // user study data for count shape task (TODO set up corresponding answer list in server)
    public GameObject[] prefabList1;
    public int[] answerList1;
    public CountTaskType[] taskTypeList1;
    public GameObject[] prefabList2;
    public int[] answerList2;
    public CountTaskType[] taskTypeList2;

    private int currTrialIndex = 0;
    private int currConditionIndex = 0;

    [HideInInspector] public int[] countByObjectType = null;
    [HideInInspector] public int idxOfTaskShape = -1;

    private GameObject[] spawnedObjects;
    private bool spawned = false;
    private Vector3 startingPoint;
    private float side = 1f;
    private int n_side = 5;
    private int n;
    private float delta;

    // Start is called before the first frame update
    void Start()
    {
        n = n_side * n_side * n_side;
        countByObjectType = new int[9];
        spawnedObjects = new GameObject[n];
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartTask(ScreenManager screenManager, int conditionIndex, int trialIndex, bool isFinalTask)
    {
        currTrialIndex = trialIndex;
        currConditionIndex = conditionIndex;

        Vector3 pos = screenManager.GetScreenPosition();//dimensions.bottomLeft;
        Quaternion rot = screenManager.GetScreenRotation() * Quaternion.Euler(0, 180,0);//dimensions.bottomLeft;
        CountTaskType currCountShapeType;

        if (conditionIndex == 0)
        {
            veObjectParent = Instantiate(prefabList1[trialIndex % prefabList1.Length], pos, rot);
            currCountShapeType = taskTypeList1[trialIndex % taskTypeList1.Length];
        }
        else
        {
            veObjectParent = Instantiate(prefabList2[trialIndex % prefabList2.Length], pos, rot);
            currCountShapeType = taskTypeList2[trialIndex % taskTypeList2.Length];
        }
        idxOfTaskShape = (int)currCountShapeType;

        veObjectParent.tag = "VEObjectParent";
        veObjectParent.layer = LayerMask.NameToLayer("VEObjectLayer");
        veObjectBounds = new Bounds();

        int cnt;
        Vector3 center = Vector3.zero;
        for (cnt = 0; cnt < n; cnt++)
        {
            var ch = veObjectParent.transform.GetChild(0);
            spawnedObjects[cnt] = ch.gameObject;
            ch.parent = null;
            veObjectBounds.Encapsulate(ch.position);
            center += ch.transform.GetChild(0).GetComponent<Renderer>().bounds.center;
            veObjectBounds.Encapsulate(ch.transform.GetChild(0).GetComponent<Renderer>().bounds);
        }

        center /= cnt;
        veObjectBounds.center = center;
        veObjectParent.transform.position = center;  // important for the pivot placement
        foreach (var obj in spawnedObjects)
            obj.transform.parent = veObjectParent.transform;

        //SetTaskText(text);
        spawned = true;
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
        int cnt = 0;
        foreach (Transform obj in veObjectParent.transform)
        {
            Destroy(obj.gameObject);
            spawnedObjects[cnt++] = null;
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



    public void SetTaskText(TMP_Text textMeshProText, int currConditionIndex, int currTrialIndex)
    {
        CountTaskType type;
        if (currConditionIndex == 0)
        {
            type = taskTypeList1[currTrialIndex];
        }
        else
        {
            type = taskTypeList2[currTrialIndex];
        }
        
        textMeshProText.text = "Count " + (int)type switch
        {
            0 => "red cubes.",
            1 => "green cubes.",
            2 => "blue cubes.",
            3 => "red spheres.",
            4 => "green spheres.",
            5 => "blue spheres.",
            6 => "red pyramids.",
            7 => "green pyramids.",
            8 => "blue pyramids.",
            _ => "Meow.",
        };
    }

    public Answer GetAnswer()
    {
        return Answer.Pending;//todo
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
