using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlatformDetector : MonoBehaviour
{
#if !UNITY_EDITOR && UNITY_WEBGL
[DllImport("__Internal")]
private static extern bool IsMobile();
#endif

    public bool IsMobileDevice()
    {
#if !UNITY_EDITOR && UNITY_WEBGL
        return IsMobile();
#else
        return false;
#endif
    }

    [SerializeField] [TextArea] private string phoneScene = "HopeLite_MobileOperator_ShopMetal";
    [SerializeField] [TextArea] private string computerScene = "HopeLite_PCOperator_ShopMetal";
    [SerializeField] private float delay = 1f;

    private void Start()
    {
        StartCoroutine(DetectAndLoad());
    }

    private IEnumerator DetectAndLoad()
    {
        yield return new WaitForSeconds(delay);

        SceneManager.LoadScene(IsMobileDevice() ? phoneScene : computerScene);
    }
}
