/* UltimateJoystickEditor.cs */
/* Written by Kaz Crowe */
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using TankAndHealerStudioAssets;
using System.Linq;
using System.Xml;
using System;

[CustomEditor( typeof( UltimateJoystick ) )]
public class UltimateJoystickEditor : Editor
{
	UltimateJoystick targ;
	bool isInProjectWindow = false;

	// -----< JOYSTICK SETTINGS >----- //
	Canvas parentCanvas;
	Sprite joystickBaseSprite, joystickSprite;
	Color baseColor = Color.white;
	SerializedProperty anchor, positionHorizontal, positionVertical;
	SerializedProperty joystickSize, joystickRadius;
	SerializedProperty relativeTransform, centerAngle, orbitDistance;
	SerializedProperty activationRange, customActivationRange;
	SerializedProperty activationWidth, activationHeight;
	SerializedProperty activationPositionHorizontal, activationPositionVertical;
	SerializedProperty overridePositioning, applyConstraintDuringOverride, saveAsPlayerPrefs, uniqueId;
	SerializedProperty inputHandling, dynamicPositioning, gravity, extendRadius;
	SerializedProperty axis, deadZone, tapDecayRate, transmitEventData;

	// -----< VISUAL OPTIONS >----- //
	SerializedProperty disableVisuals, inputTransition;
	SerializedProperty useFade, useScale;
	SerializedProperty transitionUntouchedDuration, transitionTouchedDuration;
	SerializedProperty fadeUntouched, fadeTouched, scaleTouched;
	SerializedProperty showHighlight, highlightColor;
	SerializedProperty highlightBase, highlightJoystick;
	Image highlightBaseImage, highlightJoystickImage;
	Sprite highlightBaseSprite, highlightJoystickSprite;
	SerializedProperty showTension, tensionColorNone, tensionColorFull;
	SerializedProperty tensionType, rotationOffset, tensionDeadZone;
	Sprite tensionAccentSprite;
	SerializedProperty TensionAccents;
	bool noSpriteDirection = false;
	float tensionScale = 1.0f;
	List<string> spriteDirectionOptions = new List<string>()
	{
		"Up",
		"Down",
		"Left",
		"Right"
	};
	List<float> spriteDirectionRotationMod = new List<float>()
	{
		0.0f,
		180.0f,
		270.0f,
		90.0f,
		0.0f
	};
	int currentSpriteDirection = 0;

	// ------< SCRIPT REFERENCE >------ //
	SerializedProperty joystickName;

	// DEVELOPMENT MODE //
	public bool showDefaultInspector = false;
	class ExampleCode
	{
		public string optionName = "";
		public string optionDescription = "";
		public string basicCode = "";
	}
	ExampleCode[] PublicExampleCode = new ExampleCode[]
	{
		new ExampleCode() { optionName = "GetHorizontalAxis", optionDescription = "Returns the horizontal axis value of the Ultimate Joystick.", basicCode = "float h = joystick.GetHorizontalAxis();" },
		new ExampleCode() { optionName = "GetVerticalAxis", optionDescription = "Returns the vertical axis value of the Ultimate Joystick.", basicCode = "float v = joystick.GetVerticalAxis();" },
		new ExampleCode() { optionName = "GetHorizontalAxisRaw", optionDescription = "Returns the raw horizontal axis value of the Ultimate Joystick.", basicCode = "float h = joystick.GetHorizontalAxisRaw();" },
		new ExampleCode() { optionName = "GetVerticalAxisRaw", optionDescription = "Returns the raw vertical axis value of the Ultimate Joystick.", basicCode = "float v = joystick.GetVerticalAxisRaw();" },
		new ExampleCode() { optionName = "GetInputActive", optionDescription = "Returns the current state of interaction on the joystick.", basicCode = "bool joystickInputActive = joystick.GetInputActive();" },
		new ExampleCode() { optionName = "GetDistance", optionDescription = "Returns the distance of the joystick image from the center of the Ultimate Joystick.", basicCode = "float distance = joystick.GetDistance();" },
		new ExampleCode() { optionName = "GetAngle", optionDescription = "Returns the angle that the joystick is currently from center.", basicCode = "float angle = joystick.GetAngle();" },
		new ExampleCode() { optionName = "Disable", optionDescription = "Disables the Ultimate Joystick.", basicCode = "joystick.Disable();" },
		new ExampleCode() { optionName = "Enable", optionDescription = "Enables the Ultimate Joystick.", basicCode = "joystick.Enable();" },
	};
	ExampleCode[] StaticExampleCode = new ExampleCode[]
	{
		new ExampleCode() { optionName = "GetHorizontalAxis ( string joystickName )", optionDescription = "Returns the horizontal axis value of the targeted Ultimate Joystick.", basicCode = "float h = UltimateJoystick.GetHorizontalAxis( \"{0}\" );" },
		new ExampleCode() { optionName = "GetVerticalAxis ( string joystickName )", optionDescription = "Returns the vertical axis value of the targeted Ultimate Joystick.", basicCode = "float v = UltimateJoystick.GetVerticalAxis( \"{0}\" );" },
		new ExampleCode() { optionName = "GetHorizontalAxisRaw ( string joystickName )", optionDescription = "Returns the raw horizontal axis value of the targeted Ultimate Joystick.", basicCode = "float h = UltimateJoystick.GetHorizontalAxisRaw( \"{0}\" );" },
		new ExampleCode() { optionName = "GetVerticalAxisRaw ( string joystickName )", optionDescription = "Returns the raw vertical axis value of the targeted Ultimate Joystick.", basicCode = "float v = UltimateJoystick.GetVerticalAxisRaw( \"{0}\" );" },
		new ExampleCode() { optionName = "GetInputActive ( string joystickName )", optionDescription = "Returns the bool value of the current state of interaction of the targeted Ultimate Joystick.", basicCode = "if( UltimateJoystick.GetInputActive( \"{0}\" ) )" },
		new ExampleCode() { optionName = "GetDistance ( string joystickName )", optionDescription = "Returns the distance of the joystick image from the center of the targeted Ultimate Joystick.", basicCode = "float distance = UltimateJoystick.GetDistance( \"{0}\" );" },
		new ExampleCode() { optionName = "GetAngle ( string joystickName )", optionDescription = "Returns the angle that the joystick currently is from the center.", basicCode = "float angle = UltimateJoystick.GetAngle( \"{0}\" );" },
		new ExampleCode() { optionName = "Disable ( string joystickName )", optionDescription = "Disables the targeted Ultimate Joystick.", basicCode = "UltimateJoystick.Disable( \"{0}\" );" },
		new ExampleCode() { optionName = "Enable ( string joystickName )", optionDescription = "Enables the targeted Ultimate Joystick.", basicCode = "UltimateJoystick.Enable( \"{0}\" );" },
		new ExampleCode() { optionName = "ReturnComponent ( string joystickName )", optionDescription = "Returns the Ultimate Joystick component that has been registered with the targeted name.", basicCode = "UltimateJoystick movementJoystick = UltimateJoystick.ReturnComponent( \"{0}\" );" },
	};
	List<string> exampleCodeOptions = new List<string>();
	int exampleCodeIndex = 0;

	// SCENE GUI //
	bool DisplayCenterAngle = false;
	bool DisplayActivationRange = false;
	bool DisplayActivationCustomWidth = false;
	bool DisplayActivationCustomHeight = false;
	bool DisplayRadius = false;
	bool DisplayAxis = false;
	bool DisplayDeadZone = false;
	bool DisplayTensionDeadZone = false;
	static bool isDirty = false;
	Vector2 currentJoystickSize = Vector2.zero;

	// Gizmo Colors //
	Color colorDefault = Color.black;
	Color colorValueChanged = Color.black;

	// EDITOR STYLES //
	GUIStyle handlesCenteredText = new GUIStyle();
	GUIStyle collapsableSectionStyle = new GUIStyle();

	// DRAG AND DROP //
	bool disableDragAndDrop = false;
	bool isDraggingObject = false;
	Vector2 dragAndDropMousePos = Vector2.zero;
	double dragAndDropStartTime = 0.0f;
	double dragAndDropCurrentTime = 0.0f;
	bool DragAndDropHover
	{
		get
		{
			if( disableDragAndDrop )
				return false;

			if( DragAndDrop.objectReferences.Length == 0 )
			{
				dragAndDropStartTime = 0.0f;
				dragAndDropCurrentTime = 0.0f;
				isDraggingObject = false;
				return false;
			}

			isDraggingObject = true;

			var rect = GUILayoutUtility.GetLastRect();
			if( Event.current.type == EventType.Repaint && rect.Contains( Event.current.mousePosition ) )
			{
				if( dragAndDropStartTime == 0.0f )
				{
					dragAndDropStartTime = EditorApplication.timeSinceStartup;
					dragAndDropCurrentTime = 0.0f;
				}

				if( dragAndDropMousePos == Event.current.mousePosition )
					dragAndDropCurrentTime = EditorApplication.timeSinceStartup - dragAndDropStartTime;
				else
				{
					dragAndDropStartTime = EditorApplication.timeSinceStartup;
					dragAndDropCurrentTime = 0.0f;
				}

				if( dragAndDropCurrentTime >= 0.5f )
				{
					dragAndDropStartTime = 0.0f;
					dragAndDropCurrentTime = 0.0f;
					return true;
				}

				dragAndDropMousePos = Event.current.mousePosition;
			}

			return false;
		}
	}


