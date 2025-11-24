using UnityEngine;
using UnityEngine.InputSystem;

public class ViveControllerInput : MonoBehaviour
{
    public InputActionProperty triggerAction;
    public InputActionProperty gripAction;
    public InputActionProperty trackpadPressAction;
    public InputActionProperty trackpadTouchAction;
    public InputActionProperty trackpadPositionAction;

    private VarjoVergengeHandlerPrototype vergengeHandler;
    private VarjoVergengeHandlerPrototypeOutlines vergenceOutlineHandler;

    private void Start()
    {
        vergengeHandler = GetComponent<VarjoVergengeHandlerPrototype>();
        vergenceOutlineHandler = GetComponent<VarjoVergengeHandlerPrototypeOutlines>();
    }

    void Update()
    {
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            //vergengeHandler.changeSeletion(true);
            vergenceOutlineHandler.changeSelection(true);
        }
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame) 
        {
            //vergengeHandler.changeSeletion(false);
            vergenceOutlineHandler.changeSelection(false);
        }
        else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
        {
            //vergengeHandler.changeConfirmation(true);
            vergenceOutlineHandler.changeConfirmation(true);
        }
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
        {
            //vergengeHandler.changeConfirmation(false);
            vergenceOutlineHandler.changeConfirmation(false);
        }
        else if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            //vergengeHandler.changeCircularPlaneConfirmation();
            vergenceOutlineHandler.changeCircularPlaneConfirmation();
        }


        if (triggerAction.action.WasPressedThisFrame())
        {
            Debug.Log("Trigger Pressed - Perform Action A");
        }

        if (gripAction.action.WasPressedThisFrame())
        {
            Debug.Log("Grip Button Pressed - Perform Action B");

        }
        // Trackpad Clicked
        if (trackpadPressAction.action.WasPressedThisFrame())
        {
            Debug.Log("Trackpad Pressed!");
        }

        // Trackpad Touched
        if (trackpadTouchAction.action.WasPressedThisFrame())
        {
            Debug.Log("Trackpad Touched!");
        }

    }

}