﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Predator Missile", "supreme", "1.1.1")]
    [Description("Allows players to call in controllable missile")]
    public class PredatorMissile : RustPlugin
    {
        #region Class Fields

#pragma warning disable CS0649
        [PluginReference] private Plugin ImageLibrary, NoEscape;
#pragma warning restore CS0649

        private static PredatorMissile _pluginInstance;
        private PluginConfig _pluginConfig;

        private const string UsePermission = "predatormissile.use";
        private const string AdminPermission = "predatormissile.admin";
        private const string RocketPrefab = "assets/prefabs/ammo/rocket/rocket_basic.prefab";
        private const string ChairPrefab = "assets/prefabs/vehicle/seats/standingdriver.prefab";
        private const string PlayerPrefab = "assets/prefabs/player/player.prefab";
        private const string UnlockEffectPrefab = "assets/prefabs/locks/keypad/effects/lock.code.unlock.prefab";

        private const string RocketExplosionEffectPrefab =
            "assets/content/vehicles/mlrs/effects/pfx_mlrs_rocket_explosion_air.prefab";

        private const string TremorEffectPrefab = "assets/bundled/prefabs/fx/takedamage_generic.prefab";
        private const string ShakeAdsEffectPrefab = "assets/prefabs/weapons/m92/effects/attack_shake_ads.prefab";
        private const string SmallOilrigName = "OilrigAI";
        private const string LargeOilrigName = "OilrigAI2";
        private const uint PredatorMissileIconId = 2764775859;

        private readonly UiPosition _containerPosition = new UiPosition(0f, 0f, 1f, 1f);
        private readonly UiPosition _loadingContainerPosition = new UiPosition(0.422f, 0.5315f, 0.577f, 0.558f);
        private readonly UiPosition _loadingLabelPosition = new UiPosition(0.098f, 0.41f, 1f, 1f);
        private readonly UiPosition _loadingPanelPosition = new UiPosition(0f, 0f, 0.99f, 0.32f);
        private readonly UiPosition _loadingImagePosition = new UiPosition(0.009f, 0.42f, 0.08f, 0.98f);

        private readonly Hash<ulong, Timer> _cachedTimers = new Hash<ulong, Timer>();
        private readonly Hash<ulong, DateTime> _cachedCooldowns = new Hash<ulong, DateTime>();
        private readonly Hash<string, Effect> _cachedEffects = new Hash<string, Effect>();

        private readonly Hash<BasePlayer, PredatorMissileController> _cachedComponents =
            new Hash<BasePlayer, PredatorMissileController>();

        private readonly Hash<BasePlayer, BasePlayer> _cachedClonedPlayers = new Hash<BasePlayer, BasePlayer>();
        private readonly HashSet<LootContainer> _cachedLootContainers = new HashSet<LootContainer>();
        private readonly Hash<string, Vector3> _cachedOilrigs = new Hash<string, Vector3>();

        private readonly object _returnObject = true;

        private ItemDefinition _hoodie;
        private ItemDefinition _pants;
        private ItemDefinition _boots;

        private enum PermissionValueType : byte
        {
            None = 0,
            Delay = 1,
            Radius = 2,
            Height = 3,
            Lifespan = 4
        }

        #endregion

        #region Hooks

        // ReSharper disable once UnusedMember.Local
        private void Init()
        {
            _pluginInstance = this;
            var perms = new HashSet<string>
            {
                UsePermission, AdminPermission
            };

            foreach (var perm in _pluginConfig.PermissionsValue.Keys)
            {
                perms.Add(perm);
            }

            foreach (var perm in perms)
            {
                permission.RegisterPermission(perm, this);
            }
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            LoadData();
            ImageLibrary.Call("AddImage", "https://i.postimg.cc/L6g4f5Ft/Predator-Missile.png", "PredatorMissileImage");
            ImageLibrary.Call("AddImage", "https://i.postimg.cc/wTvWJqck/Predator-Missile-Loading-Icon.png",
                "PredatorMissileLoadingIcon");

            _hoodie = ItemManager.FindItemDefinition("hoodie");
            _pants = ItemManager.FindItemDefinition("pants");
            _boots = ItemManager.FindItemDefinition("shoes.boots");

            FindOilrigs();
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyUis(player);
            }

            foreach (var component in _cachedComponents.Values)
            {
                component.DestroyThis();
            }

            _pluginInstance = null;
        }

        // ReSharper disable once UnusedMember.Local
        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (oldItem?.skin == PredatorMissileIconId && newItem?.skin != PredatorMissileIconId)
            {
                ResetActivation(player);
                return;
            }

            if (newItem?.skin != PredatorMissileIconId) return;
            if (!HasPermission(player, UsePermission)) return;
            SendTip(player, Lang(LangKeys.TipMessage, player));
            if (IsCooldown(player, "Use1"))
            {
                player.ChatMessage(
                    $"You can't use the rocket, it's on recharge : {ConvertTime(_dataBase.CooldownUse1[player.userID])}");
                return;
            }

            if (IsCooldown(player, "Use2"))
            {
                player.ChatMessage(
                    $"You can't use the rocket, it's on recharge : {ConvertTime(_dataBase.CooldownUse2[player.userID])}");
                return;
            }

            if (IsCooldown(player, "Use3"))
            {
                player.ChatMessage(
                    $"You can't use the rocket, it's on recharge : {ConvertTime(_dataBase.CooldownUse3[player.userID])}");
                return;
            }

            var requiredTime = GetPermissionValue(player, _pluginConfig.PermissionsValue, PermissionValueType.Delay);
            var time = 0;
            _cachedTimers[player.userID]?.Destroy();
            _cachedTimers[player.userID] = timer.Every(1f, () =>
            {
                if (!player.serverInput.IsDown(BUTTON.USE) || player.GetActiveItem()?.skin != PredatorMissileIconId)
                {
                    time = 0;
                    DestroyUis(player);
                    return;
                }

                time++;
                DisplayLoadingUi(player, time, requiredTime);
            });
        }

        // ReSharper disable once UnusedMember.Local
        private void OnPlayerDeath(BasePlayer clonedPlayer, HitInfo hitInfo)
        {
            var player = _cachedClonedPlayers[clonedPlayer];
            if (!player) return;
            _cachedComponents[player].Restore();
            _cachedComponents[player].DestroyThis();
            player.Die(new HitInfo(hitInfo.Initiator, player, hitInfo.damageTypes.GetMajorityDamageType(),
                hitInfo.damageTypes.Total()));
        }

        // ReSharper disable once UnusedMember.Local
        private object OnPlayerCorpseSpawn(BasePlayer clonedPlayer)
        {
            var player = _cachedClonedPlayers[clonedPlayer];
            return !player ? null : _returnObject;
        }

        // ReSharper disable once UnusedMember.Local
        private object OnTurretTarget(AutoTurret turret, BasePlayer clonedPlayer)
        {
            if (!clonedPlayer) return null;
            var player = _cachedClonedPlayers[clonedPlayer];
            if (!player) return null;
            return turret.IsAuthed(player) ? _returnObject : null;
        }

        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void OnLootEntity(BasePlayer player, LootContainer lootContainer)
        {
            if (_cachedLootContainers.Contains(lootContainer) ||
                !_pluginConfig.LootContainers.ContainsKey(lootContainer.ShortPrefabName)) return;


            if (UnityEngine.Random.Range(0f, 100f) <= _pluginConfig.LootContainers[lootContainer.ShortPrefabName])
            {
                if (lootContainer.inventory.IsFull())
                    lootContainer.inventory.capacity = lootContainer.inventory.capacity + 1;
                CreatePredatorMissileItem(1).MoveToContainer(lootContainer.inventory);
            }

            _cachedLootContainers.Add(lootContainer);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnEntityKill(LootContainer lootContainer)
        {
            if (!_cachedLootContainers.Contains(lootContainer)) return;
            _cachedLootContainers.Remove(lootContainer);
        }

        #endregion

        #region Core Methods

        private void InitializePredatorMissile(BasePlayer player)
        {
            var predatorMissileController = player.GetComponent<PredatorMissileController>();
            if (predatorMissileController) return;
            TakeRequestedItems(player, new Dictionary<string, int>
            {
                ["coal"] = 1
            });
            player.gameObject.AddComponent<PredatorMissileController>();
            if (HasPermission(player, "predatormissile.rocket3"))
                _dataBase.CooldownUse3.Add(player.userID, CurrentTime() + _pluginConfig.RocketCD3);
            else if (HasPermission(player, "predatormissile.rocket2"))
                _dataBase.CooldownUse2.Add(player.userID, CurrentTime() + _pluginConfig.RocketCD2);
            else if (HasPermission(player, "predatormissile.rocket1"))
                _dataBase.CooldownUse1.Add(player.userID, CurrentTime() + _pluginConfig.RocketCD1);
            else SendMessage(player, Lang(LangKeys.NoPermission, player));
            SaveData();
        }

        #endregion

        #region Helper Methods

        private bool HasPermission(BasePlayer player, string perm) =>
            permission.UserHasPermission(player.UserIDString, perm);

        private int GetPermissionValue(BasePlayer player, Hash<string, PermissionValues> permissions,
            PermissionValueType permissionValueType)
        {
            switch (permissionValueType)
            {
                case PermissionValueType.Delay:
                {
                    foreach (KeyValuePair<string, PermissionValues> perm in permissions.OrderBy(p => p.Value.Delay))
                    {
                        if (HasPermission(player, perm.Key))
                        {
                            return perm.Value.Delay;
                        }
                    }

                    return _pluginConfig.PermissionsValue[UsePermission].Delay;
                }
                case PermissionValueType.Radius:
                {
                    foreach (KeyValuePair<string, PermissionValues> perm in permissions.OrderByDescending(p =>
                                 p.Value.Radius))
                    {
                        if (HasPermission(player, perm.Key))
                        {
                            return perm.Value.Radius;
                        }
                    }

                    return _pluginConfig.PermissionsValue[UsePermission].Radius;
                }
                case PermissionValueType.Height:
                {
                    foreach (KeyValuePair<string, PermissionValues> perm in permissions.OrderByDescending(p =>
                                 p.Value.Height))
                        if (HasPermission(player, perm.Key))
                            return perm.Value.Height;
                    return _pluginConfig.PermissionsValue[UsePermission].Height;
                }
                case PermissionValueType.Lifespan:
                {
                    foreach (KeyValuePair<string, PermissionValues> perm in permissions.OrderByDescending(p =>
                                 p.Value.Lifespan))
                        if (HasPermission(player, perm.Key))
                            return perm.Value.Lifespan;
                    return _pluginConfig.PermissionsValue[UsePermission].Lifespan;
                }
            }

            return 0;
        }

        private static void DestroyUis(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "PredatorMissileUi");
            CuiHelper.DestroyUi(player, "LoadingUi");
            CuiHelper.DestroyUi(player, "LoadingProgressUi");
        }

        private void SendEffectTo(BasePlayer player, string effectPrefab)
        {
            var effect = _cachedEffects[effectPrefab];
            if (effect == null)
            {
                effect = new Effect(effectPrefab, Vector3.zero, Vector3.zero)
                {
                    attached = true
                };
                _cachedEffects[effectPrefab] = effect;
            }

            effect.entity = player.net.ID;
            EffectNetwork.Send(effect, player.net.connection);
        }

        private Item CreatePredatorMissileItem(int amount)
        {
            var predatorMissile = ItemManager.CreateByName("coal", amount, PredatorMissileIconId);
            predatorMissile.name = "Predator Missile";
            return predatorMissile;
        }

        private void ResetActivation(BasePlayer player)
        {
            _cachedTimers[player.userID]?.Destroy();
            DestroyUis(player);
        }

        private void TakeRequestedItems(BasePlayer player, Dictionary<string, int> requestedItems)
        {
            List<Item> items = Facepunch.Pool.GetList<Item>();
            foreach (KeyValuePair<string, int> item in requestedItems)
            {
                ItemDefinition itemDefinition = ItemManager.FindItemDefinition(item.Key);
                player.inventory.Take(items, itemDefinition.itemid, item.Value);
            }

            foreach (var item in items) item.Remove();
            Facepunch.Pool.FreeList(ref items);
        }

        private void SendTip(BasePlayer player, string text)
        {
            if (string.IsNullOrEmpty(text) || IsOnCooldown(player)) return;
            player.SendConsoleCommand("showtoast", 0, text);
            _cachedCooldowns[player.userID] = DateTime.UtcNow.AddSeconds(300f);
        }

        private BasePlayer FindPlayer(string arg)
        {
            return BasePlayer.activePlayerList.FirstOrDefault(p =>
                       p.displayName.Contains(arg, CompareOptions.OrdinalIgnoreCase) || p.UserIDString.Contains(arg))
                   ?? BasePlayer.sleepingPlayerList.FirstOrDefault(p =>
                       p.IsConnected && p.displayName.Contains(arg, CompareOptions.OrdinalIgnoreCase) ||
                       p.UserIDString.Contains(arg));
        }

        private bool IsOnCooldown(BasePlayer player)
        {
            return _cachedCooldowns[player.userID] > DateTime.UtcNow;
        }

        private string CanPlayerUsePredatorMissile(BasePlayer player)
        {
            if (_pluginConfig.Blocks.Mounted && player.isMounted) return Lang(LangKeys.Blocked.Mounted, player);
            if (_pluginConfig.Blocks.SafeZone && player.InSafeZone()) return Lang(LangKeys.Blocked.InSafeZone, player);
            if (_pluginConfig.Blocks.RaidBlock && IsRaidBlocked(player))
                return Lang(LangKeys.Blocked.RaidBlocked, player);
            if (_pluginConfig.Blocks.CombatBlock && IsCombatBlocked(player))
                return Lang(LangKeys.Blocked.CombatBlocked, player);
            if (_pluginConfig.Blocks.Swimming && player.IsSwimming()) return Lang(LangKeys.Blocked.Swimming, player);
            if (_pluginConfig.Blocks.Flying && player.IsFlying || player.isInAir)
                return Lang(LangKeys.Blocked.Flying, player);
            var parent = player.GetComponentInParent<BaseEntity>();
            if (parent)
            {
                if (_pluginConfig.Blocks.CargoShip && parent is CargoShip)
                    return Lang(LangKeys.Blocked.OnCargoShip, player);
                if (_pluginConfig.Blocks.HotAirBalloon && parent is HotAirBalloon)
                    return Lang(LangKeys.Blocked.OnHotAirBalloon, player);
                if (_pluginConfig.Blocks.Lift && parent is Lift) return Lang(LangKeys.Blocked.InLift, player);
                if (_pluginConfig.Blocks.ScrapHeli && parent is ScrapTransportHelicopter)
                    return Lang(LangKeys.Blocked.InScrapTransportHeli, player);
            }

            if (_pluginConfig.Blocks.NoBuildingPrivilege && !player.CanBuild())
                return Lang(LangKeys.Blocked.BuildingBlocked, player);
            if (_pluginConfig.Blocks.Oilrigs && IsOnRig(player)) return Lang(LangKeys.Blocked.OnOilrig, player);
            return string.Empty;
        }

        private bool IsRaidBlocked(BasePlayer player)
        {
            return NoEscape?.Call<bool>("IsRaidBlocked", player) ?? false;
        }

        private bool IsCombatBlocked(BasePlayer player)
        {
            return NoEscape?.Call<bool>("IsCombatBlocked", player) ?? false;
        }

        private void FindOilrigs()
        {
            foreach (var monumentInfo in TerrainMeta.Path.Monuments)
            {
                switch (monumentInfo.name)
                {
                    case SmallOilrigName:
                        _cachedOilrigs[SmallOilrigName] = monumentInfo.transform.position;
                        break;
                    case LargeOilrigName:
                        _cachedOilrigs[LargeOilrigName] = monumentInfo.transform.position;
                        break;
                }
            }
        }

        private bool IsOnRig(BasePlayer player)
        {
            return _cachedOilrigs.Values.Any(oilrigPosition =>
                Vector3.Distance(player.transform.position, oilrigPosition) < 80f);
        }

        #endregion

        #region Class Handlers

        private class PredatorMissileController : FacepunchBehaviour
        {
            private BasePlayer Player { get; set; }

            private BasePlayer ClonedPlayer { get; set; }

            private InventoryBackup Backup { get; set; }

            private TimedExplosive Rocket { get; set; }

            private BaseVehicleSeat Chair { get; set; }

            private Vector3 InitialPosition { get; set; }

            private List<Timer> Timers { get; set; } = new List<Timer>();

            private void Awake()
            {
                Player = GetComponent<BasePlayer>();
                Player.inventory.containerMain.SetLocked(true);
                Player.inventory.containerBelt.SetLocked(true);
                Player.inventory.containerWear.SetLocked(true);
                _pluginInstance._cachedComponents.Add(Player, this);
                Player.GetHeldEntity()?.SetHeld(false);
                Player.PauseFlyHackDetection(60f);
                Player.PauseSpeedHackDetection(60f);
                InitialPosition = Player.transform.position;
                Rocket = CreateRocket(Player);
                Chair = CreateChair(Rocket, Player);
                ClonedPlayer = CreateClone(Player);
                _pluginInstance._cachedClonedPlayers[ClonedPlayer] = Player;
                Backup = new InventoryBackup(Player, ClonedPlayer);
                Backup.Backup();
                _pluginInstance.DisplayMissileUi(Player);
                VanishPlayer(Player);
                Player.inventory.containerWear.AddItem(_pluginInstance._hoodie, 1, 883710255);
                Player.inventory.containerWear.AddItem(_pluginInstance._pants, 1, 883709785);
                Player.inventory.containerWear.AddItem(_pluginInstance._boots, 1, 883709405);

                Player.SendNetworkUpdateImmediate();
                Timers.Add(_pluginInstance.timer.Every(1f, () =>
                {
                    _pluginInstance.SendEffectTo(Player, UnlockEffectPrefab);
                    _pluginInstance.SendEffectTo(Player, ShakeAdsEffectPrefab);
                }));

                Timers.Add(_pluginInstance.timer.Once(0.15f, () =>
                {
                    _pluginInstance.SendEffectTo(Player, RocketExplosionEffectPrefab);
                    _pluginInstance.SendEffectTo(Player, RocketExplosionEffectPrefab);
                    _pluginInstance.SendEffectTo(Player, RocketExplosionEffectPrefab);
                }));

                Timers.Add(_pluginInstance.timer.Every(3f,
                    () => { _pluginInstance.SendEffectTo(Player, TremorEffectPrefab); }));
            }

            private void FixedUpdate()
            {
                if (!Player || !Rocket)
                {
                    DestroyThis();
                    return;
                }

                Rocket.transform.rotation = Player.eyes.rotation;
            }

            private TimedExplosive CreateRocket(BasePlayer player)
            {
                var rocket = GameManager.server.CreateEntity(RocketPrefab,
                    player.transform.position + new Vector3(0f,
                        _pluginInstance.GetPermissionValue(player, _pluginInstance._pluginConfig.PermissionsValue,
                            PermissionValueType.Height))) as TimedExplosive;
                if (!rocket) return null;
                rocket.Spawn();
                rocket.CancelInvoke(rocket.Explode);
                rocket.Invoke(rocket.Explode,
                    _pluginInstance.GetPermissionValue(player, _pluginInstance._pluginConfig.PermissionsValue,
                        PermissionValueType.Lifespan));
                rocket.globalBroadcast = true;
                rocket.creatorEntity = player;
                var radius = _pluginInstance.GetPermissionValue(player, _pluginInstance._pluginConfig.PermissionsValue,
                    PermissionValueType.Radius);
                rocket.minExplosionRadius = radius;
                rocket.explosionRadius = radius;
                return rocket;
            }

            private BaseVehicleSeat CreateChair(TimedExplosive rocket, BasePlayer player)
            {
                var chair = GameManager.server.CreateEntity(ChairPrefab) as BaseVehicleSeat;
                if (!chair) return null;
                chair.Spawn();
                chair.SetParent(rocket);
                Transform transform1;
                (transform1 = chair.transform).rotation = Quaternion.Euler(360f, 0f, 0f);
                transform1.position = rocket.transform.position + Vector3.down * 1.8f + Vector3.forward * 1f;
                player.MountObject(chair);
                return chair;
            }

            private BasePlayer CreateClone(BasePlayer player)
            {
                var clonedPlayer =
                    (BasePlayer) GameManager.server.CreateEntity(PlayerPrefab, InitialPosition,
                        player.transform.rotation);
                if (!clonedPlayer) return null;
                clonedPlayer.Spawn();
                clonedPlayer.displayName = player.displayName;
                clonedPlayer.health = player.health;
                clonedPlayer.SendNetworkUpdateImmediate();
                return clonedPlayer;
            }

            private void VanishPlayer(BasePlayer player)
            {
                player.OnNetworkSubscribersLeave(Net.sv.connections.Where(connection => connection.connected &&
                    connection.isAuthenticated &&
                    connection.player is BasePlayer && connection.player != player).ToList());
                player.DisablePlayerCollider();
                player.syncPosition = false;
                player._limitedNetworking = true;
            }

            private void UnVanishPlayer(BasePlayer player)
            {
                player._limitedNetworking = false;
                player.syncPosition = true;
                player.EnablePlayerCollider();
                player.UpdateNetworkGroup();
                player.SendNetworkUpdate();
            }

            public void Restore()
            {
                Backup.Restore();
            }

            public void DestroyThis()
            {
                Destroy(this);
            }

            private void OnDestroy()
            {
                UnVanishPlayer(Player);
                Player.inventory.containerMain.SetLocked(false);
                Player.inventory.containerBelt.SetLocked(false);
                Player.inventory.containerWear.SetLocked(false);
                DestroyUis(Player);
                foreach (Timer ts in Timers) ts?.Destroy();
                Player.DismountObject();
                Backup.Restore();
                _pluginInstance._cachedClonedPlayers.Remove(ClonedPlayer);
                ClonedPlayer.Kill();
                Player.Teleport(InitialPosition);
                Player.UpdateProtectionFromClothing();
                _pluginInstance._cachedComponents.Remove(Player);
            }
        }

        private class InventoryBackup
        {
            private BasePlayer Player { get; set; }

            private BasePlayer ClonedPlayer { get; set; }

            private bool Restored { get; set; }

            public InventoryBackup(BasePlayer player, BasePlayer clonedPlayer)
            {
                Player = player;
                ClonedPlayer = clonedPlayer;
            }

            public void Backup()
            {
                CopyItems(Player.inventory.containerMain.itemList, ClonedPlayer.inventory.containerMain.itemList);
                CopyItems(Player.inventory.containerBelt.itemList, ClonedPlayer.inventory.containerBelt.itemList);
                CopyItems(Player.inventory.containerWear.itemList, ClonedPlayer.inventory.containerWear.itemList);
                Player.inventory.SendSnapshot();
                ClonedPlayer.inventory.SendSnapshot();
            }

            public void Restore()
            {
                if (Restored) return;
                Restored = true;
                Player.inventory.Strip();
                CopyItems(ClonedPlayer.inventory.containerMain.itemList, Player.inventory.containerMain.itemList);
                CopyItems(ClonedPlayer.inventory.containerBelt.itemList, Player.inventory.containerBelt.itemList);
                CopyItems(ClonedPlayer.inventory.containerWear.itemList, Player.inventory.containerWear.itemList);
                Player.inventory.SendSnapshot();
                ClonedPlayer.inventory.SendSnapshot();
            }

            private void CopyItems(List<Item> from, List<Item> to)
            {
                to.AddRange(from);
                from.Clear();
            }
        }

        #endregion

        #region Commands

        [ChatCommand("rocket")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void GiveRocket(BasePlayer player, string command, string[] args)
        {
            if (permission.UserHasPermission(player.UserIDString, "predatormissile.rocket2"))
            {
                if (IsCooldown(player, "Give"))
                {
                    player.ChatMessage(
                        $"The rocket is being recharged : {ConvertTime(_dataBase.CooldownGive[player.userID])}");
                    return;
                }

                player.GiveItem(CreatePredatorMissileItem(1));
                _dataBase.CooldownGive.Add(player.userID, _pluginConfig.RocketGiveCD + CurrentTime());
            }
            else SendMessage(player, Lang(LangKeys.NoPermission, player));
        }

        [ConsoleCommand("cleadr")]
        // ReSharper disable once UnusedMember.Local
        private void asdasd(ConsoleSystem.Arg arg)
        {
            if (arg.Player() != null) return;
            _dataBase.CooldownGive.Clear();
        }

        private static string ConvertTime(int seconds)
        {
            var time = TimeSpan.FromSeconds(seconds - CurrentTime());
            return $"{time.Days}D {time.Hours}H {time.Minutes}M {time.Seconds}S";
        }

        [ConsoleCommand("predatormissile.give")]
        // ReSharper disable once UnusedMember.Local
        private void PredatorMissileGiveCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            if (player && !HasPermission(player, AdminPermission))
            {
                SendMessage(player, Lang(LangKeys.NoPermission, player));
                return;
            }

            if (arg.Args == null || arg.Args.Length < 2)
            {
                SendReply(arg, Lang(LangKeys.SyntaxError));
                return;
            }

            var target = FindPlayer(arg.Args[0]);
            if (!target)
            {
                SendReply(arg, Lang(LangKeys.TargetNotFound));
                return;
            }

            var amount = Convert.ToInt32(arg.Args[1]);
            target.GiveItem(CreatePredatorMissileItem(amount));
        }

        #endregion

        #region Configuration

        private class PluginConfig
        {
            [DefaultValue(0)]
            [JsonProperty(PropertyName = "Chat Icon Id")]
            public ulong ChatIconId { get; set; }

            [JsonProperty(PropertyName = "Rocket use1 cooldown")]
            public int RocketCD1 { get; set; }

            [JsonProperty(PropertyName = "Rocket use2 cooldown")]
            public int RocketCD2 { get; set; }

            [JsonProperty(PropertyName = "Rocket use3 cooldown")]
            public int RocketCD3 { get; set; }

            [JsonProperty(PropertyName = "Rocket give cooldown")]
            public int RocketGiveCD { get; set; }

            [JsonProperty(PropertyName = "Permissions & values")]
            public Hash<string, PermissionValues> PermissionsValue { get; set; }

            [JsonProperty(PropertyName = "Loot container prefabs & chance to spawn a predator missile")]
            public Hash<string, float> LootContainers { get; set; }

            [JsonProperty(PropertyName = "Blocks")]
            public Blocks Blocks { get; set; }
        }

        private class Blocks
        {
            [JsonProperty(PropertyName = "Safe Zone Block")]
            public bool SafeZone { get; set; }

            [JsonProperty(PropertyName = "Mounted Block (Chair/Vehicles)")]
            public bool Mounted { get; set; }

            [JsonProperty(PropertyName = "Swimming Block")]
            public bool Swimming { get; set; }

            [JsonProperty(PropertyName = "Flying Block")]
            public bool Flying { get; set; }

            [JsonProperty(PropertyName = "Cargo Ship Block")]
            public bool CargoShip { get; set; }

            [JsonProperty(PropertyName = "Hot Air Balloon Block")]
            public bool HotAirBalloon { get; set; }

            [JsonProperty(PropertyName = "Lift Block")]
            public bool Lift { get; set; }

            [JsonProperty(PropertyName = "Scrap Transport Helicopter Block")]
            public bool ScrapHeli { get; set; }

            [JsonProperty(PropertyName = "No Building Privilege Block")]
            public bool NoBuildingPrivilege { get; set; }

            [JsonProperty(PropertyName = "Raid Block")]
            public bool RaidBlock { get; set; }

            [JsonProperty(PropertyName = "Combat Block")]
            public bool CombatBlock { get; set; }

            [JsonProperty(PropertyName = "Oilrigs Block")]
            public bool Oilrigs { get; set; }
        }

        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig pluginConfig)
        {
            pluginConfig.PermissionsValue = pluginConfig.PermissionsValue ?? new Hash<string, PermissionValues>
            {
                ["predatormissile.use"] = new PermissionValues
                {
                    Delay = 8,
                    Radius = 10,
                    Height = 75,
                    Lifespan = 15
                },
                ["predatormissile.vip"] = new PermissionValues
                {
                    Delay = 6,
                    Radius = 15,
                    Height = 100,
                    Lifespan = 20
                },
                ["predatormissile.vip2"] = new PermissionValues
                {
                    Delay = 4,
                    Radius = 20,
                    Height = 125,
                    Lifespan = 25
                },
            };

            pluginConfig.LootContainers = pluginConfig.LootContainers ?? new Hash<string, float>
            {
                ["crate_normal"] = 20f,
                ["crate_normal_2"] = 10f,
                ["crate_elite"] = 50f,
                ["crate_basic"] = 1f
            };

            pluginConfig.Blocks = pluginConfig.Blocks ?? new Blocks
            {
                SafeZone = true,
                Mounted = true,
                Swimming = true,
                Flying = true,
                CargoShip = true,
                HotAirBalloon = true,
                Lift = true,
                NoBuildingPrivilege = true,
                ScrapHeli = true,
                RaidBlock = true,
                CombatBlock = true,
                Oilrigs = true
            };

            return pluginConfig;
        }

        private class PermissionValues
        {
            [JsonProperty(PropertyName = "Seconds required to call in a missile strike")]
            public int Delay { get; set; }

            [JsonProperty(PropertyName = "Rocket explosion radius")]
            public int Radius { get; set; }

            [JsonProperty(PropertyName = "Rocket height")]
            public int Height { get; set; }

            [JsonProperty(PropertyName = "Rocket lifespan (seconds)")]
            public int Lifespan { get; set; }
        }

        #endregion

        #region Language

        private class LangKeys
        {
            public const string TipMessage = nameof(TipMessage);
            public const string NoPermission = nameof(NoPermission);
            public const string SyntaxError = nameof(SyntaxError);
            public const string TargetNotFound = nameof(TargetNotFound);
            public const string BlockedUsage = nameof(BlockedUsage);

            public class Blocked
            {
                private const string Base = nameof(Blocked) + ".";
                public const string Mounted = Base + nameof(Mounted);
                public const string InSafeZone = Base + nameof(InSafeZone);
                public const string RaidBlocked = Base + nameof(RaidBlocked);
                public const string CombatBlocked = Base + nameof(CombatBlocked);
                public const string BuildingBlocked = Base + nameof(BuildingBlocked);
                public const string Swimming = Base + nameof(Swimming);
                public const string Flying = Base + nameof(Flying);
                public const string OnCargoShip = Base + nameof(OnCargoShip);
                public const string OnHotAirBalloon = Base + nameof(OnHotAirBalloon);
                public const string InLift = Base + nameof(InLift);
                public const string InScrapTransportHeli = Base + nameof(InScrapTransportHeli);
                public const string OnOilrig = Base + nameof(OnOilrig);
            }
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [LangKeys.TipMessage] =
                    "Hold <color=#acfa58>E</color> while having the predator missile in hand in order to activate it!",
                [LangKeys.NoPermission] = "No permission!",
                [LangKeys.SyntaxError] = "Use predatormissile.give playerName/Id amount!",
                [LangKeys.TargetNotFound] = "Target not found!",
                [LangKeys.BlockedUsage] = "You cannot initialize the predator missile! ({0})",
                [LangKeys.Blocked.Mounted] = "Mounted",
                [LangKeys.Blocked.InSafeZone] = "In Safe Zone",
                [LangKeys.Blocked.RaidBlocked] = "Raid Blocked",
                [LangKeys.Blocked.CombatBlocked] = "Combat Blocked",
                [LangKeys.Blocked.BuildingBlocked] = "Building Blocked",
                [LangKeys.Blocked.Swimming] = "Swimming",
                [LangKeys.Blocked.Flying] = "Flying",
                [LangKeys.Blocked.OnCargoShip] = "On Cargo Ship",
                [LangKeys.Blocked.OnHotAirBalloon] = "On Hot Air Balloon",
                [LangKeys.Blocked.InLift] = "In Lift",
                [LangKeys.Blocked.InScrapTransportHeli] = "In Scrap Transport Heli",
                [LangKeys.Blocked.OnOilrig] = "On Oilrig"
            }, this);
        }

        private string Lang(string key, BasePlayer player = null)
        {
            return lang.GetMessage(key, this, player.UserIDString);
        }

        private string Lang(string key, BasePlayer player = null, params object[] args)
        {
            try
            {
                return string.Format(Lang(key, player), args);
            }
            catch (Exception ex)
            {
                PrintError($"Lang Key '{key}' threw exception:\n{ex}");
                throw;
            }
        }

        private void SendMessage(BasePlayer player, string key) =>
            player.SendConsoleCommand("chat.add", 2, _pluginConfig.ChatIconId, Lang(key, player));

        #endregion

        #region UI Helpers

        private static class Ui
        {
            private static string UiPanel { get; set; }

            public static CuiElementContainer Container(string color, float alpha, float fadeIn, float fadeOut,
                UiPosition pos, bool useCursor, string panel, string parent = "Overlay")
            {
                UiPanel = panel;
                return new CuiElementContainer
                {
                    {
                        new CuiPanel
                        {
                            Image = {Color = Color(color, alpha), FadeIn = fadeIn},
                            RectTransform = {AnchorMin = pos.GetMin(), AnchorMax = pos.GetMax()},
                            CursorEnabled = useCursor,
                            FadeOut = fadeOut
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
            }

            [Flags]
            public enum BorderEnum : byte
            {
                Top = 1,
                Left = 2,
                Bottom = 4,
                Right = 8,
                All = 15
            }

            // ReSharper disable once UnusedMember.Local
            public static void Outline(CuiElementContainer container, UiPosition pos, string color, float alpha,
                float fadeIn, float fadeOut, int size = 1, BorderEnum border = BorderEnum.All)
            {
                if ((border & BorderEnum.Top) == BorderEnum.Top)
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = $"{pos.XMin} {pos.YMax}", AnchorMax = $"{pos.XMax} {pos.YMax}",
                            OffsetMin = $"0 -{size}"
                        },
                        Image = {Color = Color(color, alpha), FadeIn = fadeIn},
                        FadeOut = fadeOut
                    }, UiPanel);
                }

                if ((border & BorderEnum.Left) == BorderEnum.Left)
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = $"{pos.XMin} {pos.YMin}", AnchorMax = $"{pos.XMin} {pos.YMax}",
                            OffsetMin = $"-{size} -{size}", OffsetMax = $"1 {size}"
                        },
                        Image = {Color = Color(color, alpha), FadeIn = fadeIn},
                        FadeOut = fadeOut
                    }, UiPanel);
                }

                if ((border & BorderEnum.Bottom) == BorderEnum.Bottom)
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = $"{pos.XMin} {pos.YMin}", AnchorMax = $"{pos.XMax} {pos.YMin}",
                            OffsetMin = $"0 -{size}"
                        },
                        Image = {Color = Color(color, alpha), FadeIn = fadeIn},
                        FadeOut = fadeOut
                    }, UiPanel);
                }

                if ((border & BorderEnum.Right) == BorderEnum.Right)
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform =
                        {
                            AnchorMin = $"{pos.XMax} {pos.YMin}", AnchorMax = $"{pos.XMax} {pos.YMax}",
                            OffsetMin = $"0 -{size}", OffsetMax = $"{size * 2} {size}"
                        },
                        Image = {Color = Color(color, alpha), FadeIn = fadeIn},
                        FadeOut = fadeOut
                    }, UiPanel);
                }
            }

            // ReSharper disable once UnusedMember.Local
            public static void TextOutline(CuiElementContainer container, string text, string tcolor, float talpha,
                string ocolor, float oalpha, int size, UiPosition pos, float fadeIn, float fadeOut,
                TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiElement
                {
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Color = Color(tcolor, talpha), FontSize = size, Align = align, Text = text,
                            Font = "robotocondensed-regular.ttf", FadeIn = fadeIn
                        },
                        new CuiOutlineComponent {Distance = "1.5 1.5", Color = Color(ocolor, oalpha)},
                        new CuiRectTransformComponent {AnchorMin = pos.GetMin(), AnchorMax = pos.GetMax()}
                    },
                    FadeOut = fadeOut,
                    Parent = UiPanel
                });
            }

            public static void Label(CuiElementContainer container, string text, float alpha, string color,
                float fadeIn, float fadeOut, int size, UiPosition pos, TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiLabel
                    {
                        Text =
                        {
                            FontSize = size, Align = align, Text = text, Font = "robotocondensed-regular.ttf",
                            Color = Color(color, alpha), FadeIn = fadeIn
                        },
                        RectTransform = {AnchorMin = pos.GetMin(), AnchorMax = pos.GetMax()},
                        FadeOut = fadeOut
                    },
                    UiPanel);
            }

            public static void Panel(CuiElementContainer container, string color, float alpha, float fadeIn,
                float fadeOut, UiPosition pos, bool useCursor)
            {
                container.Add(new CuiPanel
                    {
                        Image = {Color = Color(color, alpha), FadeIn = fadeIn},
                        RectTransform = {AnchorMin = pos.GetMin(), AnchorMax = pos.GetMax()},
                        CursorEnabled = useCursor,
                        FadeOut = fadeOut
                    },
                    UiPanel);
            }

            // ReSharper disable once UnusedMember.Local
            public static void Button(CuiElementContainer container, string color, float alpha, string text,
                string tcolor, float talpha, float fadeIn, float fadeOut, int size, UiPosition pos, string command,
                TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiButton
                    {
                        Button = {Color = Color(color, alpha), Command = command, FadeIn = fadeIn},
                        RectTransform = {AnchorMin = pos.GetMin(), AnchorMax = pos.GetMax()},
                        Text =
                        {
                            Text = text, Color = Color(tcolor, talpha), FontSize = size,
                            Font = "robotocondensed-regular.ttf", Align = align, FadeIn = fadeIn
                        },
                        FadeOut = fadeOut
                    },
                    UiPanel);
            }

            // ReSharper disable once UnusedMember.Local
            public static void InputBox(CuiElementContainer container, string color, string text, int size,
                float fadeOut, int charsLimit, UiPosition pos, string command,
                TextAnchor align = TextAnchor.MiddleCenter)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = UiPanel,
                    FadeOut = fadeOut,
                    Components =
                    {
                        new CuiInputFieldComponent
                        {
                            Text = text,
                            FontSize = size,
                            Color = color,
                            Command = command,
                            Align = align,
                            CharsLimit = charsLimit,
                            Font = "robotocondensed-regular.ttf"
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = pos.GetMin(),
                            AnchorMax = pos.GetMax()
                        }
                    }
                });
            }

            public static void Image(CuiElementContainer container, string url, UiPosition pos, string color,
                float alpha, float fadeIn, float fadeOut)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = UiPanel,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = !url?.StartsWith("http") ?? false ? url : null,
                            Url = url?.StartsWith("http") ?? false ? url : null, FadeIn = fadeIn,
                            Color = Color(color, alpha)
                        },
                        new CuiRectTransformComponent {AnchorMin = pos.GetMin(), AnchorMax = pos.GetMax()}
                    },
                    FadeOut = fadeOut
                });
            }

            private static string Color(string hexColor, float alpha)
            {
                hexColor = hexColor.TrimStart('#');
                int red = int.Parse(hexColor.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                int green = int.Parse(hexColor.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                int blue = int.Parse(hexColor.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return $"{red / 255.0} {green / 255.0} {blue / 255.0} {alpha / 255}";
            }
        }

        private class UiPosition
        {
            public float XMin { get; set; }
            public float YMin { get; set; }
            public float XMax { get; set; }
            public float YMax { get; set; }
            private bool Validate { get; }

            public UiPosition(float xMin, float yMin, float xMax, float yMax, bool val = true)
            {
                XMin = xMin;
                YMin = yMin;
                XMax = xMax;
                YMax = yMax;
                Validate = val;
            }

            public string GetMin() => $"{XMin} {YMin}";
            public string GetMax() => $"{XMax} {YMax}";

            // ReSharper disable once UnusedMember.Local
            public void SetX(float xPos, float xMax)
            {
                XMin = xPos;
                XMax = xMax;
            }

            // ReSharper disable once UnusedMember.Local
            public void SetY(float yMin, float yMax)
            {
                YMin = yMin;
                YMax = yMax;
            }

            // ReSharper disable once UnusedMember.Local
            public void ModifyX(float delta)
            {
                XMin += delta;
                XMax += delta;
            }

            // ReSharper disable once UnusedMember.Local
            public void ModifyXPad(float padding)
            {
                var spacing = (XMax - XMin + Math.Abs(padding)) * (padding < 0 ? -1 : 1);
                XMin += spacing;
                XMax += spacing;
            }

            // ReSharper disable once UnusedMember.Local
            public UiPosition CopyX(float yPos, float yMax)
            {
                return new UiPosition(XMin, yPos, XMax, yMax);
            }

            // ReSharper disable once UnusedMember.Local
            public void ModifyY(float delta)
            {
                YMin += delta;
                YMax += delta;
            }

            // ReSharper disable once UnusedMember.Local
            public UiPosition CopyY(float xPos, float yMax)
            {
                return new UiPosition(xPos, YMin, yMax, YMax);
            }

            // ReSharper disable once UnusedMember.Local
            public void ModifyYPad(float padding)
            {
                var spacing = (YMax - YMin + Math.Abs(padding)) * (padding < 0 ? -1 : 1);
                YMin += spacing;
                YMax += spacing;
            }
        }

        #endregion

        #region UI Creation & Display

        private void DisplayMissileUi(BasePlayer player)
        {
            var container = Ui.Container("#ffffff", 0f, 0f, 0f, _containerPosition, false,
                "PredatorMissileUi", "Hud");
            Ui.Image(container, ImageLibrary.Call<string>("GetImage", "PredatorMissileImage"), _containerPosition,
                "#ffffff", 255f, 0f, 0f);

            CuiHelper.DestroyUi(player, "PredatorMissileUi");
            CuiHelper.AddUi(player, container);
        }

        private void DisplayLoadingUi(BasePlayer player, int seconds, int requiredSeconds)
        {
            var canPlayerUsePredatorMissile = CanPlayerUsePredatorMissile(player);
            if (!string.IsNullOrEmpty(canPlayerUsePredatorMissile))
            {
                SendMessage(player, Lang(LangKeys.BlockedUsage, null, canPlayerUsePredatorMissile));
                return;
            }

            var container = Ui.Container("#ffffff", 0f, 0f, 0f, _loadingContainerPosition, false,
                "LoadingUi", "Hud");
            Ui.Label(container, $"<b>Activating Predator Missile...</b>", 200f, "#ffffff", 0f, 0f, 11,
                _loadingLabelPosition, TextAnchor.MiddleLeft);
            Ui.Panel(container, "#ffffff", 150f, 0f, 0f, _loadingPanelPosition, false);

            Ui.Image(container, ImageLibrary.Call<string>("GetImage", "PredatorMissileLoadingIcon"),
                _loadingImagePosition, "#ffffff", 200f, 0f, 0f);

            CuiHelper.DestroyUi(player, "LoadingUi");
            CuiHelper.AddUi(player, container);

            DisplayLoadingProgressUi(player, seconds, requiredSeconds);
        }

        private void DisplayLoadingProgressUi(BasePlayer player, int seconds, int requiredSeconds)
        {
            var container = Ui.Container("#ffffff", 0f, 0f, 0f, _loadingContainerPosition, false,
                "LoadingProgressUi", "Hud");
            Ui.Panel(container, "#ffffff", 250f, 0f, 0f,
                new UiPosition(0f, 0f, Mathf.Lerp(0f, 0.99f, Convert.ToSingle(seconds) / requiredSeconds), 0.32f),
                false);

            CuiHelper.DestroyUi(player, "LoadingProgressUi");
            CuiHelper.AddUi(player, container);

            if (seconds != requiredSeconds) return;
            ResetActivation(player);
            InitializePredatorMissile(player);
        }

        #endregion

        #region [Fix-Kira] / [Добавление кд]

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0);
        private static int CurrentTime() => (int) DateTime.UtcNow.Subtract(Epoch).TotalSeconds;
        private StoredData _dataBase = new StoredData();

        private class StoredData
        {
            public Dictionary<ulong, int> CooldownUse1 = new Dictionary<ulong, int>();
            public Dictionary<ulong, int> CooldownUse2 = new Dictionary<ulong, int>();
            public Dictionary<ulong, int> CooldownUse3 = new Dictionary<ulong, int>();
            public Dictionary<ulong, int> CooldownGive = new Dictionary<ulong, int>();
        }

        private bool IsCooldown(BasePlayer player, string type)
        {
            switch (type)
            {
                case "Use1":
                    if (!_dataBase.CooldownUse1.ContainsKey(player.userID)) return false;
                    var cooldownuse = _dataBase.CooldownUse1[player.userID];
                    if (0 <= (cooldownuse - CurrentTime())) return true;
                    _dataBase.CooldownUse1.Remove(player.userID);
                    break;
                case "Use2":
                    if (!_dataBase.CooldownUse2.ContainsKey(player.userID)) return false;
                    var cooldownuse2 = _dataBase.CooldownUse2[player.userID];
                    if (0 <= (cooldownuse2 - CurrentTime())) return true;
                    _dataBase.CooldownUse2.Remove(player.userID);
                    break;
                case "Use3":
                    if (!_dataBase.CooldownUse3.ContainsKey(player.userID)) return false;
                    var cooldownuse3 = _dataBase.CooldownUse3[player.userID];
                    if (0 <= (cooldownuse3 - CurrentTime())) return true;
                    _dataBase.CooldownUse3.Remove(player.userID);
                    break;
                case "Give":
                    if (!_dataBase.CooldownGive.ContainsKey(player.userID)) return false;
                    var cooldowngive = _dataBase.CooldownGive[player.userID];
                    if (0 <= (cooldowngive - CurrentTime())) return true;
                    _dataBase.CooldownGive.Remove(player.userID);
                    break;
            }

            return false;
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, _dataBase);

        private void LoadData()
        {
            try
            {
                _dataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                _dataBase = new StoredData();
            }
        }

        #endregion
    }
}