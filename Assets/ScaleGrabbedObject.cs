using UnityEngine;
using VRTK;

[RequireComponent(typeof(VRTK_ControllerEvents)), RequireComponent(typeof(VRTK_InteractGrab))]
public class ScaleGrabbedObject : MonoBehaviour
{
    public float Multiplier = 1;
    public float MaximumScalePerSwipe = 2f;
    public float MinimumScalePerSwipe = 0.2f;
    public bool DisablePointerWhenScaling = true;
    public float DisableTeleportThreshold = 0.2f;

    private VRTK_ControllerEvents m_Events;
    private VRTK_InteractGrab m_Grabber;
    private Vector2 m_InitialTouchAxis;
    private Vector3 m_InitialScale;
    private GameObject m_GrabbedObject;
    private VRTK_DestinationMarker m_Pointer;
    private VRTK_BasicTeleport m_Teleporter;
    private bool m_TeleportWasDisabled;
    private bool m_CancelScaleSwipe;

    private void Start()
    {
        // Required
        m_Events = GetComponent<VRTK_ControllerEvents>();
        m_Grabber = GetComponent<VRTK_InteractGrab>();

        m_Grabber.ControllerGrabInteractableObject += OnGrab;
        m_Grabber.ControllerUngrabInteractableObject += OnUngrab;

        m_Events.TouchpadAxisChanged += OnTouchpadAxisChanged;
        m_Events.TouchpadTouchStart += OnTouchpadTouchStart;
        m_Events.TouchpadTouchEnd += OnTouchpadTouchEnd;

        // Optional
        m_Pointer = GetComponent<VRTK_DestinationMarker>();
        m_Teleporter = GetComponentInParent<VRTK_BasicTeleport>();
        if (m_Pointer && m_Teleporter) { m_Teleporter.Teleporting += OnTeleporting; }
    }

    private void OnTeleporting(object sender, DestinationMarkerEventArgs e)
    {
        // Reset and cancel current scale swipe when teleporting (because we probably accidentally scaled stuff on the way.
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
        if (!m_GrabbedObject) { return; }
        m_InitialScale = m_GrabbedObject.transform.localScale;
        m_CancelScaleSwipe = false;
    }

    private void OnTouchpadAxisChanged(object sender, ControllerInteractionEventArgs args)
    {
        if (!m_GrabbedObject || m_CancelScaleSwipe) { return; }
        var offset = args.touchpadAxis.y - m_InitialTouchAxis.y;
        var scaleChange = (1 + offset) *Multiplier;
        scaleChange = Mathf.Max(scaleChange, MinimumScalePerSwipe);
        scaleChange = Mathf.Min(scaleChange, MaximumScalePerSwipe);

        if (DisablePointerWhenScaling && m_Pointer && m_Pointer.enableTeleport && Mathf.Abs(offset) > DisableTeleportThreshold)
        {
            Debug.Log("Disabling scale because pointer because " + Mathf.Abs(offset) + " > " + DisableTeleportThreshold);
            m_Pointer.enableTeleport = false;
            m_TeleportWasDisabled = true;
        }

        var newScale = m_InitialScale*scaleChange*Multiplier;
        m_GrabbedObject.transform.localScale = newScale;
    }

    private void OnTouchpadTouchEnd(object sender, ControllerInteractionEventArgs e)
    {
        if (m_TeleportWasDisabled)
        {
            m_Pointer.enableTeleport = true;
            m_TeleportWasDisabled = false;
        }

        if (!m_GrabbedObject) { return; }

        m_InitialScale = m_GrabbedObject.transform.localScale;
    }
}