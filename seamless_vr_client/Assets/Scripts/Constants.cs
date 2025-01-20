using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Constants : MonoBehaviour
{
    /**Below data might need to change for different devices*/
    public static int VR_CAM_WIN_WIDTH = 1824; 
    public static int VR_CAM_WIN_HEIGHT = 1968;
    public static int DISPLAY_SCREEN_WIDTH = 3840;
    public static int DISPLAY_SCREEN_HEIGHT = 2160;
    public static float TIME_BEFORE_TRANSITION = 1f;
    public static float TOTAL_TRANSITION_TIME = 3f;
    public static float TOTAL_TRANSITION_TIME_ENGINE_GAME = 5f;
    public static float TOTAL_TRANSITION_TIME_COUNT_SHAPE = 6f;  // 8s before
    public static float TOTAL_TRANSITION_TIME_SHELL_GAME = 6f;
    public static float TIME_WAIT_BEFORE_DESTROY_OBJECT = 2f;

    // shell game animation setting
    public static float SHELL_GAME_TOTAL_ANIM_COUNT = 12;
    public static float SHELL_GAME_DELTA_TIME = 1f; 

    public static int TOTAL_CONDITIONS = 2;
    public static int TOTAL_TASK_NUM = 4;
    public static int TOTAL_TRIALS_PER_TASK = 3;

    // Engine Game Dataset
    public static int[,] EngineGameHighlightIndices = new int[2, 3]
    {
        { 425, 1527, 1604 },
        { 1544, 1700, 417 }
    };
    public static Quaternion[,] EngineGameExtraRotations = new Quaternion[2, 3]
    {
        {
            Quaternion.Euler(0f, 45f, 0f),
            Quaternion.Euler(0f, 0f, 0f),
            Quaternion.Euler(0f, 75f, 0f)
        },
        {
            Quaternion.Euler(0f, 45f, 0f),
            Quaternion.Euler(0f, 0f, 0f),
            Quaternion.Euler(0f, 75f, 0f)
        }
    };


    // Shell Game Dataset
    public static int[,,] ShellGameTargetIndices = new int[2, 3, 2]
    {
        {
            { 2, 4 },  // 1
            { 0, 1 },  // 2
            { 1, 3 }   // 3
        },
        {
            { 2, 3 },  // 2
            { 0, 2 },  // 3
            { 1, 3 }   // 1
        }
    };
    public static int[,,] ShellGameAnimSequences = new int[6, 12, 2]
    {
        {
            { 0, 1 },
            { 0, 2 },
            { 3, 4 },
            { 0, 4 },
            { 2, 1 },
            { 3, 0 },
            { 1, 4 },
            { 2, 4 },
            { 1, 3 },
            { 1, 0 },
            { 2, 3 },
            { 4, 3 }
        },
        {
            { 3, 4 },
            { 1, 2 },
            { 1, 4 },
            { 0, 2 },
            { 1, 3 },
            { 4, 3 },
            { 2, 0 },
            { 2, 3 },
            { 2, 4 },
            { 2, 1 },
            { 0, 3 },
            { 1, 2 }
        },
        {
            { 2, 3 },
            { 0, 1 },
            { 2, 4 },
            { 3, 4 },
            { 1, 0 },
            { 3, 2 },
            { 1, 4 },
            { 1, 2 },
            { 1, 3 },
            { 0, 4 },
            { 0, 2 },
            { 3, 1 }
        },
        {
            { 3, 4 },
            { 1, 2 },
            { 1, 4 },
            { 0, 2 },
            { 1, 3 },
            { 4, 3 },
            { 2, 0 },
            { 2, 3 },
            { 2, 4 },
            { 2, 1 },
            { 0, 3 },
            { 1, 2 }
        },
        {
            { 2, 3 },
            { 0, 1 },
            { 2, 4 },
            { 3, 4 },
            { 1, 0 },
            { 3, 2 },
            { 1, 4 },
            { 1, 2 },
            { 1, 3 },
            { 0, 4 },
            { 0, 2 },
            { 3, 1 }
        },
        {
            { 0, 1 },
            { 0, 2 },
            { 3, 4 },
            { 0, 4 },
            { 2, 1 },
            { 3, 0 },
            { 1, 4 },
            { 2, 4 },
            { 1, 3 },
            { 1, 0 },
            { 2, 3 },
            { 4, 3 }
        }        
    };

}

