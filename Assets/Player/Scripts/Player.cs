using Oculus.Interaction.HandGrab;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.Windows;

public class Player : MonoBehaviour
{
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
    private Vector3 velocity = Vector3.zero;
    private Vector3 wishVel = Vector3.zero;

    private bool bodyRotation = false;
    // in local space
    private Vector3 neutralHeadDirection;

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
            // amt of desired rotation is controlled by how far from the reference point the head has rotated
            float rotationDefecit = YRotationDefecit(neutralHeadDirection, transform.InverseTransformDirection(head.transform.forward));
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

    /// VR button to enable camera movement
    private void OnLook(InputValue value)
    {
        // enable body rotation in update
        bodyRotation = value.Get<float>()> 0.5;
        // TODO: instantaneous recenter on press (without affecting view)
        if (bodyRotation)
        {
            float rotationDefecit = YRotationDefecit(transform.forward, head.transform.forward);
            neutralHeadDirection = transform.InverseTransformDirection(head.transform.forward);
        }
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
