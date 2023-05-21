using System;
using System.Collections.Generic;
using UnityEngine;

namespace PunIntended.Tools
{
    public class UnityEventManager : LazyMonoBehaviourSingleton<UnityEventManager>
    {
        private static event Action PrivateOnUpdate;
        private static event Action PrivateOnFixedUpdate;
        private static event Action PrivateOnLateUpdate;
        private static event Action PrivateOnGUIUpdate;

        public static event Action OnUpdate
        {
            add
            {
                LazyCheck();
                PrivateOnUpdate += value;
            }
            remove
            {
                LazyCheck();
                PrivateOnUpdate -= value;
            }
        }

        public static event Action OnFixedUpdate
        {
            add
            {
                LazyCheck();
                PrivateOnFixedUpdate += value;
            }
            remove
            {
                LazyCheck();
                PrivateOnFixedUpdate -= value;
            }
        }

        public static event Action OnLateUpdate
        {
            add
            {
                LazyCheck();
                PrivateOnLateUpdate += value;
            }
            remove
            {
                LazyCheck();
                PrivateOnLateUpdate -= value;
            }
        }

        public static event Action OnGUIUpdate
        {
            add
            {
                LazyCheck();
                PrivateOnGUIUpdate += value;
            }
            remove
            {
                LazyCheck();
                PrivateOnGUIUpdate -= value;
            }
        }

        private void Update() =>        PrivateOnUpdate?.Invoke();
        private void FixedUpdate() =>   PrivateOnFixedUpdate?.Invoke();
        private void LateUpdate() =>    PrivateOnLateUpdate?.Invoke();
        private void OnGUI() =>         PrivateOnGUIUpdate?.Invoke();
    }

    public abstract class LazyMonoBehaviourSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Singleton { get; private set; }

        protected static void LazyCheck()
        {
            if (Singleton == null)
            {
                GameObject consoleGameObject = new GameObject(typeof(T).Name);
                Singleton = consoleGameObject.AddComponent<T>();
                DontDestroyOnLoad(consoleGameObject);
            }
        }
    }
}
