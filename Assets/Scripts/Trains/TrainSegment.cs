using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using System;

public class TrainSegment : Unit {
    public new TrainType Type { get => (TrainType)base.Type; protected set => base.Type = value; }

    public static List<TrainSegment> unconnectedTrains = new();

    public TrainSegment FrontTrain {
        set {
            if (value) {
                if (BackTrain && unconnectedTrains.Contains(this)) unconnectedTrains.Remove(this);
            } else {
                if (!Type.isCapWagon && !unconnectedTrains.Contains(this)) unconnectedTrains.Add(this);
            }

            GetRelativeMostFrontTrain();
            frontTrain = value;
        }

        get {
            return frontTrain;
        }
    }
    private TrainSegment frontTrain;

    public TrainSegment BackTrain {
        set {
            if (value) {
                if ((FrontTrain || Type.isCapWagon) && unconnectedTrains.Contains(this)) unconnectedTrains.Remove(this);
            } else {
                if (!unconnectedTrains.Contains(this)) unconnectedTrains.Add(this);
            }

            GetRelativeMostFrontTrain();
            backTrain = value;
        }

        get {
            return backTrain;
        }
    }
    private TrainSegment backTrain;

    public TrainSegment mostFrontTrain;

    public Transform frontPin;
    public Transform backPin;

    public new float velocity = 20f; // Velocity on the track
    public float distance; // Distance on the track
    public TrainTrack track;

    public bool isMovingForward;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);

        frontPin = new GameObject("Front pin").transform;
        frontPin.parent = transform;
        frontPin.localPosition = new Vector3(0, Type.connectionPinOffset, 0);

        backPin = new GameObject("Back pin").transform;
        backPin.parent = transform;
        backPin.localPosition = new Vector3(0, -Type.connectionPinOffset, 0);

        FrontTrain = BackTrain = null;

        track = PlayerManager.track;

        foreach(TrainSegment train in unconnectedTrains) {
            if (train == this) continue;

            if (!train.BackTrain && !Type.isCapWagon) {
                train.BackTrain = this;
                FrontTrain = train;
                break;
            }
        }
    }

    public bool IsMovingForward() {
        return velocity > 0;
    }

    public bool IsFront() {
        return mostFrontTrain == this;
    }

    public TrainSegment GetRelativeMostFrontTrain() {
        TrainSegment train = IsMovingForward() ? FrontTrain : BackTrain;
        return train ? train.GetRelativeMostFrontTrain() : this;
    }

    public TrainSegment GetRelativeBackTrain() {
        return IsMovingForward() ? BackTrain : FrontTrain;
    }
    
    public void SetDistance(float distance) {
        this.distance = distance;
        transform.position = track.GetPositionAtDistance(this.distance);
    }

    public void OnChangedDirection() {
        mostFrontTrain = GetRelativeMostFrontTrain();
    }

    protected override void Update() {
        base.Update();

        UpdateConnections();

        bool prevDirection = isMovingForward;
        isMovingForward = IsMovingForward();

        if (prevDirection != isMovingForward) OnChangedDirection();

        if (IsFront()) {
            distance += velocity * Time.deltaTime;
            UpdatePosition();
        }
    }

    protected override void FixedUpdate() {
        // Dont want usual unit behaviour
    }

    public void UpdatePosition() {
        transform.position = track.GetPositionAtDistance(distance);

        TrainSegment backTrain = GetRelativeBackTrain();

        // Update back train's position
        if (backTrain) {
            float backDistance = Type.connectionPinOffset + backTrain.Type.connectionPinOffset;
            backTrain.SetDistance(distance + (backDistance * -Mathf.Sign(velocity)));
            backTrain.UpdatePosition();
        }
 
        // Get the facing point
        float midPoint = distance + (Type.connectionPinOffset * Mathf.Sign(velocity));
        Vector2 midPosition = track.GetPositionAtDistance(midPoint);

        // Quirky quaternion stuff to make the unit rotate slowly -DO NOT TOUCH-
        Quaternion desiredRotation = Quaternion.LookRotation(Vector3.forward, (midPosition - GetPosition()).normalized);
        desiredRotation = Quaternion.Euler(0, 0, desiredRotation.eulerAngles.z);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, Type.rotationSpeed * Time.fixedDeltaTime);
    }

    private void UpdateConnections() {
        if ((FrontTrain || Type.isCapWagon) && BackTrain) return;

        foreach (TrainSegment train in unconnectedTrains) {
            if (train == this || FrontTrain == train || BackTrain == train) continue;

            if (!train.FrontTrain && !BackTrain && ValidConnection(train.frontPin, backPin)) {
                train.FrontTrain = this;
                BackTrain = train;
                break;
            } else if (!Type.isCapWagon && !train.BackTrain && !FrontTrain && ValidConnection(train.backPin, frontPin)) {
                train.BackTrain = this;
                FrontTrain = train;
                break;
            }
        }
    }

    private bool ValidConnection(Transform transform1, Transform transform2) {
        return Vector2.Distance(transform1.position, transform2.position) <= Type.connectionPinMaxDistance;
    }

    public override void HandleHeight() {
        throw new NotImplementedException();
    }

    protected override bool StopsToShoot() {
        return false;
    }

    public override void Tilt(float value) {
        
    }

    public override bool IsFleeing() {
        return false;
    }
}