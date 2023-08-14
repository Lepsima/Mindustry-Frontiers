using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class PostProcessingController : MonoBehaviour {
    public static PostProcessingController Instance;
    [SerializeField] Volume volume;

    private void Awake() {
        Instance = this;
    }

    public void SetGrainActive(bool state) {
        //foreach (VolumeComponent comp in volume.profile.components) Debug.Log(comp.name);
        volume.profile.components.Find(x => x.name == "FilmGrain(Clone)").active = state;
    }
}
