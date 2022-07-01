using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private InputActions inputActions;

    private new Camera camera;
    private float cameraPitch;

    private new Rigidbody rigidbody;

    public ChunkSystem chunkSystem;

    private bool highlightingBlock;
    private Vector3Int placePosition;
    private Vector3Int destroyPosition;
    public GameObject selectionCube;

    public GameObject breakBlockParticlesPrefab;
    private List<ParticleSystem> aliveParticleSystems = new();

    private void Awake()
    {
        camera = GetComponentInChildren<Camera>();

        rigidbody = GetComponent<Rigidbody>();
        rigidbody.freezeRotation = true;

        inputActions = new InputActions();
        inputActions.Player.Enable();

        inputActions.Player.Move.Enable();

        inputActions.Player.Look.performed += OnLook;
        inputActions.Player.Look.Enable();

        inputActions.Player.Jump.performed += OnJump;
        inputActions.Player.Jump.Enable();

        inputActions.Player.Unfocus.performed += OnUnfocus;
        inputActions.Player.Unfocus.Enable();

        inputActions.Player.Place.performed += OnPlace;
        inputActions.Player.Place.Enable();

        inputActions.Player.Break.performed += OnBreak;
        inputActions.Player.Break.Enable();
    }

    private void FixedUpdate()
    {
        var ray = new Ray(camera.transform.position, camera.transform.forward);
        Physics.Raycast(ray, out var hitData, 4.5f, LayerMask.GetMask("Chunk"));

        highlightingBlock = hitData.distance != 0;

        if (highlightingBlock)
        {
            var rawPlacePosition = hitData.point + (hitData.normal * 0.5f);
            placePosition = new Vector3Int(Mathf.FloorToInt(rawPlacePosition.x + 0.5f),
                Mathf.FloorToInt(rawPlacePosition.y + 0.5f),
                Mathf.FloorToInt(rawPlacePosition.z + 0.5f));

            var rawDestroyPosition = hitData.point + (hitData.normal * -0.5f);
            destroyPosition = new Vector3Int(Mathf.FloorToInt(rawDestroyPosition.x + 0.5f),
                Mathf.FloorToInt(rawDestroyPosition.y + 0.5f),
                Mathf.FloorToInt(rawDestroyPosition.z + 0.5f));

            selectionCube.transform.position = destroyPosition;
        }

        selectionCube.SetActive(highlightingBlock);
        
        // everything onward is only when mouse locked

        if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked) return;
        
        var moveXy = inputActions.Player.Move.ReadValue<Vector2>();
        if (moveXy != Vector2.zero)
        {
            var positionOffset = Quaternion.Euler(0, transform.localEulerAngles.y, 0) *
                                 new Vector3(moveXy.x, 0, moveXy.y);

            rigidbody.MovePosition(transform.position + positionOffset * 0.1f);
        }
    }

    private void Update()
    {
        for (var i = aliveParticleSystems.Count - 1; i >= 0; i--)
        {
            if (aliveParticleSystems[i].IsAlive()) continue;
            Destroy(aliveParticleSystems[i].gameObject);
            aliveParticleSystems.RemoveAt(i);
        }
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked) return;
        
        const float sensitivity = 0.125f;

        var lookDelta = context.ReadValue<Vector2>();
        transform.localEulerAngles += new Vector3(0, lookDelta.x * sensitivity, 0);

        cameraPitch = Mathf.Clamp(cameraPitch + -lookDelta.y * sensitivity, -90, 90);
        camera.transform.localEulerAngles = new Vector3(cameraPitch, camera.transform.localEulerAngles.y,
            camera.transform.localEulerAngles.z);
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        // move ray up slightly and check within -0.2 to 0.2
        var ray = new Ray(transform.position + new Vector3(0, 0.2f, 0), Vector3.down);
        Physics.Raycast(ray, out var hitData, 0.4f, LayerMask.GetMask("Chunk"));

        if (hitData.distance != 0)
        {
            rigidbody.AddForce(Vector3.up * 700f);
        }
    }

    private static void OnUnfocus(InputAction.CallbackContext context)
    {
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnBreak(InputAction.CallbackContext context)
    {
        if (!Application.isFocused) return;
        
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            return;
        }

        if (!highlightingBlock) return;

        chunkSystem.SetBlock(destroyPosition, DataTypes.Block.Air);
        
        var particlesGameObject = Instantiate(breakBlockParticlesPrefab, destroyPosition, Quaternion.identity);
        aliveParticleSystems.Add(particlesGameObject.GetComponent<ParticleSystem>());
    }
    
    private void OnPlace(InputAction.CallbackContext context)
    {
        if (!Application.isFocused || Cursor.lockState != CursorLockMode.Locked) return;

        if (!highlightingBlock) return;

        chunkSystem.SetBlock(placePosition, DataTypes.Block.Grass);
    }
}