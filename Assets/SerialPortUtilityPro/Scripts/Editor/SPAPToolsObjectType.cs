using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace SerialPortUtility
{
	public class SPAPToolsObjectType : EditorWindow
	{
		//spapmain.cpp
		//DLL import
		[DllImport("spap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		static extern int spapDeviceListAvailable();
		[DllImport("spap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		static extern int spapDeviceList(int deviceNum, [MarshalAs(UnmanagedType.LPStr)]System.Text.StringBuilder deviceInfo, int buffer_size);

		public SerialPortUtilityPro spapObject = null;
		private List<string> classString = new List<string>();
		private Vector2 scrollPos = Vector2.zero;

		SPAPToolsObjectType()
		{
			this.minSize = new Vector2(300, 300);
			this.maxSize = new Vector2(600, 1000);

			classString.Clear();
			Assembly asmlib = Assembly.Load("Assembly-CSharp");
			if (asmlib != null)
			{
				System.Type[] ts = asmlib.GetTypes();
				foreach (System.Type t in ts) {
					bool isHide = false;
					if (t.FullName.Contains("SerialPortUtility."))
						isHide = true;

					if ((t.IsNestedPublic || t.IsPublic) && !isHide)
						classString.Add(t.FullName);
				}
			}

			scrollPos = Vector2.zero;
		}

		void OnGUI()
		{
			if (spapObject == null)
			{
				EditorGUILayout.LabelField("Error!", EditorStyles.boldLabel);
				return;
			}

			EditorGUILayout.HelpBox("When button selected, information is set in this inspector.", MessageType.Info, true);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("The public class list of this project : ", EditorStyles.boldLabel);

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

			for (int i = 0; i < classString.Count; ++i)
			{
				string viewButton = string.Format("{0}", classString[i]);
				if (GUILayout.Button(viewButton)) {
					spapObject.ReadCompleteEventObjectType = classString[i];
					Close();
				}
			}
			EditorGUILayout.EndScrollView();
		}

		/*
		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
		{

		}
		*/
	}
}