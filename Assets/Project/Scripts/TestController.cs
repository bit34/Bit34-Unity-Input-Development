using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Com.Bit34Games.Unity.Update;
using Com.Bit34Games.Unity.Input;
using Com.Bit34Games.Unity.Camera;

public class TestController : MonoBehaviour, IPointerInputHandler
{
    //  CONSTANTS
    private const float CAMERA_POSITION_FOLLOW        = 0.5f;
    private const float CAMERA_ZOOM_FOLLOW            = 0.5f;
    private const float CAMERA_MINIMUM_SIZE           = 1;
    private const float CAMERA_MAXIMUM_SIZE           = 6;
    private const float CAMERA_SCROLL_ZOOM_MULTIPLIER = 1;
    private const int   KEYBOARD_INPUT_GROUP_GENERAL      = 1;
    private const int   KEYBOARD_INPUT_GROUP_NOT_DRAGGING = 2;


    //  MEMBERS
#pragma warning disable 0649
    //      For Editor
    [Header("Info")]
    [SerializeField] private Button     _infoButton;
    [SerializeField] private GameObject _infoPanel;
    [Header("Logging")]
    [SerializeField] private Text       _logText;
    [SerializeField] private int        _maxLog;
    [SerializeField] private Button     _clearLogButton;
    [Header("Camera controller")]
    [SerializeField] private Button     _cameraContollerButton;
    [SerializeField] private Text       _cameraContollerButtonLabel;
    [Header("Scene")]
    [SerializeField] private GameObject _boundriesObject;
    [SerializeField] private Button     _inputToggleObjectButton;
    [SerializeField] private GameObject _inputToggleObject;
    [SerializeField] private GameObject _visibilityToggleObject;
    [SerializeField] private GameObject _createdObjectPrefab;
    [SerializeField] private GameObject _createdObjectParent;
    [SerializeField] private Transform  _createdObjectPosition;

#pragma warning restore 0649
    //      Internal
    private List<string>            _logs;
    private GameObject              _createdObjectInstance;
    private GameObject              _highlightedObject;
    private TopDownCameraController _cameraController;

    //  METHODS
#region Unity callbacks
     
	void Start ()
    {
        InputManager.Initialize();
        InputManager.AddPointerHandler(this);

        _infoButton.onClick.AddListener(()=>{ _infoPanel.SetActive(!_infoPanel.activeSelf); });
        InitializeLog();
        InitializeCameraController();
        InitializeScene();
        InitializeKeyboard();
	}

    private void Update()
    {
        InputManager.SetKeyGroupState(KEYBOARD_INPUT_GROUP_NOT_DRAGGING, !_cameraController.IsDragging);
    }

#endregion

#region Logging

    private void InitializeLog()
    {
        _logs = new List<string>();
        _clearLogButton.onClick.AddListener(ClearLog);

        _logs.Add("Started");
        _logText.text = _logs[0];
    }

    private string GetObjectInfo(GameObject testGameObject)
    {
        if(testGameObject!=null)
        {
            TestInputObject         testObject = testGameObject.GetComponent<TestInputObject>();
            TestInputObjectCategory testObjectCategory = (TestInputObjectCategory)testObject.Category;
            return "[" + testObject.name + " " + testObjectCategory + "-" + testObject.Id + "]";
        }
        return "[None]";
    }

    private string GetPointerInfo(int id)
    {
        switch(id)
        {
            case PointerInputConstants.MOUSE_POINTER_ID:             return "[None  ]";
            case PointerInputConstants.MOUSE_LEFT_DRAG_POINTER_ID:   return "[Left  ]";
            case PointerInputConstants.MOUSE_RIGHT_DRAG_POINTER_ID:  return "[Right ]";
            case PointerInputConstants.MOUSE_MIDDLE_DRAG_POINTER_ID: return "[Middle]";
            default: return "[" + id + "]";
        }
    }

