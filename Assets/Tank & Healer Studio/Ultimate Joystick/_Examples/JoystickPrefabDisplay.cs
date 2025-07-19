using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TankAndHealerStudioAssets
{
	public class JoystickPrefabDisplay : MonoBehaviour
	{
		[Header( "Common Settings" )]
		public Text prefabNameText;

		[Header( "Prefabs" )]
		public List<GameObject> allPrefabs = new List<GameObject>();
		List<UltimateJoystick> allJoysticks = new List<UltimateJoystick>();
		int currentPrefabIndex = 0;


		private void Start ()
		{
			// If there are no prefabs in the list, send an error and return.
			if( allPrefabs.Count == 0 )
			{
				Debug.LogError( "No prefabs are registered in the All Prefabs list." );
				return;
			}

			// Loop through all the prefabs in the list...
			for( int i = 0; i < allPrefabs.Count; i++ )
			{
				// Create and add the prefab to the list.
				allJoysticks.Add( Instantiate( allPrefabs[ i ], transform ).GetComponent<UltimateJoystick>() );

				allJoysticks[ allJoysticks.Count - 1 ].UpdatePositioning( 50.0f, 50.0f );

				// If this is not the first object in the loop, then disable it so that only the first prefab will be visible to start.
				if( i > 0 )
					allJoysticks[ allJoysticks.Count - 1 ].Disable();
			}

			// Enable the current prefab and update text.
			EnablePrefab( allJoysticks[ 0 ] );
		}

		public void NextPrefab ()
		{
			// Disable the current prefab.
			allJoysticks[ currentPrefabIndex ].Disable();

			// Configure the target index as one more than the current.
			int targetIndex = currentPrefabIndex + 1;

			// If the target is out of range, then set it to the 0 index.
			if( targetIndex >= allJoysticks.Count )
				targetIndex = 0;

			// Enable the current prefab and update text.
			EnablePrefab( allJoysticks[ targetIndex ] );

			// Update the current index.
			currentPrefabIndex = targetIndex;
		}


		public void PreviousPrefab ()
		{
			// Disable the current prefab.
			allJoysticks[ currentPrefabIndex ].Disable();

			// Configure the target index.
			int targetIndex = currentPrefabIndex - 1;

			// If the target index is out of range, then set it to the end of the list.
			if( targetIndex < 0 )
				targetIndex = allJoysticks.Count - 1;

			// Enable the current prefab and update text.
			EnablePrefab( allJoysticks[ targetIndex ] );

			// Update the current index.
			currentPrefabIndex = targetIndex;
		}

		public void SelectPrefabInProject ()
		{
			// If this is run in the editor, then select the prefab gameobject in the project window.
#if UNITY_EDITOR
			UnityEditor.Selection.activeGameObject = allPrefabs[ currentPrefabIndex ];
#endif
		}

		void EnablePrefab ( UltimateJoystick joystick )
		{
			// Enable the prefab.
			joystick.Enable();

			// Update the name text to the current prefab.
			prefabNameText.text = "Prefab name: " + joystick.gameObject.name.Split( '(' )[ 0 ];
		}
	}
}