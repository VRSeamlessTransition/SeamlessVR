using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

public class SubTaskUIHandler : MonoBehaviour
{
    public TextMeshProUGUI m_TextInfo;
    public TextMeshProUGUI m_AnsTextInfo;
    public TMP_InputField m_NumInput;
    public GameObject m_ToggleGroup;
    public List<Toggle> toggles = new List<Toggle>(5);

    private Task currTask;
    private Vector2 cursorCoord;
    private string inputNumValue;

    void Start()
    {
        ToggleCountShapeTaskUI(false);
        ToggleHighlightTaskUI(false);
        ToggleShellGameTaskUI(false);
    }

    void Update()
    {
        switch (currTask)
        {
            case Task.IdentifyHighlightedObject:
                ListenToMouseClick();
                break;

            case Task.CountShape:
                ListenToInputValue();
                break;

            case Task.EngineGame:
                ListenToMouseClick();
                break;

            default:
                break;
        }
    }

    private void ListenToMouseClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // ignore button lick
            GameObject clickObject = EventSystem.current.currentSelectedGameObject;
            if (clickObject != null && clickObject.GetComponent<Button>() != null)
            {
                Debug.Log("click on the button, ignore mouse position");
                return;
            }

            cursorCoord = Input.mousePosition;
            m_AnsTextInfo.SetText($"Selected: {(int)cursorCoord.x}, {(int)cursorCoord.y}");
        }
    }

    public void ListenToInputValue()
    {
        inputNumValue = m_NumInput.text;
    }

    public void ToggleShellGameTaskUI(bool isShow)
    {
        if (isShow)
        {
            currTask = Task.ShellGame;
            m_ToggleGroup.SetActive(true);
            ClearToggleSelected();
        }
        else
        {
            m_ToggleGroup.SetActive(false);
            ClearToggleSelected();
            currTask = Task.None;
        }
    }

    private void ClearToggleSelected()
    {
        foreach (Toggle toggle in toggles)
        {
            toggle.isOn = false;
        }
    }

    public void ToggleHighlightTaskUI(bool isShow)
    {
        if (isShow)
        {
            currTask = Task.IdentifyHighlightedObject;
            m_AnsTextInfo.gameObject.SetActive(true);
            m_AnsTextInfo.SetText("Selected: None");
        }
        else
        {
            m_AnsTextInfo.gameObject.SetActive(false);
            currTask = Task.None;
        }
    }

    public void ToggleCountShapeTaskUI(bool isShow)
    {
        if (isShow)
        {
            currTask = Task.CountShape;
            m_AnsTextInfo.gameObject.SetActive(true);
            m_AnsTextInfo.SetText("Input Num:");
            m_NumInput.gameObject.SetActive(true);
        }
        else
        {
            m_AnsTextInfo.gameObject.SetActive(false);
            m_NumInput.gameObject.SetActive(false);
            m_NumInput.text = "0";
            currTask = Task.None;
        }
    }

    public void ToggleEngineTaskUI(bool isShow)
    {
        if (isShow)
        {
            currTask = Task.EngineGame;
            m_AnsTextInfo.gameObject.SetActive(true);
            m_AnsTextInfo.SetText("Selected: None");
        }
        else
        {
            m_AnsTextInfo.gameObject.SetActive(false);
            currTask = Task.None;
        }
    }


    public bool IsAnswerValidate()
    {
        bool isValidate = false;

        switch (currTask)
        {
            case Task.IdentifyHighlightedObject:
                if (!m_AnsTextInfo.text.Contains("None"))
                    isValidate = true;
                break;

            case Task.CountShape:
                if (int.Parse(inputNumValue) > 0)  // can add more acucrate validation
                    isValidate = true;
                break;

            case Task.ShellGame:
                int cnt = 0;
                foreach (var toggle in toggles)
                {
                    if (toggle.isOn)
                        cnt++;
                }
                if (cnt == 2)
                    isValidate = true;
                break;

            case Task.EngineGame:
                if (!m_AnsTextInfo.text.Contains("None"))
                    isValidate = true;
                break;


            default:
                break;
        }

        return isValidate;
    }

    public string GetUserAnswerStr()
    {
        string ans = "";
        switch (currTask)
        {
            case Task.IdentifyHighlightedObject:
                ans += $"{(int)cursorCoord.x};{(int)cursorCoord.y}"; // use ';' to split to avoid conflit in the output format  
                break;

            case Task.CountShape:
                ans += $"{inputNumValue}";
                break;

            case Task.ShellGame:
                List<string> ansPair = new List<string>();
                foreach (var toggle in toggles)
                {
                    if (toggle.isOn)
                    {
                        ansPair.Add(toggle.GetComponentInChildren<Text>().text);
                    }
                }
                if (ansPair.Count == 2)
                    ans += $"{ansPair[0]};{ansPair[1]}";
                break;

            case Task.EngineGame:
                ans += $"{(int)cursorCoord.x};{(int)cursorCoord.y}";
                break;

            default:
                break;
        }

        return ans;
    }

}
