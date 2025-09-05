using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    private float _walkSpeed;
    [SerializeField]
    private float _sprintSpeed;
    [SerializeField]
    private InputManager _input;
    [SerializeField]
    private float _rotationSmoothTime = 0.1f;
    
    private float _rotationSmoothVelocity;
    [SerializeField]
    private float _walkSprintTransition;
    [SerializeField]
    private float _speed;
    [SerializeField]
    private float _jumpForce;
    [SerializeField]
    private Transform _groundDetector;
    [SerializeField]
    private float _detectorRadius;
    [SerializeField]
    private LayerMask _groundLayer;
    
    private bool _isGrounded;
    [SerializeField]
    private Vector3 _upperStepOffset;
    [SerializeField]
    private float _stepCheckerDistance;
    [SerializeField]
    private float _stepForce;
    [SerializeField]
    private PlayerStances _playerstances;
    [SerializeField]
    private Transform _climbDetector;
    [SerializeField]
    private float _climbCheckDistance;
    [SerializeField]
    private LayerMask _climbableLayer;
    [SerializeField]
    private Vector3 _climbOffset;
    private Rigidbody _rigidbody;
    [SerializeField]
    private float _climbSpeed;
    [SerializeField]
    private Transform _cameraTransform;
    [SerializeField]
    private CameraManager _CameraManager;
    [SerializeField]
    private Animator _animator;


    private void CheckStep()
    {
        bool isHitLowerStep = Physics.Raycast(_groundDetector.position,transform.forward,_stepCheckerDistance);
        bool isHitUpperStep = Physics.Raycast(_groundDetector.position +_upperStepOffset,transform.forward,_stepCheckerDistance);
        if (isHitLowerStep)
        {
            if(!isHitUpperStep)
            {

            }
            _rigidbody.AddForce(0, _stepForce, 0);
        }
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _speed = _walkSpeed;
        _animator = GetComponent<Animator>();
        _playerstances = PlayerStances.Stand;
        HideAndLockCursor();
    }
    private void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimb += CancelClimb;
        _CameraManager.OnChangePerspective += ChangePerspective;
    }
    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimb += CancelClimb;
        _CameraManager.OnChangePerspective += ChangePerspective;

    }
    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
    }

    private void HideAndLockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void Move(Vector2 axisDirection)
    {
        Vector3 movementDirection = Vector3.zero;
        bool isPlayerStanding = _playerstances == PlayerStances.Stand;
        bool isPlayerClimbing = _playerstances == PlayerStances.Climb;
        if (isPlayerStanding)
        {
            switch (_CameraManager.CameraState)
            {
                case CameraState.ThirdPerson:
                    if (axisDirection.magnitude >= 0.1)
                    {
                        float rotationAngle = Mathf.Atan2(axisDirection.x, axisDirection.y) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
                        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                        movementDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                        _rigidbody.AddForce(movementDirection * Time.deltaTime * _speed);
                    }
                    break;
                case CameraState.FirstPerson:
                    transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
                    Vector3 horizontalDirection = axisDirection.x * transform.right;
                    Vector3 verticalDirection = axisDirection.y * transform.forward;
                    movementDirection = horizontalDirection + verticalDirection;
                    _rigidbody.AddForce(movementDirection * _speed * Time.deltaTime);
                    break;
                default:
                    break;
            }
            Vector3 velocity = new Vector3(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);
            _animator.SetFloat("Velocity", velocity.magnitude * axisDirection.magnitude);
            _animator.SetFloat("VelocityZ", velocity.magnitude * axisDirection.y);
            _animator.SetFloat("VelocityX", velocity.magnitude * axisDirection.x);


        }
        else if(isPlayerClimbing){
            Vector3 horizontal = axisDirection.x * transform.right;
            Vector3 vertical = axisDirection.y * transform.up;
            movementDirection = horizontal + vertical;
            _rigidbody.AddForce(movementDirection * _speed * Time.deltaTime);
        }
    }
    private void Sprint(bool isSprint)
    {
        if (isSprint)
        {
            if (_speed < _sprintSpeed)
            {
                _speed = _speed + _walkSprintTransition * Time.deltaTime;
            }
        }
        else
        {
            if (_speed > _walkSpeed)
            {
                _speed = _speed - _walkSprintTransition * Time.deltaTime;
            }
        }
    }
    private void Jump()
    {
        if (_isGrounded)
        {
            Vector3 jumpDirection = Vector3.up;
            _rigidbody.AddForce(jumpDirection * _jumpForce * Time.deltaTime);
            _animator.SetTrigger("Jump");
        }
    }
    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
        _animator.SetBool("IsGrounded", _isGrounded);
    }
    private void StartClimb()
    {
        bool isFrontOfClimbingWall = Physics.Raycast(_climbDetector.position, transform.forward, out RaycastHit hit, _climbCheckDistance, _climbableLayer);
        bool isNotClimbing = _playerstances != PlayerStances.Climb;
        if (isFrontOfClimbingWall && _isGrounded && isNotClimbing)
        {
            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            transform.position = hit.point - offset;
            _playerstances = PlayerStances.Climb;
            _rigidbody.useGravity = false;
            _speed = _climbSpeed;
            _CameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            _CameraManager.SetTPSFieldOfView(70);
        }
    }
    private void CancelClimb()
    {
        if (_playerstances == PlayerStances.Climb)
        {
            _playerstances = PlayerStances.Stand;
            _rigidbody.useGravity = true;
            transform.position -= transform.forward;
            _speed = _walkSpeed;
            _CameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
            _CameraManager.SetTPSFieldOfView(40);
        }
    }
    private void ChangePerspective()
    {
        _animator.SetTrigger("ChangePerspective");
    }
}
