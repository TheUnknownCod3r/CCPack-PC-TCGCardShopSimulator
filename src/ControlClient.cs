/*
 * ControlClient
 * Unity Game Support for Twitch Crowd Control
 * Copyright (C) 2021 TerribleTable
 * LGPL v2.1
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301
 * USA
 */

using HeathenEngineering.SteamworksIntegration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Security.Policy;
using System.Threading;
using static BepinControl.CrowdResponse;
using static HeathenEngineering.SteamworksIntegration.Currency;


namespace BepinControl
{
    public class ControlClient
    {
        public static readonly string CV_HOST = "127.0.0.1";
        public static readonly int CV_PORT = 51337;

        private Dictionary<string, CrowdDelegate> Delegate { get; set; }
        private IPEndPoint Endpoint { get; set; }
        private Queue<CrowdRequest> Requests { get; set; }
        private bool Running { get; set; }

        private bool paused = false;
        public static Socket Socket { get; set; }

        public bool inGame = true;

        public ControlClient()
        {
            Endpoint = new IPEndPoint(IPAddress.Parse(CV_HOST), CV_PORT);
            Requests = new Queue<CrowdRequest>();
            Running = true;
            Socket = null;

            Delegate = new Dictionary<string, CrowdDelegate>()
            {
                //when an effect comes in with the code it will call the paired function
                {"lights", CrowdDelegates.ToggleLights },
                {"spawn", CrowdDelegates.SpawnCustomer },
                {"spawnsmelly", CrowdDelegates.SpawnCustomerSmelly },
                {"allsmelly", CrowdDelegates.AllSmellyCustomers },
                {"open_store", CrowdDelegates.ShopControls },
                {"close_store", CrowdDelegates.ShopControls },
                {"renamestore", CrowdDelegates.ShopControls },
                {"unlockwh", CrowdDelegates.UnlockWarehouse },
                {"upgradewh", CrowdDelegates.UpgradeWarehouse },
                {"upgradestore", CrowdDelegates.UpgradeStore },
                {"teleport", CrowdDelegates.TeleportPlayer },
                {"forcemath", CrowdDelegates.ForceMath },
                {"forcepayment_cash", CrowdDelegates.ForcePaymentType},
                {"forcepayment_card", CrowdDelegates.ForcePaymentType},
                {"largebills", CrowdDelegates.LargeBills},
                {"give_100", CrowdDelegates.GiveMoney },
                {"give_1000", CrowdDelegates.GiveMoney },
                {"give_10000", CrowdDelegates.GiveMoney },
                {"take_100", CrowdDelegates.TakeMoney },
                {"take_1000", CrowdDelegates.TakeMoney },
                {"take_10000", CrowdDelegates.TakeMoney },

                {"giveempty", CrowdDelegates.GiveEmpty },
                {"giveplayerhugeempty", CrowdDelegates.SendHugeEmpty },
                //{"give_common_pack_(32)", CrowdDelegates.GiveItem },
                {"give_common_pack_(64)", CrowdDelegates.GiveItem },
                //{"give_common_box_(4)", CrowdDelegates.GiveItem },
                {"give_common_box_(8)", CrowdDelegates.GiveItem },
                //{"give_rare_pack_(32)", CrowdDelegates.GiveItem },
                {"give_rare_pack_(64)", CrowdDelegates.GiveItem },
                //{"give_rare_box_(4)", CrowdDelegates.GiveItem },
                {"give_rare_box_(8)", CrowdDelegates.GiveItem },
                //{"give_epic_pack_(32)", CrowdDelegates.GiveItem },
                {"give_epic_pack_(64)", CrowdDelegates.GiveItem },
                //{"give_epic_box_(4)", CrowdDelegates.GiveItem },
                {"give_epic_box_(8)", CrowdDelegates.GiveItem },
                //{"give_legend_pack_(32)", CrowdDelegates.GiveItem },
                {"give_legend_pack_(64)", CrowdDelegates.GiveItem },
                //{"give_legend_box_(4)", CrowdDelegates.GiveItem },
                {"give_legend_box_(8)", CrowdDelegates.GiveItem },
                //{"give_deck_box_red_(sm)", CrowdDelegates.GiveItem },
                {"give_deck_box_red", CrowdDelegates.GiveItem },
                //{"give_deck_box_green_(sm)", CrowdDelegates.GiveItem},
                {"give_deck_box_green", CrowdDelegates.GiveItem },
                //{"give_deck_box_blue_(sm)", CrowdDelegates.GiveItem },
                {"give_deck_box_blue", CrowdDelegates.GiveItem },
                //{"give_deck_box_yellow_(sm)",CrowdDelegates.GiveItem },
                {"give_deck_box_yellow", CrowdDelegates.GiveItem },
                //{"give_destiny_common_pack_(32)", CrowdDelegates.GiveItem },
                {"give_destiny_common_pack_(64)", CrowdDelegates.GiveItem },
                {"give_destiny_common_box_(8)", CrowdDelegates.GiveItem },
                //{"give_destiny_common_box_(4)", CrowdDelegates.GiveItem },
                //{"give_destiny_rare_pack_(32)", CrowdDelegates.GiveItem },
                {"give_destiny_rare_pack_(64)", CrowdDelegates.GiveItem },
                //{"give_destiny_rare_box_(4)", CrowdDelegates.GiveItem },
                {"give_destiny_rare_box_(8)", CrowdDelegates.GiveItem },
                //{"give_destiny_epic_pack_(32)", CrowdDelegates.GiveItem },
                {"give_destiny_epic_pack_(64)", CrowdDelegates.GiveItem },
                //{"give_destiny_epic_box_(4)", CrowdDelegates.GiveItem },
                {"give_destiny_epic_box_(8)", CrowdDelegates.GiveItem },
                //{"give_destiny_legend_pack_(32)", CrowdDelegates.GiveItem },
                {"give_destiny_legend_pack_(64)", CrowdDelegates.GiveItem },
                //{"give_destiny_legend_box_(4)", CrowdDelegates.GiveItem },
                {"give_destiny_legend_box_(8)", CrowdDelegates.GiveItem },
                {"give_cleanser_(16)", CrowdDelegates.GiveItem },
                {"give_cleanser_(32)", CrowdDelegates.GiveItem },
                {"give_collection_book", CrowdDelegates.GiveItem },
                {"give_d20_dice_red", CrowdDelegates.GiveItem },
                {"give_d20_dice_blue", CrowdDelegates.GiveItem },
                {"give_d20_dice_black", CrowdDelegates.GiveItem },
                {"give_d20_dice_white", CrowdDelegates.GiveItem },
                {"give_piggya_plushie", CrowdDelegates.GiveItem },
                {"give_golema_plushie", CrowdDelegates.GiveItem },
                {"give_starfisha_plushie", CrowdDelegates.GiveItem },
                {"give_bata_plushie", CrowdDelegates.GiveItem },
                {"give_toonz_plushie", CrowdDelegates.GiveItem },
                {"give_burpig_figurine", CrowdDelegates.GiveItem },
                {"give_inferhog_figurine", CrowdDelegates.GiveItem },
                {"give_blazoar_plushie", CrowdDelegates.GiveItem },
                {"give_decimite_figurine", CrowdDelegates.GiveItem },
                {"give_meganite_figurine", CrowdDelegates.GiveItem },
                {"give_giganite_statue", CrowdDelegates.GiveItem },
                {"give_trickstar_figurine", CrowdDelegates.GiveItem },
                {"give_princestar_figurine", CrowdDelegates.GiveItem },
                {"give_kingstar_plushie", CrowdDelegates.GiveItem },
                {"give_lunight_figurine", CrowdDelegates.GiveItem },
                {"give_vampicant_figurine", CrowdDelegates.GiveItem },
                {"give_dracunix_figurine", CrowdDelegates.GiveItem },
                {"give_drilceros_action_figure", CrowdDelegates.GiveItem },
                {"give_bonfiox_plushie", CrowdDelegates.GiveItem },
                {"give_premium_collection_book", CrowdDelegates.GiveItem },
                {"give_fire_battle_deck", CrowdDelegates.GiveItem },
                {"give_earth_battle_deck", CrowdDelegates.GiveItem },
                {"give_water_battle_deck", CrowdDelegates.GiveItem },
                {"give_wind_battle_deck", CrowdDelegates.GiveItem },
                {"give_fire_destiny_deck", CrowdDelegates.GiveItem },
                {"give_earth_destiny_deck", CrowdDelegates.GiveItem },
                {"give_water_destiny_deck", CrowdDelegates.GiveItem },
                {"give_wind_destiny_deck", CrowdDelegates.GiveItem },
                {"give_card_sleeves_(clear)", CrowdDelegates.GiveItem },
                {"give_card_sleeves_(tetramon)", CrowdDelegates.GiveItem },
                {"give_card_sleeves_(fire)", CrowdDelegates.GiveItem },
                {"give_card_sleeves_(earth)", CrowdDelegates.GiveItem },
                {"give_card_sleeves_(water)", CrowdDelegates.GiveItem },
                {"give_card_sleeves_(wind)", CrowdDelegates.GiveItem },
                {"give_playmat_(clamigo)", CrowdDelegates.GiveItem },
                {"give_playmat_(duel)", CrowdDelegates.GiveItem },
                {"give_playmat_(drilceros)", CrowdDelegates.GiveItem },
                {"give_playmat_(drakon)", CrowdDelegates.GiveItem },
                {"give_playmat_(the_four_dragons)", CrowdDelegates.GiveItem },
                {"give_playmat_(dracunix)", CrowdDelegates.GiveItem },
                {"give_playmat_(wispo)", CrowdDelegates.GiveItem },
                {"give_playmat_(gigatronx_evo)", CrowdDelegates.GiveItem },
                {"give_playmat_(tetramon)", CrowdDelegates.GiveItem },
                {"give_playmat_(kyrone)", CrowdDelegates.GiveItem },
                {"give_playmat_(fire)", CrowdDelegates.GiveItem },
                {"give_playmat_(earth)", CrowdDelegates.GiveItem },
                {"give_playmat_(wind)", CrowdDelegates.GiveItem },
                {"give_playmat_(lunight)", CrowdDelegates.GiveItem },
                {"give_playmat_(water)", CrowdDelegates.GiveItem },

                //{"giveatplayer_common_pack_(32)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_common_pack_(64)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_common_box_(4)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_common_box_(8)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_rare_pack_(32)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_rare_pack_(64)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_rare_box_(4)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_rare_box_(8)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_epic_pack_(32)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_epic_pack_(64)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_epic_box_(4)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_epic_box_(8)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_legend_pack_(32)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_legend_pack_(64)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_legend_box_(4)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_legend_box_(8)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_deck_box_red_(sm)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_deck_box_red", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_deck_box_green_(sm)", CrowdDelegates.GiveItemAtPlayer},
                {"giveatplayer_deck_box_green", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_deck_box_blue_(sm)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_deck_box_blue", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_deck_box_yellow_(sm)",CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_deck_box_yellow", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_destiny_common_pack_(32)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_destiny_common_pack_(64)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_destiny_common_box_(8)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_destiny_common_box_(4)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_destiny_rare_pack_(32)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_destiny_rare_pack_(64)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_destiny_rare_box_(4)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_destiny_rare_box_(8)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_destiny_epic_pack_(32)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_destiny_epic_pack_(64)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_destiny_epic_box_(4)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_destiny_epic_box_(8)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_destiny_legend_pack_(32)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_destiny_legend_pack_(64)", CrowdDelegates.GiveItemAtPlayer },
                //{"giveatplayer_destiny_legend_box_(4)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_destiny_legend_box_(8)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_cleanser_(16)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_cleanser_(32)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_collection_book", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_d20_dice_red", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_d20_dice_blue", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_d20_dice_black", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_d20_dice_white", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_piggya_plushie", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_golema_plushie", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_starfisha_plushie", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_bata_plushie", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_toonz_plushie", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_burpig_figurine", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_inferhog_figurine", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_blazoar_plushie", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_decimite_figurine", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_meganite_figurine", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_giganite_statue", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_trickstar_figurine", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_princestar_figurine", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_kingstar_plushie", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_lunight_figurine", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_vampicant_figurine", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_dracunix_figurine", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_drilceros_action_figure", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_bonfiox_plushie", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_premium_collection_book", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_fire_battle_deck", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_earth_battle_deck", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_water_battle_deck", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_wind_battle_deck", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_fire_destiny_deck", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_earth_destiny_deck", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_water_destiny_deck", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_wind_destiny_deck", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_card_sleeves_(clear)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_card_sleeves_(tetramon)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_card_sleeves_(fire)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_card_sleeves_(earth)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_card_sleeves_(water)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_card_sleeves_(wind)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(clamigo)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(duel)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(drilceros)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(drakon)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(the_four_dragons)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(dracunix)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(wispo)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(gigatronx_evo)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(tetramon)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(kyrone)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(fire)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(earth)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(wind)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(lunight)", CrowdDelegates.GiveItemAtPlayer },
                {"giveatplayer_playmat_(water)", CrowdDelegates.GiveItemAtPlayer },

                {"highfov", CrowdDelegates.HighFOV },
                {"lowfov", CrowdDelegates.LowFOV },
                {"invertx", CrowdDelegates.InvertX },
                {"inverty", CrowdDelegates.InvertY },
                {"language_english", CrowdDelegates.SetLanguage },
                {"language_french", CrowdDelegates.SetLanguage },
                {"language_german", CrowdDelegates.SetLanguage },
                {"language_italian", CrowdDelegates.SetLanguage },
                {"language_spanish", CrowdDelegates.SetLanguage },
                {"language_chineset", CrowdDelegates.SetLanguage },
                {"language_chineses", CrowdDelegates.SetLanguage },
                {"language_korean", CrowdDelegates.SetLanguage },
                {"language_thai", CrowdDelegates.SetLanguage },
                //{"language_dutch", CrowdDelegates.SetLanguage },

                {"speak_heyoo", CrowdDelegates.HeyOhh },

                {"hireworker", CrowdDelegates.HireWorker },
                {"fireworker", CrowdDelegates.FireWorker },

                {"throwitem", CrowdDelegates.ThrowItem },
                {"exactchange", CrowdDelegates.ExactChange },
                {"furniture_small_cabinet", CrowdDelegates.GiveItemFurniture },
                {"furniture_small_metal_rack", CrowdDelegates.GiveItemFurniture },
                {"furniture_play_table", CrowdDelegates.GiveItemFurniture},
                {"furniture_small_personal_shelf", CrowdDelegates.GiveItemFurniture},
                {"furniture_single_sided_shelf", CrowdDelegates.GiveItemFurniture },
                {"furniture_card_table", CrowdDelegates.GiveItemFurniture },
                {"furniture_small_warehouse_shelf", CrowdDelegates.GiveItemFurniture },
                {"furniture_small_card_display", CrowdDelegates.GiveItemFurniture },
                {"furniture_auto_scent_m100", CrowdDelegates.GiveItemFurniture },
                {"furniture_workbench", CrowdDelegates.GiveItemFurniture },
                {"furniture_double_sided_shelf", CrowdDelegates.GiveItemFurniture },
                {"furniture_big_warehouse_shelf", CrowdDelegates.GiveItemFurniture },
                {"furniture_checkout_counter", CrowdDelegates.GiveItemFurniture },
                {"furniture_auto_scent_g500", CrowdDelegates.GiveItemFurniture },
                {"furniture_card_display_table", CrowdDelegates.GiveItemFurniture },
                {"furniture_big_personal_shelf", CrowdDelegates.GiveItemFurniture },
                {"furniture_vintage_card_table", CrowdDelegates.GiveItemFurniture },
                {"furniture_wide_shelf", CrowdDelegates.GiveItemFurniture },
                {"furniture_huge_personal_shelf", CrowdDelegates.GiveItemFurniture },
                {"furniture_auto_scent_t1000", CrowdDelegates.GiveItemFurniture},
                {"furniture_big_card_display", CrowdDelegates.GiveItemFurniture },
                {"furniture_pack_machine_s", CrowdDelegates.GiveItemFurniture },
                {"furniture_pack_machine_m", CrowdDelegates.GiveItemFurniture },
                {"furniture_pack_machine_l", CrowdDelegates.GiveItemFurniture },

                {"give_necromansters", CrowdDelegates.GiveItem},
                {"giveatplayer_necromansters", CrowdDelegates.GiveItemAtPlayer },
                {"give_mafia_works",CrowdDelegates.GiveItem },
                {"giveatplayer_mafia_works",CrowdDelegates.GiveItemAtPlayer},
                {"give_claim!",CrowdDelegates.GiveItem },
                {"giveatplayer_claim!", CrowdDelegates.GiveItemAtPlayer },
                {"give_system_gate_#1",CrowdDelegates.GiveItem },
                {"giveatplayer_system_gate_#1",CrowdDelegates.GiveItemAtPlayer },
                {"give_system_gate_#2",CrowdDelegates.GiveItem },
                {"giveatplayer_system_gate_#2",CrowdDelegates.GiveItemAtPlayer },

                {"openpack_common_pack", CrowdDelegates.OpenCardPack },
                {"openpack_rare_pack", CrowdDelegates.OpenCardPack },
                {"openpack_epic_pack", CrowdDelegates.OpenCardPack },
                {"openpack_legend_pack", CrowdDelegates.OpenCardPack },
                {"openpack_destiny_common_pack", CrowdDelegates.OpenCardPack },
                {"openpack_destiny_rare_pack", CrowdDelegates.OpenCardPack },
                {"openpack_destiny_epic_pack", CrowdDelegates.OpenCardPack },
                {"openpack_destiny_legend_pack", CrowdDelegates.OpenCardPack },

                {"spawn_bread", CrowdDelegates.SpawnBread },
                {"spawn_milk", CrowdDelegates.SpawnMilk },

                {"event-hype-train", CrowdDelegates.SpawnHypeTrain },
                {"send_comic_1", CrowdDelegates.GiveItem },
                {"send_comic_2", CrowdDelegates.GiveItem },
                {"send_comic_3", CrowdDelegates.GiveItem },
                {"send_comic_4", CrowdDelegates.GiveItem },
                {"send_comic_5", CrowdDelegates.GiveItem },
                {"send_comic_6", CrowdDelegates.GiveItem },
                {"send_comic_7", CrowdDelegates.GiveItem },
                {"send_comic_8", CrowdDelegates.GiveItem },
                {"send_comic_9", CrowdDelegates.GiveItem },
                {"send_comic_10", CrowdDelegates.GiveItem },
                {"send_comic_11", CrowdDelegates.GiveItem },
                {"send_comic_12", CrowdDelegates.GiveItem },
                {"send_comic_13", CrowdDelegates.GiveItem },
                {"send_comic_14", CrowdDelegates.GiveItem },



            };
        }

