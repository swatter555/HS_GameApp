using UnityEngine;

namespace HammerAndSickle.Core.Patterns
{
    /// <summary>
    /// Generic singleton base for MonoBehaviours.
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : Singleton<T>
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance == null) Instance = (T)this;
            else Destroy(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (Instance == (T)this) Instance = null;
        }
    }
}
