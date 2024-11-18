using UnityEngine;
using UnityEngine.UI;

namespace Byn.Unity.Examples
{
	/// <summary>
	/// Helper to control the audio playback for testing.
	/// </summary>
	public class ClipPlayer : MonoBehaviour
	{
		private AudioSource mAudioSource;
		private UnityAudioInput mInput;

		private Text mAudioOnOffText;
		private Button playButton;
		private Slider stereoSlider;

		public void Awake()
		{
			mInput = GetComponent<UnityAudioInput>();
			mAudioSource = GetComponent<AudioSource>();

			playButton = GetComponentInChildren<Button>();
			stereoSlider = GetComponentInChildren<Slider>();
			playButton.onClick.AddListener(() =>
			{
				ToggleAudioOnOff();
			});
			mAudioOnOffText = playButton.GetComponentInChildren<Text>();

			stereoSlider.onValueChanged.AddListener((float val) =>
			{
				mAudioSource.panStereo = val;
				Debug.Log("Pan to " + val);
			});
		}


		public void ToggleAudioOnOff()
		{
			bool state = mAudioSource.isPlaying;
			state = !state;
			//_AudioOnOffSource.mute = state;
			if (state)
			{
				Debug.Log("Calling AudioSource.Play");
				mAudioSource.Play();
			}
			else
			{
				Debug.Log("Calling AudioSource.Stop");
				mAudioSource.Stop();
			}
		}

		public void Update()
		{
			//update each frame to detect any external components messing with the audio
			if (mAudioSource.isPlaying)
			{
				mAudioOnOffText.text = "stop audio";
			}
			else
			{
				mAudioOnOffText.text = "play audio";
			}
		}
	}
}