using System;
using UnityEngine;
using Unity.Cinemachine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum PanDirection
{
    Up,
    Down,
    Left,
    Right
}

[System.Serializable]
public class CustomInspectorObjects
{
    public bool panCameraOnContact = false;

    [HideInInspector] public PanDirection panDirection;
    [HideInInspector] public float panDistance = 3f;
    [HideInInspector] public float panTime = 0.35f;
}

public class CameraControlTrigger : MonoBehaviour
{
    public CustomInspectorObjects customInspectorObjects;

    private Collider2D _coll;

    private void Start()
    {
        _coll = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (customInspectorObjects.panCameraOnContact)
            {
                CameraManager.instance.PanCamera(
                    customInspectorObjects.panDistance,
                    customInspectorObjects.panTime,
                    customInspectorObjects.panDirection,
                    false
                );
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {

            if (customInspectorObjects.panCameraOnContact)
            {
                CameraManager.instance.PanCamera(
                    customInspectorObjects.panDistance,
                    customInspectorObjects.panTime,
                    customInspectorObjects.panDirection,
                    true
                );
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(CameraControlTrigger))]
public class CameraControlTriggerEditor : Editor
{
    private CameraControlTrigger _cameraControlTrigger;

    private void OnEnable()
    {
        _cameraControlTrigger = (CameraControlTrigger)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (_cameraControlTrigger.customInspectorObjects.panCameraOnContact)
        {
            _cameraControlTrigger.customInspectorObjects.panDirection = (PanDirection)EditorGUILayout.EnumPopup(
                "Camera Pan Direction",
                _cameraControlTrigger.customInspectorObjects.panDirection
            );

            _cameraControlTrigger.customInspectorObjects.panDistance = EditorGUILayout.FloatField(
                "Pan Distance",
                _cameraControlTrigger.customInspectorObjects.panDistance
            );

            _cameraControlTrigger.customInspectorObjects.panTime = EditorGUILayout.FloatField(
                "Pan Time",
                _cameraControlTrigger.customInspectorObjects.panTime
            );
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(_cameraControlTrigger);
        }
    }
}
#endif