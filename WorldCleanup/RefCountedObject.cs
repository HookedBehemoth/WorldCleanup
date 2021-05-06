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
