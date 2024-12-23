using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.InputSystem;

public class HPlayerStateMachine : MonoBehaviour
{
    L2PlayerInput playerInput;
    private CharacterController characterController;

    private Vector2 currentMovementInput;
    Vector3 currentMovement;
    Vector3 currentRunMovement;
    private Vector3 appliedMovement;
    private Vector3 _cameraRelativeMovement;
    private bool isMovementPressed;

    private Animator animator;
    private int isWalkingHash;
    private int isRunningHash;
    private int isJumpingHash;
    private int velocityXHash;
    private int velocityZHash;
    private int isSkill1Hash;
    private int isRecallStandingIdleHash;
    private int isSpellRecallHash;
    private float rotationFactorPerFrame = 15.0f;
    
    private int isSwimmingHash;

    private bool isRunPressed;
    float runMultiplier = 5.0f;

    private bool isSkill1Pressed;
    private HCharacterSkillBase skillScript;

    private bool isJumpPressed = false;
    private float initialJumpVelocity;
    private float maxJumpHeight = 2f;
    private float maxJumpTime = 0.75f;
    private bool isJumping = false;
    private bool _requireNewJumpPress = false;
    private bool isDiveIntoWaterPress = false;
    public bool IsDiveIntoWaterPress
    {
        get { return isDiveIntoWaterPress; }
    }
    
    float gravity = -9.8f;
    float groundedGravity = -0.05f;
    
    int jumpCount = 0;
    Dictionary<int, float> initialJumpVelocities = new Dictionary<int, float>(); 
    Dictionary<int, float> jumpGravities = new Dictionary<int, float>();

    private Coroutine currentJumpResetRoutine = null;
    private int jumpCountHash;

    //死亡
    private bool isDie = false;
    
    private HPlayerBaseState _currentState;
    private HPlayerStateFactory _states;
    
    public Camera playerCamera;

    private bool isInThirdPersonCamera = false;
    
    public void SetInThirdPersonCamera(bool value)
    {
        isInThirdPersonCamera = value;
    }
    
    #region Gets and Sets
    
    public bool IsInThirdPersonCamera
    {
        get { return isInThirdPersonCamera; }
    }
    
    public HCharacterSkillBase SkillScript
    {
        get { return skillScript; }
    }
    public int IsSkill1Hash
    {
        get { return isSkill1Hash; }
    }
    public int IsRecallStandingIdleHash
    {
        get { return isRecallStandingIdleHash; }
    }
    public int IsSpellRecallHash
    {
        get { return isSpellRecallHash; }
    }
    
    public int IsSwimmingHash
    {
        get { return isSwimmingHash; }
    }
    public bool IsJumpPressed
    {
        get { return isJumpPressed; }
    }

    private bool isInWater = false;
    private bool isFloatOnWater = false;
    
    public bool IsFloatOnWater
    {
        get { return isFloatOnWater; }
    }

    public bool IsInWater
    {
        get { return isInWater; }
    }
    
    public HPlayerBaseState CurrentState
    {
        get { return _currentState; }
        set { _currentState = value; }
    }
    
    public Animator Animator
    {
        get { return animator; }
    }
    
    public Coroutine CurrentJumpResetRoutine
    {
        get { return currentJumpResetRoutine; }
        set { currentJumpResetRoutine = value; }
    }
    
    public Dictionary<int, float> InitialJumpVelocities
    {
        get { return initialJumpVelocities; }
    }
    
    public int JumpCount
    {
        get { return jumpCount; }
        set { jumpCount = value; }
    }
    
    public int IsJumpingHash
    {
        get { return isJumpingHash; }
    }
    
    public int JumpCountHash
    {
        get { return jumpCountHash; }
    }
    
    public bool RequireNewJumpPress
    {
        get { return _requireNewJumpPress; }
        set { _requireNewJumpPress = value; }
    }
    
    public bool IsJumping
    {
        set { isJumping = value; }
    }
    
    public float CurrentMovementY
    {
        get { return currentMovement.y; }
        set { currentMovement.y = value; }
    }
    
    public float AppliedMovementY
    {
        get { return appliedMovement.y; }
        set { appliedMovement.y = value; }
    }
    
    public float GroundedGravity
    {
        get { return groundedGravity; }
    }
    
    public CharacterController CharacterController
    {
        get { return characterController; }
    }
    
    public Dictionary<int, float> JumpGravities
    {
        get { return jumpGravities; }
    }
    
    public bool IsSkill1Pressed
    {
        get { return isSkill1Pressed; }
    }
    
    public bool IsMovementPressed
    {
        get { return isMovementPressed; }
    }
    
