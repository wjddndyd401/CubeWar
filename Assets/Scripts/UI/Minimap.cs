using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Minimap : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    Vector3 localCursor;
    public event System.Action<Vector3> CameraToLookPoint;
    public event System.Action<Vector3> Move;
    public event System.Action<Vector3> AttackMove;
    public event System.Action<Vector3> SetRallyPoint;
    public event System.Action ShowRallyPoint;
    public GameObject map;
    RectTransform rect;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        localCursor = GetWorldPointFromMinimapPoint(eventData.position / transform.lossyScale);

        if (CameraToLookPoint != null && eventData.button == PointerEventData.InputButton.Left)
            CameraToLookPoint(localCursor);

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (SetRallyPoint != null) SetRallyPoint(localCursor); ShowRallyPoint();
            if (Move != null) Move(localCursor);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        localCursor = GetWorldPointFromMinimapPoint(eventData.position / transform.lossyScale);
        if (AttackMove != null && eventData.button == PointerEventData.InputButton.Left)
            AttackMove(localCursor);
    }

    public Vector3 GetWorldPointFromMinimapPoint(Vector2 minimapPoint)
    {
        Vector2 onWorldCoord = new Vector2(minimapPoint.x - rect.anchoredPosition.x, minimapPoint.y - rect.anchoredPosition.y);
        onWorldCoord.x = onWorldCoord.x / rect.sizeDelta.x * map.transform.lossyScale.x * Global.mapBaseLossyScale + map.transform.position.x;
        onWorldCoord.y = onWorldCoord.y / rect.sizeDelta.y * map.transform.lossyScale.z * Global.mapBaseLossyScale + map.transform.position.z;
        Vector3 onWorldPoint = new Vector3(onWorldCoord.x, 0, onWorldCoord.y);

        return onWorldPoint;
    }

    public Vector2 GetMinimapPointFromWorldPoint(Vector3 worldPoint)
    {
        Vector3 mLossyScale = map.transform.lossyScale * Global.mapBaseLossyScale;
        Vector2 onMiniMapPosition = new Vector2((worldPoint.x - map.transform.position.x) / mLossyScale.x, (worldPoint.z - map.transform.position.z) / mLossyScale.z);
        onMiniMapPosition.x *= rect.sizeDelta.x * rect.lossyScale.x;
        onMiniMapPosition.y *= rect.sizeDelta.y * rect.lossyScale.y;
        onMiniMapPosition.x += transform.position.x;
        onMiniMapPosition.y += transform.position.y;

        return onMiniMapPosition;
    }
}
