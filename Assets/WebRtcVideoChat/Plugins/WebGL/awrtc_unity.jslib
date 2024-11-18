/* 
 * Copyright (C) 2024 Christoph Kutza
 * 
 * Please refer to the LICENSE file for license information
 */
 /* Watch out while changing this file. A typo will cause an impossible
  * to debug error without trace / stackinfo while Unity loads.
  * "Invoking error handler due to Uncaught SyntaxError: Unexpected token function"
  */

var Unity_WebRtcNetwork =
{
	Unity_WebRtcNetwork_IsAvailable:function()
    {
		if("awrtc" in window && typeof awrtc.CAPI_WebRtcNetwork_IsAvailable === 'function')
		{
			return awrtc.CAPI_WebRtcNetwork_IsAvailable();
		}
		return false;
    },
	Unity_WebRtcNetwork_IsBrowserSupported:function()
    {
		return awrtc.CAPI_WebRtcNetwork_IsBrowserSupported();
    },
	Unity_WebRtcNetwork_Create:function(lConfiguration)
	{
		return awrtc.CAPI_WebRtcNetwork_Create(UTF8ToString(lConfiguration));
	},
	Unity_WebRtcNetwork_Release:function(lIndex)
	{
		awrtc.CAPI_WebRtcNetwork_Release(lIndex);
	},
	Unity_WebRtcNetwork_Connect:function(lIndex, lRoom)
	{
		return awrtc.CAPI_WebRtcNetwork_Connect(lIndex, UTF8ToString(lRoom));
	},
	Unity_WebRtcNetwork_StartServer:function(lIndex, lRoom)
	{
		awrtc.CAPI_WebRtcNetwork_StartServer(lIndex, UTF8ToString(lRoom));
	},
	Unity_WebRtcNetwork_StopServer:function(lIndex)
	{
		awrtc.CAPI_WebRtcNetwork_StopServer(lIndex);
	},
	Unity_WebRtcNetwork_Disconnect:function(lIndex, lConnectionId)
	{
		awrtc.CAPI_WebRtcNetwork_Disconnect(lIndex, lConnectionId);
	},
	Unity_WebRtcNetwork_Shutdown:function(lIndex)
	{
		awrtc.CAPI_WebRtcNetwork_Shutdown(lIndex);
	},
	Unity_WebRtcNetwork_Update:function(lIndex)
	{
		awrtc.CAPI_WebRtcNetwork_Update(lIndex);
	},
	Unity_WebRtcNetwork_Flush:function(lIndex)
	{
		awrtc.CAPI_WebRtcNetwork_Flush(lIndex);
	},
	Unity_WebRtcNetwork_SendData:function(lIndex, lConnectionId, lUint8ArrayDataPtr, lUint8ArrayDataOffset, lUint8ArrayDataLength, lReliable)
	{
		var sndReliable = true;
		if(lReliable == false || lReliable == 0 || lReliable == "false" || lReliable == "False")
			sndReliable = false;
		return awrtc.CAPI_WebRtcNetwork_SendDataEm(lIndex, lConnectionId, HEAPU8, lUint8ArrayDataPtr + lUint8ArrayDataOffset, lUint8ArrayDataLength, sndReliable);
	},
	Unity_WebRtcNetwork_GetBufferedAmount:function(lIndex, lConnectionId, lReliable)
	{
		var sndReliable = true;
		if(lReliable == false || lReliable == 0 || lReliable == "false" || lReliable == "False")
			sndReliable = false;
		return awrtc.CAPI_WebRtcNetwork_GetBufferedAmount(lIndex, lConnectionId, sndReliable);
	},
	Unity_WebRtcNetwork_PeekEventDataLength:function(lIndex)
	{
		return awrtc.CAPI_WebRtcNetwork_PeekEventDataLength(lIndex);
	},
	Unity_WebRtcNetwork_Dequeue:function(lIndex, lTypeIntArrayPtr, lConidIntArrayPtr, lUint8ArrayDataPtr, lUint8ArrayDataOffset, lUint8ArrayDataLength, lDataLenIntArrayPtr )
	{
		var val = awrtc.CAPI_WebRtcNetwork_DequeueEm(lIndex, HEAP32, lTypeIntArrayPtr >> 2, HEAP32, lConidIntArrayPtr >> 2, HEAPU8, lUint8ArrayDataPtr + lUint8ArrayDataOffset, lUint8ArrayDataLength, HEAP32, lDataLenIntArrayPtr >> 2);
		return val;
	},
	Unity_WebRtcNetwork_Peek:function(lIndex, lTypeIntArrayPtr, lConidIntArrayPtr, lUint8ArrayDataPtr, lUint8ArrayDataOffset, lUint8ArrayDataLength, lDataLenIntArrayPtr )
	{
		var val = awrtc.CAPI_WebRtcNetwork_PeekEm(lIndex, HEAP32, lTypeIntArrayPtr >> 2, HEAP32, lConidIntArrayPtr >> 2, HEAPU8, lUint8ArrayDataPtr + lUint8ArrayDataOffset, lUint8ArrayDataLength, HEAP32, lDataLenIntArrayPtr >> 2);
		return val;
	},
	Unity_WebRtcNetwork_RequestStats: function (lIndex)
	{
		awrtc.CAPI_WebRtcNetwork_RequestStats(lIndex);
	},
	Unity_WebRtcNetwork_DequeueRtcEvent: function (lIndex, lTypeIntArrayPtr, lConidIntArrayPtr)
	{
		var json = awrtc.CAPI_WebRtcNetwork_DequeueRtcEvent(lIndex, HEAP32, lTypeIntArrayPtr >> 2, HEAP32, lConidIntArrayPtr >> 2);
		if(json == null)
			return 0;
		
		var strToUTF8 = stringToUTF8;
		if(typeof strToUTF8 !== "function") 
		    strToUTF8 = window.Module.stringToUTF8;
		var lenBytesUTF8  = lengthBytesUTF8;
		if(typeof lenBytesUTF8 !== "function") 
		    lenBytesUTF8 = window.Module.lengthBytesUTF8;
		var malloc  = _malloc;
		if(typeof malloc !== "function") 
		    malloc = window.Module._malloc;

		var len = lengthBytesUTF8(json) + 1;
        var buffer = malloc(len);

		//copy string to C# byte[]
		strToUTF8(json, buffer, len);
		return buffer;
    },
	Unity_WebRtcNetwork_DequeueRtcEvent_Release: function (lBufferPtr) {
		
		var free  = _free;
		if(typeof free !== "function") 
		    free = window.Module._free;
		free(lBufferPtr);
    }
};

