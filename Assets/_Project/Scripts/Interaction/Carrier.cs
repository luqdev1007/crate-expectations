using CrateExpectations.Core.Input;
using UnityEngine;
using VContainer;

namespace CrateExpectations.Interaction
{
    public sealed class Carrier : MonoBehaviour
    {
        [Tooltip("Числа захвата/переноса/броска")]
        [SerializeField] private CarryDefinition _definition;

        [Tooltip("Начало луча захвата и опора точки удержания (обычно камера)")]
        [SerializeField] private Transform _rayOrigin;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private IInputReader _input;
        private Carriable _held;
        private Rigidbody _heldBody;
        private Rigidbody _anchor;
        private ConfigurableJoint _joint;

        private HoldMode _activeMode;

        private float _heldRadius;

        private CollisionDetectionMode _previousCollisionMode;
        private RigidbodyInterpolation _previousInterpolation;
        private float _previousAngularDamping;

        public bool IsCarrying => _held != null;

        public Carriable Held => _held;

        [Inject]
        public void Construct(IInputReader input) => _input = input;

        private void Start()
        {
            _input.Grab += OnGrabPressed;
            _input.Throw += OnThrowPressed;

            ValidateCarriedLayer();
        }

        private void ValidateCarriedLayer()
        {
            int carried = _definition.CarriedLayer;

            if (carried < 0 || carried > 31)
            {
                Debug.LogError($"CarryDefinition.CarriedLayer = {carried} - это не индекс слоя.", this);
                return;
            }

            int playerLayer = gameObject.layer;

            if (!Physics.GetIgnoreLayerCollision(carried, playerLayer))
            {
                Debug.LogError(
                    $"Слой переноса '{LayerMask.LayerToName(carried)}' не исключён из столкновений " +
                    $"со слоем игрока '{LayerMask.LayerToName(playerLayer)}'. Гружёный объект будет " +
                    "толкать игрока - поправь матрицу в Project Settings → Physics.", this);
            }
        }

        private void OnDestroy()
        {
            if (_input != null)
            {
                _input.Grab -= OnGrabPressed;
                _input.Throw -= OnThrowPressed;
            }

            if (_anchor != null) 
                Destroy(_anchor.gameObject);
        }

        private void OnGrabPressed()
        {
            if (IsCarrying) 
                Release(Vector3.zero);
            else 
                TryGrab();
        }

        private void OnThrowPressed()
        {
            if (!IsCarrying) 
                return;

            Release(_rayOrigin.forward * _definition.ThrowForce);
        }

        private void FixedUpdate()
        {
            if (!IsCarrying) 
                return;

            Vector3 holdPoint = GetHoldPoint();
            Vector3 toTarget = holdPoint - _heldBody.position;

            if (toTarget.sqrMagnitude > _definition.BreakDistance * _definition.BreakDistance)
            {
                Release(Vector3.zero);
                return;
            }

            if (_activeMode == HoldMode.ConfigurableJoint)
            {
                _anchor.MovePosition(holdPoint);
                _anchor.MoveRotation(_rayOrigin.rotation);
                return;
            }

            Vector3 velocity = toTarget * _definition.FollowSpeed;
            float maxVelocity = _definition.MaxVelocity;

            if (velocity.sqrMagnitude > maxVelocity * maxVelocity)
                velocity = velocity.normalized * maxVelocity;

            _heldBody.linearVelocity = velocity;
        }

        private void TryGrab()
        {
            var ray = new Ray(_rayOrigin.position, _rayOrigin.forward);
            int count = Physics.RaycastNonAlloc(
                ray, _hits, _definition.GrabDistance,
                _definition.CarriableMask, QueryTriggerInteraction.Ignore);

            Carriable nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                if (_hits[i].distance >= nearestDistance) 
                    continue;

                Carriable candidate;

                if (!TryResolveCarriable(_hits[i].collider, out candidate)) 
                    continue;

                nearest = candidate;
                nearestDistance = _hits[i].distance;
            }

            if (nearest != null) 
                Attach(nearest);
        }

