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

using UnityEngine;

namespace WorldCleanup {
    class RefCountedObject<T> where T: Object {
        int m_Count;
        T m_Value;
        public RefCountedObject(T value) {
            m_Count = 1;
            m_Value = value; 
        }
        public T Get() => m_Value;
        public void Increment() { ++m_Count; }
        public bool Decrement() {
            if (--m_Count == 0) {
                Object.DestroyImmediate(m_Value);
                m_Value = null;
                return true;
            } else {
                return false;
            }
        }
    }
}
