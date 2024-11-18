This example shows new methods for managing audio devices for the WebGL platform:
* BrowserCallFactory.GetAudioInputDevices & BrowserMediaConfig.AudioInputDevice let you show 
  and select a specific audio device
* "SetVolumePan" allows controlling the volume for each ear individually. This can be used to 
  add a spatial audio effect based on other users' positions.

=========================
Audio device selection:
=========================
In a WebGL build, select an audio device via the new dropdown menu. On the first start, the 
device names will be hidden. WebGL only exposes them after the user agrees to access a device. 
After that, the exact names will be visible (press refresh icon).

=========================
Panning:
=========================
To test the panning effect, first build the audio_pan scene for WebGL. After connecting to 
another user, click on the remote video feed once. Two sliders will appear. The slider on the 
top is for panning left / right, and the slider on the left is for volume. 
This feature is WebGL only! The panning effect is done via the browser's WebAudio API. 
See PanCallApp.cs for C# related documentation.

Pitfalls:
* We have no way of detecting if stereo playback actually works. If the OS or playback device 
  only supports mono, the result is device-specific. Either both channels are merged or only 
  one channel is used. 
* Many mobile devices might only support mono audio playback even if stereo is officially 
  supported! The phone might switch to mono if an active VoIP call is detected.
* Bluetooth headsets usually switch to mono playback once the microphone is used ("Bluetooth 
  Hands-Free Profile").
