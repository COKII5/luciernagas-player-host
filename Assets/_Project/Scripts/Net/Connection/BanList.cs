using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using UnityEngine;

namespace Fireflies.Net.Connection
{
    // Se guarda en disco para que el ban siga vigente aunque el host cierre y vuelva a abrir el juego.
    public sealed class BanList
    {
        private readonly string _filePath;
        private StoredBanList _stored = new();

        public BanList(string fileName)
        {
            _filePath = Path.Combine(Application.persistentDataPath, fileName);
        }

        public event Action Changed;

        public IReadOnlyList<string> Addresses => _stored.BannedAddresses;

        public bool Contains(IPAddress address)
        {
            return _stored.BannedAddresses.Contains(address.ToString());
        }

        public void Add(IPAddress address)
        {
            string addressText = address.ToString();
            if (_stored.BannedAddresses.Contains(addressText))
            {
                return;
            }

            _stored.BannedAddresses.Add(addressText);
            Save();
            Changed?.Invoke();
        }

        public void Remove(string address)
        {
            if (_stored.BannedAddresses.Remove(address))
            {
                Save();
                Changed?.Invoke();
            }
        }

        public void Load()
        {
            if (!File.Exists(_filePath))
            {
                return;
            }

            try
            {
                StoredBanList loaded = JsonUtility.FromJson<StoredBanList>(File.ReadAllText(_filePath));
                _stored = loaded ?? new StoredBanList();
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                Debug.LogWarning($"[BanList] Could not read {_filePath}: {exception.Message}");
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllText(_filePath, JsonUtility.ToJson(_stored, true));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[BanList] Could not write {_filePath}: {exception.Message}");
            }
        }

        [Serializable]
        private sealed class StoredBanList
        {
            [SerializeField] private List<string> _bannedAddresses = new();

            public List<string> BannedAddresses => _bannedAddresses ??= new List<string>();
        }
    }
}
