using CrateExpectations.Core.Input;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Player
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerMovementDefinition _definition;
        [SerializeField] private Transform _cameraPivot;
        [SerializeField] private Transform _groundCheckOrigin;

        private readonly Collider[] _groundHits = new Collider[8];

        private readonly Vector3[] _wallNormals = new Vector3[8];
        private int _wallNormalCount;

        private Rigidbody _rigidbody;
        private IInputReader _input;

        private float _yaw;
        private float _pitch;
        private bool _jumpRequested;

        [Inject]
        public void Construct(IInputReader input) => _input = input;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            _rigidbody.constraints = RigidbodyConstraints.FreezeRotation;

            _yaw = transform.eulerAngles.y;
        }

        private void Start() => _input.Jump += OnJumpPressed;

        private void OnDestroy()
        {
            if (_input != null) 
                _input.Jump -= OnJumpPressed;
        }

        private void OnJumpPressed() => _jumpRequested = true;

        private void Update()
        {
            Vector2 look = _input.LookInput;
            _yaw += look.x * _definition.LookSensitivity;
            _pitch -= look.y * _definition.LookSensitivity;
            _pitch = Mathf.Clamp(_pitch, _definition.PitchMin, _definition.PitchMax);
        }

        private void FixedUpdate()
        {
            Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
            _rigidbody.MoveRotation(yawRotation);

            Vector2 move = _input.MoveInput;
            Vector3 wishDir = yawRotation * new Vector3(move.x, 0f, move.y);

            if (wishDir.sqrMagnitude > 1f) 
                wishDir.Normalize();

            Vector3 targetVelocity = wishDir * _definition.MoveSpeed;
            ClipToWalls(ref targetVelocity);

            Vector3 velocity = _rigidbody.linearVelocity;
            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            horizontal = Vector3.MoveTowards(
                horizontal, targetVelocity, _definition.Acceleration * Time.fixedDeltaTime);

            ClipToWalls(ref horizontal);

            float verticalVelocity = velocity.y;

            if (_jumpRequested)
            {
                _jumpRequested = false;

                if (IsGrounded())
                    verticalVelocity = Mathf.Sqrt(2f * -Physics.gravity.y * _definition.JumpHeight);
            }

            _rigidbody.linearVelocity = new Vector3(horizontal.x, verticalVelocity, horizontal.z);

            _wallNormalCount = 0;
        }

        private void OnCollisionEnter(Collision collision) => CollectWallNormals(collision);

        private void OnCollisionStay(Collision collision) => CollectWallNormals(collision);

        private void CollectWallNormals(Collision collision)
        {
            Rigidbody other = collision.rigidbody;

            if (other != null && !other.isKinematic) 
                return;

            int contactCount = collision.contactCount;

            for (int i = 0; i < contactCount; i++)
            {
                if (_wallNormalCount == _wallNormals.Length) 
                    return;

                Vector3 normal = collision.GetContact(i).normal;

                if (Mathf.Abs(normal.y) > _definition.WallNormalMaxY) 
                    continue;

                Vector3 flat = new Vector3(normal.x, 0f, normal.z);

                if (flat.sqrMagnitude < 0.0001f) 
                    continue;

                flat.Normalize();

                if (IsKnownWallNormal(flat))
                    continue;

                _wallNormals[_wallNormalCount++] = flat;
            }
        }

        private bool IsKnownWallNormal(Vector3 normal)
        {
            for (int i = 0; i < _wallNormalCount; i++)
                if (Vector3.Dot(_wallNormals[i], normal) > 0.99f) 
                    return true;

            return false;
        }


        private void ClipToWalls(ref Vector3 velocity)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                for (int i = 0; i < _wallNormalCount; i++)
                {
                    float into = Vector3.Dot(velocity, _wallNormals[i]);

                    if (into < 0f) 
                        velocity -= _wallNormals[i] * into;
                }
            }
        }

        private void LateUpdate()
        {
            _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private bool IsGrounded()
        {
            Vector3 origin = _groundCheckOrigin.position + Vector3.down * _definition.GroundCheckDistance;
            int hitCount = Physics.OverlapSphereNonAlloc(
                origin, _definition.GroundCheckRadius, _groundHits,
                _definition.GroundMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
                if (!_groundHits[i].transform.IsChildOf(transform))
                    return true;

            return false;
        }
    }
}
