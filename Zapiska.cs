using System;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Zapiska", "Greyghost", "2.0.0")]
    internal class Zapiska : RustPlugin
    {
        #region Var

        private Configuration _config;

        private enum TypeZapiski
        {
            OnZRespawn,
            OnZConnect,
            OnZBoth
        }

        #endregion

        #region Config

        private class Configuration
        {
            [JsonProperty("Когда создается записка:(0 - При респавне, 1 - При коннекте, 2 - В обоих случаях)")]
            public TypeZapiski TypeZ = TypeZapiski.OnZConnect;

            [JsonProperty("Текст русский")] public string Textnote = "Приветствуем на сервере";
            [JsonProperty("Текст английский")] public string Textnote_eng = "Welcome on server";
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();
                SaveConfig();
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        { 
            Config.WriteObject(_config);
        }

        protected override void LoadDefaultConfig() 
        {
            _config = new Configuration();
        }

        #endregion

        #region OxideHooks

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerConnected(BasePlayer player)
        {
            var type = _config.TypeZ;
            if (type == TypeZapiski.OnZRespawn) return;
            PrintWarning("Выдаем записку при коннекте");
            GiveZapiska(player);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerRespawned(BasePlayer player)
        {
            var type = _config.TypeZ;
            if (type == TypeZapiski.OnZRespawn) return;
            PrintWarning("Выдаем записку при респавне");
            GiveZapiska(player);
        }

        #endregion

        #region Methods

        // ReSharper disable once UnusedMember.Local
        private void GiveZapiska(BasePlayer player)
        {
            if (player == null) return;
            var item = ItemManager.CreateByName("note", 1, 0);
            var language = lang.GetLanguage(player.UserIDString);
            item.text = language == "ru" ? _config.Textnote : _config.Textnote_eng;
            player.GiveItem(item);
        }

        #endregion
    }
}