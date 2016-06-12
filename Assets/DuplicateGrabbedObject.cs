using UnityEngine;
using VRTK;

public class DuplicateGrabbedObject : MonoBehaviour {
    private VRTK_ControllerEvents m_Events;
    private VRTK_InteractGrab m_Grabber;

    private void Start() {
        m_Events = GetComponent<VRTK_ControllerEvents>();
        m_Grabber = GetComponent<VRTK_InteractGrab>();
        m_Events.GripReleased += OnGripReleased;
    }

    private void OnGripReleased(object sender, ControllerInteractionEventArgs e) {
        var grabbedObject = m_Grabber.GetGrabbedObject();
        if (grabbedObject) { Instantiate(grabbedObject, grabbedObject.transform.position, grabbedObject.transform.rotation); }
    }
}