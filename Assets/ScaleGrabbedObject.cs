using UnityEngine;
using VRTK;

[RequireComponent(typeof(VRTK_ControllerEvents)), RequireComponent(typeof(VRTK_InteractGrab))]
public class ScaleGrabbedObject : MonoBehaviour
{
    public float Multiplier = 1;
    public float MaximumScalePerSwipe = 2f;
    public float MinimumScalePerSwipe = 0.2f;

    private VRTK_ControllerEvents m_Events;
    private VRTK_InteractGrab m_Grabber;
    private Vector2 m_InitialTouchAxis;
    private Vector3 m_InitialScale;
    private GameObject m_GrabbedObject;
    private bool m_CancelScaleSwipe;

    private void Start()
    {
        m_Grabber = GetComponent<VRTK_InteractGrab>();
        m_Grabber.ControllerGrabInteractableObject += OnGrab;
        m_Grabber.ControllerUngrabInteractableObject += OnUngrab;

        m_Events = GetComponent<VRTK_ControllerEvents>();
        m_Events.TouchpadTouchStart += OnTouchpadTouchStart;
        m_Events.TouchpadAxisChanged += OnTouchpadAxisChanged;
        m_Events.TouchpadPressed += OnTouchpadPressed;
    }

    private void OnTouchpadPressed(object sender, ControllerInteractionEventArgs controllerInteractionEventArgs)
    {
        if (!m_GrabbedObject) { return; }
        m_GrabbedObject.transform.localScale = m_InitialScale;
        m_CancelScaleSwipe = true;
    }

    private void OnGrab(object sender, ObjectInteractEventArgs args)
    {
        m_GrabbedObject = args.target;
        m_InitialScale = m_GrabbedObject.transform.localScale;
    }

    private void OnUngrab(object sender, ObjectInteractEventArgs args) { m_GrabbedObject = null; }

    private void OnTouchpadTouchStart(object sender, ControllerInteractionEventArgs e)
    {
        m_InitialTouchAxis = e.touchpadAxis;
        m_CancelScaleSwipe = false;
        if (m_GrabbedObject) { m_InitialScale = m_GrabbedObject.transform.localScale; }
    }

    private void OnTouchpadAxisChanged(object sender, ControllerInteractionEventArgs args)
    {
        if (!m_GrabbedObject || m_CancelScaleSwipe) { return; }
        var offset = args.touchpadAxis.y - m_InitialTouchAxis.y;
        var scaleChange = (1 + offset)*Multiplier;
        scaleChange = Mathf.Max(scaleChange, MinimumScalePerSwipe);
        scaleChange = Mathf.Min(scaleChange, MaximumScalePerSwipe);
        var newScale = m_InitialScale*scaleChange*Multiplier;
        m_GrabbedObject.transform.localScale = newScale;
    }
}