	void OnEnable ()
	{
		StoreReferences();
		Undo.undoRedoPerformed += StoreReferences;

		if( targ != null && !isInProjectWindow )
		{
			if( !targ.gameObject.GetComponent<Image>() )
				Undo.AddComponent<Image>( targ.gameObject );

			Undo.RecordObject( targ.gameObject.GetComponent<Image>(), "Null Joystick Alpha" );
			targ.gameObject.GetComponent<Image>().color = new Color( 1.0f, 1.0f, 1.0f, 0.0f );
		}

		if( EditorPrefs.HasKey( "UJ_ColorHexSetup" ) )
		{
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "UJ_ColorDefaultHex" ), out colorDefault );
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "UJ_ColorValueChangedHex" ), out colorValueChanged );
		}

		UltimateJoystick.CustomEditorPositioning = false;

		if( targ.Joystick != null && targ.Joystick.gameObject != targ.gameObject && targ.Joystick.GetComponent<Image>().raycastTarget )
		{
			Undo.RecordObject( targ.Joystick.GetComponent<Image>(), "Disable Unnecessary Joystick Raycast" );
			targ.Joystick.GetComponent<Image>().raycastTarget = false;
		}

		if( highlightBaseImage != null && highlightBaseImage.gameObject != targ.gameObject && highlightBaseImage.raycastTarget )
		{
			Undo.RecordObject( highlightBaseImage, "Disable Unnecessary Joystick Raycast" );
			highlightBaseImage.raycastTarget = false;
		}

		if( highlightJoystickImage != null && highlightJoystickImage.gameObject != targ.gameObject && highlightJoystickImage.raycastTarget )
		{
			Undo.RecordObject( highlightJoystickImage, "Disable Unnecessary Joystick Raycast" );
			highlightJoystickImage.raycastTarget = false;
		}

		if( TensionAccents != null && TensionAccents.arraySize > 0 )
		{
			for( int i = 0; i < TensionAccents.arraySize; i++ )
			{
				Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
				if( tensionImage == null )
					continue;

				if( !tensionImage.raycastTarget )
					break;

				Undo.RecordObject( tensionImage, "Disable Unnecessary Joystick Raycast" );
				tensionImage.raycastTarget = false;
			}
		}
	}

	void OnDisable ()
	{
		Undo.undoRedoPerformed -= StoreReferences;
		UltimateJoystick.CustomEditorPositioning = false;
	}

	Canvas GetParentCanvas ()
	{
		if( Selection.activeGameObject == null )
			return null;

		Transform parent = Selection.activeGameObject.transform.parent;
		while( parent != null )
		{
			if( parent.transform.GetComponent<Canvas>() )
				return parent.transform.GetComponent<Canvas>();
			
			parent = parent.transform.parent;
		}

		if( parent == null && !AssetDatabase.Contains( Selection.activeGameObject ) )
			RequestCanvas( Selection.activeGameObject );

		return null;
	}
	
	void CheckPropertyHover ( ref bool hovered )
	{
		hovered = false;
		var rect = GUILayoutUtility.GetLastRect();
		if( Event.current.type == EventType.Repaint && rect.Contains( Event.current.mousePosition ) )
			hovered = isDirty = true;
	}
	
	void PropertyUpdated ( ref bool propertyController )
	{
		propertyController = isDirty = true;
	}

	void StoreReferences ()
	{
		targ = ( UltimateJoystick )target;

		if( targ == null )
			return;

		isInProjectWindow = AssetDatabase.Contains( targ.gameObject );
		parentCanvas = GetParentCanvas();

		if( serializedObject.FindProperty( "joystickBase" ).objectReferenceValue == null )
		{
			if( isInProjectWindow )
				return;

			GameObject newGameObject = new GameObject( "Joystick Base", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
			Undo.RegisterCreatedObjectUndo( newGameObject, "Create Joystick Objects" );
			newGameObject.GetComponent<Image>().sprite = joystickBaseSprite;
			newGameObject.GetComponent<Image>().color = baseColor;

			newGameObject.transform.SetParent( targ.transform );
			newGameObject.transform.SetAsFirstSibling();

			RectTransform trans = newGameObject.GetComponent<RectTransform>();

			trans.anchorMin = new Vector2( 0.5f, 0.5f );
			trans.anchorMax = new Vector2( 0.5f, 0.5f );
			trans.pivot = new Vector2( 0.5f, 0.5f );
			trans.anchoredPosition = Vector2.zero;
			trans.localScale = Vector3.one;
			trans.localPosition = Vector3.zero;
			trans.localRotation = Quaternion.identity;

			serializedObject.FindProperty( "joystickBase" ).objectReferenceValue = newGameObject.GetComponent<RectTransform>();
			serializedObject.ApplyModifiedProperties();
		}

		if( serializedObject.FindProperty( "joystick" ).objectReferenceValue == null )
		{
			if( isInProjectWindow )
				return;

			GameObject newGameObject = new GameObject( "Joystick", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
			Undo.RegisterCreatedObjectUndo( newGameObject, "Create Joystick Objects" );

			newGameObject.GetComponent<Image>().sprite = joystickSprite;
			newGameObject.GetComponent<Image>().color = baseColor;

			newGameObject.transform.SetParent( targ.JoystickBase );
			newGameObject.transform.SetAsFirstSibling();

			RectTransform trans = newGameObject.GetComponent<RectTransform>();

			trans.anchorMin = new Vector2( 0.0f, 0.0f );
			trans.anchorMax = new Vector2( 1.0f, 1.0f );
			trans.offsetMin = Vector2.zero;
			trans.offsetMax = Vector2.zero;
			trans.pivot = new Vector2( 0.5f, 0.5f );
			trans.anchoredPosition = Vector2.zero;
			trans.localScale = Vector3.one;
			trans.localPosition = Vector3.zero;
			trans.localRotation = Quaternion.identity;

			serializedObject.FindProperty( "joystick" ).objectReferenceValue = newGameObject.GetComponent<RectTransform>();
			serializedObject.ApplyModifiedProperties();
		}

		// -------------------< JOYSTICK SETTINGS >------------------- //
		if( targ.JoystickBase != null && targ.JoystickBase.GetComponent<Image>() && targ.JoystickBase.GetComponent<Image>().sprite != null )
			joystickBaseSprite = targ.JoystickBase.GetComponent<Image>().sprite;

		if( targ.Joystick != null && targ.Joystick.GetComponent<Image>() && targ.Joystick.GetComponent<Image>().sprite != null )
			joystickSprite = targ.Joystick.GetComponent<Image>().sprite;

		baseColor = targ.JoystickBase == null ? Color.white : targ.JoystickBase.GetComponent<Image>().color;

		// JOYSTICK POSITIONING //
		anchor = serializedObject.FindProperty( "anchor" );
		activationRange = serializedObject.FindProperty( "activationRange" );
		customActivationRange = serializedObject.FindProperty( "customActivationRange" );
		activationWidth = serializedObject.FindProperty( "activationWidth" );
		activationHeight = serializedObject.FindProperty( "activationHeight" );
		activationPositionHorizontal = serializedObject.FindProperty( "activationPositionHorizontal" );
		activationPositionVertical = serializedObject.FindProperty( "activationPositionVertical" );
		inputHandling = serializedObject.FindProperty( "inputHandling" );
		dynamicPositioning = serializedObject.FindProperty( "dynamicPositioning" );
		joystickSize = serializedObject.FindProperty( "joystickSize" );
		joystickRadius = serializedObject.FindProperty( "joystickRadius" );
		if( serializedObject.FindProperty( "radiusModifier" ).floatValue > 0.0f )
		{
			joystickRadius.floatValue = serializedObject.FindProperty( "radiusModifier" ).floatValue / 5.0f;
			serializedObject.FindProperty( "radiusModifier" ).floatValue = -1.0f;
			serializedObject.ApplyModifiedProperties();
		}
		relativeTransform = serializedObject.FindProperty( "relativeTransform" );
		centerAngle = serializedObject.FindProperty( "centerAngle" );
		orbitDistance = serializedObject.FindProperty( "orbitDistance" );
		positionHorizontal = serializedObject.FindProperty( "positionHorizontal" );
		positionVertical = serializedObject.FindProperty( "positionVertical" );
		// OVERRIDE POSITIONING //
		overridePositioning = serializedObject.FindProperty( "overridePositioning" );
		applyConstraintDuringOverride = serializedObject.FindProperty( "applyConstraintDuringOverride" );
		saveAsPlayerPrefs = serializedObject.FindProperty( "saveAsPlayerPrefs" );
		uniqueId = serializedObject.FindProperty( "uniqueId" );
		if( !isInProjectWindow )
		{
			// If there is no unique ID, or there is another joystick with the same ID (VERY unlikely), then get a new random ID.
#if UNITY_2022_2_OR_NEWER
			if( string.IsNullOrEmpty( uniqueId.stringValue ) || !( FindObjectsByType<UltimateJoystick>( FindObjectsSortMode.None ).Count( x => x.uniqueId == uniqueId.stringValue ) == 1 ) )
#else
		if( string.IsNullOrEmpty( uniqueId.stringValue ) || !( FindObjectsOfType<UltimateJoystick>().Count( x => x.uniqueId == uniqueId.stringValue ) == 1 ) )
#endif
			{
				uniqueId.stringValue = Guid.NewGuid().ToString().Substring( 0, 8 );
				serializedObject.ApplyModifiedProperties();
			}
		}
		// INPUT SETTINGS //
		gravity = serializedObject.FindProperty( "gravity" );
		extendRadius = serializedObject.FindProperty( "extendRadius" );
		axis = serializedObject.FindProperty( "axis" );
		deadZone = serializedObject.FindProperty( "deadZone" );
		tapDecayRate = serializedObject.FindProperty( "tapDecayRate" );
		transmitEventData = serializedObject.FindProperty( "transmitEventData" );

		// -------------------< VISUAL OPTIONS >------------------- //
		disableVisuals = serializedObject.FindProperty( "disableVisuals" );
		// INPUT TRANSITION //
		inputTransition = serializedObject.FindProperty( "inputTransition" );
		useFade = serializedObject.FindProperty( "useFade" );
		useScale = serializedObject.FindProperty( "useScale" );
		fadeUntouched = serializedObject.FindProperty( "fadeUntouched" );
		transitionUntouchedDuration = serializedObject.FindProperty( "transitionUntouchedDuration" );
		fadeTouched = serializedObject.FindProperty( "fadeTouched" );
		scaleTouched = serializedObject.FindProperty( "scaleTouched" );
		transitionTouchedDuration = serializedObject.FindProperty( "transitionTouchedDuration" );
		// HIGHLIGHT //
		showHighlight = serializedObject.FindProperty( "showHighlight" );
		highlightBase = serializedObject.FindProperty( "highlightBase" );
		if( highlightBase.objectReferenceValue != null )
		{
			highlightBaseImage = ( Image )highlightBase.objectReferenceValue;
			if( highlightBaseImage != null && highlightBaseImage.sprite != null )
				highlightBaseSprite = highlightBaseImage.sprite;
		}
		highlightJoystick = serializedObject.FindProperty( "highlightJoystick" );
		if( highlightJoystick.objectReferenceValue != null )
		{
			highlightJoystickImage = ( Image )highlightJoystick.objectReferenceValue;
			if( highlightJoystickImage != null && highlightJoystickImage.sprite != null )
				highlightJoystickSprite = highlightJoystickImage.sprite;
		}
		highlightColor = serializedObject.FindProperty( "highlightColor" );
		// TENSION //
		showTension = serializedObject.FindProperty( "showTension" );
		tensionType = serializedObject.FindProperty( "tensionType" );
		tensionColorNone = serializedObject.FindProperty( "tensionColorNone" );
		tensionColorFull = serializedObject.FindProperty( "tensionColorFull" );
		rotationOffset = serializedObject.FindProperty( "rotationOffset" );
		tensionDeadZone = serializedObject.FindProperty( "tensionDeadZone" );
		TensionAccents = serializedObject.FindProperty( "TensionAccents" );
		noSpriteDirection = NoSpriteDirection;
		currentSpriteDirection = rotationOffset.floatValue == 0.0f ? 0 : 3;
		if( rotationOffset.floatValue >= 180.0f )
			currentSpriteDirection = rotationOffset.floatValue == 180.0f ? 1 : 2;
		if( tensionType.enumValueIndex == 0 )
		{
			spriteDirectionOptions.Add( "All Unique" );

			if( rotationOffset.floatValue == 0.0f && noSpriteDirection )
				currentSpriteDirection = spriteDirectionOptions.Count - 1;
		}
		for( int i = 0; i < TensionAccents.arraySize; i++ )
		{
			Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
			if( tensionImage == null || tensionImage.sprite == null )
				continue;

			tensionAccentSprite = tensionImage.sprite;
		}
		if( TensionAccents.arraySize > 0 && TensionAccents.GetArrayElementAtIndex( 0 ).objectReferenceValue != null )
		{
			Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( 0 ).objectReferenceValue;
			tensionScale = tensionImage.transform.localScale.x;
		}

		// -------------------< SCRIPT REFERENCE >------------------- //
		joystickName = serializedObject.FindProperty( "joystickName" );
		exampleCodeOptions = new List<string>();
		for( int i = 0; i < PublicExampleCode.Length; i++ )
			exampleCodeOptions.Add( PublicExampleCode[ i ].optionName );
	}

	public override void OnInspectorGUI ()
	{
		serializedObject.Update();

		// DEVELOPMENT INSPECTOR //
		if( EditorPrefs.GetBool( "UUI_DevelopmentMode" ) )
		{
			EditorGUILayout.Space();
			GUIStyle toolbarStyle = new GUIStyle( EditorStyles.toolbarButton ) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 11, richText = true };
			GUILayout.BeginHorizontal();
			GUILayout.Space( -10 );
			showDefaultInspector = GUILayout.Toggle( showDefaultInspector, ( showDefaultInspector ? "▼" : "►" ) + "<color=#ff0000ff> Development Inspector</color>", toolbarStyle );
			GUILayout.EndHorizontal();

			if( showDefaultInspector )
			{
				EditorGUILayout.Space();

				base.OnInspectorGUI();

				EditorGUILayout.LabelField( "End of Development Inspector", EditorStyles.centeredGreyMiniLabel );
				EditorGUILayout.Space();
				return;
			}
			else if( DragAndDropHover )
				showDefaultInspector = true;

			EditorGUILayout.Space();
		}
		// END DEVELOPMENT INSPECTOR //

		if( isInProjectWindow && ( targ.JoystickBase == null || targ.Joystick == null ) )
		{
			EditorGUILayout.HelpBox( "This joystick does not have the basic needed objects to function. The needed objects cannot be created within the project window.", MessageType.Error );
			EditorGUILayout.HelpBox( "Please drag this prefab into the scene and create all the needed objects and then apply the changes to the prefab before continuing.", MessageType.Info );
			return;
		}

		handlesCenteredText = new GUIStyle( EditorStyles.label ) { normal = new GUIStyleState() { textColor = Color.white } };

		collapsableSectionStyle = new GUIStyle( EditorStyles.label ) { alignment = TextAnchor.MiddleCenter };
		collapsableSectionStyle.active.textColor = collapsableSectionStyle.normal.textColor;

		bool valueChanged = false;
		
		// CANVAS WARNINGS //
		if( !isInProjectWindow && parentCanvas != null )
		{
			if( !parentCanvas.enabled )
			{
				EditorGUILayout.BeginVertical( "Box" );
				EditorGUILayout.HelpBox( "Parent Canvas is disabled.", MessageType.Error );
				if( GUILayout.Button( "Enable Canvas", EditorStyles.miniButton ) )
					parentCanvas.enabled = true;
				EditorGUILayout.EndVertical();
			}

			if( parentCanvas.renderMode == RenderMode.WorldSpace )
			{
				EditorGUILayout.BeginVertical( "Box" );
				EditorGUILayout.HelpBox( "The Parent Canvas is set to World Space. Please make sure that the canvas used by the Ultimate Joystick is set to Screen Space.", MessageType.Error );
				EditorGUILayout.BeginHorizontal();
				if( GUILayout.Button( "Update Canvas", EditorStyles.miniButtonLeft ) )
				{
					Undo.RecordObject( parentCanvas, "Update Canvas Render Mode" );
					parentCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
				}
				if( GUILayout.Button( "New Canvas", EditorStyles.miniButtonRight ) )
				{
					RequestCanvas( Selection.activeGameObject );
					parentCanvas = GetParentCanvas();
				}
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
			}
		}
		// END CANVAS WARNINGS //

		// ------------------------< JOYSTICK SETTINGS >----------------------- //
		EditorGUILayout.LabelField( "Joystick Settings", EditorStyles.boldLabel );
		EditorGUI.BeginChangeCheck();
		joystickBaseSprite = ( Sprite )EditorGUILayout.ObjectField( "Joystick Base Sprite", joystickBaseSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
		if( EditorGUI.EndChangeCheck() )
		{
			if( targ.JoystickBase != null && targ.JoystickBase.GetComponent<Image>() )
			{
				Undo.RecordObject( targ.JoystickBase.GetComponent<Image>(), "Update Joystick Base Sprite" );
				targ.JoystickBase.GetComponent<Image>().enabled = false;
				targ.JoystickBase.GetComponent<Image>().sprite = joystickBaseSprite;
				targ.JoystickBase.GetComponent<Image>().enabled = true;
			}
		}

		EditorGUI.BeginChangeCheck();
		joystickSprite = ( Sprite )EditorGUILayout.ObjectField( "Joystick Sprite", joystickSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
		if( EditorGUI.EndChangeCheck() )
		{
			if( targ.Joystick != null && targ.Joystick.GetComponent<Image>() )
			{
				Undo.RecordObject( targ.Joystick.GetComponent<Image>(), "Update Joystick Sprite" );
				targ.Joystick.GetComponent<Image>().enabled = false;
				targ.Joystick.GetComponent<Image>().sprite = joystickSprite;
				targ.Joystick.GetComponent<Image>().enabled = true;
			}
		}

		// BASE COLOR //
		EditorGUI.BeginChangeCheck();
		baseColor = EditorGUILayout.ColorField( "Color", baseColor );
		if( EditorGUI.EndChangeCheck() )
		{
			if( targ.Joystick != null )
			{
				if( !showHighlight.boolValue || highlightJoystickImage == null || highlightJoystickImage != targ.Joystick.GetComponent<Image>() )
				{
					Undo.RecordObject( targ.Joystick.GetComponent<Image>(), "Change Base Color" );
					targ.Joystick.GetComponent<Image>().enabled = false;
					targ.Joystick.GetComponent<Image>().color = baseColor;
					targ.Joystick.GetComponent<Image>().enabled = true;
				}
			}

			if( targ.JoystickBase != null )
			{
				if( !showHighlight.boolValue || highlightBaseImage == null || highlightBaseImage != targ.JoystickBase.GetComponent<Image>() )
				{
					Undo.RecordObject( targ.JoystickBase.GetComponent<Image>(), "Change Base Color" );
					targ.JoystickBase.GetComponent<Image>().enabled = false;
					targ.JoystickBase.GetComponent<Image>().color = baseColor;
					targ.JoystickBase.GetComponent<Image>().enabled = true;
				}
			}
		}

		EditorGUILayout.Space();
		
		// JOYSTICK POSITIONING //
		if( DisplayCollapsibleBoxSection( "Joystick Positioning", "UJ_JoystickPositioning" ) )
		{
			// CHANGE CHECK FOR APPLYING SETTINGS DURING RUNTIME //
			if( Application.isPlaying )
			{
				EditorGUILayout.HelpBox( "The application is running. Changes made here will revert when exiting play mode.", MessageType.Warning );
				EditorGUI.BeginChangeCheck();
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( anchor );
			if( EditorGUI.EndChangeCheck() )
			{
				if( anchor.enumValueIndex >= 2 )
					customActivationRange.boolValue = false;

				if( overridePositioning.boolValue )
					overridePositioning.boolValue = false;

				serializedObject.ApplyModifiedProperties();
			}

			if( anchor.enumValueIndex >= 2 )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( relativeTransform );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
			}

			if( anchor.enumValueIndex == 3 )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( centerAngle );
				CheckPropertyHover( ref DisplayCenterAngle );
				EditorGUILayout.PropertyField( orbitDistance );
				if( EditorGUI.EndChangeCheck() )
				{
					PropertyUpdated( ref DisplayCenterAngle );
					serializedObject.ApplyModifiedProperties();
				}
			}
			else
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( positionHorizontal );
				EditorGUILayout.PropertyField( positionVertical );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( joystickSize );
			if( EditorGUI.EndChangeCheck() )
				serializedObject.ApplyModifiedProperties();
			
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( joystickRadius, new GUIContent( "Radius" ) );
			CheckPropertyHover( ref DisplayRadius );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();
				PropertyUpdated( ref DisplayRadius );
			}

			EditorGUI.BeginDisabledGroup( customActivationRange.boolValue );
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( activationRange );
			CheckPropertyHover( ref DisplayActivationRange );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();
				PropertyUpdated( ref DisplayActivationRange );
			}
			EditorGUI.EndDisabledGroup();

			EditorGUI.BeginDisabledGroup( anchor.enumValueIndex >= 2 );
			EditorGUI.BeginChangeCheck();
			customActivationRange.boolValue = EditorGUILayout.ToggleLeft( "Custom Activation Range", customActivationRange.boolValue );
			if( EditorGUI.EndChangeCheck() )
				serializedObject.ApplyModifiedProperties();
			EditorGUI.EndDisabledGroup();

			if( customActivationRange.boolValue )
			{
				EditorGUI.indentLevel++;
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( activationWidth, new GUIContent( "Activation Width" ) );
				CheckPropertyHover( ref DisplayActivationCustomWidth );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();
					PropertyUpdated( ref DisplayActivationCustomWidth );
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( activationHeight, new GUIContent( "Activation Height" ) );
				CheckPropertyHover( ref DisplayActivationCustomHeight );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();
					PropertyUpdated( ref DisplayActivationCustomHeight );
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( activationPositionHorizontal, new GUIContent( "Horizontal Position" ) );
				EditorGUILayout.PropertyField( activationPositionVertical, new GUIContent( "Vertical Position" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
				EditorGUI.indentLevel--;
			}

			// CHANGE CHECK FOR APPLYING SETTINGS DURING RUNTIME //
			if( Application.isPlaying )
			{
				if( EditorGUI.EndChangeCheck() )
					targ.UpdatePositioning();
			}
		}
		EndCollapsibleBoxSection( "UJ_JoystickPositioning" );

		// OVERRIDE POSITIONING //
		if( anchor.enumValueIndex < 2 )
		{
			if( DisplayCollapsibleBoxSection( "Player Position Override", "UJ_OverridePositioning", overridePositioning, ref valueChanged ) )
			{
				EditorGUILayout.LabelField( $"Horizontal Position Constraint", EditorStyles.boldLabel );
				EditorGUILayout.BeginHorizontal();

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "repositionConstraintMin.x" ), GUIContent.none, GUILayout.Width( Screen.width / 10 ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				//GUILayout.Space( -10 );

				EditorGUI.BeginChangeCheck();
				float minHorizontalRange = serializedObject.FindProperty( "repositionConstraintMin.x" ).floatValue;
				float maxHorizontalRange = serializedObject.FindProperty( "repositionConstraintMax.x" ).floatValue;
				EditorGUILayout.MinMaxSlider( ref minHorizontalRange, ref maxHorizontalRange, 0.0f, 100.0f );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.FindProperty( "repositionConstraintMin.x" ).floatValue = Mathf.Round( minHorizontalRange * 10 ) / 10;
					serializedObject.FindProperty( "repositionConstraintMax.x" ).floatValue = Mathf.Round( maxHorizontalRange * 10 ) / 10;
					serializedObject.ApplyModifiedProperties();
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "repositionConstraintMax.x" ), GUIContent.none, GUILayout.Width( Screen.width / 10 ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();

				EditorGUILayout.LabelField( $"Vertical Position Constraint", EditorStyles.boldLabel );
				EditorGUILayout.BeginHorizontal();

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "repositionConstraintMin.y" ), GUIContent.none, GUILayout.Width( Screen.width / 10 ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				EditorGUI.BeginChangeCheck();
				float minVerticalRange = serializedObject.FindProperty( "repositionConstraintMin.y" ).floatValue;
				float maxVerticalRange = serializedObject.FindProperty( "repositionConstraintMax.y" ).floatValue;
				EditorGUILayout.MinMaxSlider( ref minVerticalRange, ref maxVerticalRange, 0.0f, 100.0f );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.FindProperty( "repositionConstraintMin.y" ).floatValue = Mathf.Round( minVerticalRange * 10 ) / 10;
					serializedObject.FindProperty( "repositionConstraintMax.y" ).floatValue = Mathf.Round( maxVerticalRange * 10 ) / 10;
					serializedObject.ApplyModifiedProperties();
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "repositionConstraintMax.y" ), GUIContent.none, GUILayout.Width( Screen.width / 10 ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();

				EditorGUI.BeginChangeCheck();
				applyConstraintDuringOverride.boolValue = EditorGUILayout.ToggleLeft( "Apply Constraint During Override", applyConstraintDuringOverride.boolValue );
				saveAsPlayerPrefs.boolValue = EditorGUILayout.ToggleLeft( "Save As PlayerPrefs", saveAsPlayerPrefs.boolValue );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				if( saveAsPlayerPrefs.boolValue )
				{
					string uniqueId = serializedObject.FindProperty( "uniqueId" ).stringValue;
					float horizontalOverride = PlayerPrefs.GetFloat( $"UJHPO_{uniqueId}" );
					float verticalOverride = PlayerPrefs.GetFloat( $"UJVPO_{uniqueId}" );
					float sizeOverride = PlayerPrefs.GetFloat( $"UJSO_{uniqueId}" );
					EditorGUILayout.LabelField( $"Unique ID: {uniqueId}", EditorStyles.miniLabel );
					EditorGUILayout.LabelField( $"Horizontal: { ( horizontalOverride < 0.0f ? "Not Overridden" : horizontalOverride.ToString() ) }", EditorStyles.miniLabel );
					EditorGUILayout.LabelField( $"Vertical: {( verticalOverride < 0.0f ? "Not Overridden" : verticalOverride.ToString() )}", EditorStyles.miniLabel );
					EditorGUILayout.LabelField( $"Size: {( sizeOverride < 0.0f ? "Not Overridden" : sizeOverride.ToString() )}", EditorStyles.miniLabel );

					EditorGUI.BeginDisabledGroup( Application.isPlaying || ( horizontalOverride < 0.0f && verticalOverride < 0.0f && sizeOverride < 0.0f ) );
					if( GUILayout.Button( "Reset Override" ) && EditorUtility.DisplayDialog( "Ultimate Joystick Position Override", "Are you sure you want to reset the stored position override information?", "Yes", "No" ) )
					{
						if( !isInProjectWindow )
							targ.ResetOverridePositioning();
						else
						{
							PlayerPrefs.SetFloat( $"UJHPO_{uniqueId}", -1.0f );
							PlayerPrefs.SetFloat( $"UJVPO_{uniqueId}", -1.0f );
							PlayerPrefs.SetFloat( $"UJSO_{uniqueId}", -1.0f );
						}
					}
					EditorGUI.EndDisabledGroup();
				}
			}
			EndCollapsibleBoxSection( "UJ_OverridePositioning", overridePositioning.boolValue );
		}
		// INPUT SETTINGS //
		if( DisplayCollapsibleBoxSection( "Input Settings", "UJ_InputSettings" ) )
		{
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( inputHandling );
			EditorGUILayout.PropertyField( dynamicPositioning );
			if( EditorGUI.EndChangeCheck() )
				serializedObject.ApplyModifiedProperties();

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( gravity );
			if( EditorGUI.EndChangeCheck() )
			{
				gravity.floatValue = Mathf.Clamp( gravity.floatValue, 0.0f, 60.0f );
				serializedObject.ApplyModifiedProperties();
			}

			// --------------------------< EXTEND RADIUS, AXIS, DEAD ZONE >-------------------------- //
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( extendRadius, new GUIContent( "Extend Radius", "Drags the joystick base to follow the touch if it is farther than the radius." ) );
			if( EditorGUI.EndChangeCheck() )
				serializedObject.ApplyModifiedProperties();

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( axis, new GUIContent( "Axis", "Constrains the joystick to a certain axis." ) );
			CheckPropertyHover( ref DisplayAxis );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();
				PropertyUpdated( ref DisplayAxis );
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.Slider( deadZone, 0.0f, 1.0f, new GUIContent( "Dead Zone", "The size of the input dead zone. All values within this range will map to neutral." ) );
			CheckPropertyHover( ref DisplayDeadZone );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();
				PropertyUpdated( ref DisplayDeadZone );
			}
			// ------------------------< END EXTEND RADIUS, AXIS, DEAD ZONE >------------------------ //

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( tapDecayRate );
			if( EditorGUI.EndChangeCheck() )
			{
				if( tapDecayRate.floatValue < 0.0f )
					tapDecayRate.floatValue = 0.0f;

				serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( transmitEventData );
			if( EditorGUI.EndChangeCheck() )
				serializedObject.ApplyModifiedProperties();
		}
		EndCollapsibleBoxSection( "UJ_InputSettings" );
		// ----------------------< END JOYSTICK SETTINGS >--------------------- //

		// --------------------------< VISUAL OPTIONS >------------------------- //
		EditorGUILayout.Space();
		EditorGUILayout.LabelField( "Visual Options", EditorStyles.boldLabel );

		// DISABLE VISUALS //
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField( disableVisuals );
		if( EditorGUI.EndChangeCheck() )
		{
			serializedObject.ApplyModifiedProperties();

			if( !targ.gameObject.GetComponent<CanvasGroup>() )
				Undo.AddComponent( targ.gameObject, typeof( CanvasGroup ) );

			if( disableVisuals.boolValue )
			{
				Undo.RecordObject( targ.gameObject.GetComponent<CanvasGroup>(), "Disable Joystick Visuals" );
				targ.gameObject.GetComponent<CanvasGroup>().alpha = 0.0f;

				EditorPrefs.SetBool( "UJ_InputTransition", false );
				EditorPrefs.SetBool( "UJ_Highlight", false );
				EditorPrefs.SetBool( "UJ_TensionAccent", false );
			}
			else
			{
				Undo.RecordObject( targ.gameObject.GetComponent<CanvasGroup>(), "Enable Joystick Visuals" );
				targ.gameObject.GetComponent<CanvasGroup>().alpha = 1.0f;
			}
		}

		EditorGUI.BeginDisabledGroup( disableVisuals.boolValue );
		{
			// INPUT TRANSITION //
			if( DisplayCollapsibleBoxSection( "Input Transition", "UJ_InputTransition", inputTransition, ref valueChanged ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( useFade );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					if( !targ.GetComponent<CanvasGroup>() )
						Undo.AddComponent( targ.gameObject, typeof( CanvasGroup ) );

					Undo.RecordObject( targ.gameObject.GetComponent<CanvasGroup>(), "Enable Joystick Fade" );
					targ.gameObject.GetComponent<CanvasGroup>().alpha = useFade.boolValue ? fadeUntouched.floatValue : 1.0f;
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( useScale );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				if( useFade.boolValue || useScale.boolValue )
				{
					EditorGUILayout.Space();

					EditorGUILayout.LabelField( "Untouched State", EditorStyles.boldLabel );

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( transitionUntouchedDuration, new GUIContent( "Transition Duration", "The time is seconds for the transition to the untouched state." ) );
					if( EditorGUI.EndChangeCheck() )
						serializedObject.ApplyModifiedProperties();

					if( useFade.boolValue )
					{
						EditorGUI.BeginChangeCheck();
						EditorGUILayout.Slider( fadeUntouched, 0.0f, 1.0f, new GUIContent( "Untouched Alpha", "The alpha of the joystick when it is not receiving input." ) );
						if( EditorGUI.EndChangeCheck() )
						{
							serializedObject.ApplyModifiedProperties();
							Undo.RecordObject( targ.gameObject.GetComponent<CanvasGroup>(), "Edit Joystick Fade" );
							targ.gameObject.GetComponent<CanvasGroup>().alpha = fadeUntouched.floatValue;
						}
					}

					EditorGUILayout.Space();

					EditorGUILayout.LabelField( "Touched State", EditorStyles.boldLabel );
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( transitionTouchedDuration, new GUIContent( "Transition Duration", "The time is seconds for the transition to the touched state." ) );
					if( useFade.boolValue )
						EditorGUILayout.Slider( fadeTouched, 0.0f, 1.0f, new GUIContent( "Touched Alpha", "The alpha of the joystick when receiving input." ) );
					if( useScale.boolValue )
						EditorGUILayout.Slider( scaleTouched, 0.0f, 2.0f, new GUIContent( "Touched Scale", "The scale of the joystick when receiving input." ) );
					if( EditorGUI.EndChangeCheck() )
						serializedObject.ApplyModifiedProperties();
				}
			}
			EndCollapsibleBoxSection( "UJ_InputTransition", inputTransition.boolValue );
			if( valueChanged )
			{
				if( !targ.gameObject.GetComponent<CanvasGroup>() )
					targ.gameObject.AddComponent<CanvasGroup>();

				if( inputTransition.boolValue && useFade.boolValue )
				{
					Undo.RecordObject( targ.gameObject.GetComponent<CanvasGroup>(), "Enable Input Transition" );
					targ.gameObject.GetComponent<CanvasGroup>().alpha = fadeUntouched.floatValue;
				}
				else
				{
					Undo.RecordObject( targ.gameObject.GetComponent<CanvasGroup>(), "Disable Input Transition" );
					targ.gameObject.GetComponent<CanvasGroup>().alpha = 1.0f;
				}
			}

			// USE HIGHLIGHT //
			if( DisplayCollapsibleBoxSection( "Highlight", "UJ_Highlight", showHighlight, ref valueChanged ) )
			{
				if( isInProjectWindow && ( highlightBaseImage == null || highlightJoystickImage == null ) )
					EditorGUILayout.HelpBox( "Objects cannot be generated while selecting a Prefab within the Project window. Please make sure to drag this prefab into the scene before trying to generate objects.", MessageType.Warning );

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( highlightColor );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					if( highlightBaseImage != null )
					{
						Undo.RecordObject( highlightBaseImage, "Update Highlight Color" );
						highlightBaseImage.enabled = false;
						highlightBaseImage.color = highlightColor.colorValue;
						highlightBaseImage.enabled = true;
					}

					if( highlightJoystickImage != null )
					{
						Undo.RecordObject( highlightJoystickImage, "Update Highlight Color" );
						highlightJoystickImage.enabled = false;
						highlightJoystickImage.color = highlightColor.colorValue;
						highlightJoystickImage.enabled = true;
					}
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( highlightBase, new GUIContent( "Base Highlight" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					if( highlightBase.objectReferenceValue != null )
					{
						highlightBaseImage = ( Image )highlightBase.objectReferenceValue;
						if( highlightBaseImage != null && highlightBaseImage.sprite != null )
							highlightBaseSprite = highlightBaseImage.sprite;
					}
					else
					{
						highlightBaseImage = null;
						highlightBaseSprite = null;
					}

					if( highlightBaseImage != null )
					{
						Undo.RecordObject( highlightBaseImage, "Assign Base Highlight" );
						highlightBaseImage.enabled = false;
						highlightBaseImage.color = highlightColor.colorValue;
						highlightBaseImage.enabled = true;
					}
				}

				EditorGUI.BeginChangeCheck();
				highlightBaseSprite = ( Sprite )EditorGUILayout.ObjectField( "└ Image Sprite", highlightBaseSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
				if( EditorGUI.EndChangeCheck() )
				{
					if( highlightBaseImage != null )
					{
						Undo.RecordObject( highlightBaseImage, "Update Base Highlight Sprite" );
						highlightBaseImage.enabled = false;
						highlightBaseImage.sprite = highlightBaseSprite;
						highlightBaseImage.enabled = true;
					}
				}

				if( highlightBaseImage == null && !isInProjectWindow )
				{
					EditorGUI.BeginDisabledGroup( highlightBaseSprite == null || Application.isPlaying );
					if( GUILayout.Button( "Generate Base Highlight", EditorStyles.miniButton ) )
					{
						GameObject baseHightlightObject = new GameObject();
						baseHightlightObject.AddComponent<RectTransform>();
						baseHightlightObject.AddComponent<CanvasRenderer>();
						baseHightlightObject.AddComponent<Image>();

						baseHightlightObject.GetComponent<Image>().sprite = highlightBaseSprite;
						baseHightlightObject.GetComponent<Image>().color = highlightColor.colorValue;

						baseHightlightObject.transform.SetParent( targ.JoystickBase );
						baseHightlightObject.transform.SetAsFirstSibling();

						baseHightlightObject.name = "Base Highlight";

						RectTransform trans = baseHightlightObject.GetComponent<RectTransform>();

						trans.anchorMin = new Vector2( 0.0f, 0.0f );
						trans.anchorMax = new Vector2( 1.0f, 1.0f );
						trans.offsetMin = Vector2.zero;
						trans.offsetMax = Vector2.zero;
						trans.pivot = new Vector2( 0.5f, 0.5f );
						trans.anchoredPosition = Vector2.zero;
						trans.localScale = Vector3.one;
						trans.localPosition = Vector3.zero;
						trans.localRotation = Quaternion.identity;

						serializedObject.FindProperty( "highlightBase" ).objectReferenceValue = baseHightlightObject.GetComponent<Image>();
						serializedObject.ApplyModifiedProperties();

						highlightBaseImage = baseHightlightObject.GetComponent<Image>();

						Undo.RegisterCreatedObjectUndo( baseHightlightObject, "Create Base Highlight Object" );
					}
					EditorGUI.EndDisabledGroup();
				}

				EditorGUILayout.Space();

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( highlightJoystick, new GUIContent( "Joystick Highlight" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					if( highlightJoystick.objectReferenceValue != null )
					{
						highlightJoystickImage = ( Image )highlightJoystick.objectReferenceValue;
						if( highlightJoystickImage != null && highlightJoystickImage.sprite != null )
							highlightJoystickSprite = highlightJoystickImage.sprite;
					}
					else
					{
						highlightJoystickImage = null;
						highlightJoystickSprite = null;
					}

					if( highlightJoystickImage != null )
					{
						Undo.RecordObject( highlightJoystickImage, "Assign Joystick Highlight" );
						highlightJoystickImage.enabled = false;
						highlightJoystickImage.color = highlightColor.colorValue;
						highlightJoystickImage.enabled = true;
					}
				}

				EditorGUI.BeginChangeCheck();
				highlightJoystickSprite = ( Sprite )EditorGUILayout.ObjectField( "└ Image Sprite", highlightJoystickSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
				if( EditorGUI.EndChangeCheck() )
				{
					if( highlightJoystickImage != null )
					{
						Undo.RecordObject( highlightJoystickImage, "Update Joystick Highlight Sprite" );
						highlightJoystickImage.enabled = false;
						highlightJoystickImage.sprite = highlightJoystickSprite;
						highlightJoystickImage.enabled = true;
					}
				}

				if( highlightJoystickImage == null && !isInProjectWindow )
				{
					EditorGUI.BeginDisabledGroup( highlightJoystickSprite == null || Application.isPlaying );
					if( GUILayout.Button( "Generate Joystick Highlight", EditorStyles.miniButton ) )
					{
						GameObject joystickHighlightObject = new GameObject();
						joystickHighlightObject.AddComponent<RectTransform>();
						joystickHighlightObject.AddComponent<CanvasRenderer>();
						joystickHighlightObject.AddComponent<Image>();

						joystickHighlightObject.GetComponent<Image>().sprite = highlightJoystickSprite;
						joystickHighlightObject.GetComponent<Image>().color = highlightColor.colorValue;

						joystickHighlightObject.transform.SetParent( targ.Joystick );

						joystickHighlightObject.name = "Joystick Highlight";

						RectTransform trans = joystickHighlightObject.GetComponent<RectTransform>();

						trans.anchorMin = new Vector2( 0.0f, 0.0f );
						trans.anchorMax = new Vector2( 1.0f, 1.0f );
						trans.offsetMin = Vector2.zero;
						trans.offsetMax = Vector2.zero;
						trans.pivot = new Vector2( 0.5f, 0.5f );
						trans.anchoredPosition = Vector2.zero;
						trans.localScale = Vector3.one;
						trans.localPosition = Vector3.zero;
						trans.localRotation = Quaternion.identity;

						serializedObject.FindProperty( "highlightJoystick" ).objectReferenceValue = joystickHighlightObject.GetComponent<Image>();
						serializedObject.ApplyModifiedProperties();

						highlightJoystickImage = joystickHighlightObject.GetComponent<Image>();

						Undo.RegisterCreatedObjectUndo( joystickHighlightObject, "Create Base Highlight Object" );
					}
					EditorGUI.EndDisabledGroup();
				}
			}
			EndCollapsibleBoxSection( "UJ_Highlight", showHighlight.boolValue );
			if( valueChanged )
			{
				if( highlightBaseImage != null && highlightBaseImage.gameObject != targ.JoystickBase.gameObject )
				{
					Undo.RecordObject( highlightBaseImage.gameObject, ( showHighlight.boolValue ? "Enable" : "Disable" ) + " Joystick Highlight" );
					highlightBaseImage.gameObject.SetActive( showHighlight.boolValue );
				}

				if( highlightJoystickImage != null && highlightJoystickImage.gameObject != targ.Joystick.gameObject )
				{
					Undo.RecordObject( highlightJoystickImage.gameObject, ( showHighlight.boolValue ? "Enable" : "Disable" ) + " Joystick Highlight" );
					highlightJoystickImage.gameObject.SetActive( showHighlight.boolValue );
				}
			}

			// TENSION //
			if( DisplayCollapsibleBoxSection( "Tension Accent", "UJ_TensionAccent", showTension, ref valueChanged ) )
			{
				if( isInProjectWindow && TensionAccents.arraySize == 0 )
					EditorGUILayout.HelpBox( "Objects cannot be generated while selecting a Prefab within the Project window. Please make sure to drag this prefab into the scene before trying to generate objects.", MessageType.Warning );

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( tensionColorNone, new GUIContent( "Tension None", "The color displayed when the joystick\nis closest to center." ) );
				EditorGUILayout.PropertyField( tensionColorFull, new GUIContent( "Tension Full", "The color displayed when the joystick\nis at the furthest distance." ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					for( int i = 0; i < TensionAccents.arraySize; i++ )
					{
						Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
						if( tensionImage == null )
							continue;

						Undo.RecordObject( tensionImage, "Update Tension Color" );
						tensionImage.color = tensionColorNone.colorValue;
					}
				}

				int currentTensionType = tensionType.intValue;
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( tensionType, new GUIContent( "Tension Type", "This option determines how the tension accent will be displayed, whether by using 4 images to show each direction or by using just one image to highlight the direction that the joystick is being used." ) );
				if( EditorGUI.EndChangeCheck() )
				{
					if( tensionType.intValue != currentTensionType )
					{
						GenerateTensionImages();

						if( tensionType.enumValueIndex == 0 )
							spriteDirectionOptions.Add( "All Unique" );
						else
						{
							spriteDirectionOptions.RemoveAt( spriteDirectionOptions.Count - 1 );
							if( currentSpriteDirection > spriteDirectionOptions.Count - 1 )
								currentSpriteDirection = 0;
						}
					}

					serializedObject.ApplyModifiedProperties();

					if( TensionAccents.arraySize > 0 && TensionAccents.GetArrayElementAtIndex( 0 ).objectReferenceValue != null )
					{
						Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( 0 ).objectReferenceValue;
						tensionScale = tensionImage.transform.localScale.x;
					}
				}

				if( TensionAccents.arraySize == 0 || !TensionObjectAssigned )
				{
					if( !isInProjectWindow )
					{
						tensionAccentSprite = ( Sprite )EditorGUILayout.ObjectField( "Tension Sprite", tensionAccentSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );

						EditorGUI.BeginDisabledGroup( tensionAccentSprite == null || Application.isPlaying );

						if( GUILayout.Button( "Generate Tension Images", EditorStyles.miniButton ) )
							GenerateTensionImages();

						EditorGUI.EndDisabledGroup();
					}
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.Slider( tensionDeadZone, 0.0f, 1.0f, new GUIContent( "Dead Zone", "The distance that the joystick will need to move from center before the tension image will start to display tension." ) );
					CheckPropertyHover( ref DisplayTensionDeadZone );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();
						PropertyUpdated( ref DisplayTensionDeadZone );
					}

					if( tensionType.enumValueIndex == 1 )
					{
						EditorGUI.BeginChangeCheck();
						tensionScale = EditorGUILayout.Slider( new GUIContent( "Tension Scale", "The overall scale of the tension accent image." ), tensionScale, 0.0f, 2.0f );
						if( EditorGUI.EndChangeCheck() )
						{
							Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( 0 ).objectReferenceValue;
							Undo.RecordObject( tensionImage.transform, "Modify Tension Scale" );
							tensionImage.transform.localScale = Vector3.one * tensionScale;
						}
					}

					EditorGUI.BeginDisabledGroup( Application.isPlaying );
					{
						int previousDirection = currentSpriteDirection;
						EditorGUI.BeginChangeCheck();
						currentSpriteDirection = EditorGUILayout.Popup( new GUIContent( "Sprite Direction", "The direction that the original sprite is pointing." ), currentSpriteDirection, spriteDirectionOptions.ToArray() );
						if( EditorGUI.EndChangeCheck() && previousDirection != currentSpriteDirection )
						{
							bool identicalSprites = true;

							for( int i = 0; i < TensionAccents.arraySize; i++ )
							{
								Image tensionImage1 = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
								if( tensionImage1 != null && tensionImage1.sprite != null )
								{
									for( int n = i + 1; n < TensionAccents.arraySize; n++ )
									{
										Image tensionImage2 = ( Image )TensionAccents.GetArrayElementAtIndex( n ).objectReferenceValue;
										if( tensionImage2 != null && tensionImage2.sprite != null )
										{
											if( tensionImage1.sprite != tensionImage2.sprite )
											{
												identicalSprites = false;
												break;
											}
										}
									}
									break;
								}
							}

							if( identicalSprites || EditorUtility.DisplayDialog( "Ultimate Joystick", "You are about to overwrite any settings made with the \"None\" Origin Option selected. Are you sure you want to do this?", "Continue", "Cancel" ) )
							{
								if( !identicalSprites )
								{
									for( int i = 0; i < TensionAccents.arraySize; i++ )
									{
										Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
										if( tensionImage == null )
											continue;

										Undo.RecordObject( tensionImage, "Update Tension Sprite" );
										tensionImage.sprite = tensionAccentSprite;
									}
								}

								rotationOffset.floatValue = spriteDirectionRotationMod[ currentSpriteDirection ];
								serializedObject.ApplyModifiedProperties();

								for( int i = 0; i < TensionAccents.arraySize; i++ )
								{
									Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
									if( tensionImage == null )
										continue;

									Undo.RecordObject( tensionImage.transform, "Update Rotation Offset" );
									tensionImage.transform.localEulerAngles = new Vector3( 0, 0, ( 90 * i ) + rotationOffset.floatValue );
								}

								noSpriteDirection = NoSpriteDirection;

								if( currentSpriteDirection == 4 )
								{
									for( int i = 0; i < TensionAccents.arraySize; i++ )
									{
										Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
										if( tensionImage == null )
											continue;

										Undo.RecordObject( tensionImage.transform, "Update Rotation Offset" );
										tensionImage.transform.localEulerAngles = Vector3.zero;
									}

									noSpriteDirection = NoSpriteDirection;
								}
							}
						}

						if( !noSpriteDirection )
						{
							EditorGUI.BeginChangeCheck();
							tensionAccentSprite = ( Sprite )EditorGUILayout.ObjectField( "Tension Sprite", tensionAccentSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
							if( EditorGUI.EndChangeCheck() )
							{
								for( int i = 0; i < TensionAccents.arraySize; i++ )
								{
									Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
									if( tensionImage == null )
										continue;

									Undo.RecordObject( tensionImage, "Update Tension Sprite" );
									tensionImage.enabled = false;
									tensionImage.sprite = tensionAccentSprite;
									tensionImage.enabled = true;
								}
							}
						}
						else
						{
							for( int i = 0; i < TensionAccents.arraySize; i++ )
							{
								Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
								if( tensionImage == null )
									continue;

								Sprite targetSprite = tensionImage.sprite;
								string tensionDirection = i == 0 ? "Up" : "Left";
								if( i >= 2 )
									tensionDirection = i == 2 ? "Down" : "Right";

								EditorGUI.BeginChangeCheck();
								targetSprite = ( Sprite )EditorGUILayout.ObjectField( "Tension Sprite " + tensionDirection, targetSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
								if( EditorGUI.EndChangeCheck() )
								{
									Undo.RecordObject( tensionImage, "Update Tension Sprite" );
									tensionImage.sprite = targetSprite;
								}
							}
						}
					}
					EditorGUI.EndDisabledGroup();
				}
			}
			EndCollapsibleBoxSection( "UJ_TensionAccent", showTension.boolValue );
			if( valueChanged )
			{
				for( int i = 0; i < TensionAccents.arraySize; i++ )
				{
					Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
					if( tensionImage == null )
						continue;

					Undo.RecordObject( tensionImage.gameObject, ( showTension.boolValue ? "Enable" : "Disable" ) + " Tension Accent" );
					tensionImage.gameObject.SetActive( showTension.boolValue );
				}
			}
		}
		EditorGUI.EndDisabledGroup();
		// ------------------------< END VISUAL OPTIONS >----------------------- //

		// -------------------------< SCRIPT REFERENCE >------------------------ //
		EditorGUILayout.Space();
		EditorGUILayout.LabelField( "Script Reference", EditorStyles.boldLabel );
		#if ENABLE_INPUT_SYSTEM
		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField( serializedObject.FindProperty( "_controlPath" ) );
		if( EditorGUI.EndChangeCheck() )
			serializedObject.ApplyModifiedProperties();
		#endif

		EditorGUI.BeginChangeCheck();
		EditorGUILayout.PropertyField( joystickName );
		if( EditorGUI.EndChangeCheck() )
		{
			serializedObject.ApplyModifiedProperties();

			if( joystickName.stringValue == string.Empty )
			{
				exampleCodeIndex = 0;
				exampleCodeOptions = new List<string>();
				for( int i = 0; i < PublicExampleCode.Length; i++ )
					exampleCodeOptions.Add( PublicExampleCode[ i ].optionName );
			}
			else
			{
				exampleCodeOptions = new List<string>();
				for( int i = 0; i < StaticExampleCode.Length; i++ )
					exampleCodeOptions.Add( StaticExampleCode[ i ].optionName );
			}
		}

		if( DisplayCollapsibleBoxSection( "Example Code Generator", "UJ_ExampleCode" ) )
		{
			GUIStyle wordWrappedTextArea = new GUIStyle( GUI.skin.textArea ) { wordWrap = true };
			if( joystickName.stringValue == string.Empty )
			{
				EditorGUILayout.LabelField( "Needed Variable", EditorStyles.boldLabel );
				EditorGUILayout.TextArea( "// Place this variable in your script.\npublic UltimateJoystick joystick;", wordWrappedTextArea );
				EditorGUILayout.Space();
				exampleCodeIndex = EditorGUILayout.Popup( "Public Function", exampleCodeIndex, exampleCodeOptions.ToArray() );
			}
			else
				exampleCodeIndex = EditorGUILayout.Popup( "Static Function", exampleCodeIndex, exampleCodeOptions.ToArray() );
			
			EditorGUILayout.LabelField( "Function Description", EditorStyles.boldLabel );
			GUIStyle wordWrappedLabel = new GUIStyle( GUI.skin.label ) { wordWrap = true };
			if( joystickName.stringValue == string.Empty )
				EditorGUILayout.LabelField( PublicExampleCode[ exampleCodeIndex ].optionDescription, wordWrappedLabel );
			else
				EditorGUILayout.LabelField( StaticExampleCode[ exampleCodeIndex ].optionDescription, wordWrappedLabel );

			EditorGUILayout.LabelField( "Example Code", EditorStyles.boldLabel );
			if( joystickName.stringValue == string.Empty )
				EditorGUILayout.TextArea( PublicExampleCode[ exampleCodeIndex ].basicCode, wordWrappedTextArea );
			else
				EditorGUILayout.TextArea( string.Format( StaticExampleCode[ exampleCodeIndex ].basicCode, joystickName.stringValue ), wordWrappedTextArea );
		}
		EndCollapsibleBoxSection( "UJ_ExampleCode" );

		if( GUILayout.Button( "Open Documentation" ) )
			UltimateJoystickReadmeEditor.OpenReadmeDocumentation();
		// -----------------------< END SCRIPT REFERENCE >---------------------- //
		
		EditorGUILayout.Space();

		if( !disableDragAndDrop && isDraggingObject )
			Repaint();

		if( isDirty )
			SceneView.RepaintAll();

		isDirty = false;
	}

	bool NoSpriteDirection
	{
		get
		{
			bool noDirection = false;

			for( int i = 0; i < TensionAccents.arraySize; i++ )
			{
				Image tensionImage1 = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
				if( tensionImage1 != null )
				{
					for( int n = i + 1; n < TensionAccents.arraySize; n++ )
					{
						Image tensionImage2 = ( Image )TensionAccents.GetArrayElementAtIndex( n ).objectReferenceValue;
						if( tensionImage2 != null )
						{
							if( tensionImage1.transform.eulerAngles.z == tensionImage2.transform.eulerAngles.z )
							{
								noDirection = true;
								break;
							}
						}
					}
					break;
				}
			}

			return noDirection;
		}
	}

	bool TensionObjectAssigned
	{
		get
		{
			for( int i = 0; i < TensionAccents.arraySize; i++ )
			{
				Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
				if( tensionImage != null )
					return true;
			}

			return false;
		}
	}

	void GenerateTensionImages ()
	{
		if( tensionAccentSprite == null || isInProjectWindow )
			return;

		if( TensionAccents.arraySize > 0 )
		{
			List<GameObject> gameObjectsToDestroy = new List<GameObject>();
			for( int i = 0; i < TensionAccents.arraySize; i++ )
			{
				Image tensionImage = ( Image )TensionAccents.GetArrayElementAtIndex( i ).objectReferenceValue;
				if( tensionImage != null )
					gameObjectsToDestroy.Add( tensionImage.gameObject );
			}

			serializedObject.FindProperty( "TensionAccents" ).ClearArray();
			serializedObject.ApplyModifiedProperties();

			for( int i = 0; i < gameObjectsToDestroy.Count; i++ )
				Undo.DestroyObjectImmediate( gameObjectsToDestroy[ i ] );
		}

		if( tensionType.enumValueIndex == 0 )
		{
			for( int i = 0; i < 4; i++ )
			{
				serializedObject.FindProperty( "TensionAccents" ).InsertArrayElementAtIndex( i );
				serializedObject.ApplyModifiedProperties();

				GameObject newGameObject = new GameObject();
				newGameObject.AddComponent<RectTransform>();
				newGameObject.AddComponent<CanvasRenderer>();
				newGameObject.AddComponent<Image>();

				if( tensionAccentSprite != null )
				{
					newGameObject.GetComponent<Image>().sprite = tensionAccentSprite;
					newGameObject.GetComponent<Image>().color = tensionColorNone.colorValue;
				}
				else
					newGameObject.GetComponent<Image>().color = Color.clear;

				newGameObject.transform.SetParent( targ.JoystickBase );
				newGameObject.transform.SetSiblingIndex( targ.Joystick.transform.GetSiblingIndex() );

				newGameObject.name = "Tension Accent " + ( i == 0 ? "Up" : "Left" );
				if( i >= 2 )
					newGameObject.name = "Tension Accent " + ( i == 2 ? "Down" : "Right" );

				RectTransform trans = newGameObject.GetComponent<RectTransform>();

				trans.anchorMin = new Vector2( 0.0f, 0.0f );
				trans.anchorMax = new Vector2( 1.0f, 1.0f );
				trans.offsetMin = Vector2.zero;
				trans.offsetMax = Vector2.zero;
				trans.pivot = new Vector2( 0.5f, 0.5f );
				trans.anchoredPosition = Vector2.zero;
				trans.localScale = Vector3.one;
				trans.localPosition = Vector3.zero;
				trans.localRotation = Quaternion.identity;
				trans.localEulerAngles = new Vector3( 0, 0, ( 90 * i ) + rotationOffset.floatValue );

				serializedObject.FindProperty( string.Format( "TensionAccents.Array.data[{0}]", i ) ).objectReferenceValue = newGameObject.GetComponent<Image>();
				serializedObject.ApplyModifiedProperties();

				Undo.RegisterCreatedObjectUndo( newGameObject, "Create Tension Accent Object" );
			}
		}
		else
		{
			serializedObject.FindProperty( "TensionAccents" ).InsertArrayElementAtIndex( 0 );
			serializedObject.ApplyModifiedProperties();

			GameObject newGameObject = new GameObject();
			RectTransform trans = newGameObject.AddComponent<RectTransform>();
			newGameObject.AddComponent<CanvasRenderer>();
			newGameObject.AddComponent<Image>();

			if( tensionAccentSprite != null )
			{
				newGameObject.GetComponent<Image>().sprite = tensionAccentSprite;
				newGameObject.GetComponent<Image>().color = tensionColorNone.colorValue;
			}
			else
				newGameObject.GetComponent<Image>().color = Color.clear;

			newGameObject.transform.SetParent( targ.JoystickBase );
			newGameObject.transform.SetSiblingIndex( targ.Joystick.transform.GetSiblingIndex() );

			newGameObject.name = "Tension Accent Free";
			
			trans.anchorMin = new Vector2( 0.0f, 0.0f );
			trans.anchorMax = new Vector2( 1.0f, 1.0f );
			trans.offsetMin = Vector2.zero;
			trans.offsetMax = Vector2.zero;
			trans.pivot = new Vector2( 0.5f, 0.5f );
			trans.anchoredPosition = Vector2.zero;
			trans.localScale = Vector3.one;
			trans.localPosition = Vector3.zero;
			trans.localRotation = Quaternion.identity;
			trans.localEulerAngles = new Vector3( 0, 0, rotationOffset.floatValue );

			serializedObject.FindProperty( string.Format( "TensionAccents.Array.data[{0}]", 0 ) ).objectReferenceValue = newGameObject.GetComponent<Image>();
			serializedObject.ApplyModifiedProperties();

			Undo.RegisterCreatedObjectUndo( newGameObject, "Create Tension Accent Object" );
		}
	}

	// ----------------------< EDITOR GUI HELPER FUNCTIONS >----------------------- //
	bool DisplayCollapsibleBoxSection ( string sectionTitle, string editorPref, bool error = false )
	{
		if( error )
			sectionTitle += " <color=#ff0000ff>*</color>";

		EditorGUILayout.BeginVertical( "Box" );

		if( EditorPrefs.GetBool( editorPref ) )
			collapsableSectionStyle.fontStyle = FontStyle.Bold;

		if( GUILayout.Button( sectionTitle, collapsableSectionStyle ) )
			EditorPrefs.SetBool( editorPref, !EditorPrefs.GetBool( editorPref ) );

		if( EditorPrefs.GetBool( editorPref ) )
			collapsableSectionStyle.fontStyle = FontStyle.Normal;

		return EditorPrefs.GetBool( editorPref );
	}

	bool DisplayCollapsibleBoxSection ( string sectionTitle, string editorPref, SerializedProperty enabledProp, ref bool valueChanged, bool error = false )
	{
		valueChanged = false;

		if( error )
			sectionTitle += " <color=#ff0000ff>*</color>";

		EditorGUILayout.BeginVertical( "Box" );

		if( EditorPrefs.GetBool( editorPref ) && enabledProp.boolValue )
			collapsableSectionStyle.fontStyle = FontStyle.Bold;

		EditorGUILayout.BeginHorizontal();

		EditorGUI.BeginChangeCheck();
		enabledProp.boolValue = EditorGUILayout.Toggle( enabledProp.boolValue, GUILayout.Width( 25 ) );
		if( EditorGUI.EndChangeCheck() )
		{
			serializedObject.ApplyModifiedProperties();

			if( enabledProp.boolValue )
				EditorPrefs.SetBool( editorPref, true );
			else
				EditorPrefs.SetBool( editorPref, false );

			valueChanged = true;
			isDirty = true;
		}

		GUILayout.Space( -25 );

		EditorGUI.BeginDisabledGroup( !enabledProp.boolValue );
		if( GUILayout.Button( sectionTitle, collapsableSectionStyle ) )
			EditorPrefs.SetBool( editorPref, !EditorPrefs.GetBool( editorPref ) );
		EditorGUI.EndDisabledGroup();

		EditorGUILayout.EndHorizontal();

		if( EditorPrefs.GetBool( editorPref ) )
			collapsableSectionStyle.fontStyle = FontStyle.Normal;

		return EditorPrefs.GetBool( editorPref ) && enabledProp.boolValue;
	}

	void EndCollapsibleBoxSection ( string editorPref, bool sectionEnabled = true )
	{
		if( EditorPrefs.GetBool( editorPref ) )
			GUILayout.Space( 1 );
		else if( sectionEnabled && DragAndDropHover )
			EditorPrefs.SetBool( editorPref, true );

		EditorGUILayout.EndVertical();
	}

	static string FormatDebug ( string error, string solution, string objectName )
	{
		return "<b>Ultimate Joystick Editor</b>\n" +
			"<color=red><b>×</b></color> <i><b>Error:</b></i> " + error + ".\n" +
			"<color=green><b>√</b></color> <i><b>Solution:</b></i> " + solution + ".\n" +
			"<color=cyan><b>∙</b></color> <i><b>Object:</b></i> " + objectName + "\n";
	}
	
	void UpdateOverridePositioning ()
	{
		Vector2 joystickLocalPosition = ( Vector2 )parentCanvas.transform.InverseTransformPoint( targ.JoystickBase.position ) + ( parentCanvas.GetComponent<RectTransform>().sizeDelta / 2 );

		joystickLocalPosition.x = ( ( joystickLocalPosition.x - ( targ.JoystickBase.sizeDelta.x / 2 ) ) / ( parentCanvas.GetComponent<RectTransform>().sizeDelta.x - targ.JoystickBase.sizeDelta.x ) ) * 100;
		joystickLocalPosition.y = ( ( joystickLocalPosition.y - ( targ.JoystickBase.sizeDelta.y / 2 ) ) / ( parentCanvas.GetComponent<RectTransform>().sizeDelta.y - targ.JoystickBase.sizeDelta.y ) ) * 100;

		if( anchor.enumValueIndex == 1 )
			joystickLocalPosition.x = -( joystickLocalPosition.x - 100 );
		
		positionHorizontal.floatValue = Mathf.Clamp( joystickLocalPosition.x, 0, 100 );
		positionVertical.floatValue = Mathf.Clamp( joystickLocalPosition.y, 0, 100 );
		serializedObject.ApplyModifiedProperties();
		Undo.RecordObject( targ, "Custom Editor Positioning" );
		targ.UpdatePositioning();
	}

	void OnSceneGUI ()
	{
		if( targ == null || Selection.activeGameObject == null || Application.isPlaying || Selection.objects.Length > 1 || parentCanvas == null )
			return;

		if( targ.JoystickBase == null )
			return;

		Vector3 canvasScale = parentCanvas.transform.localScale;

		RectTransform trans = targ.transform.GetComponent<RectTransform>();
		float actualJoystickSize = targ.JoystickBase.sizeDelta.x * canvasScale.x;
		float halfSize = ( actualJoystickSize / 2 ) - ( actualJoystickSize / 20 );

		Handles.color = colorDefault;

		if( !customActivationRange.boolValue && anchor.enumValueIndex < 2 )
		{
			Event e = Event.current;

			if( e.type == EventType.MouseDown )
			{
				UltimateJoystick.CustomEditorPositioning = true;
				currentJoystickSize = targ.GetComponent<RectTransform>().sizeDelta;
			}
			else if( e.type == EventType.MouseDrag && UltimateJoystick.CustomEditorPositioning )
			{
				if( targ.GetComponent<RectTransform>().sizeDelta != currentJoystickSize )
				{
					UltimateJoystick.CustomEditorPositioning = false;
					targ.UpdatePositioning();
				}
			}
			else if( e.type == EventType.MouseUp && UltimateJoystick.CustomEditorPositioning )
			{
				UltimateJoystick.CustomEditorPositioning = false;
				UpdateOverridePositioning();
			}
		}

		if( EditorPrefs.GetBool( "UJ_JoystickPositioning" ) )
		{
			if( customActivationRange.boolValue )
			{
				if( DisplayActivationCustomWidth )
				{
					Handles.color = colorValueChanged;
					Handles.DrawLine( trans.TransformPoint( trans.rect.center + new Vector2( trans.rect.xMin, trans.rect.yMax ) ), trans.TransformPoint( trans.rect.center + new Vector2( trans.rect.xMin, trans.rect.yMin ) ) );
					Handles.DrawLine( trans.TransformPoint( trans.rect.center + new Vector2( trans.rect.xMax, trans.rect.yMax ) ), trans.TransformPoint( trans.rect.center + new Vector2( trans.rect.xMax, trans.rect.yMin ) ) );
					Handles.ArrowHandleCap( 0, trans.TransformPoint( trans.rect.center - new Vector2( trans.rect.xMin + halfSize, 0 ) ), parentCanvas.transform.rotation * Quaternion.Euler( 0, 90, 0 ), halfSize, EventType.Repaint );
					Handles.ArrowHandleCap( 0, trans.TransformPoint( trans.rect.center + new Vector2( trans.rect.xMin + halfSize, 0 ) ), parentCanvas.transform.rotation * Quaternion.Euler( 180, 90, 0 ), halfSize, EventType.Repaint );
					Handles.DrawLine( trans.TransformPoint( trans.rect.center + new Vector2( trans.rect.xMin, 0 ) ), trans.TransformPoint( trans.rect.center + new Vector2( trans.rect.xMax, 0 ) ) );
				}

				if( DisplayActivationCustomHeight )
				{
					Handles.color = colorValueChanged;
					Handles.DrawLine( trans.TransformPoint( trans.rect.center + new Vector2( trans.rect.xMin, trans.rect.yMax ) ), trans.TransformPoint( trans.rect.center + new Vector2( trans.rect.xMax, trans.rect.yMax ) ) );
					Handles.DrawLine( trans.TransformPoint( trans.rect.center + new Vector2( trans.rect.xMin, trans.rect.yMin ) ), trans.TransformPoint( trans.rect.center + new Vector2( trans.rect.xMax, trans.rect.yMin ) ) );
					Handles.ArrowHandleCap( 0, trans.TransformPoint( trans.rect.center - new Vector2( 0, trans.rect.yMin + halfSize ) ), parentCanvas.transform.rotation * Quaternion.Euler( -90, 90, 0 ), halfSize, EventType.Repaint );
					Handles.ArrowHandleCap( 0, trans.TransformPoint( trans.rect.center + new Vector2( 0, trans.rect.yMin + halfSize ) ), parentCanvas.transform.rotation * Quaternion.Euler( 90, 90, 0 ), halfSize, EventType.Repaint );
					Handles.DrawLine( trans.TransformPoint( trans.rect.center + new Vector2( 0, trans.rect.yMin ) ), trans.TransformPoint( trans.rect.center + new Vector2( 0, trans.rect.yMax ) ) );
				}
			}
			else
			{
				if( DisplayActivationRange )
				{
					Handles.color = colorValueChanged;
					Handles.DrawWireDisc( targ.JoystickBase.position, targ.transform.forward, ( trans.sizeDelta.x / 2 ) * canvasScale.x );
					Handles.Label( trans.position + ( -trans.transform.up * ( ( trans.sizeDelta.x / 2 ) * canvasScale.x ) ), "Activation Range: " + activationRange.floatValue, handlesCenteredText );
				}
			}

			if( DisplayRadius )
			{
				Handles.color = colorValueChanged;
				Handles.DrawWireDisc( targ.JoystickBase.position, targ.JoystickBase.transform.forward, actualJoystickSize * ( joystickRadius.floatValue / 2 ) );
				Handles.Label( targ.JoystickBase.position + ( -trans.transform.up * ( actualJoystickSize * ( joystickRadius.floatValue / 2 ) ) ) - new Vector3( HandleUtility.GetHandleSize( targ.JoystickBase.position ) / 3, 0, 0 ), "Radius: " + joystickRadius.floatValue, handlesCenteredText );
			}

			if( anchor.enumValueIndex >= 2 && relativeTransform.objectReferenceValue != null )
			{
				Handles.color = colorDefault;
				RectTransform relativeTransformRect = ( RectTransform )relativeTransform.objectReferenceValue;
				Rect rect = new Rect();
				rect.center = relativeTransformRect.position - ( Vector3 )( relativeTransformRect.sizeDelta / 2 );
				rect.size = relativeTransformRect.sizeDelta;
				Handles.DrawSolidRectangleWithOutline( rect, new Color( 0, 0, 0, 0.5f ), Color.white );
				handlesCenteredText.alignment = TextAnchor.MiddleCenter;
				Handles.Label( relativeTransformRect.position - new Vector3( HandleUtility.GetHandleSize( relativeTransformRect.position ) / 3, relativeTransformRect.sizeDelta.y / 2, 0 ), "Relative Transform", handlesCenteredText );

				if( anchor.enumValueIndex == 3 )
				{
					if( DisplayCenterAngle )
						Handles.color = colorValueChanged;

					Handles.DrawLine( rect.center, targ.JoystickBase.position );
				}
			}
		}

		if( EditorPrefs.GetBool( "UJ_InputSettings" ) )
		{
			if( DisplayAxis )
			{
				Handles.color = colorValueChanged;

				if( axis.enumValueIndex != 1 )
				{
					Handles.ArrowHandleCap( 0, targ.JoystickBase.position, parentCanvas.transform.rotation * Quaternion.Euler( 90, 90, 0 ), halfSize, EventType.Repaint );
					Handles.ArrowHandleCap( 0, targ.JoystickBase.position, parentCanvas.transform.rotation * Quaternion.Euler( -90, 90, 0 ), halfSize, EventType.Repaint );
				}

				if( axis.enumValueIndex != 2 )
				{
					Handles.ArrowHandleCap( 0, targ.JoystickBase.position, parentCanvas.transform.rotation * Quaternion.Euler( 0, 90, 0 ), halfSize, EventType.Repaint );
					Handles.ArrowHandleCap( 0, targ.JoystickBase.position, parentCanvas.transform.rotation * Quaternion.Euler( 180, 90, 0 ), halfSize, EventType.Repaint );
				}
			}

			if( DisplayDeadZone && deadZone.floatValue > 0.0f )
			{
				Color redColor = Color.red;
				redColor.a = 0.25f;
				Handles.color = redColor;
				Handles.DrawSolidDisc( targ.JoystickBase.position, targ.transform.forward, ( actualJoystickSize / 2 ) * deadZone.floatValue );

				Handles.color = colorValueChanged;
				Handles.DrawWireDisc( targ.JoystickBase.position, targ.transform.forward, ( actualJoystickSize / 2 ) * deadZone.floatValue );
			}
		}
		
		if( EditorPrefs.GetBool( "UJ_TensionAccent" ) )
		{
			if( DisplayTensionDeadZone && tensionDeadZone.floatValue > 0.0f )
			{
				Color redColor = Color.red;
				redColor.a = 0.25f;
				Handles.color = redColor;
				Handles.DrawSolidDisc( targ.JoystickBase.position, targ.transform.forward, ( ( actualJoystickSize / 2 ) * joystickRadius.floatValue ) * tensionDeadZone.floatValue );

				Handles.color = colorValueChanged;
				Handles.DrawWireDisc( targ.JoystickBase.position, targ.transform.forward, ( ( actualJoystickSize / 2 ) * joystickRadius.floatValue ) * tensionDeadZone.floatValue );
			}
		}
	}
	
	// ---------------------------------< CANVAS CREATOR FUNCTIONS >--------------------------------- //
	static void CreateNewCanvas ( GameObject child )
	{
		GameObject canvasObject = new GameObject( "Canvas" );
		canvasObject.layer = LayerMask.NameToLayer( "UI" );
		Canvas canvas = canvasObject.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvasObject.AddComponent<GraphicRaycaster>();
		canvasObject.AddComponent<CanvasScaler>();
		Undo.RegisterCreatedObjectUndo( canvasObject, "Create Joystick" );
		Undo.SetTransformParent( child.transform, canvasObject.transform, "Create Joystick" );
		CreateEventSystem();
	}

	static void CreateEventSystem ()
	{
#if UNITY_2022_2_OR_NEWER
		EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
#else
		EventSystem eventSystem = FindObjectOfType<EventSystem>();
#endif
		if( eventSystem == null )
		{
			GameObject newEventSystemObject = new GameObject( "EventSystem", typeof( EventSystem ) );
#if ENABLE_INPUT_SYSTEM
			newEventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
			newEventSystemObject.AddComponent<StandaloneInputModule>();
#endif
			Undo.RegisterCreatedObjectUndo( newEventSystemObject, "Create Joystick" );
		}
#if ENABLE_INPUT_SYSTEM
		else if( eventSystem.gameObject.GetComponent<StandaloneInputModule>() )
		{
			DestroyImmediate( eventSystem.gameObject.GetComponent<StandaloneInputModule>() );
			eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
		}
#endif
	}

	public static void RequestCanvas ( GameObject child )
	{
#if UNITY_2022_2_OR_NEWER
		UnityEngine.Canvas[] allCanvas = ( UnityEngine.Canvas[] )FindObjectsByType<UnityEngine.Canvas>( FindObjectsSortMode.None );
#else
		UnityEngine.Canvas[] allCanvas = ( UnityEngine.Canvas[] )FindObjectsOfType<UnityEngine.Canvas>();
#endif

		for( int i = 0; i < allCanvas.Length; i++ )
		{
			if( allCanvas[ i ].enabled == true && allCanvas[ i ].renderMode != RenderMode.WorldSpace )
			{
				Undo.SetTransformParent( child.transform, allCanvas[ i ].transform, "Create Joystick" );
				CreateEventSystem();
				return;
			}
		}
		CreateNewCanvas( child );
	}
	// -------------------------------< END CANVAS CREATOR FUNCTIONS >------------------------------- //
}