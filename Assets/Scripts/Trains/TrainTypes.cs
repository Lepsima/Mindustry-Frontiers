using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using System;

namespace Frontiers.Content {
    public class TrainType : UnitType {
        public bool isCapWagon = false;
        public float connectionPinOffset = 2.4f;
        public float connectionPinMaxDistance = 0.1f;

        public TrainType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }
}