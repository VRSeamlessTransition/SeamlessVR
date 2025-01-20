using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Task
{
    CountShape,
    IdentifyHighlightedObject,
    ShellGame,
    EngineGame,
    None
}

public class Constants : MonoBehaviour
{
    public static int DISPLAY_SCREEN_WIDTH = 3840;
    public static int DISPLAY_SCREEN_HEIGHT = 2160;
    public static string DIR_PATH = @"C:\seamlessvr";
    public static int TOTAL_TASK_NUM = 4;   
    public static int TOTAL_TRIALS_PER_TASK = 3;
    public static int TOTAL_ANS_TYPE_PER_ROW = 3;

    public static string GetCountShapeType(int i) {
        return i switch
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

    public static string QUESTION_COUNT_SHAPE = "Please enter the number of\n";
    public static string QUESTION_HIGHLIGHT_OBJECTS = "Please use the mouse to point at the highlighted object\n";
    public static string QUESTION_SHELLGAME = "Please choose which bowl contains the ball\n";
    public static string QUESTION_ENGINE_GAME = "Please use the mouse to point at the highlighted element\n";

    // shell game data parems
    public static int SHELL_GAME_TOTAL_ANIM_COUNT = 12;
    public static float SHELL_GAME_DELTA_TIME = 1f;

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
    public static int[,,] ShellGameAnswers = new int[2, 3, 2]
    {
        {
            { 2, 3 },  // 1 dataset , start from 1-5
            { 2, 5 },  // 2
            { 4, 5 }   // 3
        },
        {
            { 1, 4 },  // 2
            { 2, 3 },  // 3
            { 1, 5 }   // 1
        }
    };


    // Engine Game Dataset
    public static int[,] EngineGameHighlightIndices = new int[2, 3]
    {
        { 425, 1527, 1604 },
        { 1544, 1700, 417 }
    };

    // Count Shape Dataset
    public static int[,] CountShapeAnswers = new int[2, 3]
    {
        { 15, 11, 17 },
        { 16, 12, 14 }
    };
}
