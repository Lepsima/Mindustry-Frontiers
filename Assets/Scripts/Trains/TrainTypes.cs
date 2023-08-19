using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Frontiers.Content;
using System;

namespace Frontiers.Content {
    public class TrainEntityType : EntityType {
        public bool isCapWagon = false;
        public float connectionPinOffset = 1f;
        public float connectionPinMaxDistance = 0.1f;

        public TrainEntityType(string name, Type type, int tier = 1) : base(name, type, tier) {

        }
    }
}