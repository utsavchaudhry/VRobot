/* UltimateJoystick.cs */
/* Written by Kaz Crowe */
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;
#endif

[ExecuteAlways]
#if ENABLE_INPUT_SYSTEM
public class UltimateJoystick : OnScreenControl, IPointerDownHandler, IDragHandler, IPointerUpHandler
#else
public class UltimateJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
#endif
{
	// INTERNAL CALCULATIONS //
	/// <summary>
	/// The parent Canvas component that this Ultimate Joystick is inside of.
	/// </summary>
	public Canvas ParentCanvas { get; private set; }
	/// <summary>
	/// The RectTransform associated with the parent canvas.
	/// </summary>
	public RectTransform ParentCanvasTransform { get; private set; }
	/// <summary>
	/// The Graphic Raycaster component attached to the parent canvas.
	/// </summary>
	public GraphicRaycaster ParentCanvasRaycaster { get; private set; }
	Vector2 parentCanvasSize = Vector2.zero;
	/// <summary>
	/// The base RectTransform component of this Ultimate Joystick.
	/// </summary>
	public RectTransform BaseTransform { get; private set; }
	Vector2 defaultBasePosition = Vector2.zero;
	Vector2 joystickBaseCenter = Vector2.zero;
	Rect joystickRect;
	/// <summary>
	/// Returns the current value of the horizontal axis.
	/// </summary>
	public float HorizontalAxis { get; private set; }
	/// <summary>
	/// Returns the current value of the vertical axis.
	/// </summary>
	public float VerticalAxis { get; private set; }
	/// <summary>
	/// The current state of input being active on this Ultimate Joystick.
	/// </summary>
	public bool InputActive { get; private set; }
	bool interactable = true;
	/// <summary>
	/// Determines if the joystick is interactable or not.
	/// </summary>
	public bool Interactable
	{
		get => interactable;
		set
		{
			ResetJoystick();
			interactable = value;
		}
	}
	int interactPointerId = -10;

	// --------------- < JOYSTICK SETTINGS > --------------- //
	[SerializeField] [Tooltip( "The base of the joystick." )]
	private RectTransform joystickBase;
	/// <summary>
	/// The RectTransform used as the base of the joystick.
	/// </summary>
	public RectTransform JoystickBase { get => joystickBase; }
	[SerializeField] [Tooltip( "The center joystick that follows the input." )]
	private RectTransform joystick;
	/// <summary>
	/// The RectTransform used as the actual joystick.
	/// </summary>
	public RectTransform Joystick { get => joystick; }
	public Color BaseColor
	{
		get
		{
			if( joystickBase == null )
				return Color.clear;

			return joystickBase.GetComponent<Image>().color;
		}
		set
		{
			if( joystickBase != null )
				joystickBase.GetComponent<Image>().color = value;

			if( joystick != null )
				joystick.GetComponent<Image>().color = value;
		}
	}
	// Joystick Positioning //
	enum Anchor { Left, Right, RelativeToTransform, OrbitTransform }
	[SerializeField] [Tooltip( "Determines which side of the screen the joystick should be anchored to." )]
	private Anchor anchor = Anchor.Left;
	[SerializeField] [Tooltip( "The size of the area in which the joystick can be initiated." )] [Range( 0.0f, 2.0f )]
	private float activationRange = 1.0f;
	[SerializeField] [Tooltip( "Allows the ability to define a specific area on the screen where the player can interact with the joystick." )]
	private bool customActivationRange = false;
	[SerializeField] [Tooltip( "The percentage of the screen width to use in the activation area." )] [Range( 0.0f, 100.0f )]
	private float activationWidth = 50.0f;
	[SerializeField] [Tooltip( "The percentage of the screen height to use in the activation area." )] [Range( 0.0f, 100.0f )]
	private float activationHeight = 75.0f;
	[SerializeField] [Tooltip( "The horizontal position of the activation area." )] [Range( 0.0f, 100.0f )]
	private float activationPositionHorizontal = 0.0f;
	[SerializeField] [Tooltip( "The vertical position of the activation area." )] [Range( 0.0f, 100.0f )]
	private float activationPositionVertical = 0.0f;
	[SerializeField] [Tooltip( "The overall size of the joystick on the screen." )] [Range( 1.0f, 5.0f )]
	private float joystickSize = 2.5f;
	[SerializeField] [Tooltip( "The distance the joystick can move from the center position." )] [Range( 0.01f, 1.5f )]
	private float joystickRadius = 1.0f;
	float radius = 1.0f;
	[SerializeField] [Tooltip( "The horizontal position of the joystick on the screen." )] [Range( 0.0f, 100.0f )]
	private float positionHorizontal = 5.0f;
	[SerializeField] [Tooltip( "The vertical position of the joystick on the screen." )] [Range( 0.0f, 100.0f )]
	private float positionVertical = 20.0f;
	[SerializeField] [Tooltip( "The RectTransform that the joystick will position itself relative to." )]
	private RectTransform relativeTransform;
	Vector2 relativeTransformSize, relativeTransformPosition;
	[SerializeField] [Tooltip( "The center angle for the joystick to orbit around the relative transform." )] [Range( -180.0f, 180.0f )]
	private float centerAngle = 0.0f;
	[SerializeField] [Tooltip( "The distance for the joystick to orbit around the transform." )] [Range( 0.0f, 2.0f )]
	private float orbitDistance = 1.0f;
	// Override Positioning //
	[SerializeField] [Tooltip( "Determines if the joystick should be able to move and resize at runtime, or if the values set in the editor should be the only ones used." )]
	private bool overridePositioning = false;
	/// <summary>
	/// Returns true if the player is currently overriding and adjusting the position of this Ultimate Joystick.
	/// </summary>
	public bool IsOverridingPosition{ get; private set; }
	Vector2 inputRelativePosition = Vector2.zero;
	[SerializeField] [Tooltip( "The minimum position constraint to apply to the override positioning." )]
	private Vector2 repositionConstraintMin = new Vector2( 0, 0 );
	[SerializeField] [Tooltip( "The maximum position constraint to apply to the override positioning." )]
	private Vector2 repositionConstraintMax = new Vector2( 100, 100 );
	[SerializeField] [Tooltip( "Should the constraint be applied during the override? This will stop the position at the min and max values of the constraint so that it cannot exceed it." )]
	private bool applyConstraintDuringOverride = false;
	[Tooltip( "[INTERNAL] Do not adjust." )] [HideInInspector]
	public string uniqueId;
	[SerializeField] [Tooltip( "Should the size and position override values be stored as PlayerPrefs local to each machine?" )]
	private bool saveAsPlayerPrefs = true;
	Vector2 joystickPositionOverride = new Vector2( -1, -1 );
	float joystickSizeOverride = -1;
	/// <summary>
	/// This callback is called any time the position or size is overridden and provides the listener with the new Vector2 position value and float size value.
	/// </summary>
	public event Action<Vector2, float> OnOverridePositioning;
	// Input Settings //
	enum InputHandling { EventSystem, TouchInputExclusive }
	[SerializeField] [Tooltip( "Determines how the input is handled for this joystick, whether by the EventSystem or by directly calculating the touch input on the screen." )]
	private InputHandling inputHandling = InputHandling.EventSystem;
	[SerializeField] [Tooltip( "Determines if the joystick will center itself on position of the initial touch or not." )]
	private bool dynamicPositioning = false;
	[SerializeField] [Tooltip( "The amount of \"gravity\" to apply to the joystick when returning to center. A lower value will cause the joystick to return to center slower." )]
	private float gravity = 60.0f;
	float gravitySpeed;
	[SerializeField] [Tooltip( "Should the joystick base move to follow the input if it is farther than the radius." )]
	private bool extendRadius = false;
	enum Axis { Both, X, Y }
	[SerializeField] [Tooltip( "Determines which axis the joystick should be allowed to move on." )]
	private Axis axis = Axis.Both;
	[SerializeField] [Tooltip( "The size of the input dead zone. All values within this range will map to neutral." )]
	private float deadZone = 0.0f;
	[SerializeField] [Tooltip( "The time in seconds that tap calculations will check for." )] [FormerlySerializedAs( "tapCountDuration" )]
	private float tapDecayRate = 0.5f;
	float currentTapTime = 0.0f;
	int tapCount = 0;
	/// <summary>
	/// This callback notifies any subscribers the tap count any time a touch is initiated on the joystick within the tap decay rate.
	/// </summary>
	public event Action<int> OnTapAchieved;
	/// <summary>
	/// This callback will be called only if the joystick has been released within the tap decay time.
	/// </summary>
	public event Action OnTapReleased; 
	[SerializeField] [Tooltip( "Should the joystick attempt to transmit event data to any graphics that are positioned below the joystick?" )]
	private bool transmitEventData = false;
	List<IDragHandler> dragHandlers = new List<IDragHandler>();
	List<IPointerUpHandler> pointerUpHandlers = new List<IPointerUpHandler>();
	List<GameObject> transmittedObjects = new List<GameObject>();
	bool eventDataCalculated = false;

	// --------------- < VISUAL OPTIONS > --------------- //
	[SerializeField] [Tooltip( "Disables the visuals of the joystick." )]
	private bool disableVisuals = false;
	// Input Transition //
	[SerializeField] [Tooltip( "Transitions between the different input states for more visual feedback." )]
	private bool inputTransition = false;
	[SerializeField] [Tooltip( "Time in seconds to transition to the default state." )]
	private float transitionUntouchedDuration = 0.1f;
	[SerializeField] [Tooltip( "Time in seconds to transition to the interacted state." )]
	private float transitionTouchedDuration = 0.1f;
	float fadeInSpeed, fadeOutSpeed, scaleInSpeed, scaleOutSpeed;
	[SerializeField] [Tooltip( "Should the joystick fade alpha when being interacted with?" )]
	private bool useFade = false;
	CanvasGroup joystickGroup;
	[SerializeField] [Tooltip( "The alpha to apply by default." )]
	private float fadeUntouched = 1.0f;
	[SerializeField] [Tooltip( "The alpha to apply to the joystick when it is interacted with." )]
	private float fadeTouched = 0.5f;
	[SerializeField] [Tooltip( "Should the joystick scale the size of the joystick when interacting?" )]
	private bool useScale = false;
	[SerializeField] [Tooltip( "The scale value to apply to the joystick when interacting with it." )]
	private float scaleTouched = 0.9f;
	// Highlight //
	[SerializeField] [Tooltip( "Allows images to be used for highlights to the joystick visuals." )]
	private bool showHighlight = false;
	[SerializeField] [Tooltip( "The color to apply to the highlight images." )]
	private Color highlightColor = Color.white;
	/// <summary>
	/// The color used on the highlight images.
	/// </summary>
	public Color HighlightColor
	{
		get => highlightColor;
		set
		{
			// If the user doesn't want to show highlight, then return.
			if( !showHighlight )
			{
				Debug.LogWarning( FormatDebug( "You are attempting to update the highlight color of this joystick, but the Highlight option has not been enabled", "Please exit play mode and enable the Highlight option on this Ultimate Joystick", gameObject.name ) );
				return;
			}

			// Assigned the new color.
			highlightColor = value;

			// if the base highlight is assigned then apply the color.
			if( highlightBase != null )
				highlightBase.color = highlightColor;

			// If the joystick highlight image is assigned, apply the highlight color.
			if( highlightJoystick != null )
				highlightJoystick.color = highlightColor;
		}
	}
	[SerializeField] [Tooltip( "The Image component to use for the highlight base." )]
	private Image highlightBase;
	[SerializeField] [Tooltip( "The Image component to use for the joystick highlight." )]
	private Image highlightJoystick;
	// Tension Accent //
	[SerializeField] [Tooltip( "Uses images to visually display direction and distance tension." )]
	private bool showTension = false;
	[SerializeField] [Tooltip( "The default color of the tension images." )]
	private Color tensionColorNone = Color.white;
	/// <summary>
	/// The color displayed when there is no tension.
	/// </summary>
	public Color TensionColorNone
	{
		get => tensionColorNone;
		set
		{
			tensionColorNone = value;

			if( !showTension )
			{
				Debug.LogWarning( FormatDebug( "You are attempting to update the tension color of this joystick, but the Tension Accent option has not been enabled", "Please exit play mode and enable the Tension Accent option on this Ultimate Joystick", gameObject.name ) );
				return;
			}

			TensionAccentDisplay();
		}
	}
	[SerializeField] [Tooltip( "The color of the tension when it's at full distance." )]
	private Color tensionColorFull = Color.white;
	/// <summary>
	/// The color displayed when there is full tension.
	/// </summary>
	public Color TensionColorFull
	{
		get => tensionColorFull;
		set
		{
			tensionColorFull = value;

			if( !showTension )
			{
				Debug.LogWarning( FormatDebug( "You are attempting to update the tension color of this joystick, but the Tension Accent option has not been enabled", "Please exit play mode and enable the Tension Accent option on this Ultimate Joystick", gameObject.name ) );
				return;
			}

			TensionAccentDisplay();
		}
	}
	enum TensionType { Directional, Free }
	[SerializeField] [Tooltip( "The type of tension displayed, either directional or free moving." )]
	private TensionType tensionType = TensionType.Directional;
	[SerializeField] [Tooltip( "The rotation offset of the original sprite used for the tension images." )]
	private float rotationOffset = 0.0f;
	[SerializeField] [Tooltip( "The dead zone value before tension will start to be displayed." )]
	private float tensionDeadZone = 0.0f;
	[SerializeField] [Tooltip( "The list of tension images used." )]
	private List<Image> TensionAccents = new List<Image>();

	// --------------- < SCRIPT REFERENCE > --------------- //
	static Dictionary<string,UltimateJoystick> UltimateJoysticks = new Dictionary<string, UltimateJoystick>();
	[SerializeField] [Tooltip( "The string key value to register this Ultimate Joystick component with for referencing static functions." )]
	private string joystickName;
	#if ENABLE_INPUT_SYSTEM
	[InputControl( layout = "Vector2" )] [SerializeField]
	private string _controlPath;
	protected override string controlPathInternal
	{
		get => _controlPath;
		set => _controlPath = value;
	}
	#endif

	// PUBLIC CALLBACKS //
	/// <summary>
	/// Called on the frame that catches the input down on the joystick image.
	/// </summary>
	public event Action OnPointerDownCallback;
	/// <summary>
	/// Called on the frames when the input on the joystick is being moved.
	/// </summary>
	public event Action OnDragCallback;
	/// <summary>
	/// Called on the frame that the input is released.
	/// </summary>
	public event Action OnPointerUpCallback;
	/// <summary>
	/// Called when the positioning of the joystick is updated.
	/// </summary>
	public event Action OnUpdatePositioning;

	#if UNITY_EDITOR
	public static bool CustomEditorPositioning = false;
	#endif


	/// <summary>
	/// [INTERNAL] Called by Unity when this script instance is initialized on scene load.
	/// </summary>
	void Awake ()
	{
		// If the game is not being run and the joystick name has been assigned...
		if( Application.isPlaying && joystickName != string.Empty )
		{
			// If the static dictionary has this joystick registered, then remove it from the list.
			if( UltimateJoysticks.ContainsKey( joystickName ) )
				UltimateJoysticks.Remove( joystickName );

			// Then register the joystick.
			UltimateJoysticks.Add( joystickName, this );
		}

#if ENABLE_INPUT_SYSTEM
		if( !UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled )
			UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
#endif
	}

	/// <summary>
	/// [INTERNAL] Called by Unity on the first frame when this script is enabled.
	/// </summary>
	void Start ()
	{
//		// If there is no unique ID, or there is another joystick with the same ID (VERY unlikely), then get a new random ID.
//#if UNITY_2022_2_OR_NEWER
//		if( overridePositioning && saveAsPlayerPrefs && ( string.IsNullOrEmpty( uniqueId ) || !( FindObjectsByType<UltimateJoystick>( FindObjectsSortMode.None ).Count( x => x.uniqueId == uniqueId ) == 1 ) ) )
//			uniqueId = Guid.NewGuid().ToString().Substring( 0, 8 );
//#else
//		if( overridePositioning && saveAsPlayerPrefs && ( string.IsNullOrEmpty( uniqueId ) || !( FindObjectsOfType<UltimateJoystick>().Count( x => x.uniqueId == uniqueId ) == 1 ) ) )
//			uniqueId = Guid.NewGuid().ToString().Substring( 0, 8 );
//#endif

#if ENABLE_INPUT_SYSTEM && UNITY_EDITOR
#if UNITY_2022_2_OR_NEWER
		EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
#else
		EventSystem eventSystem = FindObjectOfType<EventSystem>();
#endif

		// If the user has the new Input System and there is still an old input system component on the event system...
		if( eventSystem != null && eventSystem.gameObject.GetComponent<StandaloneInputModule>() )
		{
			if( Application.isPlaying )
			{
				// Destroy the old component and add the new one so there will be no errors.
				DestroyImmediate( eventSystem.gameObject.GetComponent<StandaloneInputModule>() );
				eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
			}
			else
			{
				UnityEditor.Undo.DestroyObjectImmediate( eventSystem.gameObject.GetComponent<StandaloneInputModule>() );
				UnityEditor.Undo.AddComponent( eventSystem.gameObject, typeof( UnityEngine.InputSystem.UI.InputSystemUIInputModule ) );
			}
		}
#endif

		// If the game is not running then return.
		if( !Application.isPlaying )
			return;

#if UNITY_EDITOR
		// If the user has the Enter Play Mode settings enabled, but does not have the scene reload, then call the Awake() function since it was not invoked on playing the application.
		if( UnityEditor.EditorSettings.enterPlayModeOptionsEnabled && UnityEditor.EditorSettings.enterPlayModeOptions.HasFlag( UnityEditor.EnterPlayModeOptions.DisableSceneReload ) )
			Awake();
#endif

		// If the user wants to transition on different input states...
		if( inputTransition )
		{
			// Try to store the canvas group.
			joystickGroup = GetComponent<CanvasGroup>();

			// If the canvas group is still null, then add a canvas group component.
			if( joystickGroup == null )
				joystickGroup = BaseTransform.gameObject.AddComponent<CanvasGroup>();

			// Configure the transition speeds.
			fadeInSpeed = ( 1.0f / transitionTouchedDuration ) * Mathf.Abs( fadeTouched - fadeUntouched );
			fadeOutSpeed = ( 1.0f / transitionUntouchedDuration ) * Mathf.Abs( fadeTouched - fadeUntouched );
			scaleInSpeed = 1.0f / transitionTouchedDuration * Mathf.Abs( 1.0f - scaleTouched );
			scaleOutSpeed = 1.0f / transitionUntouchedDuration * Mathf.Abs( 1.0f - scaleTouched );
		}

		// If the parent canvas is null...
		if( ParentCanvas == null )
		{
			// Then try to get the parent canvas component.
			OnTransformParentChanged();

			// If it is still null, then log a error and return.
			if( ParentCanvas == null )
			{
				Debug.LogError( FormatDebug( "The Ultimate Joystick is not placed within a Canvas GameObject. Disabling this component to avoid errors", "Please ensure that the Ultimate Joystick is placed within a UI Canvas", gameObject.name ) );
				enabled = false;
				return;
			}
		}
		
		// Update the positioning of the joystick.
		UpdatePositioning();
	}

	/// <summary>
	/// [INTERNAL] Called by Unity every frame.
	/// </summary>
	void Update ()
	{
#if UNITY_EDITOR
		// Keep the joystick updated while the game is not being played.
		if( !Application.isPlaying )
		{
			if( !CustomEditorPositioning )
				UpdatePositioning();

			return;
		}
#endif

		// If the stored canvas size is not the same as the current canvas size, then update positioning.
		if( parentCanvasSize != ParentCanvasTransform.sizeDelta )
			UpdatePositioning();

		// If the user wants the joystick positioned relative to another transform, and that transform is assigned...
		if( ( anchor == Anchor.RelativeToTransform || anchor == Anchor.OrbitTransform ) && relativeTransform != null )
		{
			// If the transform has changed at all...
			if( relativeTransformSize != relativeTransform.sizeDelta || relativeTransformPosition != ( Vector2 )relativeTransform.position )
			{
				// Store the new transform information.
				relativeTransformSize = relativeTransform.sizeDelta;
				relativeTransformPosition = relativeTransform.position;

				// Update the positioning.
				UpdatePositioning();
			}
		}

		// If the user wants the input to be handled exclusively by touch input, then process the touch input.
		if( inputHandling == InputHandling.TouchInputExclusive )
		{
#if ENABLE_INPUT_SYSTEM
			// If there are touches on the screen...
			if( UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0 )
			{
				// Loop through each finger on the screen...
				for( int touchId = 0; touchId < UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count; touchId++ )
				{
					// If a finger id has been stored, and this finger id is not the same as the stored finger id, then continue.
					if( interactPointerId >= 0 && interactPointerId != UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[ touchId ].touchId )
						continue;
				
					Vector2 touchPosition = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[ touchId ].screenPosition;

					if( UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[ touchId ].phase == UnityEngine.InputSystem.TouchPhase.Began )
						OnInputDown( touchPosition, UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[ touchId ].touchId );
					else if( UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[ touchId ].phase == UnityEngine.InputSystem.TouchPhase.Ended )
						OnInputUp( touchPosition );
					else
						OnInputDrag( touchPosition );
				}
			}
			// Else reset the joystick.
			else if( interactPointerId >= 0 )
				ResetJoystick();
#if UNITY_EDITOR
			// If there are no touches and this code is being run in the editor, check for mouse input.
			if( UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == 0 )
			{
				// Store the mouse device.
				Mouse mouse = InputSystem.GetDevice<Mouse>();

				// If the mouse button is down this frame, call OnInputDown with -1 for pointer index. This will bypass the reset in the code above.
				if( mouse.leftButton.wasPressedThisFrame )
					OnInputDown( mouse.position.ReadValue(), -1 );
				// Else if the mouse button is released this frame, call OnInputUp().
				else if( mouse.leftButton.wasReleasedThisFrame )
					OnInputUp( mouse.position.ReadValue() );
				// Else if the input is active, call OnInputDrag().
				else if( InputActive )
					OnInputDrag( mouse.position.ReadValue() );
			}
#endif
#else
			// If there are touches on the screen...
			if( Input.touchCount > 0 )
			{
				// Loop through each finger on the screen...
				for( int fingerId = 0; fingerId < Input.touchCount; fingerId++ )
				{
					// If a finger id has been stored, and this finger id is not the same as the stored finger id, then continue.
					if( interactPointerId >= 0 && interactPointerId != Input.GetTouch( fingerId ).fingerId )
						continue;

					// If the touch phase has begun this frame, then call the OnInputDown function to check initial touch.
					if( Input.GetTouch( fingerId ).phase == TouchPhase.Began )
						OnInputDown( Input.GetTouch( fingerId ).position, Input.GetTouch( fingerId ).fingerId );
					// Else if the input has ended, call the OnInputUp function.
					else if( Input.GetTouch( fingerId ).phase == TouchPhase.Ended )
						OnInputUp( Input.GetTouch( fingerId ).position );
					// Else process the drag function.
					else
						OnInputDrag( Input.GetTouch( fingerId ).position );
				}
			}
			// Else reset the joystick.
			else if( interactPointerId >= 0 )
				ResetJoystick();
#if UNITY_EDITOR
			// If there are no touches and this code is being run in the editor, check for mouse input.
			if( Input.touchCount == 0 )
			{
				// If the mouse button is down this frame, call OnInputDown with -1 for pointer index. This will bypass the reset in the code above.
				if( Input.GetMouseButtonDown( 0 ) )
					OnInputDown( Input.mousePosition, -1 );
				// Else if the mouse button is released this frame, call OnInputUp().
				else if( Input.GetMouseButtonUp( 0 ) )
					OnInputUp( Input.mousePosition );
				// Else if the input is active, call OnInputDrag().
				else if( InputActive )
					OnInputDrag( Input.mousePosition );
			}
#endif
#endif
		}

		// If the joystick's position is not centered and the input is not active...
		if( gravity > 0.0f && !InputActive && joystick.localPosition != Vector3.zero )
		{
			// Move the joystick's position towards the center of the base by the calculated gravity speed.
			joystick.localPosition = Vector3.MoveTowards( joystick.localPosition, Vector3.zero, gravitySpeed * Time.deltaTime );

			// If the user a direction display option enabled, then display the direction as the joystick moves.
			if( showTension && !disableVisuals )
				TensionAccentDisplay();

			// Update the position values.
			UpdatePositionValues();
		}

		// If the user wants to transition the input...
		if( !disableVisuals && inputTransition )
		{
			// If the user wants to display a fade transition...
			if( useFade )
			{
				// If the input is currently active and the alpha is not the set touched alpha value, then lerp it over time.
				if( InputActive && joystickGroup.alpha != fadeTouched )
					joystickGroup.alpha = Mathf.MoveTowards( joystickGroup.alpha, fadeTouched, fadeInSpeed * Time.deltaTime );
				// Else if the input is NOT currently active and the alpha isn't the untouched value, lerp to it over time.
				else if( !InputActive && joystickGroup.alpha != fadeUntouched )
					joystickGroup.alpha = Mathf.MoveTowards( joystickGroup.alpha, fadeUntouched, fadeOutSpeed * Time.deltaTime );
			}

			// If the user wants to scale the joystick over time...
			if( useScale )
			{
				// If the input is currently active and the scale is not the set touched value, then move the current scale towards that value.
				if( InputActive && joystickBase.localScale != Vector3.one * scaleTouched )
					joystickBase.localScale = Vector3.one * Mathf.MoveTowards( joystickBase.localScale.x, scaleTouched, scaleInSpeed * Time.deltaTime );
				// Else if the input is NOT active and the scale is not 1, then move the current scale towards 1.
				else if( !InputActive && joystickBase.localScale != Vector3.one )
					joystickBase.localScale = Vector3.one * Mathf.MoveTowards( joystickBase.localScale.x, 1.0f, scaleOutSpeed * Time.deltaTime );
			}
		}

		// If the current tap time is higher than zero then reduce the timer.
		if( currentTapTime > 0.0f )
			currentTapTime -= Time.deltaTime;
		// Else if the tap time is less than zero, then finalize the tap time and reset the tap count.
		else if( currentTapTime < 0.0f )
		{
			currentTapTime = 0.0f;
			tapCount = 0;
		}

		// If the user is wanting to transmit event data, set the bool to false each frame. This ensures only one calculation is performed per frame and prevents an infinite loop if multiple objects/joysticks are transmitting data.
		if( transmitEventData )
			eventDataCalculated = false;
	}

	/// <summary>
	/// [INTERNAL] This function is called by Unity when the parent of this transform changes. Updates the parent canvas if it has changed.
	/// </summary>
	void OnTransformParentChanged ()
	{
		// Store the parent of this object.
		Transform parent = transform.parent;

		// If the parent is null, then just return.
		if( parent == null )
			return;

		// While the parent is assigned...
		while( parent != null )
		{
			// If the parent object has a Canvas component, then assign the ParentCanvas and transform.
			if( parent.transform.GetComponent<Canvas>() )
			{
				ParentCanvas = parent.transform.GetComponent<Canvas>();
				ParentCanvasTransform = ParentCanvas.GetComponent<RectTransform>();
				ParentCanvasRaycaster = ParentCanvas.GetComponent<GraphicRaycaster>();
				return;
			}

			// If the parent does not have a canvas, then store it's parent to loop again.
			parent = parent.transform.parent;
		}
	}

	/// <summary>
	/// [INTERNAL] This function is called by Unity when the application receives focus again.
	/// </summary>
	void OnApplicationFocus ( bool focus )
	{
		if( !Application.isPlaying || !InputActive || !focus )
			return;

		ResetJoystick();
	}

	/// <summary>
	/// [INTERNAL] Called from Unity's EventSystem when the input is pressed down on this image.
	/// </summary>
	public void OnPointerDown ( PointerEventData eventData )
	{
		// If the user wants to calculate touch input exclusively to bypass the EventSystem, then just return.
		if( inputHandling != InputHandling.EventSystem )
			return;

		// Otherwise call the OnInputDown function with the information from the EventSystem.
		OnInputDown( eventData.position, eventData.pointerId );
	}

	/// <summary>
	/// [INTERNAL] Processes the input when it has been pressed down.
	/// </summary>
	void OnInputDown ( Vector2 inputPosition, int pointerId )
	{
		// If the joystick is currently in the repositioning state...
		if( IsOverridingPosition )
		{
			// If the render mode is set to Camera, then simply store the event data position.
			if( ParentCanvas.renderMode == RenderMode.ScreenSpaceCamera )
				inputRelativePosition = inputPosition;
			// Else calculate the input relative to the joystick base position.
			else
				inputRelativePosition = BaseTransform.InverseTransformPoint( inputPosition ) - joystickBase.localPosition;

			return;
		}

		// If the user wants to attempt to transmit data to other UI objects...
		if( transmitEventData )
		{
			// If the event data has already been processed this frame, then return to avoid double calculations.
			if( eventDataCalculated )
				return;

			// Set the bool to true so that the joystick will not process the event again.
			eventDataCalculated = true;

			// Create a new PointerEventData to sent to any potential receivers.
			PointerEventData eventData = new PointerEventData( EventSystem.current );
			eventData.position = inputPosition;

			// Create a temporary list of raycast results to go through.
			List<RaycastResult> raycastResults = new List<RaycastResult>();

			// Raycast the event data and give it the temporary list.
			ParentCanvasRaycaster.Raycast( eventData, raycastResults );

			// Recreate all the stored transmit lists.
			dragHandlers = new List<IDragHandler>();
			pointerUpHandlers = new List<IPointerUpHandler>();
			transmittedObjects = new List<GameObject>();

			// Loop through all the raycast results that were found...
			for( int i = 0; i < raycastResults.Count; i++ )
			{
				// If the gameObject of the result is null, then skip this index.
				if( raycastResults[ i ].gameObject == null )
					continue;

				// If the gameObject is THIS gameObject, then skip this index.
				if( raycastResults[ i ].gameObject == gameObject )
					continue;

				// If any of the parents of the hit object are in this joystick then skip this index.
				if( raycastResults[ i ].gameObject.transform.parent == BaseTransform || raycastResults[ i ].gameObject.transform.parent == JoystickBase.transform || raycastResults[ i ].gameObject.transform.parent == Joystick.transform )
					continue;

				// If the hit object has a pointer down handler, then inform the object of the event data.
				if( raycastResults[ i ].gameObject.GetComponent<IPointerDownHandler>() != null )
					raycastResults[ i ].gameObject.GetComponent<IPointerDownHandler>().OnPointerDown( eventData );

				// If there is a drag handler on the object, then store it to send data in the OnDrag() function.
				if( raycastResults[ i ].gameObject.GetComponent<IDragHandler>() != null )
					dragHandlers.Add( raycastResults[ i ].gameObject.GetComponent<IDragHandler>() );

				// If the object has a pointer up handler, then store it to send data in OnPointerUp().
				if( raycastResults[ i ].gameObject.GetComponent<IPointerUpHandler>() != null )
					pointerUpHandlers.Add( raycastResults[ i ].gameObject.GetComponent<IPointerUpHandler>() );

				// Add this object to the list of objects that have input transmitted. This is need for calling a OnClick() for needed Button objects.
				transmittedObjects.Add( raycastResults[ i ].gameObject );
			}
		}

		// If the joystick is already in use, or the user doesn't want the joystick interactable, then return.
		if( InputActive || !interactable )
			return;

		// If the user does not want a custom activation range...
		if( !customActivationRange )
		{
			// distance = distance between the world position of the joystickBase cast to a local position of the ParentCanvas (* by scale factor) - half of the actual canvas size, and the input position.
			float distance = Vector2.Distance( ( Vector2 )( ParentCanvas.transform.InverseTransformPoint( joystickBase.position ) * ParentCanvas.scaleFactor ) + ( ( parentCanvasSize * ParentCanvas.scaleFactor ) / 2 ), inputPosition );

			// If the distance is out of range, then just return.
			if( distance / ( BaseTransform.sizeDelta.x * ParentCanvas.scaleFactor ) > 0.5f )
				return;
		}

		// Set InputActive to true since the joystick is being interacted with.
		InputActive = true;
		interactPointerId = pointerId;

		// If dynamicPositioning or disableVisuals are enabled...
		if( dynamicPositioning || disableVisuals )
		{
			// Move the joystickBase to the position of the touch.
			joystickBase.localPosition = ( Vector2 )BaseTransform.InverseTransformPoint( ParentCanvas.transform.TransformPoint( inputPosition / ParentCanvas.scaleFactor ) ) - ( parentCanvasSize / 2 );

			// Set the joystick center so that the position can be calculated correctly.
			UpdateJoystickCenter();
		}

		// If the user wants to show the input transitions...
		if( !disableVisuals && inputTransition && transitionTouchedDuration <= 0.0f && transitionUntouchedDuration <= 0.0f )
		{
			// If the user wants to fade the alpha of the joystick, then apply the touched alpha value.
			if( useFade )
				joystickGroup.alpha = fadeTouched;

			// If the user wants to scale the joystick, apply the touched scale.
			if( useScale )
				joystickBase.localScale = Vector3.one * scaleTouched;
		}

		// Increase the tap count and set the tap time for calculations.
		tapCount++;
		currentTapTime = tapDecayRate;

		// If the tap count is greater than this one, then inform any subscribers that there may be a tap event.
		if( tapCount > 1 )
			OnTapAchieved?.Invoke( tapCount );

		// Call ProcessInput with the current input information.
		ProcessInput( inputPosition );

		// Notify any subscribers that the OnPointerDown function has been called.
		OnPointerDownCallback?.Invoke();
	}

	/// <summary>
	/// [INTERNAL] Called from Unity's EventSystem when the input is being dragged.
	/// </summary>
	public void OnDrag ( PointerEventData eventData )
	{
		// If the user wants to calculate touch input exclusively to bypass the EventSystem, then just return.
		if( inputHandling != InputHandling.EventSystem )
			return;

		// Otherwise call the OnInputDrag function with the information from the EventSystem.
		OnInputDrag( eventData.position );
	}

	/// <summary>
	/// [INTERNAL] Processes the input when it has moved.
	/// </summary>
	void OnInputDrag ( Vector2 inputPosition )
	{
		// If the joystick is currently in the repositioning state...
		if( IsOverridingPosition )
		{
			// If the joystick is in a camera render canvas, then apply the position of the difference in input and store the current input for the next calculation.
			if( ParentCanvas.renderMode == RenderMode.ScreenSpaceCamera )
			{
				joystickBase.localPosition += ( Vector3 )( inputPosition - inputRelativePosition ) / ParentCanvas.scaleFactor;
				inputRelativePosition = inputPosition;
			}
			// Else just apply the position of the difference in input relative to the base transform.
			else
				joystickBase.localPosition = ( Vector2 )BaseTransform.InverseTransformPoint( inputPosition ) - inputRelativePosition;

			// Update the position override values.
			UpdatePositioningOverride();

			// If the user wants to apply the constraint during the override, then apply it if needed.
			if( applyConstraintDuringOverride && ( joystickPositionOverride.x <= repositionConstraintMin.x || joystickPositionOverride.x >= repositionConstraintMax.x || joystickPositionOverride.y <= repositionConstraintMin.y || joystickPositionOverride.y >= repositionConstraintMax.y ) )
				UpdatePositioning();

			return;
		}

		// If the user wants to attempt to transmit data to other UI objects...
		if( transmitEventData )
		{
			// If the event data has already been processed this frame, then return to avoid double calculations.
			if( eventDataCalculated )
				return;

			// Set the bool to true so that the joystick will not process the event again.
			eventDataCalculated = true;

			// Create a new PointerEventData to sent to any potential receivers.
			PointerEventData eventData = new PointerEventData( EventSystem.current );
			eventData.position = inputPosition;

			// Loop through all the stored drags handlers...
			for( int i = 0; i < dragHandlers.Count; i++ )
			{
				// If this drag handler is null, then just skip this index.
				if( dragHandlers[ i ] == null )
					continue;

				// Inform the drag target that a drag event has been executed.
				dragHandlers[ i ].OnDrag( eventData );
			}
		}

		// If the joystick has not been initialized properly, then return.
		if( !InputActive )
			return;

		// Then call ProcessInput with the info with the current input information.
		ProcessInput( inputPosition );

		// Notify any subscribers that the OnDrag function has been called.
		OnDragCallback?.Invoke();
	}

	/// <summary>
	/// [INTERNAL] Called from Unity's EventSystem when the input has been released.
	/// </summary>
	public void OnPointerUp ( PointerEventData eventData )
	{
		// If the user wants to calculate touch input exclusively to bypass the EventSystem, then just return.
		if( inputHandling != InputHandling.EventSystem )
			return;

		// Otherwise call the OnInputUp function with the information from the EventSystem.
		OnInputUp( eventData.position );
	}

	/// <summary>
	/// [INTERNAL] Processes the input when it has been released.
	/// </summary>
	void OnInputUp ( Vector2 inputPosition )
	{
		// If the joystick is currently in the repositioning state...
		if( IsOverridingPosition )
		{
			// Update the position values.
			UpdatePositioningOverride();
			UpdatePositioning();
			return;
		}

		// If the user wants to attempt to transmit data to other UI objects...
		if( transmitEventData )
		{
			// If the event data has already been processed this frame, then return to avoid double calculations.
			if( eventDataCalculated )
				return;

			// Set the bool to true so that the joystick will not process the event again.
			eventDataCalculated = true;

			// Create a new PointerEventData to sent to any potential receivers.
			PointerEventData eventData = new PointerEventData( EventSystem.current );
			eventData.position = inputPosition;

			// Loop through all the stored pointer up handlers...
			for( int i = 0; i < pointerUpHandlers.Count; i++ )
			{
				// If the stored pointer up is null, then skip this index.
				if( pointerUpHandlers[ i ] == null )
					continue;

				// Inform the stored object of the event data.
				pointerUpHandlers[ i ].OnPointerUp( eventData );
			}

			// Temporary list for the raycast results.
			List<RaycastResult> raycastResults = new List<RaycastResult>();

			// Raycast all objects with the event data and get a list of hit objects.
			ParentCanvasRaycaster.Raycast( eventData, raycastResults );

			// Loop through all the hit objects.
			for( int i = 0; i < raycastResults.Count; i++ )
			{
				// If the gameObject of the result is null, then skip this index.
				if( raycastResults[ i ].gameObject == null )
					continue;

				// If the gameObject is THIS gameObject, then skip this index.
				if( raycastResults[ i ].gameObject == gameObject )
					continue;

				// If any of the parents of the hit object are in this joystick then skip this index.
				if( raycastResults[ i ].gameObject.transform.parent == BaseTransform || raycastResults[ i ].gameObject.transform.parent == JoystickBase.transform || raycastResults[ i ].gameObject.transform.parent == Joystick.transform )
					continue;

				// If the object is not within the list of objects that was hit when the joystick was initiated, then skip this index.
				if( !transmittedObjects.Contains( raycastResults[ i ].gameObject ) )
					continue;
				
				// Otherwise check for a click handler and inform it of the event.
				if( raycastResults[ i ].gameObject.GetComponent<IPointerClickHandler>() != null )
					raycastResults[ i ].gameObject.GetComponent<IPointerClickHandler>().OnPointerClick( eventData );
			}

			// Clear the stored transmit lists.
			dragHandlers.Clear();
			pointerUpHandlers.Clear();
			transmittedObjects.Clear();
		}

		// If the joystick has not been initialized properly, then return.
		if( !InputActive )
			return;

		// Since the touch has lifted, set InputActive to false.
		InputActive = false;
		interactPointerId = -10;

		// If dynamicPositioning, disableVisuals, or extendRadius are enabled...
		if( dynamicPositioning || disableVisuals || extendRadius )
		{
			// The joystickBase needs to be reset back to the default position.
			joystickBase.localPosition = defaultBasePosition;

			// Reset the joystick center since the touch has been released.
			UpdateJoystickCenter();
		}

		// If the user doesn't want any gravity applied to the joystick...
		if( gravity <= 0.0f )
		{
			// Reset the joystick's position back to center.
			joystick.localPosition = Vector3.zero;

			// If the user is wanting to show tension, then reset that here.
			if( showTension )
				TensionAccentReset();
		}

		// If the user wants an input transition, but the durations of both touched and untouched states are zero...
		if( !disableVisuals && inputTransition && ( transitionTouchedDuration <= 0 && transitionUntouchedDuration <= 0 ) )// EDIT: I think this might be incorrect. Why check both durations?
		{
			// Then just apply the alpha.
			if( useFade )
				joystickGroup.alpha = fadeUntouched;

			// And reset the scale back to one.
			if( useScale )
				joystickBase.localScale = Vector3.one;
		}
		
		// Update the position values.
		UpdatePositionValues();

		// If the current tap time is still active by the time this input was released, then notify any subscribers that the tap was released in time.
		if( currentTapTime > 0.0f )
			OnTapReleased?.Invoke();

		// Notify any subscribers that the OnPointerUp function has been called.
		OnPointerUpCallback?.Invoke();
	}
	
	/// <summary>
	/// [INTERNAL] Processes the input and moves the joystick accordingly.
	/// </summary>
	/// <param name="inputPosition">The current position of the input.</param>
	void ProcessInput ( Vector2 inputPosition )
	{
		// Create a new Vector2 to equal the vector from the current touch to the center of joystick.
		Vector2 tempVector = inputPosition - joystickBaseCenter;
		
		// If the user wants only one axis, then zero out the opposite value.
		if( axis == Axis.X )
			tempVector.y = 0;
		else if( axis == Axis.Y )
			tempVector.x = 0;
		
		// Clamp the input according to the calculated radius.
		tempVector = Vector2.ClampMagnitude( tempVector, radius * joystickBase.localScale.x );

		// Apply the tempVector to the joystick's position.
		joystick.localPosition = ( tempVector / joystickBase.localScale.x ) / ParentCanvas.scaleFactor;
		
		// If the user wants to drag the joystick along with the touch...
		if( extendRadius )
		{
			// Store the position of the current touch.
			Vector3 currentTouchPosition = inputPosition;

			// If the user is using any axis option, then align the current touch position.
			if( axis != Axis.Both )
			{
				if( axis == Axis.X )
					currentTouchPosition.y = joystickBaseCenter.y;
				else
					currentTouchPosition.x = joystickBaseCenter.x;
			}

			// Then find the distance that the touch is from the center of the joystick.
			float touchDistance = Vector3.Distance( joystickBaseCenter, currentTouchPosition );

			// If the touchDistance is greater than the set radius...
			if( touchDistance >= radius )
			{
				// Figure out the current position of the joystick.
				Vector2 joystickPosition = joystick.localPosition / radius;
				
				// Move the joystickBase in the direction that the joystick is, multiplied by the difference in distance of the max radius.
				joystickBase.localPosition += new Vector3( joystickPosition.x, joystickPosition.y, 0 ) * ( touchDistance - radius );

				// Reconfigure the joystick center since the joystick has now moved it's position.
				UpdateJoystickCenter();
			}
		}

		// Update the position values since the joystick has been updated.
		UpdatePositionValues();

		// If the user has showTension enabled, then display the Tension.
		if( showTension && !disableVisuals )
			TensionAccentDisplay();
	}

	/// <summary>
	/// [INTERNAL] Updates the center position of the joystick base for calculations.
	/// </summary>
	void UpdateJoystickCenter ()
	{
		joystickBaseCenter = ( ( Vector2 )ParentCanvas.transform.InverseTransformPoint( joystickBase.position ) * ParentCanvas.scaleFactor ) + ( ( parentCanvasSize * ParentCanvas.scaleFactor ) / 2 );
	}

	/// <summary>
	/// [INTERNAL] This function is called only when showTension is true, and only when the joystick is moving.
	/// </summary>
	void TensionAccentDisplay ()
	{
		// If the tension accent images are null, then inform the user and return.
		if( TensionAccents.Count == 0 )
		{
			Debug.LogError( FormatDebug( "There are no tension accent images assigned", "Please select this Ultimate Joystick in the Inspector to fix the issue", gameObject.name ) );
			return;
		}

		// If the user wants to display directional tension...
		if( tensionType == TensionType.Directional )
		{
			// Calculate the joystick axis values.
			Vector2 joystickAxis = ( joystick.localPosition * ParentCanvas.scaleFactor ) / radius;

			// If the joystick is to the right...
			if( joystickAxis.x > 0 )
			{
				// Then lerp the color according to tension's X position.
				if( TensionAccents[ 3 ] != null )
					TensionAccents[ 3 ].color = Color.Lerp( tensionColorNone, tensionColorFull, joystickAxis.x <= tensionDeadZone ? 0 : ( joystickAxis.x - tensionDeadZone ) / ( 1.0f - tensionDeadZone ) );
				
				// If the opposite tension is not tensionColorNone, the make it so.
				if( TensionAccents[ 1 ] != null && TensionAccents[ 1 ].color != tensionColorNone )
					TensionAccents[ 1 ].color = tensionColorNone;
			}
			// Else the joystick is to the left...
			else
			{
				// Repeat above steps...
				if( TensionAccents[ 1 ] != null )
					TensionAccents[ 1 ].color = Color.Lerp( tensionColorNone, tensionColorFull, Mathf.Abs( joystickAxis.x ) <= tensionDeadZone ? 0 : ( Mathf.Abs( joystickAxis.x ) - tensionDeadZone ) / ( 1.0f - tensionDeadZone ) );
				if( TensionAccents[ 3 ] != null && TensionAccents[ 3 ].color != tensionColorNone )
					TensionAccents[ 3 ].color = tensionColorNone;
			}

			// If the joystick is up...
			if( joystickAxis.y > 0 )
			{
				// Then lerp the color according to tension's Y position.
				if( TensionAccents[ 0 ] != null )
					TensionAccents[ 0 ].color = Color.Lerp( tensionColorNone, tensionColorFull, joystickAxis.y <= tensionDeadZone ? 0 : ( joystickAxis.y - tensionDeadZone ) / ( 1.0f - tensionDeadZone ) );

				// If the opposite tension is not tensionColorNone, the make it so.
				if( TensionAccents[ 2 ] != null && TensionAccents[ 2 ].color != tensionColorNone )
					TensionAccents[ 2 ].color = tensionColorNone;
			}
			// Else the joystick is down...
			else
			{
				// Repeat above steps...
				if( TensionAccents[ 2 ] != null )
					TensionAccents[ 2 ].color = Color.Lerp( tensionColorNone, tensionColorFull, Mathf.Abs( joystickAxis.y ) <= tensionDeadZone ? 0 : ( Mathf.Abs( joystickAxis.y ) - tensionDeadZone ) / ( 1.0f - tensionDeadZone ) );
				if( TensionAccents[ 0 ] != null && TensionAccents[ 0 ].color != tensionColorNone )
					TensionAccents[ 0 ].color = tensionColorNone;
			}
		}
		// Else the user wants to display free tension...
		else
		{
			// If the first index tension is null, then inform the user and return to avoid errors.
			if( TensionAccents[ 0 ] == null )
			{
				Debug.LogError( FormatDebug( "There are no tension accent images assigned", "Please select this Ultimate Joystick in the Inspector to fix the issue", gameObject.name ) );
				return;
			}

			// Store the distance for calculations.
			float distance = GetDistance();

			// Lerp the color according to the distance of the joystick from center.
			TensionAccents[ 0 ].color = Color.Lerp( tensionColorNone, tensionColorFull, distance <= tensionDeadZone ? 0 : ( distance - tensionDeadZone ) / ( 1.0f - tensionDeadZone ) );
			
			// Calculate the joystick axis values.
			Vector2 joystickAxis = joystick.localPosition / radius;
			
			// Rotate the tension transform to aim at the direction that the joystick is pointing.
			TensionAccents[ 0 ].transform.localRotation = Quaternion.Euler( 0, 0, ( Mathf.Atan2( joystickAxis.y, joystickAxis.x ) * Mathf.Rad2Deg ) + rotationOffset - 90 );
		}
	}

	/// <summary>
	/// [INTERNAL] This function resets the tension image's colors back to default.
	/// </summary>
	void TensionAccentReset ()
	{
		// Loop through each tension accent.
		for( int i = 0; i < TensionAccents.Count; i++ )
		{
			// If the tension accent is unassigned, then skip this index.
			if( TensionAccents[ i ] == null )
				continue;

			// Reset the color of this tension image back to no tension.
			TensionAccents[ i ].color = tensionColorNone;
		}

		// If the joystick is using a free tension, then reset the tension rotation back to center.
		if( tensionType == TensionType.Free && TensionAccents.Count > 0 && TensionAccents[ 0 ] != null )
			TensionAccents[ 0 ].transform.localRotation = Quaternion.Euler( 0, 0, rotationOffset );
	}

	/// <summary>
	/// [INTERNAL] This function updates the position values of the joystick so that they can be referenced.
	/// </summary>
	void UpdatePositionValues ()
	{
		// Store the relative position of the joystick and divide the Vector by the radius of the joystick. This will normalize the values.
		Vector2 joystickPosition = ( joystick.localPosition * ParentCanvas.scaleFactor ) / radius;
		
		// If the user has a dead zone value assigned...
		if( deadZone > 0.0f )
		{
			// If the absolute value of the horizontal position is less than the dead zone value, then just set the horizontal position to zero.
			if( Mathf.Abs( joystickPosition.x ) <= deadZone )
				joystickPosition.x = 0.0f;
			// Else the value is over the dead zone value...
			else
			{
				// Subtract the dead zone from the horizontal position.
				joystickPosition.x -= joystickPosition.x > 0.0f ? deadZone : -deadZone;

				// Divide the horizontal value by the max distance that the joystick can move so that the value will be between 0.0 and 1.0. 
				joystickPosition.x = joystickPosition.x / ( 1.0f - deadZone );
			}

			// Same thing as above just for the vertical position.
			if( Mathf.Abs( joystickPosition.y ) <= deadZone )
				joystickPosition.y = 0.0f;
			else
			{
				// Subtract the dead zone from the vertical value.
				joystickPosition.y -= joystickPosition.y > 0.0f ? deadZone : -deadZone;

				// Divide the value by the max distance the joystick can move.
				joystickPosition.y = Mathf.Clamp( joystickPosition.y, -1.0f, 1.0f ) / ( 1.0f - deadZone );
			}
		}

		// Finally, set the horizontal and vertical axis values for reference.
		HorizontalAxis = joystickPosition.x;
		VerticalAxis = joystickPosition.y;

		#if ENABLE_INPUT_SYSTEM
		SendValueToControl( new Vector2( HorizontalAxis, VerticalAxis ) );
		#endif
	}

	/// <summary>
	/// [INTERNAL] Resets the joystick position and input information and stops any coroutines that might have been running.
	/// </summary>
	void ResetJoystick ()
	{
		// Reset InputActive.
		InputActive = false;
		interactPointerId = -10;

		// If any options that change the joystick's base position are enabled...
		if( dynamicPositioning || disableVisuals || extendRadius )
		{
			// The joystick base needs to be reset back to the default position.
			joystickBase.localPosition = defaultBasePosition;

			// Reset the joystick center since the touch has been released.
			UpdateJoystickCenter();
		}

		// Reset the joystick's position back to center.
		joystick.localPosition = Vector3.zero;

		// Update the position values.
		UpdatePositionValues();

		// If the user has showTension enabled, then reset the tension.
		if( showTension )
			TensionAccentReset();

		// If the user was wanting to transmit event data...
		if( transmitEventData )
		{
			// Loop through all the stored pointer up handlers...
			for( int i = 0; i < pointerUpHandlers.Count; i++ )
			{
				// If the information is null then skip.
				if( pointerUpHandlers[ i ] == null )
					continue;

				// Inform the object that the event needs to be released.
				pointerUpHandlers[ i ].OnPointerUp( new PointerEventData( EventSystem.current ) );
			}

			// Reset the stored transmit lists.
			dragHandlers = new List<IDragHandler>();
			pointerUpHandlers = new List<IPointerUpHandler>();
			transmittedObjects = new List<GameObject>();
		}
	}

	/// <summary>
	/// [INTERNAL] Formats and sends detailed information to the user.
	/// </summary>
	static string FormatDebug ( string error, string solution, string objectName )
	{
		return "<b>Ultimate Joystick</b>\n" +
			"<color=red><b>×</b></color> <i><b>Error:</b></i> " + error + ".\n" +
			"<color=green><b>√</b></color> <i><b>Solution:</b></i> " + solution + ".\n" +
			"<color=cyan><b>∙</b></color> <i><b>Object:</b></i> " + objectName + "\n";
	}

	/* --------------------------------------------- *** PUBLIC FUNCTIONS *** --------------------------------------------- */
	/// <summary>
	/// Updates the joystick positioning according to the provided screen position percentages.
	/// </summary>
	/// <param name="horizontalPosition">The target percentage horizontal position on the screen. (ex. 50 = 50% = center of the screen)</param>
	/// <param name="verticalPosition">The target percentage horizontal position on the screen. (ex. 50 = 50% = center of the screen)</param>
	/// <param name="size">[OPTIONAL] The target size of the joystick graphic. (Range 0.0f - 5.0f)</param>
	public void UpdatePositioning ( float horizontalPosition, float verticalPosition, float size = -1.0f )
	{
		// If the size value is assigned, then copy the value.
		if( size > 0.0f )
			joystickSize = Mathf.Clamp( size, 0.0f, 5.0f );

		// Clamp the provided values between 0 and 100.
		positionHorizontal = Mathf.Clamp( horizontalPosition, 0.0f, 100.0f );
		positionVertical = Mathf.Clamp( verticalPosition, 0.0f, 100.0f );

		// Update the positioning with the new values.
		UpdatePositioning();
	}

	/// <summary>
	/// Resets the joystick and updates the size and position of the joystick on the screen. 
	/// </summary>
	public void UpdatePositioning ()
	{
		// If the parent canvas is null, then try to get the parent canvas component.
		if( ParentCanvas == null || ParentCanvasTransform == null || ParentCanvasRaycaster == null )
			OnTransformParentChanged();

		// If it is still null, then log a error and return.
		if( ParentCanvas == null )
		{
#if UNITY_EDITOR
			if( UnityEditor.Selection.activeGameObject != null && UnityEditor.Selection.activeGameObject.scene != null && UnityEditor.Selection.activeGameObject == gameObject )
				Debug.LogError( FormatDebug( "The Ultimate Joystick is not placed within a Canvas GameObject", "Please ensure that the Ultimate Joystick is placed within a UI Canvas", gameObject.name ) );
#endif
			return;
		}

		// If any of the needed components are left unassigned, then inform the user and return.
		if( joystickBase == null )
		{
			if( Application.isPlaying )
				Debug.LogError( FormatDebug( "The Joystick Base property is unassigned", "Please assign the Joystick Base property before trying to use this Ultimate Joystick", gameObject.name ) );

			return;
		}

		// If the game is running, then reset the joystick.
		if( Application.isPlaying )
			ResetJoystick();

		// Store the rect trans size. If the canvas size is ever different, this function will run since the position will need to be updated.
		parentCanvasSize = ParentCanvasTransform.sizeDelta;

		// Set the current reference size for scaling.
		float referenceSize = Mathf.Min( parentCanvasSize.y, parentCanvasSize.x );

		// Configure the target size for the joystick graphic.
		float textureSize = referenceSize * ( joystickSize / 10 );

		// If base transform is null, store this object's RectTrans so that it can be positioned.
		if( BaseTransform == null )
			BaseTransform = GetComponent<RectTransform>();

		// Force the anchors and pivot so the joystick will function correctly. This is also needed here for older versions of the Ultimate Joystick that didn't use these rect transform settings.
		BaseTransform.anchorMin = Vector2.zero;
		BaseTransform.anchorMax = Vector2.zero;
		BaseTransform.pivot = new Vector2( 0.5f, 0.5f );
		BaseTransform.localScale = Vector3.one;

		// Set the anchors of the joystick base. It is important to have the anchors centered for calculations.
		joystickBase.anchorMin = new Vector2( 0.5f, 0.5f );
		joystickBase.anchorMax = new Vector2( 0.5f, 0.5f );
		joystickBase.pivot = new Vector2( 0.5f, 0.5f );

		// Configure the position that the user wants the joystick to be located.
		Vector2 joystickPosition = new Vector2( parentCanvasSize.x * ( positionHorizontal / 100 ) - ( textureSize * ( positionHorizontal / 100 ) ) + ( textureSize / 2 ), parentCanvasSize.y * ( positionVertical / 100 ) - ( textureSize * ( positionVertical / 100 ) ) + ( textureSize / 2 ) ) - ( parentCanvasSize / 2 );

		// If the user wants the joystick anchored to the right of the screen, then flip the horizontal position.
		if( anchor == Anchor.Right )
			joystickPosition.x = -joystickPosition.x;
		// Else if the user wants the positioning to be relative to another transform, and the transform is assigned...
		else if( ( int )anchor >= 2 && relativeTransform != null )
		{
			// Store the relative transform information for updating the positioning if it changes.
			relativeTransformSize = relativeTransform.sizeDelta;
			relativeTransformPosition = relativeTransform.position;

			// Calculate the center position of the relative transform.
			joystickPosition = ParentCanvas.transform.InverseTransformPoint( relativeTransform.position ) - ( Vector3 )( relativeTransform.sizeDelta * ( relativeTransform.pivot - new Vector2( 0.5f, 0.5f ) ) );

			// Reconfigure the texture size and reference size based on the relative transform.
			textureSize = Mathf.Max( relativeTransform.sizeDelta.x, relativeTransform.sizeDelta.y ) * ( joystickSize / 5 );

			// If the user wants to orbit a transform...
			if( anchor == Anchor.OrbitTransform )
			{
				// Add to the position of the joystick according to the users center angle and distance options.
				joystickPosition.x += ( Mathf.Cos( ( -centerAngle * Mathf.Deg2Rad ) + ( 90 * Mathf.Deg2Rad ) ) * ( Mathf.Max( relativeTransform.sizeDelta.x, relativeTransform.sizeDelta.y ) * orbitDistance ) );
				joystickPosition.y += ( Mathf.Sin( ( -centerAngle * Mathf.Deg2Rad ) + ( 90 * Mathf.Deg2Rad ) ) * ( Mathf.Max( relativeTransform.sizeDelta.x, relativeTransform.sizeDelta.y ) * orbitDistance ) );
			}
			else
			{
				// Fix the position data to be between -0.5 and 0.5 for easy calculations.
				Vector2 positionData = new Vector2( positionHorizontal - 50, positionVertical - 50 ) / 100;
				
				// Configure the new button position according to the relative transform.
				joystickPosition += ( ( Vector2.one * Mathf.Max( relativeTransform.sizeDelta.x, relativeTransform.sizeDelta.y ) * 2.0f ) * positionData ) - ( ( Vector2.one * textureSize ) * positionData );
			}
		}

		// If the user wants a custom touch size...
		if( customActivationRange && ( int )anchor <= 1 )
		{
			// Apply the size of the custom activation range.
			BaseTransform.sizeDelta = new Vector2( parentCanvasSize.x * ( activationWidth / 100 ), parentCanvasSize.y * ( activationHeight / 100 ) );

			// Configure the base position minus half the canvas position size.
			Vector2 baseTransformPosition = new Vector2( parentCanvasSize.x * ( activationPositionHorizontal / 100 ) - ( BaseTransform.sizeDelta.x * ( activationPositionHorizontal / 100 ) ) + ( BaseTransform.sizeDelta.x / 2 ), parentCanvasSize.y * ( activationPositionVertical / 100 ) - ( BaseTransform.sizeDelta.y * ( activationPositionVertical / 100 ) ) + ( BaseTransform.sizeDelta.y / 2 ) ) - ( parentCanvasSize / 2 );

			// If the user wants the joystick anchored to the right of the screen, then flip the horizontal position of this base.
			if( anchor == Anchor.Right )
				baseTransformPosition.x = -baseTransformPosition.x;

			// Apply the calculated position.
			BaseTransform.localPosition = baseTransformPosition;

			// Apply the size and position to the joystickBase.
			joystickBase.sizeDelta = new Vector2( textureSize, textureSize );
			joystickBase.localPosition = BaseTransform.transform.InverseTransformPoint( ParentCanvas.transform.TransformPoint( joystickPosition ) );
		}
		else
		{
			// Apply the joystick size multiplied by the activation range.
			BaseTransform.sizeDelta = new Vector2( textureSize, textureSize ) * activationRange;

			// Apply the imagePosition.
			BaseTransform.localPosition = joystickPosition;

			// Apply the size and position to the joystickBase.
			joystickBase.sizeDelta = new Vector2( textureSize, textureSize );
			joystickBase.localPosition = Vector3.zero;
		}

		// If the game is running and the user wants to allow override positioning from the player...
		if( Application.isPlaying && overridePositioning )
		{
			// If the user wants the players settings stored in PlayerPrefs...
			if( saveAsPlayerPrefs )
			{
				if( !PlayerPrefs.HasKey( $"UJHPO_{uniqueId}" ) || !PlayerPrefs.HasKey( $"UJVPO_{uniqueId}" ) || !PlayerPrefs.HasKey( $"UJSO_{uniqueId}" ) )
				{
					PlayerPrefs.SetFloat( $"UJHPO_{uniqueId}", -1.0f );
					PlayerPrefs.SetFloat( $"UJVPO_{uniqueId}", -1.0f );
					PlayerPrefs.SetFloat( $"UJSO_{uniqueId}", -1.0f );
				}
				
				// If there is stored information on the 
				if( PlayerPrefs.GetFloat( $"UJHPO_{uniqueId}" ) >= 0.0f || PlayerPrefs.GetFloat( $"UJVPO_{uniqueId}" ) >= 0.0f || PlayerPrefs.GetFloat( $"UJSO_{uniqueId}" ) >= 0.0f )
				{
					joystickPositionOverride.x = PlayerPrefs.GetFloat( $"UJHPO_{uniqueId}" );
					joystickPositionOverride.y = PlayerPrefs.GetFloat( $"UJVPO_{uniqueId}" );
					joystickSizeOverride = PlayerPrefs.GetFloat( $"UJSO_{uniqueId}" );
				}
			}

			// If the stored override value is assigned to something...
			if( joystickPositionOverride != new Vector2( -1, -1 ) || joystickSizeOverride > 0.0f )
			{
				// Temporary positioning vector for figuring out which values to use to position the joystick.
				Vector2 positioningToUse = joystickPositionOverride == new Vector2( -1, -1 ) ? new Vector2( positionHorizontal, positionVertical ) : joystickPositionOverride;

				// Configure the target size for the joystick graphic.
				textureSize = Mathf.Min( parentCanvasSize.y, parentCanvasSize.x ) * ( ( joystickSizeOverride <= 0.0f ? joystickSize : joystickSizeOverride ) / 10 );

				// Configure the position that the user wants the joystick to be located.
				joystickPosition = new Vector2( parentCanvasSize.x * ( positioningToUse.x / 100 ) - ( textureSize * ( positioningToUse.x / 100 ) ) + ( textureSize / 2 ), parentCanvasSize.y * ( positioningToUse.y / 100 ) - ( textureSize * ( positioningToUse.y / 100 ) ) + ( textureSize / 2 ) ) - ( parentCanvasSize / 2 );

				// If the user wants the joystick anchored to the right of the screen, then flip the horizontal position.
				if( anchor == Anchor.Right )
					joystickPosition.x = -joystickPosition.x;

				// If the user wants a custom touch size...
				if( customActivationRange )
				{
					// Apply the size of the custom activation range.
					BaseTransform.sizeDelta = new Vector2( parentCanvasSize.x * ( activationWidth / 100 ), parentCanvasSize.y * ( activationHeight / 100 ) );

					// Configure the base position minus half the canvas position size.
					Vector2 baseTransformPosition = new Vector2( parentCanvasSize.x * ( activationPositionHorizontal / 100 ) - ( BaseTransform.sizeDelta.x * ( activationPositionHorizontal / 100 ) ) + ( BaseTransform.sizeDelta.x / 2 ), parentCanvasSize.y * ( activationPositionVertical / 100 ) - ( BaseTransform.sizeDelta.y * ( activationPositionVertical / 100 ) ) + ( BaseTransform.sizeDelta.y / 2 ) ) - ( parentCanvasSize / 2 );

					// If the user wants the joystick anchored to the right of the screen, then flip the horizontal position of this base.
					if( anchor == Anchor.Right )
						baseTransformPosition.x = -baseTransformPosition.x;

					// Apply the new position minus half the canvas position size.
					BaseTransform.localPosition = baseTransformPosition;

					// Apply the size and position to the joystickBase.
					joystickBase.sizeDelta = new Vector2( textureSize, textureSize );
					joystickBase.localPosition = BaseTransform.transform.InverseTransformPoint( ParentCanvas.transform.TransformPoint( joystickPosition ) );
				}
				else
				{
					// Apply the joystick size multiplied by the activation range.
					BaseTransform.sizeDelta = new Vector2( textureSize, textureSize ) * activationRange;

					// Apply the imagePosition.
					BaseTransform.localPosition = joystickPosition;

					// Apply the size and position to the joystickBase.
					joystickBase.sizeDelta = new Vector2( textureSize, textureSize );
					joystickBase.localPosition = Vector3.zero;
				}
			}
		}

		// Store the default position of the joystick base for reference.
		defaultBasePosition = joystickBase.localPosition;

		// Configure the size of the Ultimate Joystick's radius.
		radius = ( joystickBase.sizeDelta.x * ParentCanvas.scaleFactor ) * ( joystickRadius / 2 );

		// Update the joystick center so that reference positions can be configured correctly.
		UpdateJoystickCenter();

		// Configure the actual size delta and position of the base trans regardless of the canvas scaler setting.
		Vector2 baseSizeDelta = BaseTransform.sizeDelta * ParentCanvas.scaleFactor;
		Vector2 baseLocalPosition = BaseTransform.localPosition * ParentCanvas.scaleFactor;

		// Calculate the rect of the base trans.
		joystickRect = new Rect( new Vector2( baseLocalPosition.x - ( baseSizeDelta.x / 2 ), baseLocalPosition.y - ( baseSizeDelta.y / 2 ) ) + ( ( parentCanvasSize * ParentCanvas.scaleFactor ) / 2 ), baseSizeDelta );

		// Configure the speed that the joystick will need to travel towards center for the users gravity setting.
		gravitySpeed = ( joystickBase.sizeDelta.x / 2 ) * gravity;

		// Notify any subscribers that the UpdatePositioning function has been called.
		if( Application.isPlaying )
			OnUpdatePositioning?.Invoke();
	}
	
	/// <summary>
	/// Returns a float value between -1 and 1 representing the horizontal value of the Ultimate Joystick.
	/// </summary>
	public float GetHorizontalAxis ()
	{
		return HorizontalAxis;
	}

	/// <summary>
	/// Returns a float value between -1 and 1 representing the vertical value of the Ultimate Joystick.
	/// </summary>
	public float GetVerticalAxis ()
	{
		return VerticalAxis;
	}

	/// <summary>
	/// Returns a raw value of -1, 0 or 1 representing the raw horizontal value of the Ultimate Joystick.
	/// </summary>
	public float GetHorizontalAxisRaw ()
	{
		return Math.Sign( HorizontalAxis );
	}

	/// <summary>
	/// Returns raw a value of -1, 0 or 1 representing the raw vertical value of the Ultimate Joystick.
	/// </summary>
	public float GetVerticalAxisRaw ()
	{
		return Math.Sign( VerticalAxis );
	}

	/// <summary>
	/// Returns a float value between 0 and 1 representing the distance of the joystick from the base.
	/// </summary>
	public float GetDistance ()
	{
		return Vector3.Distance( joystick.localPosition * ParentCanvas.scaleFactor, Vector3.zero ) / radius;
	}

	/// <summary>
	/// Returns if the players input is currently active on this joystick or not.
	/// </summary>
	public bool GetInputActive ()
	{
		return InputActive;
	}

	/// <summary>
	/// Returns the current angle that the joystick is from the center in degrees.
	/// </summary>
	public float GetAngle ()
	{
		// If the joystick is resting at center, then just return 0 for the angle.
		if( HorizontalAxis == 0.0f && VerticalAxis == 0.0f )
			return 0.0f;

		// Store the calculation of the angle with the horizontal and vertical axis values.
		float angle = Mathf.Atan2( VerticalAxis, HorizontalAxis ) * Mathf.Rad2Deg;

		// Since radians actually go counter-clockwise, flip the value so most people will understand it.
		angle *= -1;

		// Additionally, radians actually start with directly right being 0, so add 90 degrees to make the value start straight up.
		angle += 90;

		// If the value is less than 0, then add 360 so that the value goes from 0-360 instead of -180-180.
		if( angle < 0 )
			angle += 360;

		// Return the calculated angle.
		return angle;
	}
	
	/// <summary>
	/// Disables the Ultimate Joystick object.
	/// </summary>
	public void Disable ()
	{
		// Set the InputActive to false since the joystick is being disabled.
		InputActive = false;
		interactPointerId = -10;

		// If the joystick center has been changed, then reset it.
		if( dynamicPositioning || disableVisuals || extendRadius )
		{
			joystickBase.localPosition = defaultBasePosition;
			UpdateJoystickCenter();
		}
		
		// Reset the position of the joystick.
		joystick.localPosition = Vector3.zero;

		// Update the joystick position values since the joystick has been reset.
		UpdatePositionValues();
		
		// If the user is displaying tension accents, then reset them here.
		if( showTension )
			TensionAccentReset();

		// If the user wants to show a transition on the different input states...
		if( !disableVisuals && inputTransition )
		{
			// If the user is displaying a fade, then reset to the untouched state.
			if( useFade )
				joystickGroup.alpha = fadeUntouched;

			// If the user is scaling the joystick, then reset the scale.
			if( useScale )
				joystickBase.transform.localScale = Vector3.one;
		}
		
		// Disable the gameObject.
		gameObject.SetActive( false );
	}

	/// <summary>
	/// Enables the Ultimate Joystick.
	/// </summary>
	public void Enable ()
	{
		// Reset the joystick's position again.
		joystick.localPosition = Vector3.zero;

		// Enable the gameObject.
		gameObject.SetActive( true );
	}

	/// <summary>
	/// Checks to see if the provided input is within range of the Ultimate Joystick.
	/// </summary>
	/// <param name="inputPosition">The input value to check.</param>
	public bool InputInRange ( Vector2 inputPosition )
	{
		// If the user does not want a custom activation range...
		if( !customActivationRange )
		{
			// distance = distance between the world position of the joystickBase cast to a local position of the ParentCanvas (* by scale factor) - half of the actual canvas size, and the input position.
			float distance = Vector2.Distance( ( Vector2 )( ParentCanvas.transform.InverseTransformPoint( joystickBase.position ) * ParentCanvas.scaleFactor ) + ( ( parentCanvasSize * ParentCanvas.scaleFactor ) / 2 ), inputPosition );

			// If the distance is out of range, then return false.
			if( distance / ( BaseTransform.sizeDelta.x * ParentCanvas.scaleFactor ) <= 0.5f )
				return true;
		}
		else
		{
			// If the joystickRect contains the input position, then return true.
			if( joystickRect.Contains( inputPosition ) )
				return true;
		}

		// Else none of the above is true, so return false.
		return false;
	}

	/// <summary>
	/// Disables normal interaction to the joystick and allows the user to move the joystick to a new position on the screen.
	/// </summary>
	public void StartOverridePositioning ()
	{
		// If the user has not enabled the override positioning option then inform the user, enable the override positioning, and disable storing the PlayerPrefs.
		if( !overridePositioning )
		{
			Debug.LogWarning( FormatDebug( "You are attempting to override the position of this joystick, however the Player Position Override option has not been enabled", "Please exit play mode and enable the Player Position Override option on the Ultimate Joystick component", gameObject.name ) );
			overridePositioning = true;
			saveAsPlayerPrefs = false;
		}

		// If the input is currently active on the joystick, reset it to avoid unwanted behavior.
		if( InputActive )
			ResetJoystick();

		// Set IsOverridingPosition to true so that the joystick's position can be overridden.
		IsOverridingPosition = true;
	}

	/// <summary>
	/// Re-enables normal interaction to the joystick, saving the position of the joystick to the position override object.
	/// </summary>
	public void StopOverridePositioning ()
	{
		// If the user has not enabled the override positioning option then inform the user, enable the override positioning, and disable storing the PlayerPrefs.
		if( !overridePositioning )
		{
			Debug.LogWarning( FormatDebug( "You are attempting to override the position of this joystick, however the Player Position Override option has not been enabled", "Please exit play mode and enable the Player Position Override option on the Ultimate Joystick component", gameObject.name ) );
			overridePositioning = true;
			saveAsPlayerPrefs = false;
		}

		// Set IsOverridingPosition to false so that the joystick can be interacted with again.
		IsOverridingPosition = false;

		// Update the stored override information and update the joystick's positioning.
		UpdatePositioningOverride();
		UpdatePositioning();
	}

	/// <summary>
	/// Updates the stored overrides for the joystick position if the Player Position Override option is enabled.
	/// </summary>
	/// <param name="horizontalPosition">The target percentage horizontal position on the screen. (ex. 50 = 50% = center of the screen)</param>
	/// <param name="verticalPosition">The target percentage horizontal position on the screen. (ex. 50 = 50% = center of the screen)</param>
	public void SetOverridePosition ( float horizontalPosition, float verticalPosition )
	{
		// If the user has not enabled the override positioning option then inform the user, enable the override positioning, and disable storing the PlayerPrefs.
		if( !overridePositioning )
		{
			Debug.LogWarning( FormatDebug( "You are attempting to override the position of this joystick, however the Player Position Override option has not been enabled", "Please exit play mode and enable the Player Position Override option on the Ultimate Joystick component", gameObject.name ) );
			overridePositioning = true;
			saveAsPlayerPrefs = false;
		}

		// Store the position values to override.
		joystickPositionOverride = new Vector2( Mathf.Clamp( horizontalPosition, 0.0f, 100.0f ), Mathf.Clamp( verticalPosition, 0.0f, 100.0f ) );

		// If the user wants to save the size override in PlayerPrefs, then set the value.
		if( saveAsPlayerPrefs )
		{
			PlayerPrefs.SetFloat( $"UJHPO_{uniqueId}", joystickPositionOverride.x );
			PlayerPrefs.SetFloat( $"UJVPO_{uniqueId}", joystickPositionOverride.y );
		}

		// Update the positioning to reflect the new size.
		UpdatePositioning();

		// Inform any subscribers that the position has been overridden.
		OnOverridePositioning?.Invoke( joystickPositionOverride, joystickSizeOverride );
	}

	/// <summary>
	/// Overrides the joystick with a new size.
	/// </summary>
	/// <param name="newSize">New size of the joystick.</param>
	public void SetOverrideSize ( float newSize )
	{
		// If the user has not enabled the override positioning option then inform the user, enable the override positioning, and disable storing the PlayerPrefs.
		if( !overridePositioning )
		{
			Debug.LogWarning( FormatDebug( "You are attempting to override the position of this joystick, however the Player Position Override option has not been enabled", "Please exit play mode and enable the Player Position Override option on the Ultimate Joystick component", gameObject.name ) );
			overridePositioning = true;
			saveAsPlayerPrefs = false;
		}

		// If the user wants to save the size override in PlayerPrefs, then set the value.
		if( saveAsPlayerPrefs )
			PlayerPrefs.SetFloat( $"UJSO_{uniqueId}", newSize );
		// Otherwise just store the provided size value to use.
		else
			joystickSizeOverride = newSize;

		// Update the positioning to reflect the new size.
		UpdatePositioning();

		// Inform any subscribers that the position has been overridden.
		OnOverridePositioning?.Invoke( joystickPositionOverride, joystickSizeOverride );
	}

	/// <summary>
	/// Resets the stored override values for this Ultimate Joystick.
	/// </summary>
	public void ResetOverridePositioning ()
	{
		// If the user has not enabled the override positioning option then inform the user, enable the override positioning, and disable storing the PlayerPrefs.
		if( !overridePositioning )
		{
			Debug.LogWarning( FormatDebug( "You are attempting to override the position of this joystick, however the Player Position Override option has not been enabled", "Please exit play mode and enable the Player Position Override option on the Ultimate Joystick component", gameObject.name ) );
			overridePositioning = true;
			saveAsPlayerPrefs = false;
		}

		// Reset stored overrides.
		joystickPositionOverride = new Vector2( -1, -1 );
		joystickSizeOverride = -1.0f;

		// Reset relevant PlayerPrefs.
		PlayerPrefs.SetFloat( $"UJHPO_{uniqueId}", -1.0f );
		PlayerPrefs.SetFloat( $"UJVPO_{uniqueId}", -1.0f );
		PlayerPrefs.SetFloat( $"UJSO_{uniqueId}", -1.0f );

		// Update the positioning of the joystick.
		UpdatePositioning();
	}

	/// <summary>
	/// Updates the stored position override values to apply to the joystick.
	/// </summary>
	void UpdatePositioningOverride ()
	{
		// Configure the position override according to the local position of the joystick base on the canvas.
		joystickPositionOverride = ( Vector2 )ParentCanvasTransform.InverseTransformPoint( joystickBase.position ) + ( parentCanvasSize / 2 );

		// Adjust the X and Y values to be percentages for positioning.
		joystickPositionOverride.x = ( ( joystickPositionOverride.x - ( joystickBase.sizeDelta.x / 2 ) ) / ( parentCanvasSize.x - joystickBase.sizeDelta.x ) ) * 100;
		joystickPositionOverride.y = ( ( joystickPositionOverride.y - ( joystickBase.sizeDelta.y / 2 ) ) / ( parentCanvasSize.y - joystickBase.sizeDelta.y ) ) * 100;

		// If the user has this joystick set to the right, then inverse the position value so that it starts from right to left.
		if( anchor == Anchor.Right )
			joystickPositionOverride.x = -( joystickPositionOverride.x - 100 );

		joystickPositionOverride.x = Mathf.Clamp( joystickPositionOverride.x, repositionConstraintMin.x, repositionConstraintMax.x );
		joystickPositionOverride.y = Mathf.Clamp( joystickPositionOverride.y, repositionConstraintMin.y, repositionConstraintMax.y );

		// If the user wants to store the override positioning to PlayerPrefs, then set them to the overridden values.
		if( saveAsPlayerPrefs )
		{
			PlayerPrefs.SetFloat( $"UJHPO_{uniqueId}", joystickPositionOverride.x );
			PlayerPrefs.SetFloat( $"UJVPO_{uniqueId}", joystickPositionOverride.y );
		}

		// Inform any subscribers that the position has been overridden.
		OnOverridePositioning?.Invoke( joystickPositionOverride, joystickSizeOverride );
	}
	/* ------------------------------------------- *** END PUBLIC FUNCTIONS *** ------------------------------------------- */

	/// <summary>
	/// [INTERNAL] Returns with a confirmation about the existence of the targeted Ultimate Joystick.
	/// </summary>
	static bool JoystickConfirmed ( string joystickName )
	{
		// If the dictionary list doesn't contain this joystick name...
		if( !UltimateJoysticks.ContainsKey( joystickName ) )
		{
			// Log a warning to the user and return false.
			Debug.LogError( FormatDebug( $"No Ultimate Joystick has been registered with the name: {joystickName}", $"Find the Ultimate Joystick in your scene and in the Script Reference section, assign the name: {joystickName}", "Unknown (User Script)" ) );
			return false;
		}

		// Return true because the dictionary does contain the joystick name.
		return true;
	}

	/* --------------------------------------------- *** STATIC FUNCTIONS *** --------------------------------------------- */
	/// <summary>
	/// Finds the Ultimate Joystick component that has been registered with the targeted name and returns it.
	/// </summary>
	/// <param name="joystickName">The name registered to the target Ultimate Joystick.</param>
	public static UltimateJoystick ReturnComponent ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return null;

		return UltimateJoysticks[ joystickName ];
	}

	/// <summary>
	/// Returns a float value between -1 and 1 representing the horizontal value of the Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name registered to the target Ultimate Joystick.</param>
	public static float GetHorizontalAxis ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return 0.0f;

		return UltimateJoysticks[ joystickName ].GetHorizontalAxis();
	}

	/// <summary>
	/// Returns a float value between -1 and 1 representing the vertical value of the Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name registered to the target Ultimate Joystick.</param>
	public static float GetVerticalAxis ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return 0.0f;

		return UltimateJoysticks[ joystickName ].GetVerticalAxis();
	}

	/// <summary>
	/// Returns a value of -1, 0 or 1 representing the raw horizontal value of the Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name registered to the target Ultimate Joystick.</param>
	public static float GetHorizontalAxisRaw ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return 0.0f;

		return UltimateJoysticks[ joystickName ].GetHorizontalAxisRaw();
	}

	/// <summary>
	/// Returns a value of -1, 0 or 1 representing the raw vertical value of the Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name registered to the target Ultimate Joystick.</param>
	public static float GetVerticalAxisRaw ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return 0.0f;

		return UltimateJoysticks[ joystickName ].GetVerticalAxisRaw();
	}

	/// <summary>
	/// Returns a float value between 0 and 1 representing the distance of the joystick from the base.
	/// </summary>
	/// <param name="joystickName">The name registered to the target Ultimate Joystick.</param>
	public static float GetDistance ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return 0.0f;

		return UltimateJoysticks[ joystickName ].GetDistance();
	}

	/// <summary>
	/// Returns the current state of the input being active on the joystick or not.
	/// </summary>
	/// <param name="joystickName">The name registered to the target Ultimate Joystick.</param>
	public static bool GetInputActive ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return false;

		return UltimateJoysticks[ joystickName ].InputActive;
	}

	/// <summary>
	/// Returns the current angle that the joystick is from the center in degrees.
	/// </summary>
	/// <param name="joystickName">The name registered to the target Ultimate Joystick.</param>
	public static float GetAngle ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return 0.0f;

		return UltimateJoysticks[ joystickName ].GetAngle();
	}

	/// <summary>
	/// Disables the targeted Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name registered to the target Ultimate Joystick.</param>
	public static void Disable ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return;

		UltimateJoysticks[ joystickName ].Disable();
	}

	/// <summary>
	/// Enables the targeted Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name registered to the target Ultimate Joystick.</param>
	public static void Enable ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return;

		UltimateJoysticks[ joystickName ].Enable();
	}

	/// <summary>
	/// Checks to see if the provided input is within range of the targeted Ultimate Joystick.
	/// </summary>
	/// <param name="joystickName">The name registered to the target Ultimate Joystick.</param>
	/// <param name="inputPosition">The input value to check.</param>
	public static bool InputInRange ( string joystickName, Vector2 inputPosition )
	{
		if( !JoystickConfirmed( joystickName ) )
			return false;

		return UltimateJoysticks[ joystickName ].InputInRange( inputPosition );
	}
	/* ------------------------------------------- *** END STATIC FUNCTIONS *** ------------------------------------------- */

	// OBSOLETE //
	#pragma warning disable CS0414
	[SerializeField]
	[Obsolete()]
	private float radiusModifier = 4.5f;
	#pragma warning restore

	[Obsolete( "Please use the GetInputActive function or the InputActive property." )]
	public bool GetJoystickState ()
	{
		return InputActive;
	}

	[Obsolete( "Please use the GetInputActive function." )]
	public static bool GetJoystickState ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return false;

		return UltimateJoysticks[ joystickName ].InputActive;
	}

	[Obsolete( "Please use the HighlightColor property." )]
	public void UpdateHighlightColor ( Color newColor )
	{
		HighlightColor = newColor;
	}
	
	[Obsolete( "Please use the TensionColorNone and TensionColorFull properties." )]
	public void UpdateTensionColors ( Color newTensionNone, Color newTensionFull )
	{
		TensionColorNone = newTensionNone;
		TensionColorFull = newTensionFull;
	}

	[Obsolete( "Please use the ReturnComponent function." )]
	public static UltimateJoystick GetUltimateJoystick ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return null;

		return UltimateJoysticks[ joystickName ];
	}

	[Obsolete( "Please use the OnTapAchieved or OnTapReleased callback instead." )]
	public bool GetTapCount ( int targetTapCount = 2 )
	{
		// If the OLD method of getting tap count was used, then reset the new tap stuff to avoid constant double tap.
		if( tapCount >= targetTapCount )
		{
			currentTapTime = 0.0f;
			tapCount = 0;
			return true;
		}

		return false;
	}

	[Obsolete( "Please use the OnTapAchieved or OnTapReleased callback instead." )]
	public static bool GetTapCount ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return false;

		return UltimateJoysticks[ joystickName ].GetTapCount();
	}

	[Obsolete( "Please use the Disable() function now." )]
	public void DisableJoystick ()
	{
		Disable();
	}

	[Obsolete( "Please use the Disable() function now." )]
	public static void DisableJoystick ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return;

		UltimateJoysticks[ joystickName ].DisableJoystick();
	}

	[Obsolete( "Please use the Enable() function now." )]
	public void EnableJoystick ()
	{
		Enable();
	}

	[Obsolete( "Please use the Enable() function now." )]
	public static void EnableJoystick ( string joystickName )
	{
		if( !JoystickConfirmed( joystickName ) )
			return;

		UltimateJoysticks[ joystickName ].EnableJoystick();
	}
}