using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;

    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineCamera[] _allVirtualCameras;

    [Header("Controls for Lerping Y Damping During Jump/Fall")]
    [SerializeField] private float _fallPanAmount = 0.25f;
    [SerializeField] private float _fallPanTime = 0.35f;
    public float _fallSpeedYDampingChangeThreshold = -15f;

    public bool IsLerpingYDamping { get; private set; }
    public bool LerpedFromPlayerFalling { get; set; }

    private Coroutine _lerpYPanCoroutine;
    private Coroutine _panCameraCoroutine;

    private CinemachineCamera _currentCamera;
    private CinemachinePositionComposer _positionComposer;

    private float _normYPanAmount;
    private Vector3 _startingTrackedObjectOffset;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        for (int i = 0; i < _allVirtualCameras.Length; i++)
        {
            if (_allVirtualCameras[i].enabled)
            {
                // Set initial active camera
                _currentCamera = _allVirtualCameras[i];
                // In Cinemachine 3, procedural components are retrieved via GetComponent
                _positionComposer = _currentCamera.GetComponent<CinemachinePositionComposer>();
                break;
            }
        }

        // Cache default values
        if (_positionComposer != null)
        {
            _normYPanAmount = _positionComposer.Damping.y;
            _startingTrackedObjectOffset = _positionComposer.TargetOffset;
        }
    }

    #region Lerp Y Damping

    public void LerpYDamping(bool isPlayerFalling)
    {
        if (_lerpYPanCoroutine != null)
        {
            StopCoroutine(_lerpYPanCoroutine);
        }
        _lerpYPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
    }

    private IEnumerator LerpYAction(bool isPlayerFalling)
    {
        IsLerpingYDamping = true;

        float startDampAmount = _positionComposer.Damping.y;
        float endDampAmount = isPlayerFalling ? _fallPanAmount : _normYPanAmount;

        if (isPlayerFalling)
        {
            LerpedFromPlayerFalling = true;
        }

        float elapsedTime = 0f;
        while (elapsedTime < _fallPanTime)
        {
            elapsedTime += Time.deltaTime;
            float lerpedAmount = Mathf.Lerp(startDampAmount, endDampAmount, elapsedTime / _fallPanTime);
            
            Vector3 currentDamping = _positionComposer.Damping;
            currentDamping.y = lerpedAmount;
            _positionComposer.Damping = currentDamping;

            yield return null;
        }

        Vector3 finalDamping = _positionComposer.Damping;
        finalDamping.y = endDampAmount;
        _positionComposer.Damping = finalDamping;

        IsLerpingYDamping = false;
    }

    #endregion

    #region Pan Camera

    public void PanCamera(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        if (_panCameraCoroutine != null)
        {
            StopCoroutine(_panCameraCoroutine);
        }
        _panCameraCoroutine = StartCoroutine(PanCameraCoroutine(panDistance, panTime, panDirection, panToStartingPos));
    }

    private IEnumerator PanCameraCoroutine(float panDistance, float panTime, PanDirection panDirection, bool panToStartingPos)
    {
        Vector3 endPos = Vector3.zero;
        Vector3 startingPos = Vector3.zero;

        if (!panToStartingPos)
        {
            switch (panDirection)
            {
                case PanDirection.Up:
                    endPos = Vector3.up;
                    break;
                case PanDirection.Down:
                    endPos = Vector3.down;
                    break;
                case PanDirection.Left:
                    endPos = Vector3.right;
                    break;
                case PanDirection.Right:
                    endPos = Vector3.left;
                    break;
            }

            endPos *= panDistance;
            startingPos = _startingTrackedObjectOffset;
            endPos += startingPos;
        }
        else
        {
            startingPos = _positionComposer.TargetOffset;
            endPos = _startingTrackedObjectOffset;
        }

        float elapsedTime = 0f;
        while (elapsedTime < panTime)
        {
            elapsedTime += Time.deltaTime;
            Vector3 panOffset = Vector3.Lerp(startingPos, endPos, elapsedTime / panTime);
            _positionComposer.TargetOffset = panOffset;

            yield return null;
        }

        _positionComposer.TargetOffset = endPos;
    }

    #endregion
}