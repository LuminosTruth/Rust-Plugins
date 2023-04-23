using System;
using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("ISStatsReborn", "MalfiSQ", "1.0.0")]
    [Description("Статистика игрока")]
    public class ISStatsReborn : RustPlugin
    {
        #region [Vars] / [Переменные]

        [PluginReference] private Plugin ImageLibrary;

        private const string UIMain = "DrawUIMain";
        private StoredData DataBase = new StoredData();

        public const string Sharp = "assets/content/ui/ui.background.tile.psd";
        private const string BlurInMenu = "assets/content/ui/uibackgroundblur-ingamemenu.mat";

        #endregion

        #region [DataBase] / [БазаДанных]

        public class StoredData
        {
            public Dictionary<ulong, GeneralData> PlayersData = new Dictionary<ulong, GeneralData>();
        }

        public class GeneralData
        {
            public int General;
            public int Kills;
            public int Deaths;
            public int BradleyK;
            public int HeliK;
            public int Sulfur;
            public int Stone;
            public int Metal;
        }

        #endregion

        #region [DrawUI] / [СозданиеUI]

        private void DrawUIMain(BasePlayer player)
        {
            var db = DataBase.PlayersData[player.userID];
            var ui = new CuiElementContainer();
            ui.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image =
                {
                    Color = "0.00 0.00 0.00 0.45",
                    Material = BlurInMenu
                },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, "Overlay", UIMain);

            ui.Add(new CuiButton
            {
                Button =
                {
                    Close = UIMain,
                    Color = "0 0 0 0"
                },
                Text = { Text = " " },
                RectTransform =
                {
                    AnchorMin = "0 0",
                    AnchorMax = "1 1"
                }
            }, UIMain, "CloseButton");

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.MainPanel",
                Parent = UIMain,
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.51 0.51 0.51 1.00",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.3197917 0.3064815",
                        AnchorMax = "0.6192709 0.6268519"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Avatar",
                Parent = $"{UIMain}.MainPanel",
                Components =
                {
                    new CuiRawImageComponent
                    {
                        Png = GetImage($"{UIMain}.ServerLogo")
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.002608729 0.6705201",
                        AnchorMax = "0.1965216 0.9971098"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.NamePan",
                Parent = $"{UIMain}.MainPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.68 0.65 1.00 1.00",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.2060872 0.6820807",
                        AnchorMax = "0.9930432 0.9884393"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.PlayerName",
                Parent = $"{UIMain}.NamePan",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleLeft,
                        Color = "1.00 1.00 1.00 1.00",
                        FontSize = 18,
                        FadeIn = 1f,
                        Text = $"Статистика игрока: {player.displayName}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.003314435 0.02830195",
                        AnchorMax = "0.99779 0.971698"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.KillsPan",
                Parent = $"{UIMain}.MainPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1.00 0.52 0.52 1.00",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.006956459 0.433526",
                        AnchorMax = "0.4539129 0.5289018"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.KillsText",
                Parent = $"{UIMain}.KillsPan",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1.00 1.00 1.00 1.00",
                        FontSize = 15,
                        FadeIn = 1f,
                        Text = $"Убийств: {db.Kills}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.003890872 -1.015151",
                        AnchorMax = "1.011673 2.015151"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.DeathsPan",
                Parent = $"{UIMain}.MainPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1.00 0.52 0.52 1.00",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.006956459 0.5462426",
                        AnchorMax = "0.4539129 0.6416184"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.DeathsText",
                Parent = $"{UIMain}.DeathsPan",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1.00 1.00 1.00 1.00",
                        FontSize = 15,
                        FadeIn = 1f,
                        Text = $"Смертей: {db.Deaths}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01556408 -1.015151",
                        AnchorMax = "0.9922178 2.015151"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.ResPan",
                Parent = $"{UIMain}.MainPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "0.68 0.65 1.00 1.00",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.4673735 0.02776195",
                        AnchorMax = "0.9856337 0.643369"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Stone",
                Parent = $"{UIMain}.ResPan",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1.00 0.52 0.52 1.00",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.6821593",
                        AnchorMax = "1.001007 0.9849755"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.StoneCount",
                Parent = $"{UIMain}.Stone",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1.00 1.00 1.00 1.00",
                        FontSize = 15,
                        FadeIn = 1f,
                        Text = $"Камня добыто: {db.Stone}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.01005024 -0.2751961",
                        AnchorMax = "0.9815745 1.275196"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Metal",
                Parent = $"{UIMain}.ResPan",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1.00 0.52 0.52 1.00",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.3521137",
                        AnchorMax = "1.001678 0.6549298"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.MetalCount",
                Parent = $"{UIMain}.Metal",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1.00 1.00 1.00 1.00",
                        FontSize = 15,
                        FadeIn = 1f,
                        Text = $"Металла добыто: {db.Metal}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.006700456 -0.2751957",
                        AnchorMax = "0.9882743 1.275196"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.Sulfur",
                Parent = $"{UIMain}.ResPan",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1.00 0.52 0.52 1.00",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0.02347566",
                        AnchorMax = "1.001678 0.321597"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.SulfurCount",
                Parent = $"{UIMain}.Sulfur",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1.00 1.00 1.00 1.00",
                        FontSize = 15,
                        FadeIn = 1f,
                        Text = $"Серы добыто: {db.Sulfur}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.0134005 -0.2751955",
                        AnchorMax = "0.9882743 1.275195"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.BradleyPan",
                Parent = $"{UIMain}.MainPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1.00 0.52 0.52 1.00",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.006956459 0.2369944",
                        AnchorMax = "0.4539129 0.3323702"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.BradleyText",
                Parent = $"{UIMain}.BradleyPan",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1.00 1.00 1.00 1.00",
                        FontSize = 13,
                        FadeIn = 1f,
                        Text = $"Танки: {db.BradleyK}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 -1.015151",
                        AnchorMax = "1.007782 2.015151"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.HeliPan",
                Parent = $"{UIMain}.MainPanel",
                Components =
                {
                    new CuiImageComponent
                    {
                        Color = "1.00 0.52 0.52 1.00",
                        Material = Sharp
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0.006956459 0.127168",
                        AnchorMax = "0.4539129 0.2225438"
                    }
                }
            });

            ui.Add(new CuiElement
            {
                Name = $"{UIMain}.HeliText",
                Parent = $"{UIMain}.HeliPan",
                Components =
                {
                    new CuiTextComponent
                    {
                        Align = TextAnchor.MiddleCenter,
                        Color = "1.00 1.00 1.00 1.00",
                        FontSize = 13,
                        FadeIn = 1f,
                        Text = $"Вертолеты: {db.HeliK}"
                    },
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 -1.015151",
                        AnchorMax = "0.9805446 2.015151"
                    }
                }
            });

            CuiHelper.DestroyUi(player, UIMain);
            CuiHelper.AddUi(player, ui);
        }

        #endregion

        #region [Hooks] / [Крюки]

        // ReSharper disable once UnusedMember.Local
        private void OnServerInitialized()
        {
            AddImage("https://imgur.com/jsargGb", "ServerLogo");

            LoadData();
        }

        // ReSharper disable once UnusedMember.Local
        private void Unload()
        {
            SaveData();
        }


        // ReSharper disable once UnusedMember.Local
        private object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            if (player != null)
            {
                if (!DataBase.PlayersData.ContainsKey(player.userID))
                {
                    DataBase.PlayersData.Add(player.userID, new GeneralData());
                }

                var db1 = DataBase.PlayersData[player.userID];
                db1.Deaths++;
                db1.General++;
            }

            if (info != null)
            {
                if (!DataBase.PlayersData.ContainsKey(player.userID))
                {
                    DataBase.PlayersData.Add(player.userID, new GeneralData());
                }

                var db2 = DataBase.PlayersData[player.userID];
                db2.Kills++;
                db2.General++;
            }

            return null;
        }

        // ReSharper disable once UnusedMember.Local
        private object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {

            var player = entity.ToPlayer();
            if (!DataBase.PlayersData.ContainsKey(player.userID))
            {
                DataBase.PlayersData.Add(player.userID, new GeneralData());
            }
            var db = DataBase.PlayersData[player.userID];

            switch (item.info.shortname)
            {
                case "stones":
                    db.Stone++;
                    db.General++;
                    break;
                case "sulfur.ore":
                    db.Sulfur++;
                    db.General++;
                    break;
                case "metal.ore":
                    db.Metal++;
                    db.General++;
                    break;
            }

            return null;
        }

        #endregion

        #region [Methods] / [Методы]

        private string GetImage(string image)
        {
            return (string)ImageLibrary?.Call("GetImage", image);
        }

        private void AddImage(string url, string name)
        {
            ImageLibrary?.Call("AddImage", url, name);
        }

        #endregion

        #region [ChatCommands] / [Чат Команды]

        [ChatCommand("stats")]
        private void OpenStats(BasePlayer player)
        {
            if (player == null) return;

            if (!DataBase.PlayersData.ContainsKey(player.userID))
            {
                DataBase.PlayersData.Add(player.userID, new GeneralData());
            }

            DrawUIMain(player);
        }

        #endregion

        #region [Helpers] / [Вспомогательный код]

        private void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject(Name, DataBase);
        }

        private void LoadData()
        {
            try
            {
                DataBase = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(Name);
            }
            catch (Exception)
            {
                DataBase = new StoredData();
            }
        }

        #endregion
    }
}