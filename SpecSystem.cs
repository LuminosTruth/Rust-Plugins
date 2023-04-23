using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("SpecSystem", "famusov", "0.0.1")]
    class SpecSystem : RustPlugin
    {
        #region reference

        [PluginReference] Plugin ImageLibrary;

        public string GetImage(string imagename, ulong skin = 0) =>
            (string) ImageLibrary.Call("GetImage", imagename, skin);
        // private static ConfigData cfg;

        #endregion

        #region config

        /*  class ConfigData
          {
              public string access_token;
              public List<int> admins_id = new List<int>();
              public int cooldown;
          }*/

        #endregion

        #region data

        public Dictionary<ulong, MediumTubs> FirstData = new Dictionary<ulong, MediumTubs>();
        public Dictionary<ulong, MediumTubs> SecondData = new Dictionary<ulong, MediumTubs>();
        public Dictionary<ulong, MediumTubs> ThirdData = new Dictionary<ulong, MediumTubs>();

        public class MediumTubs
        {
            [JsonProperty("Название категории")] public string displayName;
            [JsonProperty("Картинка категории")] public string image;

            [JsonProperty("Список маленьких кнопок")]
            public Dictionary<ulong, SmallTubs> SDPages = new Dictionary<ulong, SmallTubs>();
        }

        public class SmallTubs
        {
            [JsonProperty("Название маленькой кнопки")]
            public string displayName;

            [JsonProperty("Картинка маленькой кнопки")]
            public string image;

            [JsonProperty("Список прокачки")]
            public Dictionary<ulong, SkillsList> SkillDataList = new Dictionary<ulong, SkillsList>();
        }

        public class SkillsList
        {
            [JsonProperty("Название навыка")] public string displayName;
            [JsonProperty("Настройки уровней")] public List<LevelSettings> SkillLevels = new List<LevelSettings>();
        }

        public class LevelSettings
        {
            [JsonProperty("Номер уровня")] public int levelnumber;
            [JsonProperty("Описание уровня")] public string levelText;

            [JsonProperty("Стоимость в экономике")]
            public int priceEconomic;

            [JsonProperty("Команды в консоль (%STEAMID%)")]
            public List<string> commands = new List<string>();
        }

        #endregion

        #region playerData

        private Dictionary<ulong, PagesData> PlayerData = new Dictionary<ulong, PagesData>();

        public class PagesData
        {
            [JsonProperty("Главные страницы")]
            public Dictionary<ulong, MediumData> PagesPlayerData = new Dictionary<ulong, MediumData>();
        }

        public class MediumData
        {
            [JsonProperty("Сколько очков прокачки")]
            public int scoreUpgrade;

            [JsonProperty("Список средних кнопок")]
            public Dictionary<ulong, MediumSettings> MPData = new Dictionary<ulong, MediumSettings>();
        }

        public class MediumSettings
        {
            [JsonProperty("Открыта ли игроку")] public bool openClose;

            [JsonProperty("Список маленьких кнопок")]
            public Dictionary<ulong, SmallSettings> SPData = new Dictionary<ulong, SmallSettings>();
        }

        public class SmallSettings
        {
            [JsonProperty("Открыта ли игроку")] public bool openClose;

            [JsonProperty("Закончена ли полностью")]
            public bool performed;

            [JsonProperty("Выполнено всего")] public int countAcceptedSmall;

            [JsonProperty("Список скиллов")]
            public Dictionary<ulong, SkillSettings> SkillsData = new Dictionary<ulong, SkillSettings>();
        }

        public class SkillSettings
        {
            [JsonProperty("Сколько уровней выполнено")]
            public int levelsCompite;
        }

        #endregion

        #region Hook

        private void Init()
        {
            #region ConfigLoad

            // cfg = Config.ReadObject<ConfigData>();

            #endregion

            #region DataLoad

            FirstData = Interface.Oxide.DataFileSystem
                .ReadObject<Dictionary<ulong, MediumTubs>>("SpecSystem/FirstData");
            SecondData =
                Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, MediumTubs>>("SpecSystem/SecondData");
            ThirdData = Interface.Oxide.DataFileSystem
                .ReadObject<Dictionary<ulong, MediumTubs>>("SpecSystem/ThirdData");
            PlayerData =
                Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, PagesData>>("SpecSystem/PlayerData");

            BasePlayer.activePlayerList.ToList().ForEach(OnPlayerInit);
            //SaveData();

            #endregion
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (!PlayerData.ContainsKey(player.userID))
            {
                MediumData NewData = new MediumData();
                PagesData NewUser = new PagesData()
                {
                    PagesPlayerData = new Dictionary<ulong, MediumData>()
                };
                PlayerData.Add(player.userID, NewUser);
                var pdata = PlayerData[player.userID].PagesPlayerData;
                pdata.Add(1, NewData);
                pdata.Add(2, NewData);
                pdata.Add(3, NewData);
                //   pdata[1].scoreUpgrade = 1;
                //   pdata[2].scoreUpgrade = 1;
                //   pdata[3].scoreUpgrade = 1;
                SaveData();
            }
        }

        void Unload()
        {
            // SaveData();
        }

        void OnServerSave()
        {
            SaveData();
        }

        void SaveData()
        {
            Interface.Oxide.DataFileSystem.WriteObject("SpecSystem/FirstData", FirstData);
            Interface.Oxide.DataFileSystem.WriteObject("SpecSystem/SecondData", SecondData);
            Interface.Oxide.DataFileSystem.WriteObject("SpecSystem/ThirdData", ThirdData);
            Interface.Oxide.DataFileSystem.WriteObject("SpecSystem/PlayerData", PlayerData);
        }

        [ChatCommand("menu")]
        private void OpenCmd(BasePlayer player)
        {
            var b = new SmallTubs
            {
                displayName = "231",
                image = "213",
                SkillDataList = new Dictionary<ulong, SkillsList>()
            };
            var c = new SkillsList
            {
                displayName = "CUMSHOT",
                SkillLevels = new List<LevelSettings>()
            };
            var e = new LevelSettings
            {
                levelnumber = 1,
                levelText = "Дефолт текст",
                priceEconomic = 3,
                commands = new List<string>()
            };
            var a = new MediumTubs
            {
                displayName = "fdsa",
                image = "123",
                SDPages = new Dictionary<ulong, SmallTubs>(),
            };
            if (FirstData.IsEmpty())
            {
                FirstData.Add(1241, a);
                FirstData[1241].SDPages.Add(43214, b);
                FirstData[1241].SDPages[43214].SkillDataList.Add(15433, c);
                FirstData[1241].SDPages[43214].SkillDataList[15433].SkillLevels.Add(e);
            }

            SaveData();
            RenderMainUI(player);
        }

        [ConsoleCommand("owerpage")]
        private void cmdNextPage(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var data = ulong.Parse(arg.Args[0]);
            var page = ulong.Parse(arg.Args[1]);
            RenderMedium(player, data, page);
        }

        [ConsoleCommand("owerpagesmall")]
        private void cmdNextPageSmall(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var data = ulong.Parse(arg.Args[0]);
            var medium = ulong.Parse(arg.Args[1]);
            var page = ulong.Parse(arg.Args[2]);
            RenderSmall(player, data, medium, page);
        }

        [ConsoleCommand("owerpageskill")]
        private void cmdNextPageSkill(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var data = ulong.Parse(arg.Args[0]);
            var medium = ulong.Parse(arg.Args[1]);
            var small = ulong.Parse(arg.Args[2]);
            var page = ulong.Parse(arg.Args[3]);
            RenderSkill(player, data, medium, small);
        }

        [ConsoleCommand("tryupgradeskill")]
        private void cmdUpSkill(ConsoleSystem.Arg arg)
        {
            var player = arg.Player();
            var data = ulong.Parse(arg.Args[0]);
            var medium = ulong.Parse(arg.Args[1]);
            var small = ulong.Parse(arg.Args[2]);
            var skill = ulong.Parse(arg.Args[3]);
            TryUpgradeSkill(player, data, medium, small, skill);
        }

        private void TryUpgradeSkill(BasePlayer player, ulong data, ulong medium, ulong small, ulong skill)
        {
            var Data = GetDataPage(data);
            var pdata = PlayerData[player.userID].PagesPlayerData[data];
            if (!pdata.MPData.ContainsKey(medium)) return;
            if (!pdata.MPData[medium].SPData.ContainsKey(small)) return;
            if (pdata.MPData[medium].SPData[small].performed) return;
            if (pdata.MPData[medium].SPData[small].SkillsData[skill].levelsCompite ==
                Data[medium].SDPages[small].SkillDataList[skill].SkillLevels.Count) return;

            pdata.MPData[medium].SPData[small].SkillsData[skill].levelsCompite++;
            pdata.MPData[medium].SPData[small].countAcceptedSmall++;
            var countSmall = Data[medium].SDPages[small].SkillDataList.SelectMany(a => a.Value.SkillLevels).Count();

            if (pdata.MPData[medium].SPData[small].countAcceptedSmall == countSmall)
            {
                pdata.MPData[medium].SPData[small].performed = true;
                foreach (var p in pdata.MPData[medium].SPData.Where(p => !p.Value.openClose))
                {
                    p.Value.openClose = true;
                    break;
                }
            }

            RenderSkill(player, data, medium, small);
            foreach (var d in Data[medium].SDPages[small].SkillDataList[skill].SkillLevels
                         .Where(c => pdata.MPData[medium].SPData[small].SkillsData[skill].levelsCompite ==
                                     c.levelnumber).SelectMany(c => c.commands))
                rust.RunServerCommand(d.Replace("%STEAMID%", player.userID.ToString()));
        }

        [ConsoleCommand("chatadd")]
        private void ChatText(ConsoleSystem.Arg arg)
        {
            var text1 = arg.Args[0];
            var text2 = arg.Args[1];
            Server.Broadcast($"{text1} {text2}");
        }

        private Dictionary<ulong, MediumTubs> GetDataPage(ulong data)
        {
            Dictionary<ulong, MediumTubs> Data;
            switch (data)
            {
                case 1:
                    Data = FirstData;
                    break;
                case 2:
                    Data = SecondData;
                    break;
                case 3:
                    Data = ThirdData;
                    break;
                default:
                    Data = FirstData;
                    break;
            }

            return Data;
        }

        private void RenderMainUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "ReportSearch");
            CuiElementContainer container = new CuiElementContainer();
            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                Image = {Color = "0.14 0.14 0.14 0.9", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                RectTransform = {AnchorMin = "0 0", AnchorMax = $"1 1"},
            }, "Overlay", "ReportSearch");

            container.Add(new CuiElement
            {
                Parent = "ReportSearch",
                Name = "bbclose",
                Components =
                {
                    new CuiButtonComponent {Close = "ReportSearch", Color = "1 1 1 0"},
                    new CuiRectTransformComponent
                        {AnchorMin = "1 1", AnchorMax = $"1 1", OffsetMin = "-200 -50", OffsetMax = "0 0"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = "bbclose",
                Components =
                {
                    new CuiTextComponent
                    {
                        Color = "1 1 1 1", FontSize = 26, Font = "robotocondensed-bold.ttf",
                        Align = TextAnchor.MiddleCenter, Text = "ЗАКРЫТЬ"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = $"1 1"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = "ReportSearch",
                Components =
                {
                    new CuiTextComponent
                    {
                        Color = "1 1 1 1", FontSize = 26, Font = "robotocondensed-bold.ttf",
                        Align = TextAnchor.MiddleCenter, Text = "МЕНЮШКА"
                    },
                    new CuiRectTransformComponent
                        {AnchorMin = "0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = "-200 240", OffsetMax = "200 280"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = "ReportSearch",
                Name = "dd",
                Components =
                {
                    new CuiButtonComponent {Color = "0 0 0 0"},
                    new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5"},
                }
            });
            container.Add(new CuiElement
            {
                Parent = "dd",
                Name = "f",
                Components =
                {
                    new CuiButtonComponent {Command = "owerpage 1 0", Color = "0.15 0.15 0.15 0.9"},
                    new CuiRectTransformComponent
                    {
                        AnchorMin = "0 0", AnchorMax = "0 0",
                        OffsetMin = $"-100 -200",
                        OffsetMax = $"100 200"
                    },
                }
            });
            container.Add(new CuiElement
            {
                Parent = "f",
                Components =
                {
                    new CuiTextComponent
                    {
                        Color = "1 1 1 1", FontSize = 20, Font = "robotocondensed-bold.ttf",
                        Align = TextAnchor.MiddleCenter, Text = "ВИДКРОЙ МЭНЭ"
                    },
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = $"1 0.1"},
                }
            });


            CuiHelper.AddUi(player, container);
        }

        private void RenderMedium(BasePlayer player, ulong data, ulong page)
        {
            CuiHelper.DestroyUi(player, "dd");
            var container = new CuiElementContainer();

            var Data = GetDataPage(data);
            var pdata = PlayerData[player.userID].PagesPlayerData[data];
            var ooo = 0;
            foreach (var oo in Data.Skip(int.Parse(page.ToString()) * 5))
            {
                if (ooo == 5) continue;
                ooo++;
            }

            var SHIRINA = 200;
            var SKOLKOKNOPOK = ooo;
            var pos = 0 - ((SKOLKOKNOPOK) * SHIRINA + (SKOLKOKNOPOK - 1) * (50)) / 2;

            int pp = 0;
            int x = 0;
            foreach (var p in Data)
            {
                if (!pdata.MPData.ContainsKey(p.Key))
                {
                    var b = new MediumSettings
                    {
                        openClose = false,
                        SPData = new Dictionary<ulong, SmallSettings>()
                    };
                    Puts(p.Key.ToString());
                    pdata.MPData.Add(p.Key, b);
                }
            }

            SaveData();
            container.Add(new CuiElement
            {
                Parent = "ReportSearch",
                Name = "dd",
                Components =
                {
                    new CuiButtonComponent {Color = "0 0 0 0"},
                    new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5"},
                }
            });
            foreach (var a in Data.Skip(int.Parse(page.ToString()) * 5))
            {
                if (pp == 5) continue;
                container.Add(new CuiElement
                {
                    Parent = "dd",
                    Name = "f",
                    Components =
                    {
                        new CuiButtonComponent
                            {Command = $"owerpagesmall {data} {a.Key} 0", Color = "0.15 0.15 0.15 0.9"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0", AnchorMax = "0 0",
                            OffsetMin = $"{pos} -200",
                            OffsetMax = $"{pos + SHIRINA} 200"
                        },
                    }
                });
                pos += SHIRINA + 50;
                container.Add(new CuiElement
                {
                    Parent = "f",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Color = "1 1 1 1", FontSize = 20, Font = "robotocondensed-bold.ttf",
                            Align = TextAnchor.MiddleCenter, Text = a.Value.displayName.ToUpper()
                        },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = $"1 0.1"},
                    }
                });
                if (!pdata.MPData[a.Key].openClose)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "f",
                        Components =
                        {
                            new CuiImageComponent {Color = "0.8 0.8 0.8 1", Sprite = "assets/icons/lock.png"},
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = "-32 -32",
                                OffsetMax = "32 32"
                            },
                        }
                    });
                }

                x++;

                pp++;
            }


            if (pp >= 5)
            {
                container.Add(new CuiElement
                {
                    Name = "button13",
                    Parent = "dd",
                    Components =
                    {
                        new CuiButtonComponent
                            {Color = "0.00 0.25 0.42 0.992", Command = $"owerpage {data} {page + 1}"},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "0 -20", OffsetMax = "40 20"},
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "button13",
                    Components =
                    {
                        new CuiImageComponent {Color = "1 1 1 1", Sprite = "assets/icons/exit.png"},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "10 10", OffsetMax = "40 40"},
                    }
                });
            }

            if (page > 0)
            {
                container.Add(new CuiElement
                {
                    Name = "button13",
                    Parent = "dd",
                    Components =
                    {
                        new CuiButtonComponent
                            {Color = "0.00 0.25 0.42 0.992", Command = $"owerpage {data} {page - 1}"},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = "-40 -20", OffsetMax = "0 20"},
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "button13",
                    Components =
                    {
                        new CuiImageComponent {Color = "1 1 1 1", Sprite = "assets/icons/enter.png"},
                        new CuiRectTransformComponent
                            {AnchorMin = "0 0", AnchorMax = $"0 0", OffsetMin = "10 10", OffsetMax = "40 40"},
                    }
                });
            }

            CuiHelper.AddUi(player, container);
        }

        private void RenderSmall(BasePlayer player, ulong data, ulong medium, ulong page)
        {
            CuiHelper.DestroyUi(player, "dd");
            CuiElementContainer container = new CuiElementContainer();
            var Data = GetDataPage(data);
            var datasmall = Data[medium].SDPages;
            var datasmallskip = datasmall.Skip(int.Parse(page.ToString()) * 18);

            var pdata = PlayerData[player.userID].PagesPlayerData[data].MPData[medium];
            foreach (var p in datasmall)
            {
                if (!pdata.SPData.ContainsKey(p.Key))
                {
                    var b = new SmallSettings
                    {
                        openClose = false,
                        performed = false,
                        SkillsData = new Dictionary<ulong, SkillSettings>()
                    };
                    pdata.SPData.Add(p.Key, b);
                }
            }

            SaveData();
            container.Add(new CuiElement
            {
                Parent = "ReportSearch",
                Name = "dd",
                Components =
                {
                    new CuiButtonComponent {Color = "0 0 0 0"},
                    new CuiRectTransformComponent {AnchorMin = "0.08 0.8", AnchorMax = "0.08 0.8"},
                }
            });

            int pp = 0;
            int x = 0;
            int y = 0;
            foreach (var i in datasmallskip)
            {
                if (pp == 18) continue;

                container.Add(new CuiElement
                {
                    Name = "button12",
                    Parent = "dd",
                    Components =
                    {
                        new CuiImageComponent {Color = "0.15 0.15 0.15 0.9"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"{30 + (x * 340)} {-120 - (y * 130)}",
                            OffsetMax = $"{360 + (x * 340)} {0 - (y * 130)}"
                        },
                    }
                });
                var countSmall = 0;
                foreach (var a in Data[medium].SDPages[i.Key].SkillDataList)
                {
                    foreach (var b in a.Value.SkillLevels)
                    {
                        countSmall++;
                    }
                }

                double procentAnchor = (double) pdata.SPData[i.Key].countAcceptedSmall / countSmall;

                container.Add(new CuiElement
                {
                    Parent = "button12",
                    Components =
                    {
                        new CuiButtonComponent
                            {Color = "0.5 1 0.5 0.6", Material = "assets/content/ui/uibackgroundblur-ingamemenu.mat"},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = $"1 {procentAnchor}"},
                    }
                });


                container.Add(new CuiElement
                {
                    Parent = "button12",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Color = "1 1 1 1", FontSize = 18, Font = "robotocondensed-bold.ttf",
                            Align = TextAnchor.LowerCenter, Text = i.Value.displayName.ToUpper()
                        },
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = $"1 0.2"},
                    }
                });
                if (pdata.SPData[i.Key].openClose)
                {
                    if (pdata.SPData[i.Key].performed)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = "button12",
                            Components =
                            {
                                new CuiImageComponent {Color = "1 1 1 1", Sprite = "assets/icons/check.png"},
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = "-32 -32",
                                    OffsetMax = "32 32"
                                },
                            }
                        });
                    }
                    else
                    {
                        container.Add(new CuiElement
                        {
                            Parent = "button12",
                            Components =
                            {
                                new CuiImageComponent {Color = "1 1 1 1", Sprite = "assets/icons/upgrade.png"},
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = "-32 -32",
                                    OffsetMax = "32 32"
                                },
                            }
                        });
                    }
                }

                if (!pdata.SPData[i.Key].openClose)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "button12",
                        Components =
                        {
                            new CuiImageComponent {Color = "1 1 1 1", Sprite = "assets/icons/lock.png"},
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = "-32 -32",
                                OffsetMax = "32 32"
                            },
                        }
                    });
                }

                container.Add(new CuiElement
                {
                    Parent = "button12",
                    Components =
                    {
                        new CuiButtonComponent
                            {Command = $"owerpageskill {data} {medium} {i.Key} {page}", Color = "0.5 1 0.5 0"},
                        new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = $"1 1"},
                    }
                });
                x++;
                if (x == 3)
                {
                    x = 0;
                    y++;
                }

                pp++;
            }

            CuiHelper.AddUi(player, container);
        }

        private void RenderSkill(BasePlayer player, ulong data, ulong medium, ulong small)
        {
            CuiHelper.DestroyUi(player, "dd");
            CuiElementContainer container = new CuiElementContainer();
            var Data = GetDataPage(data);

            var dataskill = Data[medium].SDPages[small].SkillDataList;

            var pdata = PlayerData[player.userID].PagesPlayerData[data].MPData[medium].SPData[small];
            foreach (var p in dataskill)
            {
                if (!pdata.SkillsData.ContainsKey(p.Key))
                {
                    var b = new SkillSettings
                    {
                        levelsCompite = 0
                    };
                    pdata.SkillsData.Add(p.Key, b);
                }
            }

            SaveData();
            container.Add(new CuiElement
            {
                Parent = "ReportSearch",
                Name = "dd",
                Components =
                {
                    new CuiImageComponent {Color = "0 0 0 0"},
                    new CuiRectTransformComponent {AnchorMin = "0.5 0.5", AnchorMax = "0.5 0.5"},
                }
            });
            int x = 0;
            foreach (var i in dataskill)
            {
                container.Add(new CuiElement
                {
                    Name = "button12",
                    Parent = "dd",
                    Components =
                    {
                        new CuiImageComponent {Color = "0.15 0.15 0.15 0.9"},
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 0", AnchorMax = "0 0", OffsetMin = $"-300 {140 - (x * 110)}",
                            OffsetMax = $"300 {240 - (x * 110)}"
                        },
                    }
                });
                container.Add(new CuiElement
                {
                    Parent = "button12",
                    Components =
                    {
                        new CuiTextComponent
                        {
                            Color = "1 1 1 1",
                            FontSize = 18, 
                            Font = "robotocondensed-bold.ttf",
                            Align = TextAnchor.UpperLeft,
                            Text = i.Value.displayName.ToUpper()
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = "0 1", 
                            AnchorMax = $"0 1",
                            OffsetMin = "15 -35",
                            OffsetMax = "150 -7"
                        },
                    }
                });
                if (pdata.openClose)
                    if (pdata.SkillsData[i.Key].levelsCompite != i.Value.SkillLevels.Count)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = "button12",
                            Name = "upp",
                            Components =
                            {
                                new CuiButtonComponent
                                {
                                    Command = $"tryupgradeskill {data} {medium} {small} {i.Key}",
                                    Color = "0.15 0.6 0.15 0"
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "1 0", AnchorMax = "1 0", OffsetMin = $"-100 10", OffsetMax = $"-10 80"
                                },
                            }
                        });
                        container.Add(new CuiElement
                        {
                            Parent = "upp",
                            Components =
                            {
                                new CuiImageComponent {Color = "1 1 1 1", Sprite = "assets/icons/add.png"},
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0.5 0.5", AnchorMax = $"0.5 0.5", OffsetMin = "-20 -20",
                                    OffsetMax = "20 20"
                                },
                            }
                        });
                    }

                var xx = 0;
                int widthPanel = 600;
                double widthSkill = (double) widthPanel / (double) i.Value.SkillLevels.Count;
                foreach (var b in i.Value.SkillLevels)
                {
                    var color = "0.8 0.8 0.8 1";
                    if (pdata.SkillsData[i.Key].levelsCompite >= b.levelnumber)
                    {
                        color = "0.15 0.6 0.15 1";
                    }

                    container.Add(new CuiElement
                    {
                        Name = "dah",
                        Parent = "button12",
                        Components =
                        {
                            new CuiImageComponent {Color = color},
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "0 0", AnchorMax = "0 0",
                                OffsetMin = $"{(xx == 0 ? 0 : (widthSkill * xx) + 5)} 0",
                                OffsetMax = $"{widthSkill + (widthSkill * xx)} 5"
                            },
                        }
                    });
                    xx++;
                }

                x++;

                foreach (var b in i.Value.SkillLevels)
                {
                    if (pdata.SkillsData[i.Key].levelsCompite + 1 == b.levelnumber)
                    {
                        container.Add(new CuiElement
                        {
                            Parent = "button12",
                            Components =
                            {
                                new CuiTextComponent
                                {
                                    Color = "0.8 0.8 0.8 1", FontSize = 13, Font = "robotocondensed-regular.ttf",
                                    Align = TextAnchor.UpperLeft, Text = b.levelText
                                },
                                new CuiRectTransformComponent
                                {
                                    AnchorMin = "0 1", AnchorMax = $"0 1", OffsetMin = "15 -90", OffsetMax = "300 -26"
                                },
                            }
                        });
                    }

                    if (pdata.SkillsData[i.Key].levelsCompite != i.Value.SkillLevels.Count)
                        if (pdata.SkillsData[i.Key].levelsCompite + 1 == b.levelnumber)
                        {
                            container.Add(new CuiElement
                            {
                                Parent = "button12",
                                Components =
                                {
                                    new CuiTextComponent
                                    {
                                        Color = "1 1 1 1", FontSize = 14, Font = "robotocondensed-regular.ttf",
                                        Align = TextAnchor.MiddleCenter, Text = b.priceEconomic.ToString() + "$"
                                    },
                                    new CuiRectTransformComponent
                                    {
                                        AnchorMin = "1 1", AnchorMax = $"1 1", OffsetMin = "-100 -25",
                                        OffsetMax = "-10 -8"
                                    },
                                }
                            });
                        }
                }

                if (pdata.SkillsData[i.Key].levelsCompite == i.Value.SkillLevels.Count)
                {
                    container.Add(new CuiElement
                    {
                        Parent = "button12",
                        Components =
                        {
                            new CuiImageComponent {Color = "1 1 1 1", Sprite = "assets/icons/check.png"},
                            new CuiRectTransformComponent
                            {
                                AnchorMin = "1 0.5", AnchorMax = $"1 0.5", OffsetMin = "-75 -20", OffsetMax = "-35 20"
                            },
                        }
                    });
                }
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion
    }
}