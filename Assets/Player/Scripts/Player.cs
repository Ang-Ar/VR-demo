using Oculus.Interaction.HandGrab;
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
    [Range(0f, 1f)]
    [Tooltip("unused")]
    private float lookDamping;
    [SerializeField]
    [Tooltip("maximum angle in degrees between body and recentering hand")]
    private float lookDeadZone;

    private Vector2 moveInput;
    private Vector3 velocity = Vector3.zero;
    private Vector3 wishVel = Vector3.zero;

    private Hand cameraHand; 

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

        if (cameraHand != null)
        {
            Vector3 flatScaler = new Vector3(1, 0, 1);
            Vector3 flatcurrent = Vector3.Scale(transform.forward, flatScaler);
            Vector3 flatDesired = Vector3.Scale(cameraHand.transform.forward, flatScaler);
            float rotationDefecit = Vector3.SignedAngle(flatcurrent, flatDesired, Vector3.up);
            if (Mathf.Abs(rotationDefecit) > lookDeadZone)
            {
                transform.Rotate(0, rotationDefecit*Time.deltaTime, 0, Space.World);
            }
        }
    }

    #endregion

    #region Input messages

    /// only used for PC debugging
    private void OnLook(InputValue value)
    {
        const float hSens = 0.25f;
        const float vSens = 0.25f;
        Vector2 lookDelta = value.Get<Vector2>();

        head.transform.Rotate(-lookDelta.y * vSens, 0, 0, Space.Self);
        if (cameraHand == null)
        {
            transform.Rotate(0, lookDelta.x * hSens, 0, Space.World);
            handLeft.transform.rotation = head.transform.rotation;
            handRight.transform.rotation = head.transform.rotation;
        }
        else
        {
            handLeft.transform.Rotate(0, lookDelta.x * hSens, 0, Space.World);
            handRight.transform.Rotate(0, lookDelta.x * hSens, 0, Space.World);
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

    public void OnLookleft(InputValue value)
    {
        OnLookAny(value, handLeft);
    }

    public void OnLookright(InputValue value)
    {
        OnLookAny(value, handRight);
    }

    public void OnLookAny(InputValue value, Hand hand)
    {
        if (value.Get<float>() >= 0.5)
            cameraHand = hand;
        else
            cameraHand = null;
    }

    #endregion

    #region Custom methods

    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
    }

    #endregion
}
