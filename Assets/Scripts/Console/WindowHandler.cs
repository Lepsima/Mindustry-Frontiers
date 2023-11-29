using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Frontiers.Windows {
    
    public class WindowHandler {
        public static List<WindowHandler> windowHandlers = new();

        // The windows handled by this instance
        public List<Window> activeWindows = new();
        private short nextWindowID = 0;

        // The size given to this handler, doesnt need to match the user's screen size
        public Vector2 screenSize;

        // The offset of the handler in the user's screen
        public Vector2 screenOffset;

        // The rect transform in the canvas
        public RectTransform transform;

        public WindowHandler(Vector2 size, Vector2 offset) {
            screenSize = size;
            screenOffset = offset;

            windowHandlers.Add(this);
        }

        public void Close() {
            foreach(Window window in activeWindows) {
                window.Close();
            }

            windowHandlers.Remove(this);
        }

        public void OpenWindow(Window window) {
            if (nextWindowID == short.MaxValue) {
                // Will be funny if happens to someone
                Close();
                return;
            }

            //window.Open(this, nextWindowID);
            nextWindowID++;
        }

        public void Move(Vector2 position) {
            screenOffset = position;
        }

        public Vector2 ScreenToHandlerPosition(Vector2 screenPosition) {
            return screenPosition - screenOffset;
        }
    }
}