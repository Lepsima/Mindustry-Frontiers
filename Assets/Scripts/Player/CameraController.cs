using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using Cinemachine;

public class CameraController : MonoBehaviour {
    public static CameraController Instance;

    [SerializeField] float moveSpeed = 2f;
    [SerializeField] float zoomSpeed = 30f;
    [SerializeField] float zoomInMultiplier = 2f;
    [SerializeField] float zoomOutMultiplier = 1f;
    [SerializeField] [Range(1, 50)] float zoomClampMin = 10f;
    [SerializeField] [Range(1, 50)] float zoomClampMax = 50f;

    private CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin;
    private CinemachineVirtualCamera virtualCamera;
    private Transform playerTransform;

    public bool forceFollow = false;
    public int unitFollowIndex = 0;

    readonly List<ShakeModifier> shakeModifiers = new();
    readonly int maxShakeModifiers = 32;
    float cameraShakeIntensity = 0f;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        playerTransform = transform.GetChild(0);
        playerTransform.parent = null;

        virtualCamera = GetComponent<CinemachineVirtualCamera>();
        cinemachineBasicMultiChannelPerlin = virtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        Follow(playerTransform);
    }


    public void Update() {
        if (!forceFollow) playerTransform.position += Time.deltaTime * virtualCamera.m_Lens.OrthographicSize * new Vector3(Input.GetAxis("Horizontal") * moveSpeed, Input.GetAxis("Vertical") * moveSpeed, 0);

        float highestIntensity = cameraShakeIntensity;
        cameraShakeIntensity = 0f;

        for (int i = shakeModifiers.Count - 1; i >= 0; i--) {
            ShakeModifier shakeModifier = shakeModifiers[i];

            if (shakeModifier.Ended()) shakeModifiers.Remove(shakeModifier);
            else highestIntensity = Mathf.Max(shakeModifier.Intensity(), highestIntensity);
        }

        cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = highestIntensity;
    }

    public float CameraSize() => virtualCamera.m_Lens.OrthographicSize;

    public void ChangeCameraSize(float delta) {
        float change = delta * zoomSpeed * (delta < 0f ? zoomOutMultiplier : zoomInMultiplier) * Time.deltaTime;
        if (!forceFollow) virtualCamera.m_Lens.OrthographicSize = Mathf.Clamp(virtualCamera.m_Lens.OrthographicSize - change, zoomClampMin, zoomClampMax);
    }

    public bool IsFollowingPlayer() => virtualCamera.Follow == playerTransform;

    public void ChangeFollowingUnit(int increment) {
        if (MapManager.Map.units.Count == 0) return;

        unitFollowIndex += increment;

        if (unitFollowIndex >= MapManager.Map.units.Count) unitFollowIndex = 0;
        if (unitFollowIndex < 0) unitFollowIndex = MapManager.Map.units.Count - 1;

        Unit unit = MapManager.Map.units[unitFollowIndex];
        Transform unitTransform = unit.transform;

        unit.OnDestroyed += OnFollowingUnitDestroyed;

        Follow(unitTransform);
    }

    public void OnFollowingUnitDestroyed(object sender, Entity.EntityArg e) {
        // If there's a registered killer, follow that entity
        Follow(e.other != null ? e.other.transform : null);
    }

    public void FixFollow(Transform target, float fovSize) {
        virtualCamera.Follow = target == null ? playerTransform : target;
        virtualCamera.m_Lens.OrthographicSize = fovSize;
        forceFollow = true;
    }

    public void Follow(Transform target, bool forceFollow = false) {
        if (this.forceFollow && virtualCamera.Follow != null) return;

        virtualCamera.Follow = target == null ? playerTransform : target;
        this.forceFollow = forceFollow;
    }

    public void UnFollow(Vector2 position) {
        forceFollow = false;
        playerTransform.position = position;
        Follow(playerTransform);
    }

    public void CameraShake(float intensity, float time, float decreaseTime, float delay) {
        if (shakeModifiers.Count > maxShakeModifiers) shakeModifiers.RemoveAt(0);
        shakeModifiers.Add(new(intensity, time, decreaseTime, delay));
    }

    public void CameraShake(Vector2 origin, float range, float intensity, float time, float decreaseTime) {
        float distance = Vector2.Distance(origin, virtualCamera.transform.position);
        if (distance > range) return;

        // Calculate
        float decreased = Mathf.Lerp(0, intensity, Mathf.Clamp01((time - Time.time) * (1 / (time - decreaseTime))));
        cameraShakeIntensity = Mathf.Lerp(0, decreased, Mathf.Cos(distance / range * Mathf.PI) * range + range);
    }

    public void StopCameraShake() => shakeModifiers.Clear();

    class ShakeModifier {
        public float time;
        public float decreaseTime;

        public float delay;

        public float intensity;

        public ShakeModifier(float intensity, float time, float decreaseTime = -1f, float delay = 0f) {
            this.time = Time.time + time + delay;     
            this.decreaseTime = decreaseTime == -1f ? this.time : Time.time + decreaseTime + delay; 
            this.delay = Time.time + delay;

            this.intensity = intensity;
        }

        public bool Ended() => time <= Time.time;

        public float Intensity() => delay > Time.time ? 0f : Mathf.Lerp(0, intensity, Mathf.Clamp01((time - Time.time) * (1 / (time - decreaseTime))));
    }
}