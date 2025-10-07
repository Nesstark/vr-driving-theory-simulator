using UnityEngine;
using System.Collections.Generic;

public class VRButtonInteractor : MonoBehaviour
{
    [SerializeField] private int buttonIndex;
    [SerializeField] private SelectionController controller;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Collider entered: {other.name} with tag {other.tag}");
        if (other.CompareTag("Hand") || other.CompareTag("Controller"))
        {
            controller.ToggleButtonSelection(buttonIndex);
            Debug.Log($"Button {buttonIndex} pressed.");
        }
    }

    void Start()
    {
        if (controller == null)
            controller = FindAnyObjectByType<SelectionController>();
    }
}