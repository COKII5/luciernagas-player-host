using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Fireflies.Config;
using Fireflies.Net.Connection;
using Fireflies.UI.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fireflies.UI.Screens
{
    public sealed class MainMenuScreen : SessionScreen
    {
        private const string NamePrefsKey = "fireflies.player_name";
        private const string SlotPrefsKey = "fireflies.player_slot";
        private const string AddressPrefsKey = "fireflies.join_address";
        private const string PortPrefsKey = "fireflies.port";
        private const string DefaultJoinAddress = "127.0.0.1";
        private const string SelectedColorClass = "color-option--selected";
        private const string InvalidFieldClass = "field--invalid";
        private const int MinPort = 1024;

        [SerializeField] private PlayerPalette _palette;

        private readonly List<Button> _colorOptions = new();
        private TextField _nameField;
        private TextField _hostPortField;
        private TextField _joinAddressField;
        private TextField _joinPortField;
        private Label _colorName;
        private Label _status;
        private byte _selectedSlot;

        protected override string RootElementName => "main-menu";

        protected override void BindElements()
        {
            _nameField = Root.Q<TextField>("player-name");
            _hostPortField = Root.Q<TextField>("host-port");
            _joinAddressField = Root.Q<TextField>("join-address");
            _joinPortField = Root.Q<TextField>("join-port");
            _colorName = Root.Q<Label>("color-name");
            _status = Root.Q<Label>("menu-status");

            Root.Q<Button>("host-button").clicked += HostSession;
            Root.Q<Button>("join-button").clicked += JoinSession;
            Root.Q<Button>("quit-button").clicked += Application.Quit;
            UiFactory.MakeButtonsUnfocusable(Root);

            BuildColorPicker(Root.Q("color-picker"));
            LoadPreferences();
        }

        public void ShowStatus(string message)
        {
            _status.text = message;
        }

        protected override void OnShown()
        {
            _status.text = string.Empty;
        }

        private void BuildColorPicker(VisualElement container)
        {
            container.Clear();
            _colorOptions.Clear();
            for (byte slot = 0; slot < _palette.SlotCount; slot++)
            {
                byte chosenSlot = slot;
                Button option = UiFactory.Button(string.Empty, () => SelectSlot(chosenSlot), "color-option");
                option.style.backgroundImage = new StyleBackground(_palette.GetPortrait(slot));
                option.tooltip = _palette.GetColorName(slot);
                _colorOptions.Add(option);
                container.Add(option);
            }
        }

        private void SelectSlot(byte slot)
        {
            _selectedSlot = slot;
            for (int i = 0; i < _colorOptions.Count; i++)
            {
                bool isSelected = i == slot;
                _colorOptions[i].EnableInClassList(SelectedColorClass, isSelected);
                StyleColor border = isSelected ? new StyleColor(_palette.GetColor(i)) : new StyleColor(StyleKeyword.Null);
                SetBorderColor(_colorOptions[i], border);
            }

            _colorName.text = _palette.GetColorName(slot);
            _colorName.style.color = _palette.GetColor(slot);
        }

        private static void SetBorderColor(VisualElement element, StyleColor color)
        {
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
        }

        private void HostSession()
        {
            if (!TryReadName(out string playerName) || !TryReadPort(_hostPortField, out int port))
            {
                return;
            }

            SavePreferences();
            if (!Runner.TryHost(playerName, _selectedSlot, port, out string error))
            {
                ShowStatus(error);
            }
        }

        private void JoinSession()
        {
            bool nameIsValid = TryReadName(out string playerName);
            bool addressIsValid = TryReadAddress(out IPAddress address);
            bool portIsValid = TryReadPort(_joinPortField, out int port);
            if (!nameIsValid || !addressIsValid || !portIsValid)
            {
                return;
            }

            SavePreferences();
            if (!Runner.TryJoin(new IPEndPoint(address, port), playerName, _selectedSlot, out string error))
            {
                ShowStatus(error);
            }
        }

        private bool TryReadName(out string playerName)
        {
            playerName = PlayerNameRules.Normalize(_nameField.value);
            return Validate(_nameField, PlayerNameRules.IsValid(playerName), "Escribe un nombre de 1 a 16 letras o números.");
        }

        private bool TryReadAddress(out IPAddress address)
        {
            bool isValid = IPAddress.TryParse(_joinAddressField.value.Trim(), out address) && address.AddressFamily == AddressFamily.InterNetwork;
            return Validate(_joinAddressField, isValid, "La IP debe tener el formato 192.168.1.20.");
        }

        private bool TryReadPort(TextField field, out int port)
        {
            bool isValid = int.TryParse(field.value.Trim(), out port) && port >= MinPort && port <= IPEndPoint.MaxPort;
            return Validate(field, isValid, "El puerto debe ser un número entre 1024 y 65535.");
        }

        private bool Validate(TextField field, bool isValid, string errorMessage)
        {
            field.EnableInClassList(InvalidFieldClass, !isValid);
            if (!isValid)
            {
                ShowStatus(errorMessage);
            }

            return isValid;
        }

        private void LoadPreferences()
        {
            string defaultPort = Runner.NetworkSettings.DefaultPort.ToString();
            _nameField.value = PlayerPrefs.GetString(NamePrefsKey, string.Empty);
            _joinAddressField.value = PlayerPrefs.GetString(AddressPrefsKey, DefaultJoinAddress);
            _hostPortField.value = PlayerPrefs.GetString(PortPrefsKey, defaultPort);
            _joinPortField.value = _hostPortField.value;
            int savedSlot = Mathf.Clamp(PlayerPrefs.GetInt(SlotPrefsKey, 0), 0, _palette.SlotCount - 1);
            SelectSlot((byte)savedSlot);
        }

        private void SavePreferences()
        {
            PlayerPrefs.SetString(NamePrefsKey, _nameField.value.Trim());
            PlayerPrefs.SetString(AddressPrefsKey, _joinAddressField.value.Trim());
            PlayerPrefs.SetString(PortPrefsKey, _hostPortField.value.Trim());
            PlayerPrefs.SetInt(SlotPrefsKey, _selectedSlot);
            PlayerPrefs.Save();
        }
    }
}