    private void ClearLog()
    {
        _logs.Clear();
        _logText.text = "";
    }

    private void Log(string message)
    {
        _logs.Add(message);

        while(_logs.Count > _maxLog)
        {
            _logs.RemoveAt(0);
        }

        string fullLog = "";
        foreach(string logMessage in _logs)
        {
            fullLog += logMessage + "\n";
        }
        _logText.text = fullLog;

//        Debug.Log(message);
    }
    
#endregion

#region Camera Controller

    private void InitializeCameraController()
    {
        Bounds worldBounds = _boundriesObject.GetComponent<Renderer>().bounds;
        _cameraController = new TopDownCameraController();
        _cameraController.SetupMovement(CAMERA_POSITION_FOLLOW);
        _cameraController.SetMovementLimits(worldBounds.min.x, worldBounds.max.x, worldBounds.min.z, worldBounds.max.z);
        _cameraController.SetupZoom(CAMERA_ZOOM_FOLLOW, CAMERA_MINIMUM_SIZE, CAMERA_MAXIMUM_SIZE, true, CAMERA_SCROLL_ZOOM_MULTIPLIER);

        _cameraContollerButton.onClick.AddListener(()=>
        {
            SetCameraController(!_cameraController.IsActive);
        });

        SetCameraController(true);
    }

    private void SetCameraController(bool state)
    {
        _cameraController.SetActivate(state);
        if(state)
        {
            _cameraContollerButtonLabel.text = "Camera Control : Active";
        }
        else
        {
            _cameraContollerButtonLabel.text = "Camera Control : Deactive";
        }
    }

    private void StartCameraDrag(int pointerId)
    {
        Vector2 startPosition;
        Vector2 currentPosition;
        InputManager.GetPointerPosition(pointerId, out startPosition, out currentPosition);
        _cameraController.DragWithPointer(pointerId, startPosition);
    }

    private void ShowAllObjects()
    {
        Bounds bounds = new Bounds();
        bounds = IterateObjectBounds(transform, bounds);
        _cameraController.SetPosition(bounds.center, false);
        _cameraController.SetSize(bounds.size.z*0.5f);
    }

    private Bounds IterateObjectBounds(Transform objectTransform, Bounds bounds)
    {
        for (int i = 0; i < objectTransform.childCount; i++)
        {
            Transform childTransform = objectTransform.GetChild(i);
            if (childTransform.GetComponent<TestInputObject>() != null && 
                childTransform.gameObject.activeInHierarchy)
            {
                Renderer childRenderer = childTransform.GetComponent<Renderer>();
                if (childRenderer != null)
                {
                    bounds.Encapsulate(childRenderer.bounds);
                }
            }
            bounds = IterateObjectBounds(childTransform, bounds);
        }
        return bounds;
    }

#endregion

#region Scene

    private void InitializeScene()
    {
        UpdateManager.Add(ToggleObject, this, TimeSpan.FromSeconds(2));

        _inputToggleObjectButton.onClick.AddListener(()=>
        {
            TestInputObject testObject = _inputToggleObject.GetComponent<TestInputObject>();
            testObject.SetState(!testObject.IsInputEnabled);
        });
    }

    private void ToggleObject()
    {
        _visibilityToggleObject.SetActive(!_visibilityToggleObject.activeSelf);

        if(_createdObjectInstance==null)
        {
            _createdObjectInstance = Instantiate(_createdObjectPrefab,_createdObjectPosition.position, Quaternion.identity, _createdObjectParent.transform);
            _createdObjectInstance.name = _createdObjectPrefab.name;
        }
        else
        {
            Destroy(_createdObjectInstance);
            _createdObjectInstance = null;
        }
    }

    private void HighlightObject(GameObject objectToHighlight)
    {
        if (_highlightedObject != null)
        {
            _highlightedObject.GetComponent<TestInputObject>().SetHighlight(false);
            _highlightedObject = null;
        }

        if (objectToHighlight != null)
        {
            _highlightedObject = objectToHighlight;
            _highlightedObject.GetComponent<TestInputObject>().SetHighlight(true);
        }
    }

#endregion
     
#region IInputHandler implementations
    