var Unity_MediaNetwork =
{
    Unity_BrowserCallFactory_InjectJsCode: function (jscode) {
		var txt = UTF8ToString(jscode);
		eval(txt);
    },
    //function awrtc.CAPI_MediaNetwork_IsAvailable(): boolean
    Unity_MediaNetwork_IsAvailable: function () {
		//hacky way to make sure the device info is available as early as possible
		awrtc.CAPI_DeviceApi_Update();
        if ("awrtc" in window && typeof awrtc.CAPI_MediaNetwork_IsAvailable === 'function') {
            return awrtc.CAPI_MediaNetwork_IsAvailable();
        }
        return false;
    },
    Unity_MediaNetwork_HasUserMedia: function () {
        return awrtc.CAPI_MediaNetwork_HasUserMedia();
    },
    //function CAPI_MediaNetwork_Create(lJsonConfiguration):number
    Unity_MediaNetwork_Create: function (lJsonConfiguration) {
        return awrtc.CAPI_MediaNetwork_Create(UTF8ToString(lJsonConfiguration));
    },
	//function CAPI_MediaNetwork_Configure(lIndex: number, audio: boolean, video: boolean,
    //minWidth: number, minHeight: number,
    //maxWidth: number, maxHeight: number,
    //idealWidth: number, idealHeight: number,
    //minFps: number, maxFps: number, idealFps: number, deviceName: string = "",
    //videoCodecs: string[] = [], videoBitrateKbits: number = -1, videoContentHint: string = "") 
	Unity_MediaNetwork_Configure: function (lIndex, audio, video,
        minWidth, minHeight,
        maxWidth, maxHeight,
        idealWidth, idealHeight,
		minFps, maxFps, idealFps, deviceName, videoCodecsPtr, videoCodecsLen, videoBitrateKbits, videoContentHint,
		audioInputDevice) {
		
		//the video codecs come in as a ptr to a C array
		var videoCodecs = [];
        for (var i = 0; i < videoCodecsLen; i++) {
            videoCodecs.push(Pointer_stringify(Module.HEAP32[(videoCodecsPtr / 4) + i]));
        }

        awrtc.CAPI_MediaNetwork_Configure(lIndex, audio, video, 
			minWidth, minHeight, maxWidth, maxHeight, idealWidth, idealHeight, minFps, maxFps, idealFps,
			UTF8ToString(deviceName), videoCodecs, videoBitrateKbits, UTF8ToString(videoContentHint), UTF8ToString(audioInputDevice));
    },
    //function CAPI_MediaNetwork_GetConfigurationState(lIndex: number): number
    Unity_MediaNetwork_GetConfigurationState: function (lIndex) {
        return awrtc.CAPI_MediaNetwork_GetConfigurationState(lIndex);
    },
    Unity_MediaNetwork_GetConfigurationError_Length: function (lIndex) {
        return awrtc.CAPI_MediaNetwork_GetConfigurationError_Length(lIndex);
	},
    //function CAPI_MediaNetwork_GetConfigurationError(lIndex: number): string
    Unity_MediaNetwork_GetConfigurationError: function (lIndex, lBufferPtr, lBufferLen) {

		var errorMessage = awrtc.CAPI_MediaNetwork_GetConfigurationError(lIndex);		
		
		var strToUTF8 = stringToUTF8;
		if(typeof strToUTF8 !== "function") 
		    strToUTF8 = window.Module.stringToUTF8;

		//copy string to C# byte[]
		strToUTF8(errorMessage, lBufferPtr, lBufferLen);
    },
    //function awrtc.CAPI_MediaNetwork_ResetConfiguration(lIndex: number) : void 
    Unity_MediaNetwork_ResetConfiguration: function (lIndex) {
        awrtc.CAPI_MediaNetwork_ResetConfiguration(lIndex);
    },
    //function awrtc.CAPI_MediaNetwork_TryGetFrame(lIndex: number, lConnectionId: number, lWidthInt32Array: Int32Array, lWidthIntArrayIndex: number, lHeightInt32Array: Int32Array, lHeightIntArrayIndex: number, lBufferUint8Array: Uint8Array, lBufferUint8ArrayOffset: number, lBufferUint8ArrayLength: number): boolean
    Unity_MediaNetwork_TryGetFrame: function (lIndex, lConnectionId, lWidthInt32ArrayPtr, lHeightInt32ArrayPtr, lBufferUint8ArrayPtr, lBufferUint8ArrayOffset, lBufferUint8ArrayLength) {
        return awrtc.CAPI_MediaNetwork_TryGetFrame(lIndex, lConnectionId,
                                        HEAP32, lWidthInt32ArrayPtr >> 2,
                                        HEAP32, lHeightInt32ArrayPtr >> 2,
                                        HEAPU8, lBufferUint8ArrayPtr + lBufferUint8ArrayOffset, lBufferUint8ArrayLength);
    },
    //
	Unity_MediaNetwork_TryGetFrame_ToTexture: function (lIndex, lConnectionId, lWidth, lHeight, lTexture) {
        return awrtc.CAPI_MediaNetwork_TryGetFrame_ToTexture(lIndex, lConnectionId,
            lWidth,
            lHeight,
			GLctx, GL.textures[lTexture]);
    },
	/*
	Unity_MediaNetwork_TryGetFrame_ToTexture2: function (lIndex, lConnectionId, lWidthInt32ArrayPtr, lHeightInt32ArrayPtr, lTextureInt32ArrayPtr) {
		//this doesn't work yet. we have no way to cleanup memory later on
		var offset = lTextureInt32ArrayPtr >> 2;
        var texture = awrtc.CAPI_MediaNetwork_TryGetFrame_ToTexture2(lIndex, lConnectionId,
            HEAP32, lWidthInt32ArrayPtr >> 2,
            HEAP32, lHeightInt32ArrayPtr >> 2,
			GLctx);

		var id = GL.getNewId(GL.textures);
		GL.textures[id] = texture;
		HEAP32[offset] = id;
    },
	*/
    Unity_MediaNetwork_TryGetFrame_Resolution: function (lIndex, connectionId, lWidthInt32ArrayPtr, lHeightInt32ArrayPtr) {
        return awrtc.CAPI_MediaNetwork_TryGetFrame_Resolution(lIndex, connectionId,
            HEAP32, lWidthInt32ArrayPtr >> 2,
            HEAP32, lHeightInt32ArrayPtr >> 2);
    },
    //function awrtc.CAPI_MediaNetwork_TryGetFrameDataLength(lIndex: number, connectionId: number) : number
    Unity_MediaNetwork_TryGetFrameDataLength: function (lIndex, connectionId) {
        return awrtc.CAPI_MediaNetwork_TryGetFrameDataLength(lIndex, connectionId);
    },
    Unity_MediaNetwork_SetVolume: function(lIndex, volume, connectionId) {
        awrtc.CAPI_MediaNetwork_SetVolume(lIndex, volume, connectionId);
    },
    Unity_MediaNetwork_SetVolumePan: function(lIndex, volume, pan, connectionId) {
        awrtc.CAPI_MediaNetwork_SetVolumePan(lIndex, volume, pan, connectionId);
    },
    Unity_MediaNetwork_HasAudioTrack: function(lIndex, connectionId) {
        return awrtc.CAPI_MediaNetwork_HasAudioTrack(lIndex, connectionId);
    },
    Unity_MediaNetwork_HasVideoTrack: function(lIndex, connectionId) {
        return awrtc.CAPI_MediaNetwork_HasVideoTrack(lIndex, connectionId);
    },
    Unity_MediaNetwork_SetMute: function(lIndex, value) {
        awrtc.CAPI_MediaNetwork_SetMute(lIndex, value);
    },
    Unity_MediaNetwork_IsMute: function(lIndex) {
        return awrtc.CAPI_MediaNetwork_IsMute(lIndex);
    },
	Unity_DeviceApi_Update: function()
	{
		awrtc.CAPI_DeviceApi_Update();
	},
	Unity_DeviceApi_RequestUpdate: function()
	{
		awrtc.CAPI_DeviceApi_RequestUpdate();
	},
	Unity_DeviceApi_LastUpdate: function()
	{
		return awrtc.CAPI_DeviceApi_LastUpdate();
	},
	Unity_Media_GetVideoDevices_Length: function()
	{
		return awrtc.CAPI_Media_GetVideoDevices_Length();
	},
	Unity_Media_GetVideoDevices: function(lIndex, lBufferPtr, lBufferLen)
	{
		var jsres = awrtc.CAPI_Media_GetVideoDevices(lIndex);
		//will copy to HEAPU8 at lBufferPtr 
		stringToUTF8(jsres, lBufferPtr, lBufferLen);
		//returns length in bytes we have written (not including the c string null terminator )
		return lengthBytesUTF8(jsres);
	},
	Unity_Media_GetAudioInputDevices_Length: function()
	{
		return awrtc.CAPI_Media_GetAudioInputDevices_Length();
	},
	Unity_Media_GetAudioInputDevices: function(lIndex, lBufferPtr, lBufferLen)
	{
		var jsres = awrtc.CAPI_Media_GetAudioInputDevices(lIndex);
		//will copy to HEAPU8 at lBufferPtr 
		stringToUTF8(jsres, lBufferPtr, lBufferLen);
		//returns length in bytes we have written (not including the c string null terminator )
		return lengthBytesUTF8(jsres);
	},
	Unity_SLog_SetLogLevel: function(loglevel)
	{
		awrtc.CAPI_SLog_SetLogLevel(loglevel);
	},
	Unity_InitAsync: function(initmode, useAdapter)
	{
		awrtc.CAPI_InitAsync(initmode, GLctx, useAdapter);
	},
	Unity_PollInitState: function()
	{
		return awrtc.CAPI_PollInitState();
	},
	Unity_VideoInput_AddCanvasDevice: function(query, name, width, height, fps)
	{
		return awrtc.CAPI_VideoInput_AddCanvasDevice(UTF8ToString(query), UTF8ToString(name), width, height, fps);
	},
	//export function CAPI_VideoInput_AddDevice(name:string,  width: number,  height: number,  fps: number);
	Unity_VideoInput_AddDevice: function(name, width, height, fps)
	{
		awrtc.CAPI_VideoInput_AddDevice(UTF8ToString(name), width, height, fps);
	},
	//export function CAPI_VideoInput_RemoveDevice(name:string);
	Unity_VideoInput_RemoveDevice: function()
	{
		awrtc.CAPI_VideoInput_RemoveDevice(UTF8ToString(name));
	},
	//export function CAPI_VideoInput_UpdateFrame(name:string, lBufferUint8Array: Uint8Array, lBufferUint8ArrayOffset: number, lBufferUint8ArrayLength: number, width: number, height: number, rotation: number, firstRowIsBottom: boolean): boolean
	Unity_VideoInput_UpdateFrame: function(name, lBufferUint8ArrayPtr, lBufferUint8ArrayOffset, lBufferUint8ArrayLength, width, height, rotation, firstRowIsBottom)
	{
		return awrtc.CAPI_VideoInput_UpdateFrame(UTF8ToString(name), HEAPU8, lBufferUint8ArrayPtr + lBufferUint8ArrayOffset, lBufferUint8ArrayLength, width, height, rotation, firstRowIsBottom);
	},
	//export function CAPI_Media_EnableScreenCapture(name: string, captureAudio: boolean): void 
	Unity_Media_EnableScreenCapture: function(name, captureAudio)
	{
		return awrtc.CAPI_Media_EnableScreenCapture(UTF8ToString(name), captureAudio);
	}
	
}
mergeInto(LibraryManager.library, Unity_WebRtcNetwork);
mergeInto(LibraryManager.library, Unity_MediaNetwork);
