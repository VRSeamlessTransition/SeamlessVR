using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Utils : MonoBehaviour
{
    public static void SaveRenderTextureToPNGFile(RenderTexture rt, string filePath)
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(filePath + ".png", tex.EncodeToPNG());
        RenderTexture.active = null;

        Debug.Log("output texture w and h: " + rt.width + ", " + rt.height);
        Debug.Log("Saved to " + filePath);
    }

    public static void SaveTextureToPNGFile(Texture2D texture, string filePath)
    {
        byte[] bytes = ImageConversion.EncodeToPNG(texture);
        File.WriteAllBytes(filePath + ".png", bytes);
    }


}