    public bool IsRunPressed
    {
        get { return isRunPressed; }
    }
    
    public bool IsDie
    {
        get { return isDie; }
        set { isDie = value; }
    }
    
    public int IsWalkingHash
    {
        get { return isWalkingHash; }
    }
    
    public int IsRunningHash
    {
        get { return isRunningHash; }
    }

    public void SetInWater(bool inWater)
    {
        isInWater = inWater;
    }

    public void SetOnWaterFloat(bool isOnWater)
    {
        isFloatOnWater = isOnWater;
    }
    
    public float AppliedMovementX
    {
        get { return appliedMovement.x; }
        set { appliedMovement.x = value; }
    }
    
    public float AppliedMovementZ
    {
        get { return appliedMovement.z; }
        set { appliedMovement.z = value; }
    }
    
    public float RunMultiplier
    {
        get { return runMultiplier; }
    }
    
    public Vector2 CurrentMovementInput
    {
        get { return currentMovementInput; }
    }
    
    public float Gravity
    {
        get { return gravity; }
    }
    
    #endregion


    public void SetRunMultiplierSpeed(float runSpeed)
    {
        runMultiplier = runSpeed;
        if (runMultiplier <= 1)
            runMultiplier = 1;
        else if(runMultiplier >= 15)  //这里代码其实写的不太好，但是算是双重保障，不要让玩家移动的太快
            runMultiplier = 15;
    }
    
    private void Awake()
    {
        _states = new HPlayerStateFactory(this);
        _currentState = _states.Grounded();
        _currentState.EnterState();
        
        playerInput = new L2PlayerInput();
        characterController = GetComponent<CharacterController>();
        playerInput.CharacterControls.Move.started += OnMovementInput;
        playerInput.CharacterControls.Move.canceled += OnMovementInput;
        playerInput.CharacterControls.Move.performed += OnMovementInput;
        
        playerInput.CharacterControls.Run.started += OnRun;
        playerInput.CharacterControls.Run.canceled += OnRun;

        playerInput.CharacterControls.Jump.started += OnJump;
        playerInput.CharacterControls.Jump.canceled += OnJump;

        playerInput.CharacterControls.Skill1.started += OnSkill1;
        playerInput.CharacterControls.Skill1.canceled += OnSkill1;
        
        playerInput.CharacterControls.DiveIntoWater.started += OnDiveIntoWater;
        playerInput.CharacterControls.DiveIntoWater.canceled += OnDiveIntoWater;
        animator = GetComponent<Animator>();
        
        isWalkingHash = Animator.StringToHash("isWalking");
        isRunningHash = Animator.StringToHash("isRunning");
        isJumpingHash = Animator.StringToHash("isJumping");
        jumpCountHash = Animator.StringToHash("jumpCount");
        isSkill1Hash = Animator.StringToHash("isSkill1");
        isRecallStandingIdleHash = Animator.StringToHash("isRecallStandingIdle");
        isSpellRecallHash = Animator.StringToHash("isSpellRecall");
        velocityXHash = Animator.StringToHash("VelocityX");
        velocityZHash = Animator.StringToHash("VelocityZ");
        
        isSwimmingHash = Animator.StringToHash("isSwimming");
        
        //skillScript = GetComponent<HCharacterSkillBase>();
        
        SetupJumpVaraibles();
    }

    private void Start()
    {
        characterController.Move(appliedMovement * Time.deltaTime);
    }

