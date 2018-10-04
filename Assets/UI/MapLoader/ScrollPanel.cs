using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollPanel : MonoBehaviour, IScrollHandler
{
    public GameObject MapViewer;
    private Vector3 initialScale;

    [SerializeField]
    private float zoomSpeed = 0.1f;
    [SerializeField]
    private float maxZoom = 10f;
    [SerializeField]
    private float minZoom = 0.1f;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    public void OnScroll(PointerEventData eventData)
    {
        var delta = Vector3.one * (eventData.scrollDelta.y * zoomSpeed * transform.localScale.x);
        var desiredScale = transform.localScale + delta;

        desiredScale = ClampDesiredScale(desiredScale);
        transform.localScale = desiredScale;
    }

    private Vector3 ClampDesiredScale(Vector3 desiredScale)
    {
        desiredScale = Vector3.Max(initialScale * minZoom, desiredScale);
        desiredScale = Vector3.Min(initialScale * maxZoom, desiredScale);
        return desiredScale;
    }
}