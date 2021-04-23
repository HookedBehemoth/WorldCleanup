using System;
using UnityEngine;

namespace WorldCleanup.UI {
    public class Updater : MonoBehaviour {
        public static float s_UpdateInterval = Settings.s_UpdateInterval;
        private float timer = 0f;

        public Action callback;

        public Updater(IntPtr ptr) : base(ptr) { }

        void Update() {
            timer += Time.deltaTime;
            if (timer > s_UpdateInterval) {
                timer -= s_UpdateInterval;
                callback();
            }
        }
    }
}