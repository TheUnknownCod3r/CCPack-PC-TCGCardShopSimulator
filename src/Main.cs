using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Threading;
using UnityEngine.EventSystems;
using UnityEngine;
using TMPro;
using System.Net.Sockets;
using System.IO;
using System.Linq;

namespace BepinControl
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class TestMod : BaseUnityPlugin
    {
        // Mod Details
        private const string modGUID = "WarpWorld.CrowdControl";
        private const string modName = "Crowd Control";
        private const string modVersion = "1.0.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static ManualLogSource mls;

        internal static TestMod Instance = null;
        private ControlClient client = null;
        public static bool isFocused = true;
        public static bool doneItems = false;
        public static bool ForceMath = false;
        public static bool WorkersFast = false;
        public static bool ForceUseCash = false;
        public static bool ForceUseCredit = false;
        public static bool isWarehouseUnlocked = false;
        public static bool isSmelly = false;
        public static int WareHouseRoomsUnlocked = 0;
        public static int ShopRoomUnlocked = 0;
        public static string NameOverride = "";
        public static string OrgLanguage = "";
        public static string NewLanguage = "";

        public static bool isIrcConnected = false;
        private static bool isChatConnected = false; 
        private const string twitchServer = "irc.chat.twitch.tv";
        private const int twitchPort = 6667;
        private const string twitchUsername = "justinfan1337"; 
        public static string twitchChannel = ""; 
        private static TcpClient twitchTcpClient;
        private static NetworkStream twitchStream;
        private static StreamReader twitchReader;
        private static StreamWriter twitchWriter;

        void Awake()
        {
            Instance = this;
            mls = BepInEx.Logging.Logger.CreateLogSource("Crowd Control");

            mls.LogInfo($"Loaded {modGUID}. Patching.");
            harmony.PatchAll(typeof(TestMod));
            harmony.PatchAll();
            CustomerManagerPatches.ApplyPatches(harmony);

            mls.LogInfo($"Initializing Crowd Control");

            try
            {
                client = new ControlClient();
                new Thread(new ThreadStart(client.NetworkLoop)).Start();
                new Thread(new ThreadStart(client.RequestLoop)).Start();

            }
            catch (Exception e)
            {
                mls.LogInfo($"CC Init Error: {e.ToString()}");
            }

            mls.LogInfo($"Crowd Control Initialized");
        }

        public static Queue<Action> ActionQueue = new Queue<Action>();

        public static void ConnectToTwitchChat()
        {
            if (!isChatConnected && twitchChannel.Length>=1)
            {
                new Thread(new ThreadStart(StartTwitchChatListener)).Start();
                isChatConnected = true;
            }
        }

        public static void StartTwitchChatListener()
        {
            try
            {
                twitchTcpClient = new TcpClient(twitchServer, twitchPort);
                twitchStream = twitchTcpClient.GetStream();
                twitchReader = new StreamReader(twitchStream);
                twitchWriter = new StreamWriter(twitchStream);

                // Request membership and tags capabilities from Twitch
                twitchWriter.WriteLine("CAP REQ :twitch.tv/membership twitch.tv/tags");

                // Send authentication credentials
                twitchWriter.WriteLine($"NICK {twitchUsername}");
                twitchWriter.WriteLine($"JOIN #{twitchChannel}");
                twitchWriter.Flush();

                mls.LogInfo($"Connected to Twitch channel: {twitchChannel}");

                while (true)
                {
                    if (twitchStream.DataAvailable)
                    {
                        var message = twitchReader.ReadLine();
                        if (message != null)
                        {
                            // Log the PING message to keep connection alive
                            if (message.StartsWith("PING"))
                            {
                                twitchWriter.WriteLine("PONG :tmi.twitch.tv");
                                twitchWriter.Flush();
                            }
                            else if (message.Contains("PRIVMSG"))
                            {
                                var splitMessage = message.Split(new[] { ' ' }, 4);
                                if (splitMessage.Length >= 4)
                                {
                                    var tagsPart = splitMessage[0]; // This part contains tags
                                    var rawUsername = splitMessage[1];
                                    string username = rawUsername.Substring(1, rawUsername.IndexOf('!') - 1);
                                    string chatMessage = splitMessage[3].Substring(1);

                                    var badges = ParseBadges(tagsPart);

                                    if (badges.Contains("subscriber") || badges.Contains("moderator") || badges.Contains("vip") || badges.Contains("broadcaster") )
                                    {
                                        //mls.LogInfo($"[{username}] (badges: {badges}): {chatMessage}");

                                        TestMod.ActionQueue.Enqueue(() =>
                                        {
                                            List<Customer> customers = (List<Customer>)CrowdDelegates.getProperty(CSingleton<CustomerManager>.Instance, "m_CustomerList");

                                            if (customers.Count >= 1)
                                            {
                                                CustomerManager customerManager = CSingleton<CustomerManager>.Instance;
                                                List<string> textList = new List<string> { chatMessage };

                                                foreach (Customer customer in customers)
                                                {
                                                    if (customer.isActiveAndEnabled && customer.name.ToLower() == username.ToLower())
                                                    {
                                                        CrowdDelegates.setProperty(customer, "m_IsChattyCustomer", true);
                                                        CSingleton<PricePopupSpawner>.Instance.ShowTextPopup(chatMessage, 1.8f, customer.transform);
                                                    }
                                                }
                                            }
                                        });
                                    }
                                    else
                                    {
                                        mls.LogInfo($"[{username}] does not have the required badges.");
                                    }
                                }
                            }
                        }
                    }

                    // Sleep to prevent overwhelming the CPU
                    Thread.Sleep(50);
                }
            }
            catch (Exception e)
            {
                mls.LogInfo($"Twitch Chat Listener Error: {e.ToString()}");
            }
        }

        // Helper method to parse the badges from the message tags
        public static HashSet<string> ParseBadges(string tagsPart)
        {
            var badgesSet = new HashSet<string>();
            var tags = tagsPart.Split(';');

            foreach (var tag in tags)
            {
                if (tag.StartsWith("badges="))
                {
                    var badges = tag.Substring("badges=".Length).Split(',');
                    foreach (var badge in badges)
                    {
                        var badgeType = badge.Split('/')[0];
                        badgesSet.Add(badgeType);
                    }
                }
            }

            return badgesSet;
        }


        //attach this to some game class with a function that runs every frame like the player's Update()
        [HarmonyPatch(typeof(CGameManager), "Update")]
        [HarmonyPrefix]
        static void RunEffects()
        {
            if (CGameManager.Instance.m_IsGameLevel && !doneItems)//lets print all card arrays in the restock data, so we can use them
            {
                foreach (var cardPack in CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.ToArray())
                {
                    TestMod.mls.LogInfo(cardPack.name + " : Warehouse Rooms: " + UnlockRoomManager.Instance.m_LockedRoomBlockerList.Count);

                }
                doneItems = true;
            }

            while (ActionQueue.Count > 0)
            {
                Action action = ActionQueue.Dequeue();
                action.Invoke();
            }

            lock (TimedThread.threads)
            {
                foreach (var thread in TimedThread.threads)
                {
                    if (!thread.paused)
                        thread.effect.tick();
                }
            }

        }


        [HarmonyPatch(typeof(EventSystem), "OnApplicationFocus")]
        public static class EventSystem_OnApplicationFocus_Patch
        {
            public static void Postfix(bool hasFocus)
            {
                isFocused = hasFocus;
            }
        }




        [HarmonyPatch(typeof(InteractableCustomerCash), "SetIsCard")]
        public static class SetIsCardPatch
        {
            public static void Prefix(ref bool isCard)
            {
                if (ForceUseCash)
                {
                    isCard = false;
                    return;
                }
                if (ForceUseCredit)
                {
                    isCard = true;
                    return;
                }
            }
        }


        [HarmonyPatch(typeof(InteractableCashierCounter), "StartGivingChange")]
        public static class StartGivingChangePatch
        {

            public static void Prefix(InteractableCashierCounter __instance, ref bool ___m_IsUsingCard)
            {

                if (ForceUseCash)
                {
                    ___m_IsUsingCard = false;
                }

                if (ForceUseCredit)
                {
                    ___m_IsUsingCard = true;
                }

            }
        }

        public class NamePlateController : MonoBehaviour
        {
            private Camera mainCamera;

            void Start()
            {
                mainCamera = Camera.main;

                if (mainCamera == null)
                {
                    mainCamera = FindObjectOfType<Camera>();
                }
            }

            void LateUpdate()
            {
                if (mainCamera == null) return;

                Vector3 directionToCamera = mainCamera.transform.position - transform.position;
                directionToCamera.y = 0;
                Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
                transform.rotation = lookRotation * Quaternion.Euler(0, 180, 0);
            }
        }

        public static class CustomerManagerPatches
        {
            public static void ApplyPatches(Harmony harmonyInstance)
            {
                var original = typeof(CustomerManager).GetMethod("GetNewCustomer", new Type[] { });
                var postfix = new HarmonyMethod(typeof(CustomerManagerPatches).GetMethod(nameof(GetNewCustomerPostfix), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic));
                harmonyInstance.Patch(original, null, postfix);
            }

            private static void GetNewCustomerPostfix(Customer __result)
            {
                if (__result != null)
                {
                    AddNamePlateToCustomer(__result);
                    ConnectToTwitchChat();
                }
            }

            private static void AddNamePlateToCustomer(Customer customer)
            {

                if (customer.transform.Find("NamePlate") != null)
                {
                    return;
                }

                string chatterName = NameOverride;


                if (string.IsNullOrEmpty(chatterName)) return;

                GameObject namePlate = new GameObject("NamePlate");
                namePlate.transform.SetParent(customer.transform);
                namePlate.transform.localPosition = Vector3.up * 1.9f; 

                TextMeshPro tmp = namePlate.AddComponent<TextMeshPro>();
                tmp.text = $"<b>{chatterName}</b>";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 1;
                tmp.fontMaterial.EnableKeyword("OUTLINE_ON");
                tmp.outlineColor = Color.black; 
                tmp.outlineWidth = 0.2f;
                if (isSmelly) tmp.color = new Color(0.0f, 1.0f, 0.0f);
                


                namePlate.AddComponent<NamePlateController>();
            }
        }



    }

}
