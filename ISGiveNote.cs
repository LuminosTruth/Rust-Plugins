using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("ISGiveNote", "MalfiSQ", "1.0.0")]
    [Description("Выдача записки")]
    public class ISGiveNote : RustPlugin
    {
        #region [Vars] / [Переменные]

        public enum NoteType
        {
            Respawn,
            Connected,
            Both
        }

        #endregion

        #region [Configuration] / [Конфигурация]

        private ConfigData _config;

        public class ConfigData
        {
            [JsonProperty(PropertyName = "ISGiveNote [Configuration]")]
            public NoteConfig NoteCFG = new NoteConfig();

            public class NoteConfig
            {
                [JsonProperty(PropertyName = "Записка на русском")]
                public string NoteRU;

                [JsonProperty(PropertyName = "Записка на английском")]
                public string NoteENG;

                [JsonProperty(PropertyName = "Когда выдается записка (0 - Respawn, 1 - Connected, 2 - Both)")]
                public List<NoteType> Type;
            }
        }

        public static ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                NoteCFG = new ConfigData.NoteConfig
                {
                    NoteRU = "huy1",
                    NoteENG = "huy2",
                    Type = new List<NoteType>
                    {
                        NoteType.Respawn
                    }
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                _config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("The config file is corrupted (or does not exist), a new one was created!");
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config);
        }

        #endregion

        #region [Methods] / [Методы]

        private void GiveNote(BasePlayer player)
        {
            var note = ItemManager.CreateByName("note");

            switch (lang.GetLanguage(player.UserIDString))
            {
                case "ru":
                    note.text = _config.NoteCFG.NoteRU;
                    break;
                case "eng":
                    note.text = _config.NoteCFG.NoteENG;
                    break;
                default:
                    note.text = _config.NoteCFG.NoteENG;
                    break;
            }

            timer.Once(1f, ()=> player.GiveItem(note));
        }

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerConnected(BasePlayer player)
        {
            if (_config.NoteCFG.Type.Contains(NoteType.Connected)) GiveNote(player);
            if (_config.NoteCFG.Type.Contains(NoteType.Both)) GiveNote(player);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerRespawned(BasePlayer player)
        {
            if (_config.NoteCFG.Type.Contains(NoteType.Respawn)) GiveNote(player);
            if (_config.NoteCFG.Type.Contains(NoteType.Both)) GiveNote(player);
        }

        #endregion
    }
}