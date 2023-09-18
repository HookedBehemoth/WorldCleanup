/*
 * Copyright (c) 2021 knah
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
using Il2CppInterop.Runtime.Attributes;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace UIExpansionKit.Components
{
    public class DestroyListener : MonoBehaviour
    {

        [method:HideFromIl2Cpp]
        public event Action OnDestroyed;

        public DestroyListener(IntPtr obj0) : base(obj0)
        {
        }

        private void OnDestroy()
        {
            OnDestroyed.Invoke();
        }

        public static void Register() {
            ClassInjector.RegisterTypeInIl2Cpp<DestroyListener>();
        }
    }
}