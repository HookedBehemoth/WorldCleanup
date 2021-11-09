/*
 * Copyright (c) 2021 HookedBehemoth
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms and conditions of the GNU General Public License,
 * version 3, as published by the Free Software Foundation.
 *
 * This program is distributed in the hope it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License for
 * more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using UnityEngine;

namespace WorldCleanup.UI {
    public class Updater : MonoBehaviour {
        private float timer = 0f;

        public Action callback;

        public Updater(IntPtr ptr) : base(ptr) { }

        void Update() {
            timer += Time.deltaTime;
            var interval = Settings.s_UpdateInterval.Value;
            if (timer > interval) {
                timer -= interval;
                callback();
            }
        }
    }
}