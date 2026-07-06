using System;
using UnityEngine;

public class FishRoam : MonoBehaviour
{
    public enum roamDirection
    {
        Clockwise,
        CounterClockwise
    }
    [SerializeField] private roamDirection _roamDirection;
    private int _roamDirectionInt;
    [SerializeField] float radius = 160f;
    [SerializeField] float speed = 10f;


    [SerializeField] Transform model;
    [SerializeField] Transform rotationalCenter;

    void UpdatePositions()
    {
        _roamDirectionInt = _roamDirection == 0 ? 1 : -1;

        if (rotationalCenter == null)
            rotationalCenter = transform.GetChild(0);
        if (model == null)
            model = rotationalCenter.GetChild(0);

        model.localPosition = Vector3.right * radius * -_roamDirectionInt;
    }

    void OnValidate()
    {
        UpdatePositions();
    }

    void Update()
    {
        rotationalCenter.Rotate(transform.up, speed * _roamDirectionInt / radius);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        // Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxWidth, boxHeight, range));
        Gizmos.DrawLine(rotationalCenter.position, model.position);
        UnityEditor.Handles.DrawWireDisc(rotationalCenter.position, Vector3.up, radius, 5f);
    }
}
