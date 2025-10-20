using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace Michsky.MUIP
{
    public class InputSystemChecker : MonoBehaviour
    {
        void Awake()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            
            if (!gameObject.TryGetComponent<InputSystemUIInputModule>(out var tempModule))
            {
                gameObject.AddComponent<InputSystemUIInputModule>();
                if (gameObject.TryGetComponent<StandaloneInputModule>(out var oldModule)) { Destroy(oldModule); }
            }
#endif
        }
    }
}