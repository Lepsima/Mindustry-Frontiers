using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;

namespace Frontiers.Windows {
    public class WindowSettings {   
        // The handler associated to this window
        public WindowHandler handler;

        // The depth of the window, for draw order
        public int depth = 10;

        // Whether to draw the window or not
        public bool visible = true;

        // Whether to update the window or not
        public bool update = true;
    }

    public class ConsoleSettings : WindowSettings {
        // Does show text immediately?
        public bool immediateOutput = false;

        // Time between each letter
        public float letterSpacing = 0.01f;

        // Time between each string 
        public float stringSpacing = 0.5f;
    }

    public class Window {
        // Settings for this window
        public WindowSettings settings;

        // Window unique id
        public short id;

        // Name for the window
        public string name;

        public Window(WindowHandler handler, short id, string name) {
            this.handler = handler;
            this.id = id;
            this.name = name;
        }
    }

    public class Console : Window {
        public List<string> lines = new List<string>();

        public Console(WindowHandler handler, short id, string name) : base(handler, id, name) {

        }
    }
}