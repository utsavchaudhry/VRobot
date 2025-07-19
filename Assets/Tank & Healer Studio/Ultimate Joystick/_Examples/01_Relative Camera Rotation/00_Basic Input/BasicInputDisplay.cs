using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TankAndHealerStudioAssets
{
	public class BasicInputDisplay : MonoBehaviour
	{
		public Text keyboardInputValueText, joystickInputValueText;
		public Image keyW, keyA, keyS, keyD;
		public Color keyPressedColor = Color.white, keyDefaultColor = Color.white;
		public RectTransform joystickDisplayTransform, keyboardDisplayTransform;

		public UltimateJoystick joystick;
		public float movementSpeed = 250;

		
		private void Update ()
		{
			Vector2 keyboardMovementValue = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
			Keyboard keyboard = InputSystem.GetDevice<Keyboard>();
			UpdateKeyboardDisplay( keyW, keyboard.wKey.isPressed );
			UpdateKeyboardDisplay( keyA, keyboard.aKey.isPressed );
			UpdateKeyboardDisplay( keyS, keyboard.sKey.isPressed );
			UpdateKeyboardDisplay( keyD, keyboard.dKey.isPressed );

			if( keyboard.wKey.isPressed )
				keyboardMovementValue.y = 1.0f;
			else if( keyboard.sKey.isPressed )
				keyboardMovementValue.y = -1.0f;
			else
				keyboardMovementValue.y = 0.0f;

			if( keyboard.aKey.isPressed )
				keyboardMovementValue.x = -1.0f;
			else if( keyboard.dKey.isPressed )
				keyboardMovementValue.x = 1.0f;
			else
				keyboardMovementValue.x = 0.0f;
#else
			// KEYBOARD INPUT DISPLAY //
			UpdateKeyboardDisplay( keyW, Input.GetKey( KeyCode.W ) );
			UpdateKeyboardDisplay( keyA, Input.GetKey( KeyCode.A ) );
			UpdateKeyboardDisplay( keyS, Input.GetKey( KeyCode.S ) );
			UpdateKeyboardDisplay( keyD, Input.GetKey( KeyCode.D ) );

			keyboardMovementValue = new Vector2( Input.GetAxis( "Horizontal" ), Input.GetAxis( "Vertical" ) );
#endif
			keyboardInputValueText.text = $"Keyboard Input Values\nHorizontal: {keyboardMovementValue.x.ToString( "F2" )}\nVertical: {keyboardMovementValue.y.ToString( "F2" )}";
			keyboardDisplayTransform.Translate( new Vector3( keyboardMovementValue.x, keyboardMovementValue.y, 0 ) * Time.deltaTime * movementSpeed );
			ConstrainImageToScreen( keyboardDisplayTransform );

			// ULTIMATE JOYSTICK INPUT DISPLAY //
			joystickInputValueText.text = $"Joystick Input Values\nHorizontal: {joystick.HorizontalAxis.ToString( "F2" )}\nVertical: {joystick.VerticalAxis.ToString( "F2" )}";
			joystickDisplayTransform.Translate( new Vector3( joystick.HorizontalAxis, joystick.VerticalAxis, 0 ) * Time.deltaTime * movementSpeed );
			ConstrainImageToScreen( joystickDisplayTransform );
		}

		//void UpdateKeyboardDisplay ( Image keyImage, KeyCode keyCode )
		void UpdateKeyboardDisplay ( Image keyImage, bool keycodeDown )
		{
			if( keyImage == null )
				return;

			if( keycodeDown && keyImage.color != keyPressedColor )
				keyImage.color = keyPressedColor;
			else if( !keycodeDown && keyImage.color != keyDefaultColor )
				keyImage.color = keyDefaultColor;
		}

		void ConstrainImageToScreen ( RectTransform imageTrans )
		{
			float halfScreenWidth = Screen.width / 2;
			float halfScreenHeight = Screen.height / 2;
			imageTrans.localPosition = new Vector3( Mathf.Clamp( imageTrans.localPosition.x, -halfScreenWidth, halfScreenWidth ), Mathf.Clamp( imageTrans.localPosition.y, -halfScreenHeight, halfScreenHeight ), 0 );
		}
	}
}