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
using UnityEngine.Localization.Pseudo;
using UnityEngine.UI;

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
        private static bool isTwitchChatAllowed = true;
        private const string twitchServer = "irc.chat.twitch.tv";
        private const int twitchPort = 6667;
        private const string twitchUsername = "justinfan1337"; 
        public static string twitchChannel = ""; 
        private static TcpClient twitchTcpClient;
        private static NetworkStream twitchStream;
        private static StreamReader twitchReader;
        private static StreamWriter twitchWriter;

        private static TextMeshPro chatStatusText;



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

        private static GameObject currentTextObject = null;
        private static void CreateChatStatusText(string message)
        {
            if (currentTextObject != null)
            {
                UnityEngine.Object.Destroy(currentTextObject);
            }

            currentTextObject = new GameObject("ChatStatusText");
            TextMeshPro chatStatusText = currentTextObject.AddComponent<TextMeshPro>();

            chatStatusText.fontSize = 0.05f;
            chatStatusText.color = new Color(0.5f, 0, 1); 
            chatStatusText.alignment = TextAlignmentOptions.Center;
            chatStatusText.text = message;
            chatStatusText.lineSpacing = 1.2f; 

     
            Camera cam = FindObjectOfType<Camera>();
            Vector3 screenCenterPosition = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 0.2f));
            currentTextObject.transform.position = screenCenterPosition; 

   
            currentTextObject.transform.SetParent(cam.transform, true);
            currentTextObject.AddComponent<FaceCamera>();

            UnityEngine.Object.Destroy(currentTextObject, 3f);
        }

        public class FaceCamera : MonoBehaviour
        {
            private Camera mainCamera;

            void Start()
            {
                mainCamera = Camera.main ?? FindObjectOfType<Camera>();

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
     
                            if (message.StartsWith("PING"))
                            {
                                twitchWriter.WriteLine("PONG :tmi.twitch.tv");
                                twitchWriter.Flush();
                            }
                            else if (message.Contains("PRIVMSG"))
                            {
                                var messageParts = message.Split(new[] { ' ' }, 4);
                                if (messageParts.Length >= 4)
                                {
                                    var rawUsername = messageParts[1];
                                    string username = rawUsername.Substring(1, rawUsername.IndexOf('!') - 1);
                                    int messageStartIndex = message.IndexOf("PRIVMSG");
                                    if (messageStartIndex >= 0)
                                    {
                                        string chatMessage = messageParts[3].Substring(1);
                                        string[] chatParts = chatMessage.Split(new[] { " :" }, 2, StringSplitOptions.None);
                                        chatMessage = chatParts[1];

                                        //mls.LogInfo($"chatMessage: {chatMessage}");
                                        //mls.LogInfo($"username: {username}");
                                        var badges = ParseBadges(messageParts[0]);


                                        string badgeDisplay = "";
                                        if (badges.Contains("broadcaster"))
                                        {
                                            badgeDisplay = "[BROADCASTER]";
                                        }
                                        else if (badges.Contains("moderator"))
                                        {
                                            badgeDisplay = "[MODERATOR]";
                                        }
                                        else if (badges.Contains("vip"))
                                        {
                                            badgeDisplay = "[VIP]";
                                        }
                                        else if (badges.Contains("subscriber"))
                                        {
                                            badgeDisplay = "[SUBSCRIBER]";
                                        }

                                        if (!string.IsNullOrEmpty(badgeDisplay))
                                        {
                                            TestMod.ActionQueue.Enqueue(() =>
                                            {
                                                List<Customer> customers = (List<Customer>)CrowdDelegates.getProperty(CSingleton<CustomerManager>.Instance, "m_CustomerList");

                                                if (customers.Count >= 1)
                                                {
                                                    foreach (Customer customer in customers)
                                                    {
                                                        if (customer.isActiveAndEnabled && customer.name.ToLower() == username.ToLower())
                                                        {
                                                            // Construct the message with badge and username
                                                            //string displayMessage = $"{badgeDisplay} {username}: {chatMessage}";

                                                            // Show the message as a popup
                                                            CSingleton<PricePopupSpawner>.Instance.ShowTextPopup(chatMessage, 1.8f, customer.transform);
                                                        }
                                                    }
                                                }
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }

                    Thread.Sleep(50);
                }
            }
            catch (Exception e)
            {
                mls.LogInfo($"Twitch Chat Listener Error: {e.ToString()}");
            }
        }

        public static void DisconnectFromTwitch()
        {
            try
            {
                if (twitchWriter != null)
                {
                    // Send a PART command to leave the Twitch channel
                    twitchWriter.WriteLine("PART #jaku");
                    twitchWriter.Flush();
                    twitchWriter.Close();
                }

                if (twitchReader != null)
                {
                    twitchReader.Close();
                }

                if (twitchStream != null)
                {
                    twitchStream.Close();
                }

                if (twitchTcpClient != null)
                {
                    twitchTcpClient.Close();
                }

                mls.LogInfo("Disconnected from Twitch chat.");
            }
            catch (Exception e)
            {
                mls.LogError($"Error disconnecting from Twitch: {e.Message}");
            }
        }


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
            if (UnityEngine.Input.GetKeyDown(KeyCode.F6)) // Make sure to use UnityEngine.Input
            {
                isTwitchChatAllowed = !isTwitchChatAllowed;
                if (isChatConnected)
                {
                    DisconnectFromTwitch();
                    isChatConnected = false;
                }

                if(isTwitchChatAllowed)
                {
                    TestMod.mls.LogInfo("Twitch Chat is enabled.");
                    CreateChatStatusText("Twitch Chat is enabled.");
                }
                else
                {
                    TestMod.mls.LogInfo("Twitch Chat is disabled.");
                    CreateChatStatusText("Twitch Chat is disabled.");
                }

            }


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