    //处理角色的旋转逻辑，使角色面向移动方向，todo：使用Cinemachine之后这种旋转逻辑可能要进行修改
    void HandleRotation() 
    {
        Vector3 positionToLookAt;
        positionToLookAt.x = _cameraRelativeMovement.x;
        positionToLookAt.y = 0;
        positionToLookAt.z = _cameraRelativeMovement.z;
        
        Quaternion currentRotation = transform.rotation;
        if (isMovementPressed)
        {
            //Debug.Log("dddddddddddddd");
            if (positionToLookAt == Vector3.zero) return;
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, Time.deltaTime * rotationFactorPerFrame);
        }
        
    }

    Vector3 ConvertToCameraSpace(Vector3 vectorToRotate)
    {
        float currentYValue = vectorToRotate.y;
        if (playerCamera == null)
        {
            //playerCamera = YPlayModeController.Instance.playerCamera().GetComponent<Camera>();
            playerCamera = Camera.main; //note:这里改成了main camera，对于GameJam来说，main camera用来做player的camera是最方便的
        }
        Vector3 forward = playerCamera.transform.forward;
        Vector3 right = playerCamera.transform.right;
        if (!isInWater)
        {
            forward.y = 0;
            right.y = 0;
        }
        forward = forward.normalized;
        right = right.normalized;
        //create direction-relative input vectors, 也就是说在相机的前方和右方的投影
        Vector3 forwardRelativeVerticalInput = vectorToRotate.z * forward;
        Vector3 rightRelativeHorizontalInput = vectorToRotate.x * right;
            
        //create camera-relative movement
        Vector3 cameraRelativeMovement = forwardRelativeVerticalInput + rightRelativeHorizontalInput;
        if(!isInWater) cameraRelativeMovement.y = currentYValue;
        // else
        // {
        //     if (isJumpPressed) //在水里，按住空格键的时候只能往下游
        //     {
        //         cameraRelativeMovement.y = ((cameraRelativeMovement.y < 0) ? cameraRelativeMovement.y : 0);
        //     }
        // }
        return cameraRelativeMovement;
    }
    
    void SetupJumpVaraibles()
    {
        float timeToApex = maxJumpTime / 2.0f;
        float initialGravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
        
        float secondJumpGravity = (-2 * (maxJumpHeight + 2)) / Mathf.Pow((timeToApex * 1.25f), 2);
        float secondJumpInitialVelocity = (2 * (maxJumpHeight + 2)) / (timeToApex * 1.25f);
        
        float thirdJumpGravity = (-2 * (maxJumpHeight + 3)) / Mathf.Pow((timeToApex * 1.5f), 2);
        float thirdJumpInitialVelocity = (2 * (maxJumpHeight + 3)) / (timeToApex * 1.5f);
        
        initialJumpVelocities.Add(1, initialJumpVelocity);
        initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        initialJumpVelocities.Add(3, thirdJumpInitialVelocity);
        
        jumpGravities.Add(0, initialGravity); //设置一个0是为了处理当jumpCount reset到0的情况
        jumpGravities.Add(1, initialGravity);
        jumpGravities.Add(2, secondJumpGravity);
        jumpGravities.Add(3, thirdJumpGravity);
    }
    
    void OnJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
        _requireNewJumpPress = false;
    }

    void OnDiveIntoWater(InputAction.CallbackContext context)
    {
        isDiveIntoWaterPress = context.ReadValueAsButton();
    }

    void OnSkill1(InputAction.CallbackContext context)
    {
        isSkill1Pressed = context.ReadValueAsButton();
        Debug.Log("isSkill1Pressed: " + isSkill1Pressed);
        //if (!skillScript) return; //这东西的赋值暂时是在外部进行的，这个架构并不好，所以先注释掉
        // if(isSkill1Pressed && skillScript.isSkill1Valid() && characterController.isGrounded)
        // {
        //     animator.SetTrigger(isSkill1Hash);
        //     skillScript.SetPlayerBaseAction(playerInput);
        //     skillScript.PlaySkill1();
        // }  //note: 根据需要进行逻辑上的修改
        animator.SetTrigger(isSkill1Hash);
    }
    
    public void OnStandingIdle()
    {
        if(characterController.isGrounded)
        {
            animator.SetBool(isRecallStandingIdleHash, true);
        }
    }
    public void OnSpellRecall(bool isSpellRecall)
    {
        animator.SetBool(isSpellRecallHash, isSpellRecall);
    }
    public void OnStandingIdleBack()
    {
        animator.SetBool(isRecallStandingIdleHash, false);
        
    }
    
    void OnRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }
    
    void OnMovementInput(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        isMovementPressed = currentMovementInput.x != 0 || currentMovementInput.y != 0;
    }
    
    private void OnEnable()
    {
        playerInput.CharacterControls.Enable();
    }
    
    public void SetInputActionDisableOrEnable(bool shouldLock)
    {
        if (shouldLock)
        {
            playerInput.CharacterControls.Disable();
        }
        else
        {
            playerInput.CharacterControls.Enable();
        }
    }

    private void OnDisable()
    {
        playerInput.CharacterControls.Disable();
    }
    
    void Update()
    {
        if (!isInThirdPersonCamera)
        {
            HandleRotation();
        }
        else
        {
            animator.SetFloat(velocityXHash, appliedMovement.x);
            animator.SetFloat(velocityZHash, appliedMovement.z);
        }
        
        _currentState.UpdateStates(); //逻辑上，先Update自己，有substate的话再Update Substate
        _cameraRelativeMovement = ConvertToCameraSpace(appliedMovement);
        characterController.Move(_cameraRelativeMovement * Time.deltaTime);
    }

    
}
