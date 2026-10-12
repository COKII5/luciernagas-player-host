using System;
using System.Collections.Generic;
using Fireflies.Game.Rules;
using UnityEngine.UIElements;

namespace Fireflies.UI.Widgets
{
    // Cada regla es una fila "etiqueta  [-] valor [+]". Los clientes ven la misma tabla sin botones.
    public sealed class RulesEditorView
    {
        private const string ReadOnlyClass = "rules-editor--readonly";
        private const int TargetScoreStep = 5;
        private const int DurationStepSeconds = 30;
        private const float SpawnIntervalStep = 0.2f;
        private const int FireflyLimitStep = 2;
        private const float DashCooldownStep = 0.5f;

        private static readonly RuleDefinition[] Definitions =
        {
            new RuleDefinition("Puntos para ganar", rules => rules.TargetScore.ToString(), (rules, step) => rules.TargetScore += step * TargetScoreStep),
            new RuleDefinition("Duración", rules => UiFormat.Clock(rules.DurationSeconds), (rules, step) => rules.DurationSeconds += step * DurationStepSeconds),
            new RuleDefinition("Aparición cada", rules => $"{rules.SpawnInterval:0.0} s", (rules, step) => rules.SpawnInterval += step * SpawnIntervalStep),
            new RuleDefinition("Máximo en el claro", rules => rules.FireflyLimit.ToString(), (rules, step) => rules.FireflyLimit += step * FireflyLimitStep),
            new RuleDefinition("Capacidad del farol", rules => rules.CarryCapacity.ToString(), (rules, step) => rules.CarryCapacity += step),
            new RuleDefinition("Recarga de embestida", rules => $"{rules.DashCooldown:0.0} s", (rules, step) => rules.DashCooldown += step * DashCooldownStep),
            new RuleDefinition("Embestida", rules => rules.DashEnabled ? "Sí" : "No", (rules, step) => rules.DashEnabled = !rules.DashEnabled)
        };

        private readonly VisualElement _container;
        private readonly List<Label> _valueLabels = new();
        private MatchRules _rules;
        private Action<MatchRules> _onChanged;

        public RulesEditorView(VisualElement container)
        {
            _container = container;
            BuildRows();
        }

        public void Show(MatchRules rules, bool isEditable, Action<MatchRules> onChanged)
        {
            _rules = rules;
            _onChanged = onChanged;
            _container.EnableInClassList(ReadOnlyClass, !isEditable);
            for (int i = 0; i < Definitions.Length; i++)
            {
                _valueLabels[i].text = Definitions[i].Format(rules);
            }
        }

        private void BuildRows()
        {
            foreach (RuleDefinition definition in Definitions)
            {
                VisualElement row = UiFactory.Element("rule-row");
                row.Add(UiFactory.Label(definition.Caption, "rule-row__label"));

                VisualElement controls = UiFactory.Element("rule-row__controls");
                Label value = UiFactory.Label(string.Empty, "rule-row__value");
                controls.Add(UiFactory.Button("-", () => ApplyStep(definition, -1), "button", "rule-step"));
                controls.Add(value);
                controls.Add(UiFactory.Button("+", () => ApplyStep(definition, 1), "button", "rule-step"));
                row.Add(controls);

                _valueLabels.Add(value);
                _container.Add(row);
            }
        }

        private void ApplyStep(RuleDefinition definition, int direction)
        {
            if (_rules == null || _onChanged == null)
            {
                return;
            }

            MatchRules edited = _rules.Clone();
            definition.Step(edited, direction);
            _onChanged(edited);
        }

        private readonly struct RuleDefinition
        {
            public RuleDefinition(string caption, Func<MatchRules, string> format, Action<MatchRules, int> step)
            {
                Caption = caption;
                Format = format;
                Step = step;
            }

            public string Caption { get; }
            public Func<MatchRules, string> Format { get; }
            public Action<MatchRules, int> Step { get; }
        }
    }
}
