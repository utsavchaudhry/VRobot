using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace SerialPortUtility
{
	/// <summary>
	/// SPAP Custom Editor
	/// </summary>
	[CustomEditor(typeof(SerialPortUtilityPro))]
	public class SPAPEditor : Editor
	{
		//License
		[DllImport("spap", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
		static extern void spapIsLicensed([MarshalAs(UnmanagedType.LPStr)]StringBuilder email, [MarshalAs(UnmanagedType.LPStr)]StringBuilder code, int size);

		private SerializedProperty ReadCompleteEventObject = null;
		private SerializedProperty SystemEventObject = null;
		private string _sendText = "";
		private Vector2 _scrollPos = Vector2.zero;
		private _feedCodeDebugSel _fcds = _feedCodeDebugSel.None; 

		//for debug
		private SerializedProperty DebugConsoleMonitorProperty;
		private SerializedProperty ExpandConfigProperty;
		private SerializedProperty ExpandSPMonitorProperty;
		private SerializedProperty ExpandEventsProperty;
		private SerializedProperty ExternalConfig;
		
		private enum _baudrateSel
		{
			Rate1200bps = 1200,
			Rate2400bps = 2400,
			Rate4800bps = 4800,
			Rate9600bps = 9600,
			Rate19200bps = 19200,
			Rate38400bps = 38400,
			Rate57600bps = 57600,
			Rate115200bps = 115200,
			Rate230400bps = 230400,
			Rate460800bps = 460800,
			Rate500000bps = 500000,
			Rate921600bps = 921600,
			Rate1000000bps = 1000000,
		}

		private enum _splitFeedCodeSel
		{
			LF = 0,		//\n
			CR,			//\r
			CRLF,		//\r\n
			STX,		//\02
			ETX,		//\03
			COMMA,		//,
			COLON,		//:
			SEMICOLON,	//;
			AND,		//&
			OR,			//|
			TABHT,		//\t
			SPACE,		//space
			CUSTOMCODE,
		}

		private enum _feedCodeDebugSel
		{
			None = 0,
			LF,			//\n
			CR,			//\r
			CRLF,		//\r\n
		}

		Texture2D _icon = null;
		Texture2D _icon_free = null;
		void OnEnable()
		{
			ReadCompleteEventObject = serializedObject.FindProperty("ReadCompleteEventObject");
			SystemEventObject = serializedObject.FindProperty("SystemEventObject");

			if (_icon == null) _icon = Resources.Load<Texture2D>("spap_icon");
			if (_icon_free == null) _icon_free = Resources.Load<Texture2D>("spap_icon_free");

			DebugConsoleMonitorProperty = serializedObject.FindProperty("DebugConsoleMonitor");
			ExpandConfigProperty = serializedObject.FindProperty("ExpandConfig");
			ExpandSPMonitorProperty = serializedObject.FindProperty("ExpandSPMonitor");
			ExpandEventsProperty = serializedObject.FindProperty("ExpandEventConfig");

			ExternalConfig = serializedObject.FindProperty("ExternalConfig");

            _sendText = "";
			_scrollPos = Vector2.zero;
		}
		
		public override void OnInspectorGUI()
		{
			SerialPortUtilityPro obj = target as SerialPortUtilityPro;
			serializedObject.Update();

			//License
			/*
			StringBuilder owner = new StringBuilder(255);
			StringBuilder code = new StringBuilder(255);
			spapIsLicensed(owner, code, 255);
			obj.LicenseEmail = owner.ToString();
			obj.LicenseCode = code.ToString();

			if (owner.ToString() == string.Empty)
			{
				if (_icon_free != null)
				{
					EditorGUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					GUILayout.Label(new GUIContent(_icon_free));
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();
					GUI.backgroundColor = Color.red;
					if (GUILayout.Button("UNLOCK the 1MB transfer limited mode >>"))
					{
						//SPAPLicense.AddSPAPLicense();
						UnityEditorInternal.AssetStore.Open("content/125863");
					}
					GUI.backgroundColor = Color.white;
				}
			}
			else
			{*/
				if (_icon != null)
				{
					EditorGUILayout.BeginHorizontal();
					GUILayout.FlexibleSpace();
					GUILayout.Label(new GUIContent(_icon));
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();
				}
			//}

			GUI.backgroundColor = new Color(0.50f, 0.70f, 1.0f);
			EditorGUILayout.Space();
			
			//button
			if (GUILayout.Button("SerialPort Configure", EditorStyles.toolbarButton))
				ExpandConfigProperty.boolValue = !ExpandConfigProperty.boolValue;

			GUI.backgroundColor = Color.white;

			if (ExpandConfigProperty.boolValue)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("SerialPort Status", EditorStyles.boldLabel);

				if (EditorApplication.isPlaying)
				{
					if (obj.IsOpened())
					{
						EditorGUILayout.HelpBox("Device Opened.", MessageType.Info, true);
						GUI.backgroundColor = Color.yellow;
						if (GUILayout.Button("Close the device."))
							obj.Close();
						GUI.backgroundColor = Color.white;
					}
					else
					{
						if (obj.IsErrorFinished())
							EditorGUILayout.HelpBox("Device Error Closed.", MessageType.Error, true);
						else
							EditorGUILayout.HelpBox("Device Closed.", MessageType.Warning, true);

						GUI.backgroundColor = Color.yellow;
						if (GUILayout.Button("Open the device."))
							obj.Open();
						GUI.backgroundColor = Color.white;
					}
				}
				else
				{
					string infoString = "Device is not running.";
					EditorGUILayout.HelpBox(infoString, MessageType.Info, true);
				}
				
				EditorGUILayout.EndVertical();

				if (obj.IsOpened())
					EditorGUI.BeginDisabledGroup(true);

				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("Open Configure", EditorStyles.boldLabel);
				SerialPortUtilityPro.OpenSystem preMethod = obj.OpenMethod;
				obj.OpenMethod = (SerialPortUtilityPro.OpenSystem)EditorGUILayout.EnumPopup("Open Method", obj.OpenMethod);
				if (obj.OpenMethod != preMethod)
				{
					obj.VendorID = "";
					obj.ProductID = "";
					obj.SerialNumber = "";
					obj.Skip = 0;
				}
				int intdata = 0;
				obj.IsAutoOpen = EditorGUILayout.Toggle("Auto Open", obj.IsAutoOpen);
				switch (obj.OpenMethod)
				{
					case SerialPortUtilityPro.OpenSystem.USB:
						obj.VendorID = EditorGUILayout.TextField("Vendor ID (0000-FFFF)", obj.VendorID);
						obj.ProductID = EditorGUILayout.TextField("Product ID (0000-FFFF)", obj.ProductID);
						if (!IsHexString(obj.VendorID)) obj.VendorID = "";
						if (!IsHexString(obj.ProductID)) obj.ProductID = "";
						obj.SerialNumber = EditorGUILayout.TextField("Serial Number", obj.SerialNumber);
						EditorGUILayout.LabelField(" ","* Empty item is targeted to all devices.");
						obj.Skip = EditorGUILayout.IntField("Order (Default:0)", obj.Skip);
						if (obj.Skip < 0) obj.Skip = 0;

						GUI.backgroundColor = Color.yellow;
						GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Show the devices connected to this PC.",GUILayout.Width(300))){
							SPAPTools window = (SPAPTools)EditorWindow.GetWindow(typeof(SPAPTools), true, "Show the devices connected to this PC.", true);
							window.spapObject = obj;
							window.Show();
						}
						GUILayout.EndHorizontal();
						break;
					case SerialPortUtilityPro.OpenSystem.PCI:
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
							obj.VendorID = EditorGUILayout.TextField("Vendor ID (0000-FFFF)", obj.VendorID);
							obj.ProductID = EditorGUILayout.TextField("Device ID (0000-FFFF)", obj.ProductID);
							if (!IsHexString(obj.VendorID)) obj.VendorID = "";
							if (!IsHexString(obj.ProductID)) obj.ProductID = "";
							EditorGUILayout.LabelField(" ", "* Empty item is targeted to all devices.");
							obj.Skip = EditorGUILayout.IntField("Order (Default:0)", obj.Skip);
							if (obj.Skip < 0) obj.Skip = 0;
							
							GUI.backgroundColor = Color.yellow;
							GUILayout.BeginHorizontal();
							GUILayout.FlexibleSpace();
							if (GUILayout.Button("Show the devices connected to this PC.", GUILayout.Width(300)))
							{
								SPAPTools window = (SPAPTools)EditorWindow.GetWindow(typeof(SPAPTools), true, "Show the devices connected to this PC.", true);
								window.spapObject = obj;
								window.Show();
							}
							GUILayout.EndHorizontal();
#else
						obj.VendorID = EditorGUILayout.TextField("Device Path", obj.VendorID);
						obj.ProductID = "";
						EditorGUILayout.LabelField(" ", "* Empty item is targeted to all devices.");
						obj.Skip = EditorGUILayout.IntField("Order (Default:0)", obj.Skip);
						if (obj.Skip < 0) obj.Skip = 0;
#endif
						break;
					case SerialPortUtilityPro.OpenSystem.BluetoothSSP:
						obj.DeviceName = EditorGUILayout.TextField("Device ID", obj.DeviceName);
#if UNITY_ANDROID
						obj.BluetoothSSPNoServerMode = EditorGUILayout.Toggle("Server mode is not available", obj.BluetoothSSPNoServerMode);
#endif
						obj.Skip = EditorGUILayout.IntField("Order (Default:0)", obj.Skip);
						if (obj.Skip < 0) obj.Skip = 0;

						GUI.backgroundColor = Color.yellow;
						GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Show the devices connected to this PC.", GUILayout.Width(300)))
						{
							SPAPTools window = (SPAPTools)EditorWindow.GetWindow(typeof(SPAPTools), true, "Show the devices connected to this PC.", true);
							window.spapObject = obj;
							window.Show();
						}
						GUILayout.EndHorizontal();
						break;
					case SerialPortUtilityPro.OpenSystem.NumberOrder:
						obj.Skip = EditorGUILayout.IntField("Order (Default:0)", obj.Skip);
						if (obj.Skip < 0) obj.Skip = 0;
						break;
					case SerialPortUtilityPro.OpenSystem.TCPSerialEmulatorClient:
						obj.VendorID = EditorGUILayout.TextField("Connect IPv4 Address", obj.VendorID);
						obj.ProductID = EditorGUILayout.TextField("Connect Port", obj.ProductID);
						if (!int.TryParse(obj.ProductID, out intdata)) obj.ProductID = "";
						break;
					case SerialPortUtilityPro.OpenSystem.TCPSerialEmulatorServer:
						obj.VendorID = "";
						obj.ProductID = EditorGUILayout.TextField("Connected Port", obj.ProductID);
						if (!int.TryParse(obj.ProductID, out intdata)) obj.ProductID = "";
						break;
				}
				GUI.backgroundColor = Color.white;
				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.PropertyField(DebugConsoleMonitorProperty, new GUIContent("Enabled Debug UI Console"));
				EditorGUILayout.PropertyField(ExternalConfig, new GUIContent("Enabled External Config"));
				if (ExternalConfig.boolValue)
				{
					obj.ExternalConfigPath = (SerialPortUtilityPro.SourcePath)EditorGUILayout.EnumPopup(" Config File Path", obj.ExternalConfigPath);
					obj.ExternalConfigFileName = EditorGUILayout.TextField(" ", obj.ExternalConfigFileName);
				}
				obj.EnabledTransmission = EditorGUILayout.Toggle("Enabled Transmission", obj.EnabledTransmission);
				EditorGUILayout.EndVertical();

				if (obj.OpenMethod != SerialPortUtilityPro.OpenSystem.TCPSerialEmulatorClient &&
					obj.OpenMethod != SerialPortUtilityPro.OpenSystem.TCPSerialEmulatorServer &&
					obj.OpenMethod != SerialPortUtilityPro.OpenSystem.BluetoothSSP)
				{
					EditorGUILayout.BeginVertical(GUI.skin.box);
					EditorGUILayout.LabelField("Communication Structure", EditorStyles.boldLabel);

					if (System.Enum.IsDefined(typeof(_baudrateSel), obj.BaudRate))
					{
						obj.BaudRate = (int)(_baudrateSel)EditorGUILayout.EnumPopup("BaudRate", (_baudrateSel)obj.BaudRate);
                        obj.BaudRate = EditorGUILayout.IntField(" ", obj.BaudRate);
                    }
					else
					{
						obj.BaudRate = EditorGUILayout.IntField("BaudRate", obj.BaudRate);
					}

					obj.Parity = (SerialPortUtilityPro.ParityEnum)EditorGUILayout.EnumPopup("Parity", obj.Parity);
					obj.StopBit = (SerialPortUtilityPro.StopBitEnum)EditorGUILayout.EnumPopup("Stop Bit", obj.StopBit);
					obj.DataBit = (SerialPortUtilityPro.DataBitEnum)EditorGUILayout.EnumPopup("Data Bit", obj.DataBit);
					obj.RecvDiscardNull = EditorGUILayout.Toggle("Discard Null Receive", obj.RecvDiscardNull);
					obj.AutoRTSCTSHandshake = EditorGUILayout.Toggle("Auto RTS/CTS Handshake", obj.AutoRTSCTSHandshake);
					obj.StartEnableDTR = EditorGUILayout.Toggle("Start DTR Enable", obj.StartEnableDTR);
					EditorGUILayout.EndVertical();
				}

				if (obj.IsOpened())
					EditorGUI.EndDisabledGroup();

				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("Read Data Structure", EditorStyles.boldLabel);
				obj.ReadProtocol = (SerialPortUtilityPro.MethodSystem)EditorGUILayout.EnumPopup("Read Protocol", obj.ReadProtocol);
				switch (obj.ReadProtocol)
				{
					case SerialPortUtilityPro.MethodSystem.Streaming:
					case SerialPortUtilityPro.MethodSystem.BinaryStreaming:
						EditorGUILayout.PropertyField(ReadCompleteEventObject);
						break;
					case SerialPortUtilityPro.MethodSystem.SplitStringToArray:
						obj.SplitCode = GUISplitFeedCodeEnumPop("Split Code", obj.SplitCode);
						obj.FeedCode = GUISplitFeedCodeEnumPop("Feed Code", obj.FeedCode);
						EditorGUILayout.PropertyField(ReadCompleteEventObject);
						EditorGUILayout.HelpBox(" Receive Data Format: " +
							"AAA" + obj.SplitCode + "BBB" + obj.SplitCode + "CCC" + obj.FeedCode, MessageType.None);
						break;
					case SerialPortUtilityPro.MethodSystem.FixedLengthDataToString:
					case SerialPortUtilityPro.MethodSystem.FixedLengthDataToBinary:
						obj.FixedFeedCount = EditorGUILayout.IntField("Fixed-length Data Size", obj.FixedFeedCount);
						EditorGUILayout.PropertyField(ReadCompleteEventObject);
						break;
					case SerialPortUtilityPro.MethodSystem.SplitStringToDictionary:
						obj.SplitCode = GUISplitFeedCodeEnumPop("Split Code", obj.SplitCode);
						obj.FeedCode = GUISplitFeedCodeEnumPop("Feed Code", obj.FeedCode);
						EditorGUILayout.PropertyField(ReadCompleteEventObject);
						EditorGUILayout.HelpBox(" Receive Data Format: " +
							"AAA=DDD" + obj.SplitCode + "BBB=EEE" + obj.SplitCode + "CCC=FFF" + obj.FeedCode, MessageType.None);
						break;
					case SerialPortUtilityPro.MethodSystem.JSONToClassObject:
						obj.ReadCompleteEventObjectType = EditorGUILayout.TextField("Object Type", obj.ReadCompleteEventObjectType);
						GUI.backgroundColor = Color.yellow;
						GUILayout.BeginHorizontal();
						GUILayout.FlexibleSpace();
						if (GUILayout.Button("Show Object Type from this project.", GUILayout.Width(300)))
						{
							SPAPToolsObjectType window = (SPAPToolsObjectType)EditorWindow.GetWindow(typeof(SPAPToolsObjectType), true, "Show Object Type from this project.", true);
							window.spapObject = obj;
							window.Show();
						}
						GUILayout.EndHorizontal();
						GUI.backgroundColor = Color.white;
						EditorGUILayout.PropertyField(ReadCompleteEventObject);
						break;
					case SerialPortUtilityPro.MethodSystem.SplitStringToGameObject:
						obj.SplitCode = GUISplitFeedCodeEnumPop("Split Code", obj.SplitCode);
						obj.FeedCode = GUISplitFeedCodeEnumPop("Feed Code", obj.FeedCode);
						obj.ReadClassMembersObject = EditorGUILayout.ObjectField("GameObject", obj.ReadClassMembersObject, typeof(GameObject), true) as GameObject;
						EditorGUILayout.HelpBox(" Receive Data Format: " +
							"Class.VarName1=DDD" + obj.SplitCode + "Class.VarName2=EEE" + obj.SplitCode + "Class1.VarName1=FFF" + obj.FeedCode, MessageType.None);
						break;
					case SerialPortUtilityPro.MethodSystem.LineFeedDataToString:
					case SerialPortUtilityPro.MethodSystem.LineFeedDataToBinary:
						obj.FeedCode = GUISplitFeedCodeEnumPop("Feed Code", obj.FeedCode);
						EditorGUILayout.PropertyField(ReadCompleteEventObject);
						EditorGUILayout.HelpBox(" Receive Data Format: " +
							"AAABBBCCC" + obj.FeedCode, MessageType.None);
						break;
					case SerialPortUtilityPro.MethodSystem.ModbusASCII:
						EditorGUILayout.PropertyField(ReadCompleteEventObject);
						EditorGUILayout.HelpBox(" Receive Data Format: " +
							":AABBDDDDDDXX<CR><LF>", MessageType.None);
						break;
					case SerialPortUtilityPro.MethodSystem.ModbusRTU:
						EditorGUILayout.PropertyField(ReadCompleteEventObject);
						EditorGUILayout.HelpBox(" Receive Data Format: " +
							"MODBUS RTU HEX BINARY DATA ( Beta )", MessageType.None);
						break;
				}

				EditorGUILayout.EndVertical();

				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("Write Data Command", EditorStyles.boldLabel);
				EditorGUILayout.HelpBox("bool Write(string);\n" +
					"bool Write(string, feedCode);\n" +
					"bool WriteCRLF(string);\n" +
					"bool WriteCR(string);\n" +
					"bool WriteLF(string);\n" +
					"bool Write(List<string>, splitCode, feedCode);\n" +
					"bool Write(Dictionary<key, string>, splitCode, feedCode);\n" +
					"bool Write(SPUPMudbusData, binaryMode);" +
					"bool Write(byte[] or byte);\n" +
					"bool WriteJSON(object);" 
				, MessageType.Info);
				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.Space();
			GUI.backgroundColor = new Color(0.50f, 0.70f, 1.0f);
			if (GUILayout.Button("SerialPort Utility Events", EditorStyles.toolbarButton))
				ExpandEventsProperty.boolValue = !ExpandEventsProperty.boolValue;

			GUI.backgroundColor = Color.white;
			GUI.color = Color.white;
			if (ExpandEventsProperty.boolValue)
			{
				obj.UpdateProcessing = (SerialPortUtilityPro.UpdateMethod)EditorGUILayout.EnumPopup("Processing Update", obj.UpdateProcessing);
				if (obj.UpdateProcessing == SerialPortUtilityPro.UpdateMethod.ManualUpdate)
					EditorGUILayout.HelpBox("Please call ReadUpdate() at an arbitrary timing.", MessageType.Info);
				EditorGUILayout.LabelField("Event Handler");
				EditorGUILayout.PropertyField(SystemEventObject);
			}

			EditorGUILayout.Space();
			GUI.backgroundColor = new Color(0.50f, 0.70f, 1.0f);
			if (GUILayout.Button("SerialPort Debug Monitor", EditorStyles.toolbarButton))
				ExpandSPMonitorProperty.boolValue = !ExpandSPMonitorProperty.boolValue;
			
			GUI.backgroundColor = Color.white;
			GUI.color = Color.white;
			if (ExpandSPMonitorProperty.boolValue)
			{
				EditorGUILayout.Space();
				GUILayout.BeginHorizontal();
				if (!obj.IsOpened())
					EditorGUI.BeginDisabledGroup(true);
				_sendText = EditorGUILayout.TextField(_sendText);
				_fcds = (_feedCodeDebugSel)EditorGUILayout.EnumPopup(_fcds, GUILayout.Width(60));
				if (GUILayout.Button("Send", GUILayout.Width(60), GUILayout.Height(15)))
				{
                    if (_sendText != string.Empty) {
						string feedc = "";	//None
						switch (_fcds)
						{
							case _feedCodeDebugSel.LF: feedc = "<LF>";
								break;
							case _feedCodeDebugSel.CR: feedc = "<CR>";
								break;
							case _feedCodeDebugSel.CRLF: feedc = "<CR><LF>";
								break;
							default: break;
						}
						if (!obj.Write(_sendText, feedc))	//Write
							obj.SerialDebugAddString(" [Send Error!]", true);
                    }
                }

				GUILayout.EndHorizontal();
				EditorGUILayout.Space();
				GUILayout.BeginHorizontal();
				obj.RtsEnabled = EditorGUILayout.ToggleLeft("RTS(OUT)", obj.RtsEnabled, GUILayout.Width(80));
				obj.DtrEnabled = EditorGUILayout.ToggleLeft("DTR(OUT)", obj.DtrEnabled, GUILayout.Width(110));
				EditorGUILayout.ToggleLeft("CTS(IN)", obj.CtsHolding(), GUILayout.Width(80));
				EditorGUILayout.ToggleLeft("DSR(IN)", obj.DsrHolding(), GUILayout.Width(80));
				GUILayout.EndHorizontal();
				EditorGUILayout.Space();

				_scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, EditorStyles.textArea, GUILayout.Height(260));
				foreach(string destr in obj.GetSerialDebugString.Split("\n".ToCharArray()))
					EditorGUILayout.SelectableLabel(destr, GUILayout.Height(15.0f));
				EditorGUILayout.EndScrollView();

				if (!obj.IsOpened())
					EditorGUI.EndDisabledGroup();
			}
			EditorGUILayout.Space();

			EditorGUILayout.LabelField("SerialPort Utility Pro Version " + SerialPortUtilityPro.VersionString);
			EditorGUILayout.Space();

			//changed param
			if (GUI.changed)
			{
				if (ExpandSPMonitorProperty.boolValue)
				{
					if (obj.IsOpened())
					{
						obj.RtsEnable(obj.RtsEnabled);
						obj.DtrEnable(obj.DtrEnabled);
					}
				}

				//Todo
				serializedObject.ApplyModifiedProperties();
			}

			EditorUtility.SetDirty(target);	//editor set
		}

		private string GUISplitFeedCodeEnumPop(string itemName, string splitFeedCode)
		{
			string splitFeedCodeString = "";
			_splitFeedCodeSel sfcs;

			switch (splitFeedCode)
			{
				case "<LF>": sfcs = _splitFeedCodeSel.LF;
					break;
				case "<CR>": sfcs = _splitFeedCodeSel.CR;
					break;
				case "<CR><LF>": sfcs = _splitFeedCodeSel.CRLF;
					break;
				case "<STX>": sfcs = _splitFeedCodeSel.STX;
					break;
				case "<ETX>": sfcs = _splitFeedCodeSel.ETX;
					break;
				case ",": sfcs = _splitFeedCodeSel.COMMA;
					break;
				case ":": sfcs = _splitFeedCodeSel.COLON;
					break;
				case ";": sfcs = _splitFeedCodeSel.SEMICOLON;
					break;
				case "&": sfcs = _splitFeedCodeSel.AND;
					break;
				case "|": sfcs = _splitFeedCodeSel.OR;
					break;
				case "<HT>": sfcs = _splitFeedCodeSel.TABHT;
					break;
				case " ": sfcs = _splitFeedCodeSel.SPACE;
					break;
				default:
					sfcs = _splitFeedCodeSel.CUSTOMCODE;
					break;
			}

			sfcs = (_splitFeedCodeSel)EditorGUILayout.EnumPopup(itemName, (_splitFeedCodeSel)sfcs);

			switch (sfcs)
			{
				case _splitFeedCodeSel.LF: splitFeedCodeString = "<LF>";
					break;
				case _splitFeedCodeSel.CR: splitFeedCodeString = "<CR>";
					break;
				case _splitFeedCodeSel.CRLF: splitFeedCodeString = "<CR><LF>";
					break;
				case _splitFeedCodeSel.STX: splitFeedCodeString = "<STX>";
					break;
				case _splitFeedCodeSel.ETX: splitFeedCodeString = "<ETX>";
					break;
				case _splitFeedCodeSel.COMMA: splitFeedCodeString = ",";
					break;
				case _splitFeedCodeSel.COLON: splitFeedCodeString = ":";
					break;
				case _splitFeedCodeSel.SEMICOLON: splitFeedCodeString = ";";
					break;
				case _splitFeedCodeSel.AND: splitFeedCodeString = "&";
					break;
				case _splitFeedCodeSel.OR: splitFeedCodeString = "|";
					break;
				case _splitFeedCodeSel.TABHT: splitFeedCodeString = "<HT>";
					break;
				case _splitFeedCodeSel.SPACE: splitFeedCodeString = " ";
					break;
				default:
					splitFeedCodeString = splitFeedCode;
					break;
			}

			if (splitFeedCodeString == string.Empty)
				splitFeedCodeString = "<LF>";

			return EditorGUILayout.TextField(" ", splitFeedCodeString);
		}

		//is hex
		private bool IsHexString(string s)
		{
			if (string.IsNullOrEmpty(s))
				return false;

			foreach (char c in s)
				if (!System.Uri.IsHexDigit(c))
					return false;
			return true;
		}

		/*
		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
		{

		}
		*/
	}

	//Build Processor
	public class BuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
	{
		public int callbackOrder => 0;
		public void OnPreprocessBuild(BuildReport report)
		{

			if (PlayerSettings.Android.minSdkVersion < AndroidSdkVersions.AndroidApiLevel22)
			{
				PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel22;
				Debug.Log("SerialPort Utility Pro changed Android.minSdkVersion( >= Level 22).");
			}
		}

		public void OnPostprocessBuild(BuildReport report)
		{
		}
	}
}
