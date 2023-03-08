using Oculus.Interaction.HandGrab;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

[System.Serializable]
public class VrControlSettings
{
    public enum LookDevice { hands, headAbsolute, headRelative }
    public enum Progress { smooth, stepped }

    public LookDevice lookDevice;
    public Progress lookProgress;

    public Progress moveProgress;
}

public class Player : MonoBehaviour
{
    public VrControlSettings controlSettings = new();

    [SerializeField]
    [Tooltip("Not a hard cap, external forces can cause greater velocity")]
    private float maxSpeed;

    [SerializeField]
    [Tooltip("how long it takes to accelerate & decelerate")]
    [Range(0f, 1f)]
    private float moveDamping;

    [SerializeField]
    private float lookSpeed;
    [SerializeField]
    [Tooltip("maximum angle in degrees between body and recentering hand")]
    private float lookDeadZone;

    private Vector2 moveInput;
    private Vector3 wishVel = Vector3.zero;
    private Vector3 velocity = Vector3.zero;

    private bool bodyRotation = false;
    // in local space
    private Vector3 neutralAimDirection;
    // used for hand-based camera controls only (see VrControlSettings)
    private GameObject cameraAimDevice = null;

    [SerializeField]
    private Camera head;
    [SerializeField]
    private Hand handLeft;
    [SerializeField]
    private Hand handRight;

    #region MonoBehavior callbacks

    private void Awake()
    {
        if (handLeft == null)
            Debug.LogWarning("Player: no left hand found in children");
        if (handRight == null)
            Debug.LogWarning("Player: no right hand found in children");

        // lookDeadZone = Vector3.Dot(Vector3.forward, Quaternion.AngleAxis(lookDeadZone, Vector3.up) * Vector3.forward);
    }

    private void Update()
    {
        // convert input to a desired velocity in world space
        wishVel = transform.forward * moveInput.y + transform.right * moveInput.x;
        wishVel = wishVel.normalized * maxSpeed;
        // smooth movement
        velocity = Vector3.Lerp(velocity, wishVel, 1-Mathf.Pow(moveDamping, Time.deltaTime));
        // snap to target velocity when close
        if ((wishVel - velocity).sqrMagnitude <= 0.1)
            velocity = wishVel;
        // move
        transform.position += velocity * Time.deltaTime;

        if (bodyRotation)
        {
            // rotation is controlled by how far from the reference point the head has rotated
            float rotationDefecit = YRotationDefecit(neutralAimDirection, transform.InverseTransformDirection(cameraAimDevice.transform.forward));
            if (Mathf.Abs(rotationDefecit) > lookDeadZone)
            {
                // rotation speed scales with how far from the reference point the player is looking
                float rotationSpeed = Mathf.InverseLerp(lookDeadZone, lookDeadZone+20, Mathf.Abs(rotationDefecit)) * lookSpeed;
                rotationSpeed *= Mathf.Sign(rotationDefecit);
                transform.Rotate(0, rotationSpeed*Time.deltaTime, 0, Space.World);
            }
        }
    }

    #endregion

    #region Input messages

    /// only used for PC debugging. Acts as a standard FPS camera.
    private void OnDebuglook(InputValue value)
    {
        const float hSens = 0.25f;
        const float vSens = 0.25f;
        Vector2 lookDelta = value.Get<Vector2>();

        head.transform.Rotate(-lookDelta.y * vSens, 0, 0, Space.Self);
        head.transform.Rotate(0, lookDelta.x * hSens, 0, Space.World);
        handLeft.transform.rotation = head.transform.rotation;
        handRight.transform.rotation = head.transform.rotation;
    }

    private void OnLookleft(InputValue value)
    {
        OnLookAny(value, handLeft);
    }

    private void OnLookright(InputValue value)
    {
        OnLookAny(value, handRight);
    }

    /// VR: button to enable camera movement
    private void OnLookAny(InputValue value, Hand hand)
    {
        // enable body rotation in update
        bodyRotation = value.Get<float>() > 0.5f;

        // set the device controlling the rotation
        if (bodyRotation)
        {
            cameraAimDevice =
                controlSettings.lookDevice == VrControlSettings.LookDevice.hands ?
                hand.gameObject : head.gameObject;
        }
        else
        {
            cameraAimDevice = null;
        }

        if (bodyRotation)
        {
            if (controlSettings.lookDevice == VrControlSettings.LookDevice.headAbsolute)
                // camera rotates based on current "body" direction
                neutralAimDirection = Vector3.forward;
            else
                // camera rotates based on initial look direction
                neutralAimDirection = transform.InverseTransformDirection(head.transform.forward);
        }
    }

    private void OnGrableft(InputValue value)
    {
        OnGrabAny(value, handLeft);
    }

    private void OnGrabright(InputValue value)
    {
        OnGrabAny(value, handRight);
    }

    private void OnGrabAny(InputValue value, Hand hand)
    {
        if (value.Get<float>() > 0.5)
            hand.StartGrab();
        else
            hand.EndGrab();
    }

    private void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    private void OnTeleportleft(InputValue value)
    {
        OnTeleportAny(value, handLeft);
    }

    private void OnTeleportright(InputValue value)
    {
        OnTeleportAny(value, handRight);
    }
    public void OnTeleportAny(InputValue value, Hand hand)
    {
        if (value.Get<float>() >= 0.5f)
            hand.StartTeleport();
        else if (hand.CanTeleport)
            TeleportTo(hand.CommitTeleport());
        else
            hand.CancelTeleport();
    }

    #endregion

    #region Custom methods

    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
    }

    public float YRotationDefecit(Vector3 current, Vector3 target)
    {
        return Vector3.SignedAngle(new Vector3(current.x, 0, current.z), new Vector3(target.x, 0, target.z), Vector3.up);
    }

    #endregion
}