    public void OnPointerDown(int pointerId, Vector2 screenPosition, GameObject objectUnderPointer)
    {
//        Log(GetPointerInfo(pointerId) + "[Down  ]" + GetObjectInfo(objectUnderPointer));
    }

    public void OnPointerMove(int pointerId, Vector2 screenPosition, GameObject objectUnderPointer)
    {}

    public void OnPointerUp(int pointerId, Vector2 screenPosition, GameObject objectUnderPointer)
    {
//        Log(GetPointerInfo(pointerId) + "[Up    ]" + GetObjectInfo(objectUnderPointer));
    }

    public void OnPointerClick(int pointerId, Vector2 screenPosition, GameObject objectUnderPointer)
    {
        Log(GetPointerInfo(pointerId) + "[Click ]" + GetObjectInfo(objectUnderPointer));

        if (objectUnderPointer != null)
        {
            if (pointerId == PointerInputConstants.MOUSE_LEFT_DRAG_POINTER_ID)
            {
                _cameraController.SetPosition(objectUnderPointer.transform.position, false);
            }
            else
            if (pointerId == PointerInputConstants.MOUSE_RIGHT_DRAG_POINTER_ID)
            {
                _cameraController.SetPosition(objectUnderPointer.transform.position, true);
            }
            else
            if (pointerId == PointerInputConstants.MOUSE_MIDDLE_DRAG_POINTER_ID)
            {
                _cameraController.SetSize(CAMERA_MINIMUM_SIZE);
                _cameraController.SetPosition(objectUnderPointer.transform.position, false);
            }
        }
    }

    public void OnPointerClickCanceled(int pointerId, Vector2 screenPosition, GameObject objectUnderPointer)
    {
//        Log(GetPointerInfo(pointerId) + "[ClickX]" + GetObjectInfo(objectUnderPointer));

        if (pointerId == PointerInputConstants.MOUSE_MIDDLE_DRAG_POINTER_ID)
        {
            HighlightObject(null);
            StartCameraDrag(pointerId);
        }
    }

    public void OnPointerEnter(int pointerId, Vector2 screenPosition, GameObject objectUnderPointer)
    {
//        Log(GetPointerInfo(pointerId) + "[Enter ]" + GetObjectInfo(objectUnderPointer));

        if (_cameraController.IsDragging == false)
        {
            HighlightObject(objectUnderPointer);
        }
    }

    public void OnPointerLeave(int pointerId, Vector2 screenPosition, GameObject objectUnderPointer)
    {
//        Log(GetPointerInfo(pointerId) + "[Leave ]" + GetObjectInfo(objectUnderPointer));

        if (_cameraController.IsDragging == false && objectUnderPointer != null)
        {
            HighlightObject(null);
        }
    }

    public void OnPointerCancel(int pointerId, Vector2 screenPosition, GameObject objectUnderPointer)
    {
//        Log(GetPointerInfo(pointerId) + "[Cancel]" + GetObjectInfo(objectUnderPointer));
    }

#endregion

#region Keyboard Methods
     
    private void InitializeKeyboard()
    {
        InputManager.AddKeyboardInput(KEYBOARD_INPUT_GROUP_GENERAL,      null,                               KeyCode.C, false, ClearLog);
        InputManager.AddKeyboardInput(KEYBOARD_INPUT_GROUP_NOT_DRAGGING, new KeyCode[]{KeyCode.LeftControl}, KeyCode.A, true,  ShowAllObjects);
        
        InputManager.SetKeyGroupState(KEYBOARD_INPUT_GROUP_GENERAL,      true);
        InputManager.SetKeyGroupState(KEYBOARD_INPUT_GROUP_NOT_DRAGGING, true);
    }

#endregion
}
