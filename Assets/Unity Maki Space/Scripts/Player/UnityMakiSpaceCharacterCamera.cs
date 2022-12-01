using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Unity_Maki_Space.Scripts.Player
{
    public class UnityMakiSpaceCharacterCamera : MonoBehaviour
    {
        [Header("Framing")] public Vector2 followPointFraming = new(0f, 0f);
        public float followingSharpness = 10000f;

        [Header("Distance")] public float defaultDistance = 6f;
        public float minDistance;
        public float maxDistance = 10f;
        public float distanceMovementSpeed = 5f;
        public float distanceMovementSharpness = 10f;

        [Header("Rotation")] public bool invertX;
        public bool invertY;
        [Range(-90f, 90f)] public float defaultVerticalAngle = 20f;
        [Range(-90f, 90f)] public float minVerticalAngle = -90f;
        [Range(-90f, 90f)] public float maxVerticalAngle = 90f;
        public float rotationSpeed = 1f;

        public float rotationSharpness = 10000f;
        // public bool rotateWithPhysicsMover = false;

        [Header("Obstruction")] public float obstructionCheckRadius = 0.2f;
        public LayerMask obstructionLayers = -1;
        public float obstructionSharpness = 10000f;
        public List<Collider> ignoredColliders = new();

        public Transform Transform { get; private set; }
        public Transform FollowTransform { get; private set; }

        public Vector3 PlanarDirection { get; set; }
        public float TargetDistance { get; set; }

        private bool _distanceIsObstructed;
        private float _currentDistance;
        private float _targetVerticalAngle;
        private RaycastHit _obstructionHit;
        private int _obstructionCount;
        private readonly RaycastHit[] _obstructions = new RaycastHit[MaxObstructions];
        private float _obstructionTime;
        private Vector3 _currentFollowPosition;

        private const int MaxObstructions = 32;

        private void OnValidate()
        {
            defaultDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
            defaultVerticalAngle = Mathf.Clamp(defaultVerticalAngle, minVerticalAngle, maxVerticalAngle);
        }

        private void Awake()
        {
            Transform = transform;

            _currentDistance = defaultDistance;
            TargetDistance = _currentDistance;

            _targetVerticalAngle = 0f;

            PlanarDirection = Vector3.forward;
        }

        // Set the transform that the camera will orbit around
        public void SetFollowTransform(Transform t)
        {
            FollowTransform = t;
            PlanarDirection = FollowTransform.forward;
            _currentFollowPosition = FollowTransform.position;
        }

        public void UpdateWithInput(float deltaTime, float zoomInput, Vector3 rotationInput)
        {
            if (!FollowTransform) return;

            if (invertX) rotationInput.x *= -1f;
            if (invertY) rotationInput.y *= -1f;

            // Process rotation input
            var followUp = FollowTransform.up;
            var rotationFromInput = Quaternion.Euler(followUp * (rotationInput.x * rotationSpeed));
            PlanarDirection = rotationFromInput * PlanarDirection;
            PlanarDirection = Vector3.Cross(followUp, Vector3.Cross(PlanarDirection, followUp));
            var planarRot = Quaternion.LookRotation(PlanarDirection, followUp);

            _targetVerticalAngle -= (rotationInput.y * rotationSpeed);
            _targetVerticalAngle = Mathf.Clamp(_targetVerticalAngle, minVerticalAngle, maxVerticalAngle);
            var verticalRot = Quaternion.Euler(_targetVerticalAngle, 0, 0);
            var targetRotation = Quaternion.Slerp(Transform.rotation, planarRot * verticalRot,
                1f - Mathf.Exp(-rotationSharpness * deltaTime));

            // Apply rotation
            Transform.rotation = targetRotation;

            // Process distance input
            if (_distanceIsObstructed && Mathf.Abs(zoomInput) > 0f)
            {
                TargetDistance = _currentDistance;
            }

            TargetDistance += zoomInput * distanceMovementSpeed;
            TargetDistance = Mathf.Clamp(TargetDistance, minDistance, maxDistance);

            // Find the smoothed follow position
            _currentFollowPosition = Vector3.Lerp(_currentFollowPosition, FollowTransform.position,
                1f - Mathf.Exp(-followingSharpness * deltaTime));

            // Handle obstructions
            {
                var closestHit = new RaycastHit
                {
                    distance = Mathf.Infinity
                };
                _obstructionCount = Physics.SphereCastNonAlloc(_currentFollowPosition, obstructionCheckRadius,
                    -Transform.forward, _obstructions, TargetDistance, obstructionLayers,
                    QueryTriggerInteraction.Ignore);
                for (var i = 0; i < _obstructionCount; i++)
                {
                    var isIgnored = ignoredColliders.Any(t => t == _obstructions[i].collider);

                    if (!isIgnored && _obstructions[i].distance < closestHit.distance && _obstructions[i].distance > 0)
                    {
                        closestHit = _obstructions[i];
                    }
                }

                // If obstructions detector
                if (closestHit.distance < Mathf.Infinity)
                {
                    _distanceIsObstructed = true;
                    _currentDistance = Mathf.Lerp(_currentDistance, closestHit.distance,
                        1 - Mathf.Exp(-obstructionSharpness * deltaTime));
                }
                // If no obstruction
                else
                {
                    _distanceIsObstructed = false;
                    _currentDistance = Mathf.Lerp(_currentDistance, TargetDistance,
                        1 - Mathf.Exp(-distanceMovementSharpness * deltaTime));
                }
            }

            // Find the smoothed camera orbit position
            var targetPosition = _currentFollowPosition - ((targetRotation * Vector3.forward) * _currentDistance);

            // Handle framing
            targetPosition += Transform.right * followPointFraming.x;
            targetPosition += Transform.up * followPointFraming.y;

            // Apply position
            Transform.position = targetPosition;
        }
    }
}