        public bool isReady()
        {
            try
            {
                if (UserData.Me.FriendId.ToString() == "76561198309052504" || UserData.Me.ToString() == "76561198071868467") return false;//Ban Chris_From_Canada and Fayrore from ALL projects im associated with.
                bool isFullyLoaded = CGameManager.Instance.m_IsGameLevel;
                if (!isFullyLoaded) return false;
                //make sure the game is in focus otherwise don't let effects trigger
                if (!TestMod.isFocused) return false;
                //add check for whether the game is in a state it can accept effects
                PauseScreen PS = PauseScreen.Instance;
                bool isPaused = PauseScreen.Instance.m_ScreenGrp.activeSelf;
                if (isPaused) return false;

            }
            catch (Exception e)
            {
                TestMod.mls.LogError(e.ToString());
                return false;
            }

            return true;
        }

        public static void HideEffect(string code)
        {
            CrowdResponse res = new CrowdResponse(0, CrowdResponse.Status.STATUS_NOTVISIBLE);
            res.type = 1;
            res.code = code;
            res.Send(Socket);
        }

        public static void ShowEffect(string code)
        {
            CrowdResponse res = new CrowdResponse(0, CrowdResponse.Status.STATUS_VISIBLE);
            res.type = 1;
            res.code = code;
            res.Send(Socket);
        }

