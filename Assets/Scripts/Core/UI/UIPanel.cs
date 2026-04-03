using UnityEngine;

namespace HammerAndSickle.Core.UI
{
    /// <summary>
    /// Base class for UI panels that can be shown, hidden, and focused.
    /// </summary>
    public class UIPanel : MonoBehaviour
    {
        [SerializeField] private GameObject root;

        public bool IsActive { get; private set; }

        public void Show()
        {
            IsActive = true;
            if (root) root.SetActive(true);
            OnShow();
        }

        public void Hide()
        {
            IsActive = false;
            if (root) root.SetActive(false);
            OnHide();
        }

        public void SetFocus(bool hasFocus)
        {
            OnFocusChanged(hasFocus);
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnFocusChanged(bool hasFocus) { }
    }
}
