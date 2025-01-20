using Oculus.Interaction.Surfaces;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Utils : MonoBehaviour
{
    public static Bounds ComputeVeObjectBounds(GameObject parent)
    {
        Bounds bounds = new Bounds();
        if (parent.transform.childCount <= 0)
            return bounds;

        Vector3 center = Vector3.zero;
        int cnt = 0;
        foreach (Transform obj in parent.transform)
        {
            bounds.Encapsulate(obj.GetChild(0).gameObject.GetComponent<Renderer>().bounds);
            center += obj.GetChild(0).gameObject.GetComponent<Renderer>().bounds.center;
            cnt++;
        }

        center /= cnt;
        bounds.center = center;

        return bounds;
    }

    public static Texture2D RenderTextureToTexture2D(RenderTexture rt)
    {
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        return tex;
    }
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

    public static Vector3 PosFromWorldToLocal(Vector3 o, Vector3 right, Vector3 forward, Vector3 up, Vector3 wPos)
    {
        Vector3 pp0 = wPos - o;
        float local_x = Vector3.Dot(right, pp0);
        float local_y = Vector3.Dot(up, pp0);
        float local_z = Vector3.Dot(forward, pp0);
        Vector3 local_pos = new Vector3(local_x, local_y, local_z);

        return local_pos;
    }

}
