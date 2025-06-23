using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TankAndHealerStudioAssets
{
	[RequireComponent( typeof( Rigidbody ) )]
	public class RelativeCameraRotationExample : MonoBehaviour
	{
		Rigidbody rb;
		public UltimateJoystick moveJoystick, cameraJoystick;
		public Transform cameraBaseTransform;
		public float cameraRotationSpeed = 50.0f;
		public float moveSpeed = 5.0f;

		public enum MovementCalculations
		{
			Type01,
			Type02
		}
		public MovementCalculations movementCalculations = MovementCalculations.Type01;
		

		private void Start ()
		{
			// Store the attached rigidbody component.
			rb = GetComponent<Rigidbody>();
		}

		/// <summary>
		/// FixedUpdate() is used here because the character moves using physics. Fixed update is consistent and provides a smooth experience for the player.
		/// </summary>
		void FixedUpdate ()
		{
			// If the camera base transform is unassigned, then inform the user and return.
			if( cameraBaseTransform == null )
			{
				Debug.LogError( "The camera's base transform has not been assigned!" );
				return;
			}

			// IF either of the joysticks are unassigned, inform the user and return to avoid errors.
			if( moveJoystick == null || cameraJoystick == null )
			{
				Debug.LogError( "One or more of the joysticks have not been assigned." );
				return;
			}

			// Rotate the camera base, which is centered on the player so that it orbits around our player, according to the camera (right) joystick's horizontal axis.
			cameraBaseTransform.Rotate( new Vector3( 0, -cameraJoystick.HorizontalAxis * cameraRotationSpeed * Time.deltaTime, 0 ) );

			// If the move (left) joystick has active input on it...
			if( moveJoystick.InputActive )
			{
				// If the user wants to try the first type calculations...
				if( movementCalculations == MovementCalculations.Type01 )
				{
					// Configure the target rotation by taking ONLY the y rotation of the main camera and multiplying it the move joysticks current angle.
					Quaternion targetRotation = Quaternion.Euler( 0, Camera.main.transform.rotation.eulerAngles.y, 0 ) * Quaternion.Euler( 0, moveJoystick.GetAngle(), 0 );

					// Set this transforms rotation by transitioning between the current rotation to the target rotation.
					transform.rotation = Quaternion.Slerp( transform.rotation, targetRotation, 0.1f );

					// Move this rigidbody forward according to the move speed multiply that by the joystick's distance so the speed is variable depending on the users input.
					rb.MovePosition( transform.position + transform.forward * moveSpeed * moveJoystick.GetDistance() * Time.deltaTime );
				}
				// Else they want to do the second type calculations...
				else
				{
					// Convert the players input into world space relative to the camera base transform.
					Vector3 localInputDirection = cameraBaseTransform.TransformDirection( new Vector3( moveJoystick.HorizontalAxis, 0, moveJoystick.VerticalAxis ) );

					// Look at the direction of the input.
					transform.LookAt( transform.position + localInputDirection );

					// Move the rigidbody towards the local input direction multiplied by the defined move speed.
					rb.MovePosition( transform.position + localInputDirection * moveSpeed * Time.deltaTime );
				}
			}
		}
	}
}