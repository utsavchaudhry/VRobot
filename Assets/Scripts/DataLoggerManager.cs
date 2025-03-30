using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System;

public class DataLoggerManager : MonoBehaviour
{
    public static string Folder { get; private set; }
    public static string File { get; private set; }

    [SerializeField] private RawImage feed;
    [SerializeField] private int frequency = 5;

    private void Start()
    {
        _ = StartCoroutine(CaptureFeed());

        Folder = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        File = Folder + "/signals.csv";
    }

    private IEnumerator CaptureFeed()
    {
        float delay = 1f / frequency;
        while (true)
        {
            if (!NetMessageParser.paused && feed.texture)
            {
                ES3.SaveImage(GetTexture2D(), string.Format("{0}/{1}.jpg", Folder, DateTime.Now.ToFileTime()));
            }
            yield return new WaitForSeconds(delay);
        }
    }

    private Texture2D GetTexture2D()
    {
        // Get the texture from RawImage.
        Texture sourceTexture = feed.texture;

        // If the texture is already a Texture2D, simply cast it (provided it is readable).
        if (sourceTexture is Texture2D)
        {
            return (Texture2D)sourceTexture;
        }

        // Otherwise, assume it's a RenderTexture or other type.
        // Create a new Texture2D with the same dimensions.
        Texture2D tex2D = new(sourceTexture.width, sourceTexture.height, TextureFormat.RGBA32, false);

        // If the source texture is a RenderTexture, set it as active.
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture renderTex = sourceTexture as RenderTexture;
        if (renderTex != null)
        {
            RenderTexture.active = renderTex;
        }
        else
        {
            // Optionally, if sourceTexture is not a RenderTexture but not Texture2D either,
            // you might render it to a temporary RenderTexture.
            RenderTexture tempRT = RenderTexture.GetTemporary(sourceTexture.width, sourceTexture.height, 0, RenderTextureFormat.Default);
            Graphics.Blit(sourceTexture, tempRT);
            RenderTexture.active = tempRT;
        }

        // Read the pixels from the active RenderTexture.
        tex2D.ReadPixels(new Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0);
        tex2D.Apply();

        // Restore the previously active RenderTexture.
        RenderTexture.active = currentRT;

        // Release temporary RenderTexture if used.
        if (renderTex == null)
        {
            RenderTexture.ReleaseTemporary(RenderTexture.active);
        }

        return tex2D;
    }

    public static void SaveSignal(string signal)
    {
        ES3.AppendRaw(string.Format("{0},{1}", DateTime.Now.ToString("HH-mm-ss-fff"), signal), File);
    }
}