        public static void DisableEffect(string code)
        {
            CrowdResponse res = new CrowdResponse(0, CrowdResponse.Status.STATUS_NOTSELECTABLE);
            res.type = 1;
            res.code = code;
            res.Send(Socket);
        }

        public static void EnableEffect(string code)
        {
            CrowdResponse res = new CrowdResponse(0, CrowdResponse.Status.STATUS_SELECTABLE);
            res.type = 1;
            res.code = code;
            res.Send(Socket);
        }




        private void ClientLoop()
        {

            TestMod.mls.LogInfo("Connected to Crowd Control");
            TestMod.ActionQueue.Enqueue(() =>
            {
                TestMod.CreateChatStatusText("Connected to Crowd Control!");
            });
            var timer = new Timer(timeUpdate, null, 0, 150);


            try
            {
                while (Running)
                {
                    CrowdRequest req = CrowdRequest.Recieve(this, Socket);
                    if (req == null || req.IsKeepAlive()) continue;

                    lock (Requests)
                        Requests.Enqueue(req);
                }
            }
            catch (Exception e)
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    TestMod.CreateChatStatusText("Disconnected from Crowd Control!");
                });
                TestMod.mls.LogInfo($"Disconnected from Crowd Control. {e.ToString()}");
                Socket.Close();
            }
        }

        public void timeUpdate(System.Object state)
        {
            inGame = true;

            if (!isReady()) inGame = false;

            if (!inGame)
            {
                TimedThread.addTime(200);
                paused = true;
            }
            else if (paused)
            {
                paused = false;
                TimedThread.unPause();
                TimedThread.tickTime(200);
            }
            else
            {
                TimedThread.tickTime(200);
            }
        }

        public bool IsRunning() => Running;

        public void NetworkLoop()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            while (Running)
            {

                TestMod.mls.LogInfo("Attempting to connect to Crowd Control");
                try
                {
                    Socket = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    if (Socket.BeginConnect(Endpoint, null, null).AsyncWaitHandle.WaitOne(10000, true) && Socket.Connected)
                        ClientLoop();
                    else
                        TestMod.mls.LogInfo("Failed to connect to Crowd Control");
                    Socket.Close();
                }
                catch (Exception e)
                {
                    TestMod.mls.LogInfo(e.GetType().Name);
                    TestMod.mls.LogInfo("Failed to connect to Crowd Control");
                }


                


                Thread.Sleep(10000);
            }
        }

        public void RequestLoop()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            while (Running)
            {
                try
                {
                    if (!inGame)
                    {
                        Thread.Yield();
                    }
                    CrowdRequest req = null;
                    lock (Requests)
                    {
                        if (Requests.Count == 0)
                            continue;
                        req = Requests.Dequeue();
                    }

                    string code = req.GetReqCode();
                    try
                    {
                        CrowdResponse res;
                        if (!isReady())
                            res = new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY);
                        else
                            res = Delegate[code](this, req);
                        if (res == null)
                        {
                            new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, $"Request error for '{code}'").Send(Socket);
                        }

                        res.Send(Socket);
                    }
                    catch (KeyNotFoundException)
                    {
                        new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, $"Request error for '{code}'").Send(Socket);
                    }
                }
                catch (Exception)
                {
                    TestMod.mls.LogInfo("Disconnected from Crowd Control");
                    Socket.Close();
                }
            }
        }

        public void Stop()
        {
            Running = false;
        }

    }
}
