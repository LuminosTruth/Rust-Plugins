using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("GiveKits", "Greyghost", "1.0.2")]
    [Description("Выдача китов по привелегиям")]
    // ReSharper disable once UnusedType.Global
    public class GiveKits : RustPlugin
    {
        #region [Reference]

#pragma warning disable CS0649
        [PluginReference] private Plugin ImageLibrary;
#pragma warning restore CS0649

        #endregion

        #region [Var] / [Переменные]

        private enum TypeContent
        {
            Ammo,
            Contents
        }

        private enum ContainerItem
        {
            containerWear,
            containerBelt,
            containerMain
        }

        private const string UIMain = "UI.Main";
        private const string UIMenu = "UI.Main.Menu";
        private static WaitForSeconds _waitForSeconds = new WaitForSeconds(0.1f);

        #endregion

        #region [Dictionary] / [Словари]

        private readonly Dictionary<string, string> _images = new Dictionary<string, string>
        {
            ["GiveKits.Background1"] = "https://gspics.org/images/2022/06/17/01IS6w.jpg",
            ["GiveKits.Background2"] = "https://gspics.org/images/2022/06/17/01IRix.jpg",
        };

        #endregion

        #region [Configuraton] / [Конфигурация ]

        private static KlassConfig config = new KlassConfig();

        private class KlassConfig
        {
            [JsonProperty(PropertyName = "Список наборов к выдаче")]
            public Dictionary<String, KlassNabor> ListNaborov = new Dictionary<String, KlassNabor>();

            internal class KlassNabor
            {
                [JsonProperty("Имя набора")] public String NameNabor;

                [JsonProperty("Permission для данного набора")]
                public String PermishnDlyaNabora;

                [JsonProperty("Cooldown для данного набора")]
                public Int32 CooldownDlyaNabora;

                [JsonProperty("Список предметов в наборе")]
                public List<KlassPredmet> ListPredmeti = new List<KlassPredmet>();

                internal class KlassPredmet
                {
                    [JsonProperty("Куда класть предмет(0 - Одежда, 1 - Слоты, 2 - Инвентарь)")]
                    public ContainerItem GdePredmet;

                    [JsonProperty("Shortname предмета")] public string Shortname;
                    [JsonProperty("Количество предметов")] public int Count;
                    [JsonProperty("SkinID предмета")] public ulong SkinID;

                    [JsonProperty("Список обвесов предмета")]
                    public List<Klassobvesi> ListObvesi = new List<Klassobvesi>();

                    internal class Klassobvesi
                    {
                        [JsonProperty("Тип обвеса(0 - Патроны, 1 - Содержимое")]
                        public TypeContent TipObvesa;

                        [JsonProperty("Shortname обвеса")] public string Shortname;
                        [JsonProperty("Количество")] public int Count;
                    }
                }
            }

            public static KlassConfig GetDefaultConfig()
            {
                return new KlassConfig
                {
                    ListNaborov = new Dictionary<string, KlassNabor>
                    {
                        ["kit1"] = new KlassNabor
                        {
                            NameNabor = "Кит уровня 1",
                            PermishnDlyaNabora = "givekits.l1",
                            CooldownDlyaNabora = 3600,
                            ListPredmeti = new List<KlassNabor.KlassPredmet>
                            {
                                new KlassNabor.KlassPredmet
                                {
                                    GdePredmet = ContainerItem.containerBelt,
                                    Shortname = "rifle.ak",
                                    Count = 1,
                                    SkinID = 2620919847,
                                    ListObvesi =
                                        new List<KlassNabor.KlassPredmet.Klassobvesi>
                                        {
                                            new KlassNabor.KlassPredmet.Klassobvesi
                                            {
                                                TipObvesa = TypeContent.Ammo,
                                                Shortname = "ammo.rifle.explosive",
                                                Count = 10
                                            },
                                            new KlassNabor.KlassPredmet.Klassobvesi
                                            {
                                                TipObvesa = TypeContent.Contents,
                                                Shortname = "weapon.mod.lasersight",
                                                Count = 1
                                            }
                                        }
                                },
                                new KlassNabor.KlassPredmet
                                {
                                    GdePredmet = ContainerItem.containerBelt,
                                    Shortname = "pickaxe",
                                    Count = 1,
                                    SkinID = 2716077818,
                                    ListObvesi = new List<KlassNabor.KlassPredmet.Klassobvesi>()
                                },
                                new KlassNabor.KlassPredmet
                                {
                                    GdePredmet = ContainerItem.containerMain,
                                    Shortname = "smallwaterbottle",
                                    Count = 1,
                                    ListObvesi =
                                        new List<KlassNabor.KlassPredmet.Klassobvesi>
                                        {
                                            new KlassNabor.KlassPredmet.Klassobvesi()
                                            {
                                                TipObvesa = TypeContent.Contents,
                                                Shortname = "water",
                                                Count = 1
                                            }
                                        }
                                },
                                new KlassNabor.KlassPredmet
                                {
                                    GdePredmet = ContainerItem.containerMain,
                                    Shortname = "stones",
                                    Count = 100,
                                    ListObvesi = new List<KlassNabor.KlassPredmet.Klassobvesi>()
                                }
                            }
                        },
                        ["kit2"] = new KlassNabor
                        {
                            NameNabor = "Кит уровня 2",
                            PermishnDlyaNabora = "givekits.l2",
                            CooldownDlyaNabora = 3600,
                            ListPredmeti = new List<KlassNabor.KlassPredmet>
                            {
                                new KlassNabor.KlassPredmet
                                {
                                    GdePredmet = ContainerItem.containerMain,
                                    Shortname = "stones",
                                    Count = 2000,
                                    ListObvesi = new List<KlassNabor.KlassPredmet.Klassobvesi>()
                                }
                            }
                        }
                    }
                };
            }
        }


        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<KlassConfig>();
                if (config == null) LoadDefaultConfig();
            }
            catch
            {
                LoadDefaultConfig();
            }

            NextTick(SaveConfig);
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Файл конфигурации поврежден (или не существует), создан новый!");
            config = KlassConfig.GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion

        #region [Data] / [Данные]

        [JsonProperty("Дата с информацией о игроках")]
        private Hash<ulong, DataUsera> DataUseraList = new Hash<ulong, DataUsera>();

        private class DataUsera
        {
            [JsonProperty("Кулдаун")] public int GiveKitCooldown;
        }

        private void ReadData() => DataUseraList =
            Core.Interface.Oxide.DataFileSystem.ReadObject<Hash<ulong, DataUsera>>("Givekits");

        private void WriteData() => Core.Interface.Oxide.DataFileSystem.WriteObject("Givekits", DataUseraList);

        private void RegisteredDataUser(BasePlayer player)
        {
            if (!DataUseraList.ContainsKey(player.userID))
                DataUseraList.Add(player.userID, new DataUsera {GiveKitCooldown = 0});
        }

        #endregion

        #region [UI]

        private void OpenUI_Main(BasePlayer player)
        {
            var ui = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        CursorEnabled = true,
                        Image =
                        {
                            Color = "0.13 0.13 0.15 0.00"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.1557292 0.1870372",
                            AnchorMax = "0.8067709 0.8379631"
                        }
                    },
                    "Overlay", UIMain
                },
                new CuiElement
                {
                    Name = UIMenu,
                    Parent = UIMain,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = lang.GetLanguage(player.UserIDString) == "ru"
                                ? GetImage("GiveKits.Background1")
                                : GetImage("GiveKits.Background2")
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        }
                    }
                },
                {
                    new CuiButton
                    {
                        Button =
                        {
                            Color = "0 0 0 0",
                            Close = UIMain,
                        },
                        RectTransform =
                        {
                            AnchorMin = "0 0",
                            AnchorMax = "1 1"
                        },
                        Text =
                        {
                            Text = ""
                        }
                    },
                    UIMenu
                }
            };

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Commands] / [Команды]

        [ChatCommand("givekits")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private void GivekitCommand(BasePlayer player, string command, string[] arg)
        {
            if (arg.Length < 2) return;

            switch (arg[0])
            {
                case "create":
                case "add":
                case "new":
                {
                    if (!player.IsAdmin) return;
                    var NameKit = arg[1];
                    if (string.IsNullOrWhiteSpace(NameKit))
                    {
                        player.ChatMessage("Введите корректное название!");
                        return;
                    }

                    CreateNewKit(player, NameKit);
                    break;
                }
            }
        }

        [ConsoleCommand("givekitto")]
        // ReSharper disable once UnusedMember.Local
        private void GiveKitTo(ConsoleSystem.Arg args)
        {
            PrintWarning("Начинаем выдачу");
            if (!args.HasArgs())
            {
                PrintToConsole("[GiveKits] необходим SteamID игрока");
                PrintError("необходим SteamID игрока");
                return;
            }

            var target = BasePlayer.FindByID(ulong.Parse(args.Args[0]));
            if (target == null)
            {
                PrintToConsole("[GiveKits] нет указанного игрока");
                PrintError("нет указанного игрока");
                return;
            }

            GiveKit(target);
        }

        [ChatCommand("dkit")]
        // ReSharper disable once UnusedMember.Local
        private void GiveKitme(BasePlayer target)
        {
            if (target == null)
            {
                PrintToConsole("[GiveKits] нет указанного игрока");
                PrintError("нет указанного игрока");
                return;
            }

            GiveKit(target);
        }

        private void GiveKit(BasePlayer target)
        {
            var IsPermishExist = false;

            foreach (KlassConfig.KlassNabor nabor in from Naborkeys in config.ListNaborov.Keys
                     select config.ListNaborov[Naborkeys]
                     into nabor
                     let perm = nabor.PermishnDlyaNabora
                     where HasPermission(target, perm)
                     select nabor)
            {
                IsPermishExist = true;
                var NaborCooldown = DataUseraList[target.userID].GiveKitCooldown;
                var DataNow = Convert.ToInt32(CurrentTime());
                if (NaborCooldown >= DataNow)
                {
                    var cdleast = TimeSpan.FromSeconds(NaborCooldown - DataNow);
                    target.ChatMessage(
                        $"{cdleast.Days} дней {cdleast.Hours} часов {cdleast.Minutes} {cdleast.TotalDays}");
                    if (cdleast.TotalSeconds < 60)
                    {
                        var cdltext = $"{cdleast} секунд";
                        target.ChatMessage($"[GiveKits] До получения набора {cdltext}");
                    }
                    else if (cdleast.TotalSeconds >= 60 && cdleast.TotalSeconds < 3600)
                    {
                        var cdltext = $"{cdleast.Minutes} минут {cdleast.Seconds} секунд ";
                        target.ChatMessage($"[GiveKits] До получения набора {cdltext}");
                    }
                    else if (cdleast.TotalSeconds >= 3600)
                        target.ChatMessage(
                            "[GiveKits] До получения набора " +
                            $"{cdleast.Hours} часов {cdleast.Minutes} минут {cdleast.Seconds} секунд ");


                    return;
                }


                var ItemKit = nabor.ListPredmeti;
                var BeltAmount = ItemKit.Count(i => i.GdePredmet == ContainerItem.containerBelt);
                var WearAmount = ItemKit.Count(i => i.GdePredmet == ContainerItem.containerWear);
                var MainAmount = ItemKit.Count(i => i.GdePredmet == ContainerItem.containerMain);
                var targetinventory = target.inventory;
                var Total = BeltAmount + WearAmount + MainAmount;

                if ((targetinventory.containerBelt.capacity - targetinventory.containerBelt.itemList.Count) <
                    BeltAmount
                    || (targetinventory.containerWear.capacity - targetinventory.containerWear.itemList.Count) <
                    WearAmount
                    || (targetinventory.containerMain.capacity - targetinventory.containerMain.itemList.Count) <
                    MainAmount)
                    if (Total > (targetinventory.containerMain.capacity -
                                 targetinventory.containerMain.itemList.Count))
                    {
                        target.ChatMessage("[GiveKits] Нет места в инвентаре");
                        return;
                    }

                DataUseraList[target.userID].GiveKitCooldown = DataNow + nabor.CooldownDlyaNabora;
//                WriteData();
                foreach (var predmet in nabor.ListPredmeti)
                {
                    var item = ItemManager.CreateByName(predmet.Shortname, predmet.Count);
                    if (HasSkinID(predmet)) item.skin = predmet.SkinID;

                    foreach (var Content in predmet.ListObvesi)
                    {
                        var ItemContent = ItemManager.CreateByName(Content.Shortname, Content.Count);
                        switch (Content.TipObvesa)
                        {
                            case TypeContent.Contents:
                                ItemContent.MoveToContainer(item.contents);
                                break;
                            case TypeContent.Ammo:
                                var Weapon = item.GetHeldEntity() as BaseProjectile;
                                if (Weapon != null)
                                {
                                    Weapon.primaryMagazine.contents = ItemContent.amount;
                                    Weapon.primaryMagazine.ammoType =
                                        ItemManager.FindItemDefinition(Content.Shortname);
                                }

                                break;
                            default:
                                PrintWarning("СТРОКА : 469");
                                break;
                        }
                    }

                    target.GiveItem(item);
                }

                target.ChatMessage($"[GiveKits] Набор {nabor.NameNabor} выдан.");
            }

            if (IsPermishExist) return;
            target.ChatMessage("[GiveKits] Нет роли для выдачи кита");
            OpenUI_Main(target);
        }

        #endregion

        #region [NewKit] / [Добавление набора]

        private void CreateNewKit(BasePlayer player, string NameKit)
        {
            if (!player.IsAdmin) return;
            if (config.ListNaborov.ContainsKey(NameKit))
            {
                player.ChatMessage("Ключ данного набора уже существует!");
                return;
            }

            config.ListNaborov.Add(NameKit, new KlassConfig.KlassNabor()
            {
                NameNabor = NameKit,
                PermishnDlyaNabora = "givekits.xxxx",
                CooldownDlyaNabora = 3600,
                ListPredmeti = GetPlayerItems(player)
            });

            SaveConfig();
            player.ChatMessage($"Набор с ключем {NameKit} успешно создан");
        }

        private List<KlassConfig.KlassNabor.KlassPredmet> GetPlayerItems(BasePlayer player)
        {
            var kititems = (from item in player.inventory.containerWear.itemList
                where item != null
                select ItemToKit(item, ContainerItem.containerWear)).ToList();
            kititems.AddRange(from item in player.inventory.containerMain.itemList
                where item != null
                select ItemToKit(item, ContainerItem.containerMain));

            kititems.AddRange(from item in player.inventory.containerBelt.itemList
                where item != null
                select ItemToKit(item, ContainerItem.containerBelt));

            return kititems;
        }

        private KlassConfig.KlassNabor.KlassPredmet ItemToKit(Item item, ContainerItem containerItem)
        {
            var ItemsKit = new KlassConfig.KlassNabor.KlassPredmet
            {
                Count = item.amount,
                GdePredmet = containerItem,
                Shortname = item.info.shortname,
                SkinID = item.skin,
                ListObvesi = GetContentItem(item)
            };

            return ItemsKit;
        }

        // ReSharper disable once ParameterHidesMember
        private List<KlassConfig.KlassNabor.KlassPredmet.Klassobvesi> GetContentItem(Item Item)
        {
            var Contents = new List<KlassConfig.KlassNabor.KlassPredmet.Klassobvesi>();

            if (Item.contents != null)
                Contents.AddRange(Item.contents.itemList.Select(Content =>
                    new KlassConfig.KlassNabor.KlassPredmet.Klassobvesi
                    {
                        TipObvesa = TypeContent.Contents, Shortname = Content.info.shortname, Count = Content.amount
                    }));

            var Weapon = Item.GetHeldEntity() as BaseProjectile;
            if (Weapon == null) return Contents;
            var ContentItem =
                new KlassConfig.KlassNabor.KlassPredmet.Klassobvesi
                {
                    TipObvesa = TypeContent.Ammo,
                    Shortname = Weapon.primaryMagazine.ammoType.shortname,
                    Count = Weapon.primaryMagazine.contents == 0 ? 1 : Weapon.primaryMagazine.contents
                };
            Contents.Add(ContentItem);

            return Contents;
        }

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local

        private void Init() => ReadData();

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            ServerMgr.Instance.StartCoroutine(LoadImages());
            var KList = config.ListNaborov;
            foreach (var Perm in KList.Where(Perm => !permission.PermissionExists(Perm.Value.PermishnDlyaNabora, this)))
                permission.RegisterPermission(Perm.Value.PermishnDlyaNabora, this);
            foreach (var p in BasePlayer.activePlayerList) OnPlayerConnected(p);
            WriteData();
        }

        // ReSharper disable once ArrangeTypeMemberModifiers
        private void OnPlayerConnected(BasePlayer player)
        {
            RegisteredDataUser(player);
        }

        // ReSharper disable once UnusedMember.Local
        private void OnServerSave()
        {
            timer.Once(1f, WriteData);
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            WriteData();
        }

        #endregion

        #region Helpers

        private bool HasPermission(BasePlayer player, string permname) =>
            permission.UserHasPermission(player.UserIDString, permname);

        private bool HasSkinID(KlassConfig.KlassNabor.KlassPredmet predmet) => predmet.SkinID != 0;

        static DateTime epoch =
            new DateTime(1970, 1, 1, 0, 0, 0);

        static double CurrentTime() => DateTime.UtcNow.Subtract(epoch).TotalSeconds;

        private string GetImage(string image)
        {
            return (string) ImageLibrary.Call("GetImage", image);
        }

        private IEnumerator LoadImages()
        {
            PrintWarning("Load images...");
            foreach (var image in _images)
            {
                ImageLibrary.Call("AddImage", image.Value, image.Key);
                yield return _waitForSeconds;
            }

            PrintWarning("Image loaded");
            yield return 0;
        }

        #endregion
    }
}