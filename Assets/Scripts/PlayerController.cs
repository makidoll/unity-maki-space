using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private InputActions inputActions;

    private Camera camera;
    private float cameraPitch;

    private Rigidbody rigidbody;

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

        inputActions.Player.Focus.performed += OnFocus;
        inputActions.Player.Focus.Enable();

        inputActions.Player.Unfocus.performed += OnUnfocus;
        inputActions.Player.Unfocus.Enable();
    }

    private void FixedUpdate()
    {
        var moveXy = inputActions.Player.Move.ReadValue<Vector2>();
        if (moveXy != Vector2.zero)
        {
            var positionOffset = Quaternion.Euler(0, transform.localEulerAngles.y, 0) *
                                 new Vector3(moveXy.x, 0, moveXy.y);

            rigidbody.velocity = positionOffset * 10f;
        }
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        const float sensitivity = 0.125f;

        var lookDelta = context.ReadValue<Vector2>();
        transform.localEulerAngles += new Vector3(0, lookDelta.x * sensitivity, 0);

        cameraPitch = Mathf.Clamp(cameraPitch + -lookDelta.y * sensitivity, -90, 90);
        camera.transform.localEulerAngles = new Vector3(cameraPitch, camera.transform.localEulerAngles.y, camera.transform.localEulerAngles.z);
    }

    private static void OnFocus(InputAction.CallbackContext context)
    {
        if (Cursor.lockState == CursorLockMode.Locked) return;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private static void OnUnfocus(InputAction.CallbackContext context)
    {
        Cursor.lockState = CursorLockMode.None;
    }
}