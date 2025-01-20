using Meta.WitAi;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    public GameObject rightBlipPos;
    public GameObject leftBlipPos;
    public TMP_Text textMeshProText;
    public GameObject m_QuadPrefab;
    public Material m_ScreenFrameMat;

    private Vector3 cornerTLPos;
    private Vector3 cornerTRPos;
    private Vector3 cornerBRPos;
    private Vector3 n;
    private GameObject screenPlane;
    private GameObject blockingPlane;
    private GameObject screenWireFrame;

    private bool calibrated = false;
    private bool canCalibrate = false;
    private bool handednessDetermined = false;
    private bool cornerTLDefined = false;
    private bool cornerTRDefined = false;
    private bool cornerBRDefined = false;

    [HideInInspector] public OVRInput.RawButton triggerButton = OVRInput.RawButton.RIndexTrigger;
    [HideInInspector] public string triggerHandName;

    private GameObject blipPos;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (canCalibrate)
        {
            if (handednessDetermined == false && calibrated == false)
            {
                textMeshProText.text = "Press either trigger to set up responses.";
            }
            else if (cornerTLDefined == false && calibrated == false)
            {
                textMeshProText.text = $"Press the {triggerHandName} trigger after placing the orange marker at the top-left corner of your screen.";
            }
            else if (cornerTRDefined == false && calibrated == false)
            {
                textMeshProText.text = $"Press the {triggerHandName} trigger after placing the orange marker at the top-right corner of your screen.";
            }
            else if (cornerBRDefined == false && calibrated == false)
            {
                textMeshProText.text = $"Press the {triggerHandName} trigger after placing the orange marker at the bottom-right corner of your screen.";
            }


            if (!handednessDetermined)
            {
                if (OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger))
                {
                    triggerButton = OVRInput.RawButton.LIndexTrigger;
                    triggerHandName = "left";
                    blipPos = leftBlipPos;
                    handednessDetermined = true;
                }
                else if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
                {
                    triggerButton = OVRInput.RawButton.RIndexTrigger;
                    triggerHandName = "right";
                    blipPos = rightBlipPos;
                    handednessDetermined = true;
                }
            }
            else if (OVRInput.GetDown(triggerButton) && !cornerTLDefined)
            {
                cornerTLPos = blipPos.transform.position;
                cornerTLDefined = true;
            }

            else if (OVRInput.GetDown(triggerButton) && !cornerTRDefined)
            {
                cornerTRPos = blipPos.transform.position;
                cornerTRDefined = true;
            }

            else if (OVRInput.GetDown(triggerButton) && !cornerBRDefined)
            {
                cornerBRPos = blipPos.transform.position;
                //DrawPlane(cornerTLPos, cornerTRPos, cornerBRPos);
                DrawPlane();
                //InitScreenFrame();
                cornerBRDefined = true;
                calibrated = true;
            }
        }

    }

    // shuqi
    public Quaternion GetScreenRotation()
    {
        return Quaternion.LookRotation(GetScreenForward(), GetScreenUp());
    }

    // shuqi
    public void SetScreenVisible(bool isVisble)
    {
        if (screenPlane == null)
            return;

        screenPlane.SetActive(isVisble);
    }

    // shuqi
    void DrawPlane()
    {
        Vector3 pos = GetScreenPosition();
        Quaternion rot = GetScreenRotation();
        screenPlane = Instantiate(m_QuadPrefab, pos, rot);

        float xScale = Vector3.Distance(cornerTRPos, cornerTLPos);
        float yScale = Vector3.Distance(cornerBRPos, cornerTRPos);
        screenPlane.transform.localScale = new Vector3(xScale, yScale, 1f);

        //InitScreenFrame();
        //ToggleScreenFrame(false);
    }

    
    public bool GetCalibrated()
    {
        return calibrated;
    }

    public void SetCanCalibrate(bool val)
    {
        if (canCalibrate != val)
            canCalibrate = val;
    }


    public Vector3 GetScreenPosition()
    {
        return (cornerTLPos + cornerBRPos) / 2;
    }

    public Vector3 GetScreenForward()
    {
        return -Vector3.Cross(cornerTRPos - cornerTLPos, cornerBRPos - cornerTRPos).normalized;
    }

    public Vector3 GetScreenUp()
    {
        return (cornerTRPos - cornerBRPos).normalized;
    }

    public Vector3 GetScreenRight()
    {
        return (cornerTRPos - cornerTLPos).normalized;
    }

    public void ToggleScreenFrame(bool isVisible)
    {
        screenWireFrame.SetActive(isVisible);
    }

    public void MoveScreenBackward()
    {
        screenPlane.transform.Translate(new Vector3(0f, 0f, 0.002f));
    }

    private void InitScreenFrame()
    {
        Vector3 pos = screenPlane.transform.position + screenPlane.transform.forward * 0.003f;
        Vector3 right = screenPlane.transform.right * 0.5f * screenPlane.transform.lossyScale.x;
        Vector3 up = screenPlane.transform.up * 0.5f * screenPlane.transform.lossyScale.y;
        Vector3 topLeft = pos - right + up;
        Vector3 topRight = pos + right + up;
        Vector3 bottomRight = pos + right - up;
        Vector3 bottomLeft = pos - right - up;

        screenWireFrame = new GameObject("Screen Wireframe");
        screenWireFrame.AddComponent<LineRenderer>();
        LineRenderer lr = screenWireFrame.transform.GetComponent<LineRenderer>();

        lr.material = m_ScreenFrameMat;
        lr.positionCount = 6;
        lr.startWidth = 0.005f;
        lr.endWidth = 0.005f;
        lr.SetPosition(0, topLeft);
        lr.SetPosition(1, topRight);
        lr.SetPosition(2, bottomRight);
        lr.SetPosition(3, bottomLeft);
        lr.SetPosition(4, topLeft);
        lr.SetPosition(5, topRight);
    }


    public float GetScreenHeight()
    {
        return Vector3.Distance(cornerTRPos, cornerBRPos);
    }

    public float GetScreenWidth()
    {
        return Vector3.Distance(cornerTLPos, cornerTRPos);
    }
}