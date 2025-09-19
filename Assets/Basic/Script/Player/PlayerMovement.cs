using UnityEngine;
using System.Collections;

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
    [SerializeField]
    private float _crouchSpeed;
    [SerializeField]
    private CapsuleCollider _collider;
    [SerializeField]
    private float _glideSpeed;
    [SerializeField]
    private float _airDrag;
    [SerializeField]
    private Vector3 _glideRotationSpeed;
    [SerializeField]
    private float _minGlideRotationX;
    [SerializeField]
    private float _maxGlideRotationX;
    private bool _isPunching;
    private int _combo = 0;
    [SerializeField]
    private float _reserComboInterval;
    private Coroutine _resetCombo;
    [SerializeField]
    private Transform _hitDetector;
    [SerializeField]
    private float _hitDetectorRadius;
    [SerializeField]
    private LayerMask _hitLayer;
    [SerializeField]
    private PlayerAudioManager _playerAudioManager;



    private void CheckStep()
    {
        bool isHitLowerStep = Physics.Raycast(_groundDetector.position,transform.forward,_stepCheckerDistance);
        bool isHitUpperStep = Physics.Raycast(_groundDetector.position +_upperStepOffset,transform.forward,_stepCheckerDistance);
        if (isHitLowerStep)
        {
            if (isHitLowerStep)
            {
                if (!isHitUpperStep)
                {
                    _rigidbody.AddForce(0, _stepForce, 0);
                }
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
        _collider = GetComponent<CapsuleCollider>();
    }
    private void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimb += CancelClimb;
        _CameraManager.OnChangePerspective += ChangePerspective;
        _input.OnCrouchInput += Crouch;
        _input.OnGlideInput += StartGlide;
        _input.OnCancelGlide += CancelGlide;
        _input.OnPunchInput += Punch;
    }
    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpInput -= Jump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelClimb -= CancelClimb;
        _CameraManager.OnChangePerspective -= ChangePerspective;
        _input.OnCrouchInput-= Crouch;
        _input.OnGlideInput -= Glide;
        _input.OnCancelGlide -= CancelGlide;
        _input.OnPunchInput -= Punch;

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
        bool isPlayerCrouch = _playerstances == PlayerStances.Crouch;
        bool isPlayerGliding = _playerstances == PlayerStances.Glide;
        if ((isPlayerStanding || isPlayerCrouch) && !_isPunching)
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
        else if (isPlayerClimbing)
        {
            Vector3 horizontal = Vector3.zero;
            Vector3 vertical = Vector3.zero;
            Vector3 checkerLeftPosition = transform.position + (transform.up * 1) + (-transform.right * .75f);
            Vector3 checkerRightPosition = transform.position + (transform.up * 1) + (transform.right * 1f);
            Vector3 checkerUpPosition = transform.position + (transform.up * 2.5f);
            Vector3 checkerDownPosition = transform.position + (-transform.up * .25f);
            bool isAbleClimbLeft = Physics.Raycast(checkerLeftPosition, transform.forward, _climbCheckDistance, _climbableLayer);
            bool isAbleClimbRight = Physics.Raycast(checkerRightPosition, transform.forward, _climbCheckDistance, _climbableLayer);
            bool isAbleClimbUp = Physics.Raycast(checkerUpPosition, transform.forward, _climbCheckDistance, _climbableLayer);
            bool isAbleClimbDown = Physics.Raycast(checkerDownPosition, transform.forward, _climbCheckDistance, _climbableLayer);

            if ((isAbleClimbLeft && (axisDirection.x < 0)) || (isAbleClimbRight && (axisDirection.x > 0)))
            {
                horizontal = axisDirection.x * transform.right;
            }

            if ((isAbleClimbUp && (axisDirection.y > 0)) || (isAbleClimbDown && (axisDirection.y < 0)))
            {
                vertical = axisDirection.y * transform.up;
            }

            movementDirection = horizontal + vertical;
            _rigidbody.AddForce(movementDirection * Time.deltaTime * _climbSpeed);
            Vector3 velocity = new Vector3(_rigidbody.velocity.x, _rigidbody.velocity.y, 0);
            _animator.SetFloat("ClimbVelocityY", velocity.magnitude * axisDirection.y);
            _animator.SetFloat("ClimbVelocityX", velocity.magnitude * axisDirection.x);

}else if(isPlayerGliding){
            Vector3 rotationDegree = transform.rotation.eulerAngles;
            rotationDegree.x += _glideRotationSpeed.x * axisDirection.y * Time.deltaTime;
            rotationDegree.x = Mathf.Clamp(rotationDegree.x, _minGlideRotationX, _maxGlideRotationX);
            rotationDegree.z += _glideRotationSpeed.z * axisDirection.x * Time.deltaTime;
            transform.rotation = Quaternion.Euler(rotationDegree);

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
        if (_isGrounded && !_isPunching)
        {
            Vector3 jumpDirection = Vector3.up;
            _rigidbody.AddForce(jumpDirection * _jumpForce * Time.deltaTime);
            _animator.SetBool("IsJump", true);
            _animator.SetBool("IsJump", false);
        }
    }
    private void CheckIsGrounded()
    {
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _detectorRadius, _groundLayer);
        _animator.SetBool("IsGrounded", _isGrounded);
        if (_isGrounded)
        {
            CancelGlide();
        }
    }
    private void StartClimb()
    {
        bool isInFrontOfClimbingWall = Physics.Raycast(_climbDetector.position, transform.forward, out RaycastHit hit, _climbCheckDistance, _climbableLayer);
        bool isNotClimbing = _playerstances != PlayerStances.Climb;

        if (isInFrontOfClimbingWall && _isGrounded && isNotClimbing)
        {
            _CameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            Vector3 climbablePoint = hit.collider.bounds.ClosestPoint(transform.position);
            Vector3 direction = (climbablePoint - transform.position).normalized;
            direction.y = 0;
            transform.rotation = Quaternion.LookRotation(direction);
            Vector3 offset = (transform.forward * _climbOffset.z) - (Vector3.up * _climbOffset.y);
            transform.position = hit.point - offset;
            _playerstances = PlayerStances.Climb;
            _animator.SetBool("IsClimbing", true);
            _rigidbody.useGravity = false;
            _CameraManager.SetTPSFieldOfView(70);
        }
    }

    private void CancelClimb()
    {
        if (_playerstances == PlayerStances.Climb)
        {
            _collider.center = Vector3.up * 0.9f;
            _playerstances = PlayerStances.Stand;
            _rigidbody.useGravity = true;
            transform.position -= transform.forward;
            _speed = _walkSpeed;
            _CameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
            _CameraManager.SetTPSFieldOfView(40);
            _animator.SetBool("IsClimbing", false);
        }
    }
    private void ChangePerspective()
    {
        _animator.SetTrigger("ChangePerspective");
    }
    private void Crouch()
    {
        Vector3 checkerUpPosition = transform.position + (transform.up * 1.4f);
        bool isCantStand = Physics.Raycast(checkerUpPosition, transform.up, 0.25f, _groundLayer);
        if (_playerstances == PlayerStances.Stand)
        {
            _playerstances = PlayerStances.Crouch;
            _animator.SetBool("IsCrouch", true);
            _speed = _crouchSpeed;
            _collider.height = 1.3f;
            _collider.center = Vector3.up * 0.66f;
        }
        else if(_playerstances == PlayerStances.Crouch && !isCantStand)
        {
            _playerstances= PlayerStances.Stand;
            _animator.SetBool("IsCrouch", false);
            _speed = _walkSpeed;
            _collider.height = 1.8f;
            _collider.center = Vector3.up * 0.9f;
        }
    }
    private void Glide()
    {
        if(_playerstances == PlayerStances.Glide)
        {
            Vector3 playerRotation = transform.rotation.eulerAngles;
            float lift = playerRotation.x;
            Vector3 upForce = transform.up * (lift * _airDrag);
            Vector3 forwardForce = transform.forward * _glideSpeed;
            Vector3 totalForce = upForce + forwardForce;
            _rigidbody.AddForce(totalForce * Time.deltaTime);
        }
    }
    public void StartGlide()
    {
        if(_playerstances!= PlayerStances.Glide && !_isGrounded)
        {
            _playerstances = PlayerStances.Glide;
            _animator.SetBool("IsGliding", true);
            _CameraManager.SetFPSClampedCamera(true, transform.rotation.eulerAngles);
            _playerAudioManager.PlayGlideSfx();
        }
    }
    private void CancelGlide()
    {
        if (_playerstances == PlayerStances.Glide)
        {
            _playerstances = PlayerStances.Stand;
            _animator.SetBool("IsGliding", false);
            _CameraManager.SetFPSClampedCamera(false, transform.rotation.eulerAngles);
            _playerAudioManager.StopGlideSfx();
        }
    }
    private void Punch()
    {
        if(!_isPunching && _playerstances == PlayerStances.Stand && _isGrounded)
        {
            _isPunching = true;
            if (_combo < 3)
            {
                _combo = _combo + 1;
            }
            else
            {
                _combo = 1;
            }
            _animator.SetInteger("Combo", _combo);
            _animator.SetTrigger("Punch");
        }
    }
    private void EndPunch()
    {
        _isPunching = false;
        if (_resetCombo != null)
        {
            StopCoroutine(_resetCombo);
        }
        _resetCombo = StartCoroutine(ResetCombo());
    }
    private IEnumerator ResetCombo()
    {
        yield return new WaitForSeconds(_reserComboInterval);
        _combo = 0;
    }
    private void Hit()
    {
        Collider[] hitObject = Physics.OverlapSphere(_hitDetector.position, _hitDetectorRadius, _hitLayer);
        for (int i=0; i<hitObject.Length; i++)
        {
            if (hitObject[i].gameObject != null)
            {
                Destroy(hitObject[i].gameObject);
            }
        }
    }
}
