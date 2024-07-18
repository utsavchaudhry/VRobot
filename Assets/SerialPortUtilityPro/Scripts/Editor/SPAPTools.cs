using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

using System.Runtime.InteropServices;

namespace SerialPortUtility
{
	public class SPAPTools : EditorWindow
	{
		//spapmain.cpp
		//DLL import
		[DllImport("spap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		static extern int spapDeviceListAvailable();
		[DllImport("spap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		static extern int spapDeviceList(int deviceNum, [MarshalAs(UnmanagedType.LPStr)]System.Text.StringBuilder deviceInfo, int buffer_size);

		public SerialPortUtilityPro spapObject = null;
		private int deviceNum;
		private System.Text.StringBuilder[] deviceString;
		private int[] deviceKind;

		SPAPTools()
		{
			this.minSize = new Vector2(300, 300);
			this.maxSize = new Vector2(600, 1000);

			deviceNum = spapDeviceListAvailable();
			deviceString = new System.Text.StringBuilder[deviceNum];
			deviceKind = new int[deviceNum];
			for (int i = 0; i < deviceNum; i++)
			{
				deviceString[i] = new System.Text.StringBuilder(1024);
				deviceKind[i] = spapDeviceList(i, deviceString[i], 1024);
			}

			
			foreach (System.Text.StringBuilder str1 in deviceString)
			{
				int overlap = -1;
				foreach (System.Text.StringBuilder str2 in deviceString)
				{
					if (str1.ToString().Equals(str2.ToString()))
						overlap++;
				}
				str1.Append("," + overlap.ToString());
			}
			
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
			EditorGUILayout.LabelField(string.Format("Open Method: {0}",spapObject.OpenMethod.ToString()), EditorStyles.boldLabel);
			int foundDevice = 0;
			if(deviceNum > 0)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				System.Collections.Generic.List<string> viewSamething = new System.Collections.Generic.List<string>();
				for (int i = 0; i < deviceNum; i++) {
					int openMethod = (int)spapObject.OpenMethod;

					string[] datu = deviceString[i].ToString().Split(',');
					string viewButton = "Error";
					if (openMethod == deviceKind[i])
					{
						if (datu[0] == "null")
							continue;

						switch (spapObject.OpenMethod)
						{
							case SerialPortUtilityPro.OpenSystem.USB:
								viewButton = string.Format("VID:{0} PID:{1} ({2})\n{3}", datu[0], datu[1], datu[3], datu[2]);
								if (GUILayout.Button(viewButton))
								{
									spapObject.VendorID = datu[0];
									spapObject.ProductID = datu[1];
									spapObject.SerialNumber = datu[2];
									spapObject.Skip = int.Parse(datu[4]);
									Close();
								}
								break;
							case SerialPortUtilityPro.OpenSystem.PCI:
								viewButton = string.Format("VEN:{0} DEV:{1} ({2})", datu[0], datu[1], datu[3]);
								if (GUILayout.Button(viewButton))
								{
									spapObject.VendorID = datu[0];
									spapObject.ProductID = datu[1];
									spapObject.SerialNumber = "";
									spapObject.Skip = int.Parse(datu[4]);

									Close();
								}
								break;
							case SerialPortUtilityPro.OpenSystem.BluetoothSSP:
								viewButton = string.Format("Bluetooth VirtualCOM ({0})", datu[0]);
								if (GUILayout.Button(viewButton))
								{
									spapObject.VendorID = "";
									spapObject.ProductID = "";
									spapObject.DeviceName = datu[0];
									spapObject.Skip = 0;

									Close();
								}
								break;
						}
						viewSamething.Add(viewButton);
						foundDevice++;
					}
				}
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.LabelField(string.Format("Devices found: {0}", foundDevice));
		}

		/*
		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
		{

		}
		*/
	}
}