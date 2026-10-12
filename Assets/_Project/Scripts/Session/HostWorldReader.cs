using System.Collections.Generic;
using Fireflies.Game.Simulation;
using Fireflies.Game.State;
using Fireflies.Net.Messages;
using Fireflies.Sync;
using UnityEngine;

namespace Fireflies.Session
{
    public sealed class HostWorldReader
    {
        private readonly List<PlayerSnapshot> _playerSnapshots = new();
        private readonly List<FireflySnapshot> _fireflySnapshots = new();

        public SnapshotMessage ComposeSnapshot(MatchSimulation simulation, uint tick, double hostTime, float secondsRemaining)
        {
            _playerSnapshots.Clear();
            foreach (SimPlayer player in simulation.Players)
            {
                _playerSnapshots.Add(player.ToSnapshot());
            }

            _fireflySnapshots.Clear();
            foreach (SimFirefly firefly in simulation.Field.Fireflies)
            {
                _fireflySnapshots.Add(firefly.ToSnapshot());
            }

            return new SnapshotMessage(tick, hostTime, secondsRemaining, _playerSnapshots, _fireflySnapshots);
        }

        // La simulación avanza a pasos fijos; mezclar los dos últimos evita tirones en pantallas de más Hz.
        public static void FillRenderState(MatchSimulation simulation, float fixedStepAlpha, WorldRenderState target)
        {
            target.Clear();
            foreach (SimPlayer player in simulation.Players)
            {
                PlayerRenderState state = SnapshotInterpolator.ToRenderState(player.ToSnapshot());
                state.Position = Vector2.Lerp(player.PreviousPosition, player.Motion.Position, fixedStepAlpha);
                target.Players.Add(state);
            }

            foreach (SimFirefly firefly in simulation.Field.Fireflies)
            {
                Vector2 position = Vector2.Lerp(firefly.PreviousPosition, firefly.Position, fixedStepAlpha);
                target.Fireflies.Add(new FireflyRenderState { FireflyId = firefly.FireflyId, Position = position, IsGolden = firefly.IsGolden });
            }
        }

        public static List<PlayerValue> CalculateStandings(MatchSimulation simulation)
        {
            List<PlayerValue> standings = new List<PlayerValue>(simulation.Players.Count);
            foreach (SimPlayer player in simulation.Players)
            {
                standings.Add(new PlayerValue(player.PlayerId, (ushort)player.Score));
            }

            standings.Sort((left, right) => right.Value.CompareTo(left.Value));
            return standings;
        }
    }
}
