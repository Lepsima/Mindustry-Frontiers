using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using UnityEngine.Experimental.Rendering.Universal;

public class UnitJumpController : MonoBehaviour {
    //private Light2D jumpFlashLight;
    private ParticleSystem jumpParticleSystem;

    public float flashDuration = 0.1f, particleSpawnDelay = 0.1f, unitSpawnDelay = 0.3f;

    private Content unitToSpawn;
    private byte unitTeamCode;

    private void Awake() {
        //jumpFlashLight = GetComponentInChildren<Light2D>();
        jumpParticleSystem = GetComponentInChildren<ParticleSystem>();
    }

    public void SetStats(Vector2 position, Quaternion direction, Content content, byte teamCode) {
        transform.SetPositionAndRotation(position, direction);
        unitToSpawn = content;
        unitTeamCode = teamCode;
    }

    public void Jump() {
        Invoke(nameof(Invoke_TurnOffLight), flashDuration);
        Invoke(nameof(Invoke_StartParticles), particleSpawnDelay);
        Invoke(nameof(Invoke_SpawnUnit), unitSpawnDelay);
    }

    private void Invoke_TurnOffLight() {
        //jumpFlashLight.intensity = 0;
    }

    private void Invoke_StartParticles() {
        jumpParticleSystem.Play();
    }

    private void Invoke_SpawnUnit() {
        //MapManager.Instance.CreateUnit(transform.position, transform.rotation, unitToSpawn, unitTeamCode);
    }
}
