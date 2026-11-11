using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Handles trigger detection, toggling the prompt & slider UI on 'E' press,
/// and driving the chest opening animation based on the slider value.
/// </summary>
public class ChestTrigger : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The main interaction Canvas. If empty, automatically finds 'Interaction Module'.")]
    public GameObject canvasObject;

    [Tooltip("The prompt UI (e.g. 'Press E to Open'). Hidden when slider is active.")]
    public GameObject promptUI;

    [Tooltip("The Slider UI panel or container that pops up.")]
    public GameObject sliderPanel;

    [Tooltip("The UI Slider that controls the animation. If empty, it will be searched or created automatically.")]
    public Slider chestSlider;

    [Header("Animation References")]
    [Tooltip("Animator that controls the chest. If empty, automatically searched on this object or children/parents.")]
    public Animator chestAnimator;

    [Tooltip("Name of the animation state in the Animator Controller to scrub.")]
    public string animationStateName = "ChestOpen";

    [Tooltip("Optional AnimationClip reference if using clip sampling directly.")]
    public AnimationClip animationClip;

    [Header("Procedural Lid Fallback")]
    [Tooltip("Transform of the lid (Chest_Top). Rotates automatically if animator has no keyframes yet.")]
    public Transform lidTransform;

    [Tooltip("Rotation offset in degrees when chest is 100% open.")]
    public Vector3 openRotationAngle = new Vector3(-95f, 0f, 0f);

    [Header("Settings")]
    public string playerTag = "Player";

    private bool isPlayerInside = false;
    private bool isSliderOpen = false;
    private Quaternion initialLidRotation;
    private Quaternion openLidRotation;

    void Awake()
    {
        FindCanvasReferences();
        FindAnimationReferences();
        SetupTriggerBridgeIfNeeded();
    }

    void Start()
    {
        // Ensure UI starts closed
        if (sliderPanel != null) sliderPanel.SetActive(false);
        if (canvasObject != null) canvasObject.SetActive(false);

        // Setup slider listener
        if (chestSlider != null)
        {
            chestSlider.minValue = 0f;
            chestSlider.maxValue = 1f;
            chestSlider.onValueChanged.RemoveAllListeners();
            chestSlider.onValueChanged.AddListener(OnSliderValueChanged);
        }
    }

    void Update()
    {
        if (!isPlayerInside) return;

        // Check for 'E' key press to toggle slider
        bool ePressed = false;
        bool escPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            ePressed = Keyboard.current.eKey.wasPressedThisFrame;
            escPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        ePressed = Input.GetKeyDown(KeyCode.E);
        escPressed = Input.GetKeyDown(KeyCode.Escape);
#endif

        if (ePressed)
        {
            ToggleSlider(!isSliderOpen);
        }
        else if (escPressed && isSliderOpen)
        {
            ToggleSlider(false);
        }
    }

    /// <summary>
    /// Opens or closes the animation slider UI and handles cursor locking.
    /// </summary>
    public void ToggleSlider(bool open)
    {
        isSliderOpen = open;

        if (sliderPanel != null)
        {
            sliderPanel.SetActive(isSliderOpen);
        }

        if (promptUI != null)
        {
            // Hide prompt when slider is active, show prompt when slider is closed
            promptUI.SetActive(!isSliderOpen);
        }

        // Unlock mouse cursor for UI interaction, or relock for gameplay
        SimplePlayerController.SetInteractingWithUI(isSliderOpen);
    }

    /// <summary>
    /// Called whenever the user drags or adjusts the slider (value: 0.0 to 1.0).
    /// </summary>
    public void OnSliderValueChanged(float value)
    {
        // 1. Scrub Unity Animator if available
        if (chestAnimator != null && chestAnimator.runtimeAnimatorController != null)
        {
            chestAnimator.speed = 0f;
            chestAnimator.Play(animationStateName, 0, value);
            chestAnimator.Update(0f);
            chestAnimator.SetFloat("OpenAmount", value);
        }

        // 2. Direct AnimationClip sampling fallback
        if (animationClip != null)
        {
            float animTime = value * animationClip.length;
            animationClip.SampleAnimation(lidTransform != null ? lidTransform.gameObject : gameObject, animTime);
        }

        // 3. Procedural rotation fallback (swings the lid open smoothly)
        if (lidTransform != null && (chestAnimator == null || chestAnimator.runtimeAnimatorController == null))
        {
            lidTransform.localRotation = Quaternion.Slerp(initialLidRotation, openLidRotation, value);
        }
    }

    public void OnPlayerEnter()
    {
        isPlayerInside = true;
        isSliderOpen = false;

        if (canvasObject != null) canvasObject.SetActive(true);
        if (promptUI != null) promptUI.SetActive(true);
        if (sliderPanel != null) sliderPanel.SetActive(false);
    }

    public void OnPlayerExit()
    {
        isPlayerInside = false;

        if (isSliderOpen)
        {
            ToggleSlider(false);
        }

        if (canvasObject != null) canvasObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            OnPlayerEnter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            OnPlayerExit();
        }
    }

    public bool IsPlayer(Collider other)
    {
        if (other == null) return false;

        if (other.CompareTag(playerTag)) return true;
        if (other.transform.root != null && other.transform.root.CompareTag(playerTag)) return true;

        if (other.GetComponent<CharacterController>() != null ||
            other.GetComponentInParent<CharacterController>() != null ||
            other.GetComponent<SimplePlayerController>() != null ||
            other.GetComponentInParent<SimplePlayerController>() != null)
        {
            return true;
        }

        return false;
    }

    void FindCanvasReferences()
    {
        // 1. Find main canvas
        if (canvasObject == null)
        {
            GameObject module = GameObject.Find("Interaction Module");
            if (module != null)
            {
                canvasObject = module;
            }
            else
            {
#if UNITY_2023_1_OR_NEWER
                Canvas sceneCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
#else
                Canvas sceneCanvas = FindObjectOfType<Canvas>(true);
#endif
                if (sceneCanvas != null) canvasObject = sceneCanvas.gameObject;
            }
        }

        if (canvasObject == null) return;

        // 2. Find prompt UI (Image or child in Interaction Module)
        if (promptUI == null)
        {
            Transform img = canvasObject.transform.Find("Image");
            if (img != null)
            {
                promptUI = img.gameObject;
            }
        }

        // 3. Find or auto-create Slider
        if (chestSlider == null)
        {
            chestSlider = canvasObject.GetComponentInChildren<Slider>(true);
        }

        if (sliderPanel == null && chestSlider != null)
        {
            sliderPanel = chestSlider.gameObject;
        }

        // Auto-create a clean slider if one doesn't exist yet in the canvas!
        if (chestSlider == null)
        {
            CreateDefaultSliderUI();
        }
    }

    void CreateDefaultSliderUI()
    {
        // Create container panel
        GameObject container = new GameObject("SliderPanel", typeof(RectTransform));
        container.transform.SetParent(canvasObject.transform, false);

        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0f, 0f);
        containerRect.sizeDelta = new Vector2(400f, 100f);

        // Add background panel
        Image bgImage = container.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        // Create standard Unity Slider inside container
        DefaultControls.Resources uiResources = new DefaultControls.Resources();
        GameObject sliderObj = DefaultControls.CreateSlider(uiResources);
        sliderObj.name = "ChestSlider";
        sliderObj.transform.SetParent(container.transform, false);

        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
        sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = new Vector2(0f, -10f);
        sliderRect.sizeDelta = new Vector2(320f, 25f);

        chestSlider = sliderObj.GetComponent<Slider>();
        sliderPanel = container;
    }

    void FindAnimationReferences()
    {
        // Find Animator
        if (chestAnimator == null)
        {
            chestAnimator = GetComponent<Animator>();
            if (chestAnimator == null) chestAnimator = GetComponentInParent<Animator>();
            if (chestAnimator == null) chestAnimator = GetComponentInChildren<Animator>();
        }

        // Find Lid Transform (Chest_Top)
        if (lidTransform == null)
        {
            Transform top = transform.Find("Chest_Top");
            if (top == null && transform.parent != null)
            {
                top = transform.parent.Find("Chest_Top");
            }
            if (top != null)
            {
                lidTransform = top;
            }
        }

        // Cache initial and target lid rotations for smooth procedural rotation
        if (lidTransform != null)
        {
            initialLidRotation = lidTransform.localRotation;
            openLidRotation = initialLidRotation * Quaternion.Euler(openRotationAngle);
        }
    }

    void SetupTriggerBridgeIfNeeded()
    {
        Collider currentCollider = GetComponent<Collider>();
        if (currentCollider == null || !currentCollider.isTrigger)
        {
            Collider[] childColliders = GetComponentsInChildren<Collider>();
            foreach (var col in childColliders)
            {
                if (col.isTrigger && col.gameObject != gameObject)
                {
                    ChestTriggerBridge bridge = col.gameObject.GetComponent<ChestTriggerBridge>();
                    if (bridge == null)
                    {
                        bridge = col.gameObject.AddComponent<ChestTriggerBridge>();
                        bridge.targetTrigger = this;
                    }
                }
            }
        }
    }
}

/// <summary>
/// Helper bridge automatically attached to child trigger colliders when ChestTrigger is placed on the parent chest.
/// </summary>
public class ChestTriggerBridge : MonoBehaviour
{
    [HideInInspector]
    public ChestTrigger targetTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (targetTrigger != null && targetTrigger.IsPlayer(other))
        {
            targetTrigger.OnPlayerEnter();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (targetTrigger != null && targetTrigger.IsPlayer(other))
        {
            targetTrigger.OnPlayerExit();
        }
    }
}
