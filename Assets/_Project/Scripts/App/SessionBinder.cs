using System;
using Fireflies.Session;

namespace Fireflies.App
{
    // Evita que cada componente repita la suscripción al arranque y cierre de la sesión del runner.
    public sealed class SessionBinder
    {
        private readonly SessionRunner _runner;
        private readonly Action<IMatchSession> _onAttached;
        private readonly Action<IMatchSession> _onDetached;

        public SessionBinder(SessionRunner runner, Action<IMatchSession> onAttached, Action<IMatchSession> onDetached)
        {
            _runner = runner;
            _onAttached = onAttached;
            _onDetached = onDetached;
        }

        public IMatchSession Session { get; private set; }

        public void Enable()
        {
            _runner.SessionStarted += Attach;
            _runner.SessionEnded += Detach;
            if (_runner.Session != null)
            {
                Attach(_runner.Session);
            }
        }

        public void Disable()
        {
            _runner.SessionStarted -= Attach;
            _runner.SessionEnded -= Detach;
            Detach(SessionEndReason.LeftByUser);
        }

        private void Attach(IMatchSession session)
        {
            Session = session;
            _onAttached(session);
        }

        private void Detach(SessionEndReason reason)
        {
            if (Session == null)
            {
                return;
            }

            IMatchSession previous = Session;
            Session = null;
            _onDetached(previous);
        }
    }
}
