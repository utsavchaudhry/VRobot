using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SerialPortUtility
{
	public class DebugConsole : MonoBehaviour
	{
		private static int instanceNum = 0;

		public SerialPortUtilityPro SpupObject = null;
		private Button ViewButton;
		private Text ViewButtonText;
		private InputField SerialPortInputField;
		private GameObject Panel;
		private Text InfomationText;
		private Text DataView;
		private ContentSizeFitter DataViewSizeFitter;
		private Dropdown DataSendMode;
		private bool PanelViewed;

		private const int UPDATECOUNT = 6;
		private int UpdateUICount = 0;

        private void Awake()
        {
            instanceNum = 0;
        }

        // Use this for initialization
        void Start()
		{
			ViewButton = this.transform.Find("ViewButton").GetComponent<Button>();
			ViewButtonText = this.transform.Find("ViewButton/Text").GetComponent<Text>();
			SerialPortInputField = this.transform.Find("Panel/InputField").GetComponent<InputField>();
			Panel = this.transform.Find("Panel").gameObject;
			InfomationText = this.transform.Find("Panel/InfomationText").GetComponent<Text>();
			DataView = this.transform.Find("Panel/ScrollPanel/ScrollView/Viewport/Content").GetComponent<Text>();
			DataViewSizeFitter = this.transform.Find("Panel/ScrollPanel/ScrollView/Viewport/Content").GetComponent<ContentSizeFitter>();
			DataSendMode = this.transform.Find("Panel/Dropdown").GetComponent<Dropdown>();
			if (SpupObject == null) {
				if(this.transform.parent != null)
					SpupObject = this.transform.parent.GetComponent<SerialPortUtilityPro>();
			}

			Panel.SetActive(false);
			PanelViewed = false;

			if (SpupObject == null)
			{
				ViewButton.interactable = false;
				ViewButtonText.text = "null";
				InfomationText.text = "null";
			}
			else
			{
				ViewButtonText.text = SpupObject.gameObject.name;
				string systemName = SpupObject.OpenMethod.ToString();
				InfomationText.text = SpupObject.gameObject.name + " : " + systemName;
			}

			RectTransform rtm = this.transform.Find("ViewButton").GetComponent<RectTransform>();
			rtm.anchoredPosition = new Vector3(20.0f + (170.0f*(float)instanceNum), -20.0f, 0.0f);
			++instanceNum;

			//find EventSystem
			EventSystem eventSystem = GameObject.FindObjectOfType<EventSystem>();
			if (eventSystem == null)
			{
				GameObject obj = Resources.Load<GameObject>("Prefabs/SPUPEventSystem");
				GameObject objeve = Instantiate<GameObject>(obj);
				objeve.name = "EventSystem";
			}
		}

		// Update is called once per frame
		void Update()
		{
			if (SpupObject == null)
				return;

			if (!SpupObject.IsOpened()) {
				Panel.SetActive(false);
				PanelViewed = false;
				ViewButtonText.color = new Color(255, 0, 0);
			} else
				ViewButtonText.color = new Color(255, 255, 255);

			if (PanelViewed)
			{
				--UpdateUICount;
				if (UpdateUICount < 0)
				{
					DataView.text = SpupObject.GetSerialDebugString;

					if(true)

					DataViewSizeFitter.SetLayoutVertical();
					UpdateUICount = UPDATECOUNT;
				}
			}
		}

		public void SendButtonClick()
		{
			if (SpupObject == null)
				return;

			if (PanelViewed)
			{
				if (SerialPortInputField.text != string.Empty)
				{
					//Write
					switch(DataSendMode.value) {
						//NONE
						default:
						case 0:	if (!SpupObject.Write(SerialPortInputField.text))
							SpupObject.SerialDebugAddString(" [Send Error!]", true);
						break;
						//LF
						case 1: if (!SpupObject.WriteLF(SerialPortInputField.text))
							SpupObject.SerialDebugAddString(" [Send Error!]", true);
						break;
						//CR
						case 2: if (!SpupObject.WriteCR(SerialPortInputField.text))
							SpupObject.SerialDebugAddString(" [Send Error!]", true);
						break;
						//CRLF
						case 3: if (!SpupObject.WriteCRLF(SerialPortInputField.text))
							SpupObject.SerialDebugAddString(" [Send Error!]", true);
						break;
						//Binary
						case 4:
							if (!SpupObject.Write(HexStringToData(SerialPortInputField.text)))
								SpupObject.SerialDebugAddString(" [Send Error!]", true);
						break;
					}
				}
			}
		}

		private byte[] HexStringToData(string text)
		{
			string[] hexValuesSplit = text.Split(' ');
			byte[] retData = new byte[hexValuesSplit.Length];
			int inum = 0;
			foreach (string hex in hexValuesSplit)
			{
				try
				{
					// Convert the number expressed in base-16 to an integer.
					retData[inum] = System.Convert.ToByte(hex, 16);
					++inum;
				}
				catch(System.FormatException)
				{
					//ignore
				}
			}

			return retData;
		}

		public void CloseButtonClick()
		{
			HideWindow();

			if (SpupObject != null)
				SpupObject.Close();
		}

		public void HideWindow()
		{
			Panel.SetActive(false);
			PanelViewed = false;
		}

		public void ViewButtonClick()
		{
			bool cur_panelView = PanelViewed;

			SerialPortUtility.DebugConsole[] dc_objs = GameObject.FindObjectsOfType(typeof(SerialPortUtility.DebugConsole)) as SerialPortUtility.DebugConsole[];
			foreach (SerialPortUtility.DebugConsole dc_obj in dc_objs)
				dc_obj.HideWindow();

			if(cur_panelView == false)
			{
				//Open
				if (SpupObject != null)
					SpupObject.Open();

				Panel.SetActive(true);
				PanelViewed = true;
			}
		}
	}
}
