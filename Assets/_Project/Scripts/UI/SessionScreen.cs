using Fireflies.App;
using Fireflies.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fireflies.UI
{
    // Base de cada pantalla: busca su raíz en el documento y se engancha/desengancha de la sesión activa.
    public abstract class SessionScreen : MonoBehaviour
    {
        private const string HiddenClass = "screen--hidden";

        [SerializeField] private UIDocument _document;
        [SerializeField] private SessionRunner _runner;

        protected VisualElement Root { get; private set; }
        protected SessionRunner Runner => _runner;
        protected IMatchSession Session { get; private set; }
        protected IHostAdministration HostAdministration => Session as IHostAdministration;

        protected abstract string RootElementName { get; }

        public bool IsVisible => Root != null && !Root.ClassListContains(HiddenClass);

        protected virtual void OnEnable()
        {
            Root = _document.rootVisualElement.Q(RootElementName);
            BindElements();
            _runner.SessionStarted += AttachSession;
            _runner.SessionEnded += DetachSession;
            if (_runner.Session != null)
            {
                AttachSession(_runner.Session);
            }
        }

        protected virtual void OnDisable()
        {
            _runner.SessionStarted -= AttachSession;
            _runner.SessionEnded -= DetachSession;
            DetachSession(SessionEndReason.LeftByUser);
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

        private void AttachSession(IMatchSession session)
        {
            Session = session;
            OnSessionAttached(session);
        }

        private void DetachSession(SessionEndReason reason)
        {
            if (Session == null)
            {
                return;
            }

            IMatchSession previous = Session;
            Session = null;
            OnSessionDetached(previous);
        }
    }
}
