using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    [SerializeField]
    private float teleportRange;

    private bool teleporting = false;
    private bool targetValid = false;
    public bool CanTeleport { get => teleporting && targetValid; }

    private Vector3 teleportTarget;

    private Animator animator;
    int animationFlexID;

    [SerializeField]
    GameObject targetPosIndicator;
    LineRenderer targetDirectionIndicator;

    [SerializeField]
    Material materialValidTP;
    [SerializeField]
    Material materialInvalidTP;

    private void Start()
    {
        targetPosIndicator.SetActive(false);
        targetDirectionIndicator = GetComponent<LineRenderer>();
        targetDirectionIndicator.enabled = false;

        animator = GetComponentInChildren<Animator>();
        animationFlexID = Animator.StringToHash("Flex");
        animator.SetFloat("Pinch", 0);
        animator.SetFloat("Flex", 0);
        animator.SetFloat("IndexSlide", 0);
    }

    private void Update()
    {
        if (teleporting)
        {
            targetPosIndicator.SetActive(true);
            targetDirectionIndicator.enabled = true;

            RaycastHit result;
            targetValid = Physics.Raycast(transform.position, transform.forward, out result, teleportRange);
            if (targetValid)
            {
                teleportTarget = result.point;
                targetDirectionIndicator.material = materialValidTP;
                targetPosIndicator.GetComponent<Renderer>().material = materialValidTP;
            }
            else
            {
                teleportTarget = transform.position + transform.forward * teleportRange;
                targetDirectionIndicator.material = materialInvalidTP;
                targetPosIndicator.GetComponent<Renderer>().material = materialInvalidTP;
            }

            targetPosIndicator.transform.position = teleportTarget;
            targetDirectionIndicator.SetPosition(targetDirectionIndicator.positionCount - 1,
                                                 transform.InverseTransformPoint(teleportTarget));
        }
        else
        {
            targetPosIndicator.SetActive(false);
            targetDirectionIndicator.enabled = false;
        }
    }

    public void StartTeleport()
    {
        teleporting = true;
    }

    public Vector3 CommitTeleport()
    {
        if (!teleporting)
            throw new InvalidOperationException($"Hand \"{gameObject.name}\": attempted to commit a teleport without one ready");

        teleporting = false;
        return teleportTarget;
    }

    public void CancelTeleport()
    {
        teleporting = false;
    }

    public void StartGrab()
    {
        animator.SetFloat(animationFlexID, 1);
    }

    public void EndGrab()
    {
        animator.SetFloat(animationFlexID, 0);
    }
}
