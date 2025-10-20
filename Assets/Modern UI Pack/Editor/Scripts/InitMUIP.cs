#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Michsky.MUIP
{
    public class InitMUIP
    {
        [InitializeOnLoad]
        public class InitOnLoad
        {
            static InitOnLoad()
            {
                if (!EditorPrefs.HasKey("MUIP.HasCustomEditorData"))
                {
                    EditorPrefs.SetInt("MUIP.HasCustomEditorData", 1);
                    EditorPrefs.SetString("MUIP.CustomEditorDark", AssetDatabase.GetAssetPath(Resources.Load("MUIP-EditorDark")));
                    EditorPrefs.SetString("MUIP.CustomEditorLight", AssetDatabase.GetAssetPath(Resources.Load("MUIP-EditorLight")));
                }
            }
        }
    }
}
#endif