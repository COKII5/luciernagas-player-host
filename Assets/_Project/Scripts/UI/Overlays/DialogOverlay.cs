using Fireflies.Session;
using UnityEngine.UIElements;

namespace Fireflies.UI.Overlays
{
    public sealed class DialogOverlay : SessionScreen
    {
        private Label _title;
        private Label _text;

        protected override string RootElementName => "dialog";

        protected override void BindElements()
        {
            _title = Root.Q<Label>("dialog-title");
            _text = Root.Q<Label>("dialog-text");
            Root.Q<Button>("dialog-ok").clicked += () => SetVisible(false);
        }

        public void ShowEndReason(SessionEndReason reason)
        {
            (string title, string text) = Describe(reason);
            _title.text = title;
            _text.text = text;
            SetVisible(true);
        }

        private static (string title, string text) Describe(SessionEndReason reason)
        {
            return reason switch
            {
                SessionEndReason.Kicked => ("Expulsado", "El anfitrión te sacó de la sala."),
                SessionEndReason.Banned => ("Baneado", "El anfitrión bloqueó tu dirección IP en esta sala."),
                SessionEndReason.HostClosed => ("Sala cerrada", "El anfitrión cerró la sala."),
                SessionEndReason.ConnectionLost => ("Conexión perdida", "Dejamos de recibir datos del anfitrión."),
                SessionEndReason.ConnectionFailed => ("Sin respuesta", "No se encontró una sala en esa IP y puerto. Revisa los datos y el firewall."),
                SessionEndReason.RejectedBanned => ("Acceso denegado", "Tu dirección IP está baneada en esta sala."),
                SessionEndReason.RejectedFull => ("Sala llena", "La sala ya tiene el máximo de jugadores."),
                SessionEndReason.RejectedInvalidName => ("Nombre no válido", "Usa de 1 a 16 letras, números, espacios o guiones."),
                SessionEndReason.RejectedVersion => ("Versión distinta", "El anfitrión usa otra versión del juego."),
                _ => ("Sesión terminada", string.Empty)
            };
        }
    }
}
