using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUnitController : MonoBehaviour {
    [SerializeField] float moveSpeed, zoomSpeed;

    private void Update() {
        transform.position += new Vector3(Input.GetAxis("Horizontal") * moveSpeed, Input.GetAxis("Vertical") * moveSpeed, 0) * Time.deltaTime;
        HandleTroopCommand();
    }

    public void HandleTroopCommand() {
        if (Input.GetKeyDown(KeyCode.O)) foreach (Unit unit in MapManager.units) unit.SetMode(Unit.UnitMode.Attack);
        if (Input.GetKeyDown(KeyCode.L)) foreach (Unit unit in MapManager.units) unit.SetMode(Unit.UnitMode.Return);      
        if (Input.GetKeyDown(KeyCode.P)) foreach (Unit unit in MapManager.units) unit.SetMode(Unit.UnitMode.Patrol);      
    }

    void OnGUI() {
        if (Event.current.isKey && Event.current.type == EventType.KeyDown) HandlePlayerInput(Event.current.keyCode);
    }

    public void HandlePlayerInput(KeyCode keyCode) {
        MapManager.Instance.PlayerInput(keyCode);
    }
}