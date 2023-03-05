using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputManager : MonoBehaviour {
    public static PlayerInputManager Instance;

    public KeyCode LastPressed { get; private set; }

    //Axis Inputs
    public float HorizontalAxis { get; private set; }
    public float VerticalAxis { get; private set; }
    public Vector2 Movement { get; private set; }

    // Mouse Inputs
    public Vector2 MousePosition { get; private set; }
    public float MouseScrollDelta { get; private set; }

    // Number Inputs
    public int AlphaSelected { get; private set; }

    private void Awake() {
        Instance = this;
    }

    private void Update() {
        UpdatePlayerInputAxis();
    }

    void OnGUI() {
        if (Event.current.isKey && Event.current.type == EventType.KeyDown) HandlePlayerInput(Event.current.keyCode);
    }

    public void HandlePlayerInput(KeyCode keyCode) {

        // Get Alpha number input
        if ((int)keyCode >= (int)KeyCode.Alpha0 && (int)keyCode <= (int)KeyCode.Alpha9) AlphaSelected = (int)keyCode - (int)KeyCode.Alpha0;

        // Update the last pressed key
        LastPressed = keyCode;
    }

    public void UpdatePlayerInputAxis() {

        // Update movement axis
        HorizontalAxis = Input.GetAxis("Horizontal");
        VerticalAxis = Input.GetAxis("Vertical");

        // Get movement vector
        Movement = new Vector2(HorizontalAxis, VerticalAxis);

        // Update mouse position and scroll
        MousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        MouseScrollDelta = Input.mouseScrollDelta.y;
    }
}
