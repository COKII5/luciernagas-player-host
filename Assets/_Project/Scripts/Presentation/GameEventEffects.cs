using Fireflies.App;
using Fireflies.Config;
using Fireflies.Game.State;
using Fireflies.Session;
using UnityEngine;

namespace Fireflies.Presentation
{
    public sealed class GameEventEffects : MonoBehaviour
    {
        [SerializeField] private SessionRunner _runner;
        [SerializeField] private WorldPresenter _world;
        [SerializeField] private PlayerPalette _palette;
        [SerializeField] private ParticleSystem _depositBurst;
        [SerializeField] private ParticleSystem _hitBurst;
        [SerializeField] private float _burstHeight = 1.2f;
        [SerializeField, Range(1, 200)] private int _particlesPerPoint = 6;
        [SerializeField, Range(1, 200)] private int _hitParticleCount = 18;

        private SessionBinder _binder;

        private void OnEnable()
        {
            _binder = new SessionBinder(_runner, session => session.GameEventRaised += Play, session => session.GameEventRaised -= Play);
            _binder.Enable();
        }

        private void OnDisable()
        {
            _binder.Disable();
        }

        private void Play(GameEvent gameEvent)
        {
            switch (gameEvent.Type)
            {
                case GameEventType.Deposited:
                    PlayDeposit(gameEvent);
                    break;
                case GameEventType.DashHit when _world.TryGetPlayerPosition(gameEvent.OtherPlayerId, out Vector3 victimPosition):
                    Emit(_hitBurst, victimPosition, Color.white, _hitParticleCount);
                    break;
            }
        }

        private void PlayDeposit(GameEvent gameEvent)
        {
            PlayerInfo player = _binder.Session.FindPlayer(gameEvent.PlayerId);
            if (player == null)
            {
                return;
            }

            int slot = player.Slot;
            _world.PulseAltar(slot);
            Emit(_depositBurst, _world.GetAltarPosition(slot), _palette.GetColor(slot), gameEvent.Amount * _particlesPerPoint);
        }

        private void Emit(ParticleSystem system, Vector3 position, Color color, int count)
        {
            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = position + Vector3.up * _burstHeight,
                startColor = color,
                applyShapeToPosition = true
            };
            system.Emit(emitParams, count);
        }
    }
}
