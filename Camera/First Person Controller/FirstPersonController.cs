using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace WManager
{
    /// <summary>
    /// 第一人称控制器
    /// </summary>
    [AddComponentMenu("管理器/第一人称控制器")]
    [RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
    public class FirstPersonController : MonoBehaviour
    {
        public enum InvertMouseInput
        {
            [LabelText("不反转")] None,
            X,
            Y,
            [LabelText("两者")] Both
        }

        public enum CameraInputMode
        {
            [LabelText("传统")] Traditional,
            [LabelText("传统与限制")] TraditionalWithConstraints,
            [LabelText("复古")] Retro
        }

        public enum FootStepMode
        {
            [LabelText("静态")] Static,
            [LabelText("动态")] Dynamic
        }

        #region Variables

        #region Basic
        [TabGroup("基础"), LabelText("开始时影藏鼠标")]
        public bool hideCursorOnStart;
        [TabGroup("基础"), LabelText("重力系数"), Tooltip("确定物理引擎的重力乘以多少。")]
        public float gravityMultiplier = 1f;
        [TabGroup("基础"), LabelText("角色移动开关")]
        public bool enablePlayerMovement = true;
        [TabGroup("基础"), LabelText("最大可移动坡度"), Range(0.01f, 98.9f), Tooltip("决定玩家可以向上走的最大角度。如果为0，则不会使用坡度检测系统。")]
        public float maxSlopeAngle = 55f;
        [TabGroup("基础"), LabelText("最大可移动台阶高"), Range(0, 0.499f), Tooltip("通过将小窗台与此值进行比较来确定它是否为楼梯。值超过0.5会产生奇怪的结果。")]
        public float maxStepHeight = .2f;

        private readonly float maxWallShear = 89f;
        private PhysicMaterial zeroFrictionMaterial;
        private PhysicMaterial highFrictionMaterial;
        private bool isTouchingWalkable;
        private bool isTouchingUpright;
        private bool isTouchingFlat;
        private bool stairMiniHop = false;
        private Vector3 currentGroundNormal;
        private float lastKnownSlopeAngle;
        private float speed;
        private CapsuleCollider capsuleCollider;
        private Vector2 input;
        private float yVelocity;
        private Rigidbody mRigidBody;
        private Vector3 originalLocalPosition;
        private Vector3 previousPosition;
        private Vector3 previousVelocity;
        private Vector3 miscRefVel;
        private bool previousGrounded;
        #endregion

        #region Camera
        [SerializeField, TabGroup("相机"), LabelText("角色相机")] private Camera playerCamera;
        [SerializeField, TabGroup("相机"), LabelText("相机移动"), Tooltip("决定玩家是否可以移动相机。")]
        private bool enableCameraMovement = true;
        [TabGroup("相机"), LabelText("使用鼠标右键移动相机")]
        public bool rightMouthButtonDrived;
        [TabGroup("相机"), LabelText("鼠标输入反转"), Tooltip("确定鼠标输入是否反转，以及沿哪个轴。")]
        public InvertMouseInput mouseInputInversion = InvertMouseInput.None;
        [TabGroup("相机"), LabelText("相机输入模式"), Tooltip("确定用于相机的输入方法。\n传统:在所有轴上使用鼠标。\n传统约束:仅在Y轴上使用鼠标。\n复古:使用左右移动键。")]
        public CameraInputMode cameraInputMode = CameraInputMode.Traditional;
        [TabGroup("相机"), LabelText("相机垂直移动角"), Range(0, 180), Tooltip("确定相机垂直移动的距离。")]
        public float verticalRotationRange = 170f;
        [TabGroup("相机"), LabelText("鼠标灵敏度"), Tooltip("确定鼠标的灵敏度。")]
        public float mouseSensitivity = 10f;
        [TabGroup("相机"), LabelText("相机视野影响鼠标的程度"), Tooltip("确定相机的视野对鼠标灵敏度的影响程度。")]
        public float fovMouseSensitivity = 1f;
        [TabGroup("相机"), LabelText("相机移动平滑度"), Tooltip("确定相机移动的平滑程度。")]
        public float cameraSmoothing = 5f;
        [TabGroup("相机"), LabelText("相机抖动"), Tooltip("允许调用相机抖动事件。从外部调用此协程，持续时间为0.01到1，大小为0.01到0.5")]
        public bool enableCameraShake = false;
        [TabGroup("相机"), LabelText("冲刺视野大小"), Tooltip("确定进入冲刺时摄像机的视野会有多大。")]
        public float fovKickAmount = 2.5f;
        [TabGroup("相机"), LabelText("冲刺视野持续时间")]
        public float fovKickTime = .75f;
        private float fovRef;
        private Vector3 cameraStartingPosition;
        private float baseCameraFOV;
        private Vector3 targetAngles;
        private Vector3 followAngles;
        private Vector3 followVelocity;
        private Vector3 originalLocalRotation;
        #endregion

        #region Walk
        [TabGroup("行走"), LabelText("默认行走(false为跑)"), Tooltip("确定默认的运动模式是步行还是短跑。")]
        public bool walkByDefault = true;
        [TabGroup("行走"), LabelText("行走速度")]
        public float walkSpeed = 4f;
        private float walkSpeedInternal;
        #endregion

        #region Crouch
        [TabGroup("下蹲"), LabelText("下蹲开关"), Tooltip("决定是否允许玩家蹲伏。")]
        public bool enableCrouch = true;
        [TabGroup("下蹲"), LabelText("下蹲按键")]
        public KeyCode crouchKey = KeyCode.LeftControl;
        [TabGroup("下蹲"), LabelText("按下起立"), Tooltip("开启后需再次点击下蹲按键才能起立")]
        public bool toggleCrouch = false;
        [TabGroup("下蹲"), LabelText("下蹲移速"), Tooltip("决定玩家蹲下时移动的速度。")]
        public float crouchWalkSpeedMultiplier = .5f;
        [TabGroup("下蹲"), LabelText("下蹲后跳跃力量增幅"), Tooltip("决定蹲下时玩家的跳跃力量增加或减少的程度。")]
        public float crouchJumpPowerMultiplier = 0f;
        [TabGroup("下蹲"), LabelText("强行下蹲"), Tooltip("一个将覆盖蹲伏键以迫使玩家蹲伏的开关。")]
        public bool crouchOverride;
        private float colliderHeight;
        private bool isCrouching;
        #endregion

        #region Sprint
        [TabGroup("冲刺"), LabelText("冲刺按键"), Tooltip("确定进入冲刺需要按下什么键。")]
        public KeyCode sprintKey = KeyCode.LeftShift;
        [TabGroup("冲刺"), LabelText("冲刺速度")]
        public float sprintSpeed = 8f;
        [TabGroup("冲刺"), LabelText("使用耐力"), Tooltip("决定短跑是否会受到耐力的限制。")]
        public bool enableStamina = true;
        [TabGroup("冲刺"), LabelText("耐力消耗速度"), Tooltip("决定玩家耐力消耗的速度。")]
        public float staminaDepletionSpeed = 2f;
        [TabGroup("冲刺"), LabelText("耐力值"), Tooltip("决定玩家的耐力。如果为0，则不会使用耐力。")]
        public float staminaLevel = 50f;
        private float staminaInternal;
        private float sprintSpeedInternal;
        private bool isSprinting = false;
        #endregion

        #region Jump
        [TabGroup("跳跃"), LabelText("跳跃开关")]
        public bool enableJump = true;
        [TabGroup("跳跃"), LabelText("自动跳跃"), Tooltip("确定是否需要按下跳跃按钮才能跳跃，或者玩家是否可以在每次落地时按住跳跃按钮自动跳跃。")]
        public bool enableHoldJump;
        [TabGroup("跳跃"), LabelText("跳跃按键")]
        public KeyCode jumpKey = KeyCode.Space;
        [TabGroup("跳跃"), LabelText("跳跃力")]
        public float jumpPower = 5f;
        private bool isJumping;
        private bool didJump;
        private float jumpPowerInternal;
        #endregion

        #region Head
        [SerializeField, TabGroup("头部"), LabelText("头部位置"), Tooltip("表示头部的变换。摄影机应该是该变换的子对象。")]
        private Transform head;
        [TabGroup("头部"), LabelText("颠簸效果"), Tooltip("决定是否使用颠簸效果")]
        public bool enableHeadbob = true;
        [TabGroup("头部"), LabelText("将头部骨骼加入碰撞体")]
        public bool snapHeadJointToCollider = true;
        [TabGroup("头部"), LabelText("颠簸频率")]
        public float headbobFrequency = 1.5f;
        [TabGroup("头部"), LabelText("摆动角度"), Tooltip("头左右摆动角度")]
        public float headbobSwayAngle = 1f;
        [TabGroup("头部"), LabelText("颠簸高度"), Tooltip("头上下颠簸高度")]
        public float headbobHeight = 1f;
        [TabGroup("头部"), LabelText("水平摆动距离"), Tooltip("头在水平方向的摆动距离")]
        public float headbobHorizontalMovement = .5f;
        [TabGroup("头部"), LabelText("落地颠簸程度"), Tooltip("确定跳跃和着陆时的颠簸强度")]
        public float jumpLandJerkIntensity = 3f;
        private float headbobCycle = 0f;
        private float headbobFade = 0f;
        private float springPosition = 0f;
        private float springVelocity = 0f;
        private readonly float springElastic = 1.1f;
        private readonly float springDampen = .8f;
        private readonly float springVelocityThreshold = .05f;
        private readonly float springPositionThreshold = .05f;
        #endregion

        //todo：脚步声
        #region Footstep
        private AudioClip m_LandSound;
        [SerializeField, TabGroup("脚步"), LabelText("脚步音系统开关")]
        private bool enableAudioSFX = true;
        [SerializeField, TabGroup("脚步"), LabelText("脚步音大小")]
        private float volume = .5f;
        [SerializeField, TabGroup("脚步"), LabelText("脚步模式"), Tooltip("静态为固定播放，动态为根据接触地面播放")]
        private FootStepMode footStepMode;
        private float nextStepTime = .5f;

        private List<AudioClip> currentClipSet;
        [SerializeField, TabGroup("脚步"), LabelText("起跳声")] private AudioClip jumpSound;
        [SerializeField, TabGroup("脚步"), LabelText("着陆声")] private AudioClip landSound;
        [SerializeField, TabGroup("脚步"), LabelText("默认脚步声")] private List<AudioClip> footStepSounds;

        [Space(20)]
        [SerializeField, TabGroup("脚步"), LabelText("木头物理材料"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private PhysicMaterial woodPhysicMaterial;
        [SerializeField, TabGroup("脚步"), LabelText("木头步音"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private List<AudioClip> woodClipSet;
        [SerializeField, TabGroup("脚步"), LabelText("金属和玻璃物理材料"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private PhysicMaterial metalAndGlassPhysicMaterial;
        [SerializeField, TabGroup("脚步"), LabelText("金属和玻璃步音"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private List<AudioClip> metalAndGlassClipSet;
        [SerializeField, TabGroup("脚步"), LabelText("草物理材料"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private PhysicMaterial grassPhysicMaterial;
        [SerializeField, TabGroup("脚步"), LabelText("草步音"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private List<AudioClip> grassClipSet;
        [SerializeField, TabGroup("脚步"), LabelText("泥土碎石物理材料"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private PhysicMaterial dirtAndGravelPhysicMaterial;
        [SerializeField, TabGroup("脚步"), LabelText("泥土碎石步音"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private List<AudioClip> dirtAndGravelClipSet;
        [SerializeField, TabGroup("脚步"), LabelText("岩石和混凝土物理材料"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private PhysicMaterial rockAndConcretePhysicMaterial;
        [SerializeField, TabGroup("脚步"), LabelText("岩石和混凝土步音"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private List<AudioClip> rockAndConcreteClipSet;
        [SerializeField, TabGroup("脚步"), LabelText("泥浆物理材料"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private PhysicMaterial mudPhysicMaterial;
        [SerializeField, TabGroup("脚步"), LabelText("泥浆步音"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private List<AudioClip> mudClipSet;
        [SerializeField, TabGroup("脚步"), LabelText("自定义物理材料"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private PhysicMaterial customPhysicMaterial;
        [SerializeField, TabGroup("脚步"), LabelText("自定义步音"), ShowIf("footStepMode", FootStepMode.Dynamic)]
        private List<AudioClip> customClipSet;

        private AudioSource m_AudioSource;
        #endregion

        public bool IsGrounded { get; private set; }
        #endregion

        #region NonPublic Methods
        private void Awake()
        {
            originalLocalRotation = transform.localRotation.eulerAngles;

            walkSpeedInternal = walkSpeed;
            sprintSpeedInternal = sprintSpeed;
            jumpPowerInternal = jumpPower;
            capsuleCollider = GetComponent<CapsuleCollider>();
            IsGrounded = true;
            isCrouching = false;
            mRigidBody = GetComponent<Rigidbody>();
            mRigidBody.interpolation = RigidbodyInterpolation.Extrapolate;
            mRigidBody.collisionDetectionMode = CollisionDetectionMode.Continuous;
            colliderHeight = capsuleCollider.height;
            m_AudioSource = GetComponent<AudioSource>();
        }
        private void Start()
        {
            if (hideCursorOnStart)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }

            cameraStartingPosition = playerCamera.transform.localPosition;
            baseCameraFOV = playerCamera.fieldOfView;

            capsuleCollider.radius = capsuleCollider.height * .25f;
            staminaInternal = staminaLevel;
            zeroFrictionMaterial = new PhysicMaterial("Zero_Friction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicMaterialCombine.Minimum,
                bounceCombine = PhysicMaterialCombine.Minimum
            };
            highFrictionMaterial = new PhysicMaterial("High_Friction")
            {
                dynamicFriction = 1f,
                staticFriction = 1f,
                frictionCombine = PhysicMaterialCombine.Maximum,
                bounceCombine = PhysicMaterialCombine.Average
            };

            originalLocalPosition = snapHeadJointToCollider ? new Vector3(head.localPosition.x, capsuleCollider.height * .5f * head.localScale.y, head.localPosition.z) : head.localPosition;
            previousPosition = mRigidBody.position;
        }
        private void Update()
        {
            if (enableCameraMovement)
            {
                if (rightMouthButtonDrived)
                {
                    if (Input.GetMouseButton(1))
                    {
                        CameraMovement();
                    }
                }
                else
                {
                    CameraMovement();
                }
            }

            if (enableHoldJump ? (enableJump && Input.GetKey(jumpKey)) : (enableJump && Input.GetKeyDown(jumpKey)))
                isJumping = true;
            else if (Input.GetKeyUp(jumpKey)) isJumping = false;

            if (enableCrouch)
            {
                if (!toggleCrouch)
                    isCrouching = crouchOverride || Input.GetKey(crouchKey);
                else if (Input.GetKeyDown(crouchKey))
                    isCrouching = crouchOverride || !isCrouching;
            }
        }
        private void FixedUpdate()
        {
            if (enableStamina)
            {
                isSprinting = Input.GetKey(sprintKey) && !isCrouching && staminaInternal > 0f
                    && (Mathf.Abs(mRigidBody.velocity.x) > .01f || Mathf.Abs(mRigidBody.velocity.z) > .01f);
                if (isSprinting) staminaInternal -= staminaDepletionSpeed * 2f * Time.deltaTime;
                else if ((!Input.GetKey(sprintKey) || Mathf.Abs(mRigidBody.velocity.x) < .01f
                    || Mathf.Abs(mRigidBody.velocity.z) < .01f || isCrouching) && staminaInternal < staminaLevel)
                    staminaInternal += staminaDepletionSpeed * Time.deltaTime;
                staminaInternal = Mathf.Clamp(staminaInternal, 0f, staminaLevel);
            }
            else isSprinting = Input.GetKey(sprintKey);

            Vector3 moveDirection = Vector3.zero;
            speed = walkByDefault ? isCrouching ? walkSpeedInternal : (isSprinting ? sprintSpeedInternal : walkSpeedInternal) : (isSprinting ? walkSpeedInternal : sprintSpeedInternal);

            if (maxSlopeAngle > 0f)
            {
                if (isTouchingUpright && isTouchingWalkable)
                {
                    moveDirection = transform.forward * input.y * speed + transform.right * input.x * walkSpeedInternal;
                    if (!didJump) mRigidBody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
                }
                else if (isTouchingUpright && !isTouchingWalkable) mRigidBody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
                else
                {
                    mRigidBody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
                    moveDirection = (transform.forward * input.y * speed + transform.right * input.x * walkSpeedInternal) * (mRigidBody.velocity.y > .01f ? SlopeCheck() : .8f);
                }
            }
            else moveDirection = transform.forward * input.y * speed + transform.right * input.x * walkSpeedInternal;

            if (maxStepHeight > 0f && Physics.Raycast(transform.position - new Vector3(0f, capsuleCollider.height * .5f * transform.localScale.y - .01f, 0f), moveDirection, out RaycastHit hit,
                capsuleCollider.radius + .15f, Physics.AllLayers, QueryTriggerInteraction.Ignore) && Vector3.Angle(hit.normal, Vector3.up) > 88f)
            {
                if (!Physics.Raycast(transform.position - new Vector3(0f, (capsuleCollider.height * .5f * transform.localScale.y) - maxStepHeight, 0f), moveDirection, out hit,
                    capsuleCollider.radius + .25f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                {
                    stairMiniHop = true;
                    transform.position += new Vector3(0f, maxStepHeight * 1.2f, 0f);
                }
            }

            input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
            if (input.magnitude > 1f) input.Normalize();

            yVelocity = mRigidBody.velocity.y;
            if (IsGrounded && isJumping && jumpPowerInternal > 0f && !didJump)
            {
                if (maxSlopeAngle > 0f)
                {
                    if (isTouchingFlat || isTouchingWalkable)
                    {
                        didJump = true;
                        isJumping = false;
                        yVelocity += mRigidBody.velocity.y < .01f ? jumpPowerInternal : jumpPowerInternal / 3;
                        isTouchingWalkable = false;
                        isTouchingUpright = false;
                        isTouchingFlat = false;
                        mRigidBody.constraints = RigidbodyConstraints.None | RigidbodyConstraints.FreezeRotation;
                    }
                }
                else
                {
                    didJump = true;
                    isJumping = false;
                    yVelocity += jumpPowerInternal;
                }
            }

            if (maxSlopeAngle > 0f)
            {
                if (!didJump && lastKnownSlopeAngle > 5f && isTouchingWalkable) yVelocity *= SlopeCheck() * .25f;
                if (isTouchingUpright && !isTouchingWalkable && !didJump) yVelocity += Physics.gravity.y;
            }

            if (enablePlayerMovement) mRigidBody.velocity = moveDirection + (Vector3.up * yVelocity);
            else mRigidBody.velocity = Vector3.zero;

            if (input.magnitude > 0f || !IsGrounded) capsuleCollider.sharedMaterial = zeroFrictionMaterial;
            else capsuleCollider.sharedMaterial = highFrictionMaterial;
            mRigidBody.AddForce(Physics.gravity * (gravityMultiplier - 1f));

            if (fovKickAmount > 0f)
            {
                if (isSprinting && !isCrouching && playerCamera.fieldOfView != (baseCameraFOV + (fovKickAmount * 2f) - .01f))
                {
                    if (Mathf.Abs(mRigidBody.velocity.x) > .5f || Mathf.Abs(mRigidBody.velocity.z) > .5f)
                        playerCamera.fieldOfView = Mathf.SmoothDamp(playerCamera.fieldOfView, baseCameraFOV + fovKickAmount * 2f, ref fovRef, fovKickTime);
                }
                else if (playerCamera.fieldOfView != baseCameraFOV)
                    playerCamera.fieldOfView = Mathf.SmoothDamp(playerCamera.fieldOfView, baseCameraFOV, ref fovRef, fovKickTime * .5f);
            }

            if (enableCrouch)
            {
                if (isCrouching)
                {
                    capsuleCollider.height = Mathf.MoveTowards(capsuleCollider.height, colliderHeight / 1.5f, 5f * Time.deltaTime);
                    walkSpeedInternal = walkSpeed * crouchWalkSpeedMultiplier;
                    jumpPowerInternal = jumpPower * crouchJumpPowerMultiplier;
                }
                else
                {
                    capsuleCollider.height = Mathf.MoveTowards(capsuleCollider.height, colliderHeight, 5f * Time.deltaTime);
                    walkSpeedInternal = walkSpeed;
                    sprintSpeedInternal = sprintSpeed;
                    jumpPowerInternal = jumpPower;
                }
            }

            float xPos = 0f;
            float yPos = 0f;
            float xTilt = 0f;
            float zTilt = 0f;
            if (enableHeadbob)
            {
                Vector3 vel = (mRigidBody.position - previousPosition) / Time.deltaTime;
                Vector3 velChange = vel - previousVelocity;
                previousPosition = mRigidBody.position;
                previousVelocity = vel;
                springVelocity -= velChange.y;
                springVelocity -= springPosition * springElastic;
                springVelocity *= springDampen;
                springPosition += springVelocity * Time.deltaTime;
                springPosition = Mathf.Clamp(springPosition, -.3f, .3f);

                if (Mathf.Abs(springVelocity) < springVelocityThreshold && Mathf.Abs(springPosition) < springPositionThreshold)
                {
                    springPosition = 0f;
                    springVelocity = 0f;
                }
                float flatVel = new Vector3(vel.x, 0f, vel.z).magnitude;
                float strideLangthen = 1f + flatVel * headbobFrequency * .2f;
                headbobCycle += flatVel / strideLangthen * (Time.deltaTime / headbobFrequency);
                float bobFactor = Mathf.Sin(headbobCycle * Mathf.PI * 2f);
                float bobSwayFactor = Mathf.Sin(Mathf.PI * (2f * headbobCycle + .5f));
                bobFactor = 1f - (bobFactor * .5f + 1f);
                bobFactor *= bobFactor;

                if (jumpLandJerkIntensity > 0f && !stairMiniHop) xTilt = -springPosition * jumpLandJerkIntensity * 5.5f;
                else if (!stairMiniHop) xTilt = -springPosition;

                if (IsGrounded)
                {
                    headbobFade = new Vector3(vel.x, 0f, vel.z).magnitude < .1f
                        ? Mathf.MoveTowards(headbobFade, 0f, .5f)
                        : Mathf.MoveTowards(headbobFade, 1f, Time.deltaTime);
                    float speedHeightFactor = 1f + flatVel * .3f;
                    xPos = -headbobHorizontalMovement * .1f * headbobFade * bobSwayFactor;
                    yPos = springPosition * jumpLandJerkIntensity * .1f + headbobHeight * .1f * bobFactor * headbobFade * speedHeightFactor;
                    zTilt = bobSwayFactor * headbobSwayAngle * .1f * headbobFade;
                }
            }
            if (enableHeadbob)
            {
                head.localPosition = mRigidBody.velocity.magnitude > .1f
                    ? Vector3.MoveTowards(head.localPosition, snapHeadJointToCollider
                    ? new Vector3(originalLocalPosition.x, capsuleCollider.height * .5f * head.localScale.y, originalLocalPosition.z) + new Vector3(xPos, yPos, 0f)
                    : originalLocalPosition + new Vector3(xPos, yPos, 0f), .5f)
                    : Vector3.SmoothDamp(head.localPosition, snapHeadJointToCollider
                    ? new Vector3(originalLocalPosition.x, capsuleCollider.height * .5f * head.localScale.y, originalLocalPosition.z) + new Vector3(xPos, yPos, 0f)
                    : originalLocalPosition + new Vector3(xPos, yPos, 0f), ref miscRefVel, .15f);
                head.localRotation = Quaternion.Euler(xTilt, 0f, zTilt);
            }

            if (enableAudioSFX)
            {
                switch (footStepMode)
                {
                    case FootStepMode.Static:
                        if (IsGrounded)
                        {
                            if (!previousGrounded)
                            {
                                if (landSound)
                                {
                                    m_AudioSource.clip = landSound;
                                    m_AudioSource.volume = volume;
                                    m_AudioSource.Play();
                                }
                                nextStepTime = headbobCycle + .5f;
                            }
                            else
                            {
                                if (headbobCycle > nextStepTime)
                                {
                                    nextStepTime = headbobCycle + .5f;
                                    int n = UnityEngine.Random.Range(0, footStepSounds.Count);
                                    if (footStepSounds.Any() && footStepSounds[n] != null)
                                    {
                                        m_AudioSource.clip = footStepSounds[n];
                                        m_AudioSource.volume = volume;
                                        m_AudioSource.Play();
                                    }
                                }
                            }
                            previousGrounded = true;
                        }
                        else
                        {
                            if (previousGrounded)
                            {
                                if (jumpSound)
                                {
                                    m_AudioSource.clip = jumpSound;
                                    m_AudioSource.volume = volume;
                                    m_AudioSource.Play();
                                }
                            }
                            previousGrounded = false;
                        }
                        break;
                    case FootStepMode.Dynamic:
                        if (Physics.Raycast(transform.position, Vector3.down, out hit))
                        {
                            currentClipSet = (woodPhysicMaterial && woodPhysicMaterial == hit.collider.sharedMaterial && woodClipSet.Any()) ? // If standing on Wood
                            woodClipSet : ((grassPhysicMaterial && grassPhysicMaterial == hit.collider.sharedMaterial && grassClipSet.Any()) ? // If standing on Grass
                            grassClipSet : ((metalAndGlassPhysicMaterial && metalAndGlassPhysicMaterial == hit.collider.sharedMaterial && metalAndGlassClipSet.Any()) ? // If standing on Metal/Glass
                            metalAndGlassClipSet : ((rockAndConcretePhysicMaterial && rockAndConcretePhysicMaterial == hit.collider.sharedMaterial && rockAndConcreteClipSet.Any()) ? // If standing on Rock/Concrete
                            rockAndConcreteClipSet : ((dirtAndGravelPhysicMaterial && dirtAndGravelPhysicMaterial == hit.collider.sharedMaterial && dirtAndGravelClipSet.Any()) ? // If standing on Dirt/Gravle
                            dirtAndGravelClipSet : ((mudPhysicMaterial && mudPhysicMaterial == hit.collider.sharedMaterial && mudClipSet.Any()) ? // If standing on Mud
                            mudClipSet : ((customPhysicMaterial && customPhysicMaterial == hit.collider.sharedMaterial && customClipSet.Any()) ? // If standing on the custom material 
                            customClipSet : footStepSounds)))))); // If material is unknown, fall back

                            if (IsGrounded)
                            {
                                if (!previousGrounded)
                                {
                                    if (currentClipSet.Any())
                                    {
                                        m_AudioSource.clip = currentClipSet[UnityEngine.Random.Range(0, currentClipSet.Count)];
                                        m_AudioSource.volume = volume;
                                        m_AudioSource.Play();
                                    }
                                    nextStepTime = headbobCycle + .5f;
                                }
                                else
                                {
                                    if (headbobCycle > nextStepTime)
                                    {
                                        nextStepTime = headbobCycle + .5f;
                                        if (currentClipSet.Any())
                                        {
                                            m_AudioSource.clip = currentClipSet[UnityEngine.Random.Range(0, currentClipSet.Count)];
                                            m_AudioSource.volume = volume;
                                            m_AudioSource.Play();
                                        }
                                    }
                                }
                                previousGrounded = true;
                            }
                            else
                            {
                                if (previousGrounded)
                                {
                                    if (currentClipSet.Any())
                                    {
                                        m_AudioSource.clip = currentClipSet[UnityEngine.Random.Range(0, currentClipSet.Count)];
                                        m_AudioSource.volume = volume;
                                        m_AudioSource.Play();
                                    }
                                }
                                previousGrounded = false;
                            }
                        }
                        else
                        {
                            currentClipSet = footStepSounds;
                            if (IsGrounded)
                            {
                                if (!previousGrounded)
                                {
                                    if (landSound)
                                    {
                                        m_AudioSource.clip = landSound;
                                        m_AudioSource.volume = volume;
                                        m_AudioSource.Play();
                                        nextStepTime = headbobCycle + .5f;
                                    }
                                    else
                                    {
                                        if (headbobCycle > nextStepTime)
                                        {
                                            nextStepTime = headbobCycle + .5f;
                                            int n = UnityEngine.Random.Range(0, footStepSounds.Count);
                                            if (footStepSounds.Any())
                                            {
                                                m_AudioSource.clip = footStepSounds[n];
                                                m_AudioSource.volume = volume;
                                                m_AudioSource.Play();
                                            }
                                            footStepSounds[n] = footStepSounds[0];
                                        }
                                    }
                                    previousGrounded = true;
                                }
                                else
                                {
                                    if (previousGrounded)
                                    {
                                        if (jumpSound)
                                        {
                                            m_AudioSource.clip = jumpSound;
                                            m_AudioSource.volume = volume;
                                            m_AudioSource.Play();
                                        }
                                    }
                                    previousGrounded = false;
                                }
                            }
                        }
                        break;
                }
            }

            IsGrounded = false;
            if (maxSlopeAngle > 0f)
            {
                if (isTouchingFlat || isTouchingWalkable || isTouchingUpright) didJump = false;
                isTouchingWalkable = false;
                isTouchingUpright = false;
                isTouchingFlat = false;
            }
        }

        private void CameraMovement()
        {
            float mouseXInput;
            float mouseYInput = 0f;
            float camFOV = playerCamera.fieldOfView;
            if (cameraInputMode == CameraInputMode.Traditional || cameraInputMode == CameraInputMode.TraditionalWithConstraints)
            {
                mouseXInput = (mouseInputInversion == InvertMouseInput.None || mouseInputInversion == InvertMouseInput.Y ? 1f : -1f) * Input.GetAxis("Mouse X");
                mouseYInput = (mouseInputInversion == InvertMouseInput.None || mouseInputInversion == InvertMouseInput.X ? 1f : -1f) * Input.GetAxis("Mouse Y");
            }
            else mouseXInput = Input.GetAxis("Horizontal") * (mouseInputInversion == InvertMouseInput.None || mouseInputInversion == InvertMouseInput.Y ? 1f : -1f);
            if (targetAngles.y > 180f)
            {
                targetAngles.y -= 360f;
                followAngles.y -= 360f;
            }
            else if (targetAngles.y < -180f)
            {
                targetAngles.y += 360f;
                followAngles.y += 360f;
            }
            if (targetAngles.x > 180f)
            {
                targetAngles.x -= 360f;
                followAngles.x -= 360f;
            }
            else if (targetAngles.x < -180f)
            {
                targetAngles.x += 360f;
                followAngles.x += 360f;
            }
            targetAngles.y += mouseXInput * (mouseSensitivity - (baseCameraFOV - camFOV) * fovMouseSensitivity / 6f);

            if (cameraInputMode == CameraInputMode.Traditional)
                targetAngles.x += mouseYInput * (mouseSensitivity - (baseCameraFOV - camFOV) * fovMouseSensitivity / 6f);
            else targetAngles.x = 0f;
            targetAngles.x = Mathf.Clamp(targetAngles.x, -.5f * verticalRotationRange, .5f * verticalRotationRange);
            followAngles = Vector3.SmoothDamp(followAngles, targetAngles, ref followVelocity, cameraSmoothing * .01f);

            playerCamera.transform.localRotation = Quaternion.Euler(-followAngles.x + originalLocalRotation.x, 0f, 0f);
            transform.localRotation = Quaternion.Euler(0f, followAngles.y + originalLocalRotation.y, 0f);
        }
        private float SlopeCheck()
        {
            lastKnownSlopeAngle = Mathf.MoveTowards(lastKnownSlopeAngle, Vector3.Angle(currentGroundNormal, Vector3.up), 5f);
            return new AnimationCurve(new Keyframe(-90f, 1f), new Keyframe(0f, 1f), new Keyframe(maxSlopeAngle + 15f, 0f), new Keyframe(maxWallShear, 0f), new Keyframe(maxWallShear + .1f, 1f), new Keyframe(90f, 1f))
            { preWrapMode = WrapMode.Clamp, postWrapMode = WrapMode.ClampForever }.Evaluate(lastKnownSlopeAngle);
        }

        private void OnCollisionEnter(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                float a = Vector3.Angle(collision.GetContact(i).normal, Vector3.up);
                if (collision.GetContact(i).point.y < transform.position.y - (capsuleCollider.height * .5f - capsuleCollider.radius * .95f))
                {
                    if (!IsGrounded)
                    {
                        IsGrounded = true;
                        stairMiniHop = false;
                        if (didJump && a <= 70f) didJump = false;
                    }
                    if (maxSlopeAngle > 0f)
                    {
                        if (a < 5.1f)
                        {
                            isTouchingFlat = true;
                            isTouchingWalkable = true;
                        }
                        else if (a < maxSlopeAngle + .1f)
                            isTouchingWalkable = true;
                        else if (a < 90f) isTouchingUpright = true;
                        currentGroundNormal = collision.GetContact(i).normal;
                    }
                }
            }
        }
        private void OnCollisionStay(Collision collision)
        {
            for (int i = 0; i < collision.contactCount; i++)
            {
                float a = Vector3.Angle(collision.GetContact(i).normal, Vector3.up);
                if (collision.GetContact(i).point.y < transform.position.y - (capsuleCollider.height * .5f - capsuleCollider.radius * .95f))
                {
                    if (!IsGrounded)
                    {
                        IsGrounded = true;
                        stairMiniHop = false;
                    }
                    if (maxSlopeAngle > 0f)
                    {
                        if (a < 5.1f)
                        {
                            isTouchingFlat = true;
                            isTouchingWalkable = true;
                        }
                        else if (a < maxSlopeAngle + .1f)
                            isTouchingWalkable = true;
                        else if (a < 90f) isTouchingUpright = true;
                        currentGroundNormal = collision.GetContact(i).normal;
                    }
                }
            }
        }
        private void OnCollisionExit(Collision collision)
        {
            IsGrounded = false;
            if (maxSlopeAngle > 0f)
            {
                currentGroundNormal = Vector3.up;
                lastKnownSlopeAngle = 0f;
                isTouchingWalkable = false;
                isTouchingUpright = false;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Use this function ro rotate the camera. 
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="snap"></param>
        public void RotateCamera(Vector2 rotation, bool snap)
        {
            enableCameraMovement = !enableCameraMovement;
            if (snap)
            {
                followAngles = rotation;
                targetAngles = rotation;
            }
            else
            {
                targetAngles = rotation;
            }
            enableCameraMovement = !enableCameraMovement;
        }
        /// <summary>
        /// For a cinematic camera shake effet.
        /// </summary>
        /// <param name="duration">how long in seconds, will the camera shake event last.</param>
        /// <param name="magnitude">how much the camera will shake.</param>
        /// <returns></returns>
        public IEnumerator CameraShake(float duration, float magnitude)
        {
            float elapsed = 0;
            while (elapsed < duration && enableCameraShake)
            {
                playerCamera.transform.localPosition = Vector3.MoveTowards(playerCamera.transform.localPosition,
                    new Vector3(cameraStartingPosition.x + UnityEngine.Random.Range(-1, 1) * magnitude, cameraStartingPosition.y + UnityEngine.Random.Range(-1, 1) * magnitude,
                    cameraStartingPosition.z), magnitude * 2);
                yield return new WaitForSecondsRealtime(0.001f);
                elapsed += Time.deltaTime;
                yield return null;
            }
            playerCamera.transform.localPosition = cameraStartingPosition;
        }
        #endregion
    }
}