        private static bool TryResolveCarriable(Collider hit, out Carriable carriable)
        {
            if (hit.TryGetComponent(out carriable)) 
                return true;

            carriable = hit.GetComponentInParent<Carriable>();

            return carriable != null;
        }

        private Vector3 GetHoldPoint()
        {
            Vector3 origin = _rayOrigin.position;
            Vector3 direction = _rayOrigin.forward;
            float distance = _definition.HoldDistance;

            RaycastHit hit;

            if (_heldRadius > 0f && Physics.SphereCast(
                    origin, _heldRadius, direction, out hit, distance,
                    _definition.HoldBlockingMask, QueryTriggerInteraction.Ignore))
            {
                distance = Mathf.Max(hit.distance, _definition.MinHoldDistance);
            }

            return origin + direction * distance;
        }

        private static float MeasureRadius(Carriable carriable)
        {
            Collider[] colliders = carriable.GetComponentsInChildren<Collider>();

            if (colliders.Length == 0) 
                return 0f;

            Bounds bounds = colliders[0].bounds;

            for (int i = 1; i < colliders.Length; i++) 
                bounds.Encapsulate(colliders[i].bounds);

            Vector3 extents = bounds.extents;

            return Mathf.Min(extents.x, Mathf.Min(extents.y, extents.z));
        }

        private void Attach(Carriable carriable)
        {
            _held = carriable;
            _heldBody = carriable.Rigidbody;
            _activeMode = _definition.HoldMode;
            _heldRadius = MeasureRadius(carriable);

            _previousCollisionMode = _heldBody.collisionDetectionMode;
            _previousInterpolation = _heldBody.interpolation;
            _previousAngularDamping = _heldBody.angularDamping;

            _heldBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _heldBody.interpolation = RigidbodyInterpolation.Interpolate;
            _heldBody.angularVelocity = Vector3.zero;
            _heldBody.angularDamping = _definition.CarriedAngularDamping;

            carriable.OverrideLayers(_definition.CarriedLayer);
            carriable.MarkCarried(true);

            if (_activeMode == HoldMode.ConfigurableJoint) 
                AttachJoint();
        }

        private void Release(Vector3 impulse)
        {
            if (_joint != null)
            {
                Destroy(_joint);
                _joint = null;
            }

            _held.RestoreLayers();
            _held.MarkCarried(false);

            _heldBody.collisionDetectionMode = _previousCollisionMode;
            _heldBody.interpolation = _previousInterpolation;
            _heldBody.angularDamping = _previousAngularDamping;

            if (impulse != Vector3.zero) 
                _heldBody.AddForce(impulse, ForceMode.Impulse);

            _held = null;
            _heldBody = null;
        }

        private void AttachJoint()
        {
            EnsureAnchor();
            _anchor.transform.SetPositionAndRotation(_heldBody.position, _rayOrigin.rotation);

            var drive = new JointDrive
            {
                positionSpring = _definition.JointSpring,
                positionDamper = _definition.JointDamper,
                maximumForce = float.MaxValue,
            };

            _joint = _heldBody.gameObject.AddComponent<ConfigurableJoint>();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.connectedBody = _anchor;
            _joint.anchor = Vector3.zero;
            _joint.connectedAnchor = Vector3.zero;

            _joint.xMotion = ConfigurableJointMotion.Free;
            _joint.yMotion = ConfigurableJointMotion.Free;
            _joint.zMotion = ConfigurableJointMotion.Free;
            _joint.angularXMotion = ConfigurableJointMotion.Free;
            _joint.angularYMotion = ConfigurableJointMotion.Free;
            _joint.angularZMotion = ConfigurableJointMotion.Free;

            _joint.xDrive = drive;
            _joint.yDrive = drive;
            _joint.zDrive = drive;
            _joint.rotationDriveMode = RotationDriveMode.Slerp;
            _joint.slerpDrive = drive;
            _joint.targetRotation = Quaternion.identity;
        }

        private void EnsureAnchor()
        {
            if (_anchor != null) 
                return;

            var anchorObject = new GameObject($"{name}_CarryAnchor");
            _anchor = anchorObject.AddComponent<Rigidbody>();
            _anchor.isKinematic = true;
            _anchor.useGravity = false;
        }
    }
}
