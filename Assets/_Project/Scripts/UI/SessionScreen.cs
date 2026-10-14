using Fireflies.App;
using Fireflies.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fireflies.UI
{
    public abstract class SessionScreen : MonoBehaviour
    {
        private const string HiddenClass = "screen--hidden";

        [SerializeField] private UIDocument _document;
        [SerializeField] private SessionRunner _runner;

        private SessionBinder _binder;

        protected VisualElement Root { get; private set; }
        protected SessionRunner Runner => _runner;
        protected IMatchSession Session => _binder?.Session;
        protected IHostAdministration HostAdministration => Session as IHostAdministration;

        protected abstract string RootElementName { get; }

        public bool IsVisible => Root != null && !Root.ClassListContains(HiddenClass);

        protected virtual void OnEnable()
        {
            Root = _document.rootVisualElement.Q(RootElementName);
            BindElements();
            _binder = new SessionBinder(_runner, OnSessionAttached, OnSessionDetached);
            _binder.Enable();
        }

        protected virtual void OnDisable()
        {
            _binder.Disable();
        }

        public void SetVisible(bool visible)
        {
            if (Root == null || IsVisible == visible)
            {
                return;
            }

            Root.EnableInClassList(HiddenClass, !visible);
            if (visible)
            {
                OnShown();
            }
        }

        protected abstract void BindElements();

        protected virtual void OnSessionAttached(IMatchSession session)
        {
        }

        protected virtual void OnSessionDetached(IMatchSession session)
        {
        }

        protected virtual void OnShown()
        {
        }
    }
}
