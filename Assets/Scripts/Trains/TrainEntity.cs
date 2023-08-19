using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using Frontiers.Assets;
using System;

public class TrainEntity : Entity {
    public new TrainEntityType Type { get => (TrainEntityType)base.Type; protected set => base.Type = value; }

    public static List<TrainEntity> unconnectedTrains = new();

    public TrainEntity FrontTrain {
        set {
            if (value) {
                if (BackTrain && unconnectedTrains.Contains(this)) unconnectedTrains.Remove(this);
            } else {
                if (!Type.isCapWagon && !unconnectedTrains.Contains(this)) unconnectedTrains.Add(this);
            }

            frontTrain = value;
        }

        get {
            return frontTrain;
        }
    }
    private TrainEntity frontTrain;

    public TrainEntity BackTrain {
        set {
            if (value) {
                if ((FrontTrain || Type.isCapWagon) && unconnectedTrains.Contains(this)) unconnectedTrains.Remove(this);
            } else {
                if (!unconnectedTrains.Contains(this)) unconnectedTrains.Add(this);
            }

            backTrain = value;
        }

        get {
            return backTrain;
        }
    }
    private TrainEntity backTrain;

    public Transform frontPin;
    public Transform backPin;

    public float velocity;
    public TrainTrack track;

    public override void Set<T>(Vector2 position, Quaternion rotation, T type, int id, byte teamCode) {
        base.Set(position, rotation, type, id, teamCode);

        frontPin = new GameObject("Front pin").transform;
        frontPin.parent = transform;
        frontPin.localPosition = new Vector3(0, Type.connectionPinOffset, 0);

        backPin = new GameObject("Back pin").transform;
        backPin.parent = transform;
        backPin.localPosition = new Vector3(0, -Type.connectionPinOffset, 0);

        FrontTrain = BackTrain = null;
    }

    public override EntityType GetEntityType() {
        return Type;
    }

    public override void OnInventoryValueChange(object sender, EventArgs e) {

    }

    protected override void SetSprites() {

    }

    public void Update() {
        UpdateConnections();

    }

    private void UpdateConnections() {
        if (!Type.isCapWagon && !FrontTrain) TryConnect(frontPin, true);
        if (!BackTrain) TryConnect(backPin, false);
    }

    private void TryConnect(Transform connectionPin, bool isFront) {
        TrainEntity cTrain = null;

        foreach(TrainEntity train in unconnectedTrains) {
            if (train.FrontTrain == null && ValidConnection(train.frontPin, connectionPin)) {
                cTrain = train;
                train.FrontTrain = this;
                break;

            } else if (train.BackTrain == null && ValidConnection(train.backPin, connectionPin)) {
                cTrain = train;
                train.BackTrain = this;
                break;
            }
        }

        if (isFront) FrontTrain = cTrain;
        else BackTrain = cTrain; 
    }

    private bool ValidConnection(Transform transform1, Transform transform2) {
        return Vector2.Distance(transform1.position, transform2.position) <= Type.connectionPinMaxDistance;
    }
}