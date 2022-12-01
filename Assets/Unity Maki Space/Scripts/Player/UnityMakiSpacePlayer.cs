using System;
using System.Collections.Generic;
using Unity_Maki_Space.Scripts.Chunks;
using Unity_Maki_Space.Scripts.Managers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity_Maki_Space.Scripts.Player
{
    public class UnityMakiSpacePlayer : MonoBehaviour
    {
        [Header("Controls")] public UnityMakiSpaceCharacterCamera orbitCamera;
        public Transform cameraFollowPoint;
        public UnityMakiSpaceCharacterController character;

        [Header("Visuals")] public GameObject selectionCube;
        public GameObject breakBlockParticlesPrefab;
        private readonly List<ParticleSystem> _aliveParticleSystems = new();

        private Vector3 _lookInputVector = Vector3.zero;

        private UnityMakiSpaceInputActions _inputActions;

        private bool _highlightingBlock;
        private Vector3Int _placePosition;
        private Vector3Int _destroyPosition;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            // Tell camera to follow transform
            orbitCamera.SetFollowTransform(cameraFollowPoint);

            // Ignore the character's collider(s) for camera obstruction checks
            orbitCamera.ignoredColliders.Clear();
            orbitCamera.ignoredColliders.AddRange(character.GetComponentsInChildren<Collider>());

            _inputActions = new UnityMakiSpaceInputActions();
            _inputActions.Enable();
            _inputActions.Player.Enable();
            _inputActions.Player.Move.Enable();
            _inputActions.Player.Look.Enable();
            _inputActions.Player.Unfocus.Enable();
            _inputActions.Player.Jump.Enable();
            _inputActions.Player.Place.performed += OnPlace;
            _inputActions.Player.Place.Enable();
            _inputActions.Player.Break.performed += OnBreak;
            _inputActions.Player.Break.Enable();
        }

        private void Update()
        {
            if (_inputActions.Player.Break.ReadValue<float>() > 0)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (_inputActions.Player.Unfocus.ReadValue<float>() > 0)
            {
                Cursor.lockState = CursorLockMode.None;
            }

            for (var i = _aliveParticleSystems.Count - 1; i >= 0; i--)
            {
                if (_aliveParticleSystems[i].IsAlive()) continue;
                Destroy(_aliveParticleSystems[i].gameObject);
                _aliveParticleSystems.RemoveAt(i);
            }

            HandleCharacterInput();
        }

        private void LateUpdate()
        {
            HandleCameraInput();
        }

        private void FixedUpdate()
        {
            HandleBlockSelection();
        }

        private void HandleCameraInput()
        {
            var look = _inputActions.Player.Look.ReadValue<Vector2>();

            _lookInputVector = new Vector3(look.x, look.y, 0f);
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                _lookInputVector = Vector3.zero;
            }

            orbitCamera.UpdateWithInput(Time.deltaTime, 0, _lookInputVector);
        }

        private void HandleCharacterInput()
        {
            var moveInput = _inputActions.Player.Move.ReadValue<Vector2>();
            var inputs = new PlayerCharacterInputs
            {
                MoveAxisForward = moveInput.y,
                MoveAxisRight = moveInput.x,
                CameraRotation = orbitCamera.transform.rotation,
                JumpDown = _inputActions.Player.Jump.ReadValue<float>() > 0f
            };

            character.SetInputs(inputs);
        }

        private void HandleBlockSelection()
        {
            var ray = new Ray(orbitCamera.transform.position, orbitCamera.transform.forward);
            Physics.Raycast(ray, out var hitData, 4.5f, LayerMask.GetMask("Chunk"));

            _highlightingBlock = hitData.distance != 0;

            if (_highlightingBlock)
            {
                var rawPlacePosition = hitData.point + hitData.normal * 0.5f;
                _placePosition = new Vector3Int(
                    Mathf.FloorToInt(rawPlacePosition.x + 0.5f),
                    Mathf.FloorToInt(rawPlacePosition.y + 0.5f),
                    Mathf.FloorToInt(rawPlacePosition.z + 0.5f)
                );

                var rawDestroyPosition = hitData.point + hitData.normal * -0.5f;
                _destroyPosition = new Vector3Int(
                    Mathf.FloorToInt(rawDestroyPosition.x + 0.5f),
                    Mathf.FloorToInt(rawDestroyPosition.y + 0.5f),
                    Mathf.FloorToInt(rawDestroyPosition.z + 0.5f)
                );

                selectionCube.transform.position = _destroyPosition;
            }

            selectionCube.SetActive(_highlightingBlock);
        }

        private void OnPlace(InputAction.CallbackContext ctx)
        {
            if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked) return;

            if (!_highlightingBlock) return;

            DependencyManager.Instance.ChunkDataManager.SetWorldBlock(_placePosition, DataTypes.Block.Grass);
        }

        private void OnBreak(InputAction.CallbackContext ctx)
        {
            if (!Application.isFocused) return;

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                return;
            }

            if (!_highlightingBlock) return;

            var breakingBlock = DependencyManager.Instance.ChunkDataManager.GetWorldBlock(_destroyPosition);

            // in case the chunk mesh hasn't been updated yet and we're trying to break air
            if (breakingBlock == DataTypes.Block.Air) return;
            
            DependencyManager.Instance.ChunkDataManager.SetWorldBlock(_destroyPosition, DataTypes.Block.Air);

            var particlesGameObject = Instantiate(breakBlockParticlesPrefab, _destroyPosition, Quaternion.identity);
            particlesGameObject.GetComponent<ParticleSystemRenderer>().material =
                DependencyManager.Instance.ChunkMaterialManager.GetBreakParticleMaterial(breakingBlock);

            if (breakingBlock == DataTypes.Block.Grass)
            {
            }

            _aliveParticleSystems.Add(particlesGameObject.GetComponent<ParticleSystem>());
        }
    }
}