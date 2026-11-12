
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class ChestTrigger : MonoBehaviour
{
    [Header("UI References")]
    public GameObject canvasObject;
    public GameObject promptUI;
    public GameObject sliderPanel;
    public Slider chestSlider;
    public GameObject treasureFoundUI;

    [Header("Animation References")]
    public Animator chestAnimator;
    public string animationStateName = "ChestOpen";
    public AnimationClip animationClip;

    [Header("Procedural Lid")]
    public Transform lidTransform;
    public Vector3 openRotationAngle = new Vector3(-95f, 0f, 0f);

    [Header("Settings")]
    public string playerTag = "Player";
    public float treasureThreshold = 0.85f;
    public float endingDelay = 3f;
    public string endingSceneName = "EndingLobby";

    private bool isPlayerInside;
    private bool isSliderOpen;
    private bool treasureFound = false;

    private HashSet<Collider> playerColliders =
        new HashSet<Collider>();

    private PlayerMovement playerMovement;
    private bool movementWasEnabled;

    private Quaternion initialLidRotation;
    private Quaternion openLidRotation;

    private CursorLockMode previousCursorLockState;
    private bool previousCursorVisible;

    void Awake()
    {
        FindCanvasReferences();
        FindAnimationReferences();
        SetupTriggerBridgeIfNeeded();
    }

    void Start()
    {
        if (canvasObject != null)
            canvasObject.SetActive(false);

        if (sliderPanel != null)
            sliderPanel.SetActive(false);

        if (promptUI != null)
            promptUI.SetActive(false);

        if (treasureFoundUI != null)
            treasureFoundUI.SetActive(false);

        if (chestSlider != null)
        {
            chestSlider.minValue = 0f;
            chestSlider.maxValue = 1f;
            chestSlider.value = 0f;

            chestSlider.onValueChanged.RemoveListener(
                OnSliderValueChanged
            );

            chestSlider.onValueChanged.AddListener(
                OnSliderValueChanged
            );
        }
        else
        {
            Debug.LogError("Chest Slider not found!");
        }

        if (treasureFoundUI == null)
        {
            Debug.LogError("Treasure Found UI not found!");
        }

        if (lidTransform != null)
            lidTransform.localRotation = initialLidRotation;
    }

    void Update()
    {
        if (!isPlayerInside || treasureFound)
            return;

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
            ToggleSlider(!isSliderOpen);

        if (escPressed && isSliderOpen)
            ToggleSlider(false);
    }

    public void ToggleSlider(bool open)
    {
        if (isSliderOpen == open || treasureFound)
            return;

        isSliderOpen = open;

        if (open)
        {
            previousCursorLockState = Cursor.lockState;
            previousCursorVisible = Cursor.visible;

            if (playerMovement != null)
            {
                movementWasEnabled = playerMovement.enabled;
                playerMovement.enabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (playerMovement != null)
                playerMovement.enabled = movementWasEnabled;

            Cursor.lockState = previousCursorLockState;
            Cursor.visible = previousCursorVisible;
        }

        if (sliderPanel != null)
            sliderPanel.SetActive(open);

        if (promptUI != null)
            promptUI.SetActive(!open && isPlayerInside);
    }

    public void OnSliderValueChanged(float value)
    {
        if (treasureFound)
            return;

        value = Mathf.Clamp01(value);

        Debug.Log("Chest Slider Value: " + value);

        // Animator Controller
        if (chestAnimator != null &&
            chestAnimator.runtimeAnimatorController != null)
        {
            chestAnimator.speed = 0f;
            chestAnimator.Play(animationStateName, 0, value);
            chestAnimator.Update(0f);
        }

        // Animation Clip
        else if (animationClip != null)
        {
            GameObject target = lidTransform != null
                ? lidTransform.gameObject
                : gameObject;

            animationClip.SampleAnimation(
                target,
                value * animationClip.length
            );
        }

        // Procedural lid rotation
        else if (lidTransform != null)
        {
            lidTransform.localRotation = Quaternion.Slerp(
                initialLidRotation,
                openLidRotation,
                value
            );
        }

        // TREASURE FOUND
        if (value >= treasureThreshold && !treasureFound)
        {
            treasureFound = true;

            Debug.Log("TREASURE FOUND!");

            if (treasureFoundUI != null)
                treasureFoundUI.SetActive(true);
            else
                Debug.LogError("Treasure Found UI is not connected!");

            Invoke(nameof(LoadEndingLobby), endingDelay);
        }
    }

    private void LoadEndingLobby()
    {
        Debug.Log("Loading Ending Lobby...");

        SceneManager.LoadScene(endingSceneName);
    }

    public void OnPlayerEnter(Collider other)
    {
        if (other == null || !playerColliders.Add(other))
            return;

        if (playerColliders.Count > 1)
            return;

        if (playerMovement == null)
        {
            playerMovement = other.GetComponent<PlayerMovement>();

            if (playerMovement == null)
                playerMovement =
                    other.GetComponentInParent<PlayerMovement>();

            if (playerMovement == null)
                playerMovement =
                    other.transform.root.GetComponent<PlayerMovement>();
        }

        isPlayerInside = true;

        if (canvasObject != null)
            canvasObject.SetActive(true);

        if (promptUI != null)
            promptUI.SetActive(true);

        if (sliderPanel != null)
            sliderPanel.SetActive(false);
    }

    public void OnPlayerExit(Collider other)
    {
        if (other == null || !playerColliders.Remove(other))
            return;

        if (playerColliders.Count > 0)
            return;

        isPlayerInside = false;

        if (isSliderOpen)
            ToggleSlider(false);

        if (canvasObject != null)
            canvasObject.SetActive(false);

        playerMovement = null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
            OnPlayerEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
            OnPlayerExit(other);
    }

    public bool IsPlayer(Collider other)
    {
        if (other == null)
            return false;

        if (other.CompareTag(playerTag))
            return true;

        if (other.transform.root.CompareTag(playerTag))
            return true;

        return other.GetComponent<CharacterController>() != null ||
               other.GetComponentInParent<CharacterController>() != null;
    }

    private void FindCanvasReferences()
    {
        if (canvasObject == null)
        {
            GameObject module =
                GameObject.Find("InteractionModule");

            if (module != null)
                canvasObject = module;
        }

        if (canvasObject == null)
        {
            Debug.LogError("InteractionModule not found!");
            return;
        }

        if (promptUI == null)
        {
            Transform prompt =
                canvasObject.transform.Find("Press_E_Prompt");

            if (prompt != null)
                promptUI = prompt.gameObject;
        }

        if (sliderPanel == null)
        {
            Transform panel =
                canvasObject.transform.Find("SliderPanel");

            if (panel != null)
                sliderPanel = panel.gameObject;
        }

        if (chestSlider == null)
        {
            Transform slider =
                canvasObject.transform.Find(
                    "SliderPanel/ChestSlider"
                );

            if (slider != null)
                chestSlider = slider.GetComponent<Slider>();
        }

        if (treasureFoundUI == null)
        {
            Transform treasure =
                canvasObject.transform.Find("TreasureFound");

            if (treasure != null)
                treasureFoundUI = treasure.gameObject;
        }

        Debug.Log("Chest UI references searched.");
    }

    private void FindAnimationReferences()
    {
        if (chestAnimator == null)
            chestAnimator = GetComponentInChildren<Animator>();

        if (lidTransform == null)
            lidTransform = transform.Find("Chest_Top");

        if (lidTransform != null)
        {
            initialLidRotation = lidTransform.localRotation;

            openLidRotation = initialLidRotation *
                Quaternion.Euler(openRotationAngle);
        }
    }

    private void SetupTriggerBridgeIfNeeded()
    {
        Collider[] childColliders =
            GetComponentsInChildren<Collider>(true);

        foreach (Collider col in childColliders)
        {
            if (col.isTrigger && col.gameObject != gameObject)
            {
                ChestTriggerBridge bridge =
                    col.GetComponent<ChestTriggerBridge>();

                if (bridge == null)
                    bridge = col.gameObject
                        .AddComponent<ChestTriggerBridge>();

                bridge.targetTrigger = this;
            }
        }
    }
}

public class ChestTriggerBridge : MonoBehaviour
{
    [HideInInspector]
    public ChestTrigger targetTrigger;

    private void OnTriggerEnter(Collider other)
    {
        if (targetTrigger != null &&
            targetTrigger.IsPlayer(other))
        {
            targetTrigger.OnPlayerEnter(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (targetTrigger != null &&
            targetTrigger.IsPlayer(other))
        {
            targetTrigger.OnPlayerExit(other);
        }
    }
}
