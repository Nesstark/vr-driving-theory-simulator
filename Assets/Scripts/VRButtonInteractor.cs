using UnityEngine;
using System.Collections.Generic;

public class VRButtonInteractor : MonoBehaviour
{
    [SerializeField] private int buttonIndex;
    [SerializeField] private SelectionController controller;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Hand") || other.CompareTag("Controller"))
            controller.ToggleButtonSelection(buttonIndex);
    }

    void Start()
    {
        if (controller == null)
            controller = FindAnyObjectByType<SelectionController>();
    }
}