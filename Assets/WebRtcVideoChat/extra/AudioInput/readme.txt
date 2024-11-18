Platforms: Windows, Mac, iOS, Android
====================================
Introduction:
====================================
This example shows how to use the AudioInput C# API to forward custom audio data to the C++ plugin for sending.
By default WebRTC Video Chat will send audio by accessing a default recording device supplied directly by the platform. 
e.g. for Windows this is the device selected as "Default Communication Device" in the sound settings. 
If your app requires sending audio generated in Unity, you want to process audio before sending or have more control over the device 
selection this example shows an alternative by using the AudioInput C# API.  
Note: Some platforms may experience issues with echo cancellation if tested via local loopback. For a realistic
test connect to a second device e.g. via the browser app:
https://www.because-why-not.com/files/webrtcsamples/1.0.2/awrtc_browser/callapp.html

====================================
Typical test scenario:
====================================
1. Open the Unity scene "audio_input" in the Unity Editor and start the scene. 
2. You will see two instances of the CallApp. The app with the red background uses the new AudioInput to process audio from within Unity.
The app with the grey background uses hardware audio directly bypassing Unity and AudioInput for comparison.
At the very top you see a play audio button. This button can playback an mp3 file for testing.
3. In the red app: Tick Audio and select an audio device. For this scenario select "clip_audio"
4. Enter a passphrase and press Join
5. Join the call via another CallApp by entering the same passphrase
6. After connecting, press the "play audio" button at the top to start playback. Audio should start playing on the receiving side.
Note: Panning does not yet work on all devices as they might default to mono playback.
Alternative tests:
In step 3. select "dummy_wave". This uses the script DummyWaveAudioInput.cs to send procedurally generated audio.
Or select a device prefixed with "mic_" to send from a Microphone via Unity's Microphone class. 

====================================
Scripts overview:
====================================
AudioInputExample.cs: This takes the role of the UnityCallFactory but ensures the factory is set up for AudioInput by deactivating
the default hardware recording features. Note Calls created via UnityCallFactory.Instance.CreateCall cannot access AudioInput and
Calls created via AudioInputExample must use AudioInput and cannot fall back to the default hardware audio access. 

AudioInputCallApp.cs: A custom CallApp that uses AudioInputExample and handles the device selection + notifies a GameObject that 
generates audio when it is accessed and when access is stopped. 

VirtualAudioInput.cs: This defines base class and interfaces used by the other classes.
DummyWaveAudioInput.cs: This class generates a sine wave and forwards it to the plugin. 
Used when selecting the "dummy_wave" device.

UnityAudioInput.cs: This class can be attached to a GameObject with an AudioSource. It will then send the audio
from that source.
Used when selecting "clip_audio". Playback is controlled via the Clip Player bar on the top of the screen.

MicrophoneAudioInput.cs: Handles access of Unity's Microphone class to supply audio data.
Used when selecting an audio device prefixed with "mic_". GameObjects using this script are automatically created by
UnityMicrophoneManager.cs based on the hardware available.