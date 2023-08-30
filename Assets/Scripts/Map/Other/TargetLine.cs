using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetLine : MonoBehaviour {
    [SerializeField] LineRenderer lineRenderer;
    private Vector3 origin;

    public void SetStatic(Vector2 target, Vector2 origin) {
        enabled = false;

        transform.parent = null;

        this.origin = transform.InverseTransformPoint(origin);
        lineRenderer.SetPositions(new Vector3[2] { this.origin, target });
    }

    public void SetDynamic(Transform target, Vector2 origin) {
        enabled = true;

        transform.parent = target;
        transform.localPosition = Vector2.zero;

        this.origin = transform.InverseTransformPoint(origin);
        lineRenderer.SetPositions(new Vector3[2] { this.origin, Vector3.zero});
    }

    private void Update() {
        origin = transform.InverseTransformPoint(origin);
        lineRenderer.SetPosition(0, origin);
    }
}