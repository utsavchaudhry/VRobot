using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TankAndHealerStudioAssets
{
	[RequireComponent( typeof( Rigidbody ) )]
	public class TapCountExample : MonoBehaviour
	{
		Rigidbody rb;
		public UltimateJoystick joystick;
		public float jumpForce = 10;
		public int targetTapCount = 2;
		public int rotationModifier = 100;


		void Start ()
		{
			// Store the rigidbody component.
			rb = GetComponent<Rigidbody>();

			// Subscribe to the tap count achieved callback.
			joystick.OnTapAchieved += ( tapCount ) =>
			{
				// If the tap count value on the joystick is not equal to the target tap count we set, then return.
				if( tapCount != targetTapCount )
					return;

				// Add force to make the cube jump.
				rb.AddForce( Vector3.up * jumpForce, ForceMode.Impulse );
			};

			joystick.OnTapReleased += () =>
			{

			};
		}

		private void FixedUpdate ()
		{
			rb.AddTorque( new Vector3( joystick.VerticalAxis, 0, -joystick.HorizontalAxis ) * rotationModifier );
		}
	}
}