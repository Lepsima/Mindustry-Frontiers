using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using TMPro;

namespace Frontiers.Windows {

    public class Window : MonoBehaviour {
        // The handler associated to this window
        [HideInInspector] public WindowHandler handler;

        // Window unique id
        [HideInInspector] public short id;

        public void Move(Vector2 position) {
            transform.position = position;
        }

        public void Maximize() {

        }

        public void Minimize() {

        }

        public virtual void Open(WindowHandler handler, short id, string name) {
            this.handler = handler;
            this.id = id;
            this.name = name;
        }

        public void Close() {

        }

        protected virtual void Update() {

        }
    }
}