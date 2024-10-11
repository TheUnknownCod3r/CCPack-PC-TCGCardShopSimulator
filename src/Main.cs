using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System.Threading;
using UnityEngine;
using TMPro;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using static BepinControl.TestMod;
using System.Reflection;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace BepinControl
{
    [BepInPlugin(modGUID, modName, modVersion)]
    public class TestMod : BaseUnityPlugin
    {
        // Mod Details
        private const string modGUID = "WarpWorld.CrowdControl";
        private const string modName = "Crowd Control";
        private const string modVersion = "1.0.12.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        public static ManualLogSource mls;

        internal static TestMod Instance = null;
        private ControlClient client = null;
        public static bool loadedIntoWorld = false;
        public static bool isFocused = true;
        public static bool doneItems = false; //comment out, keep for future use if necessary

        public static bool ForceMath = false;
        public static bool WorkersFast = false;
        public static bool ForceUseCash = false;
        public static bool ForceUseCredit = false;
        public static bool ExactChange = false;
        public static bool LargeBills = false;
        public static bool CustomersOverpay = false;

        public static bool isSmelly = false;

        public static Vector3 oldcashScale = new Vector3(6.408165f, 41.87513f, 8.071795f);//cash size fix
        public static Vector3 oldcashScaleOutline = new Vector3(6.598893f, 43.12251f, 8.312037f);//cash size fix
        public static Vector3 oldCardScale = new Vector3(5.760878f, 7.510158f, 3.336215f);//card size fix
        public static Vector3 oldCardScaleOutline = new Vector3(5.897457f, 8.062623f, 3.415312f);//card size fix

        public static int WareHouseRoomsUnlocked = 0;
        public static bool isWarehouseUnlocked = false;
        public static int ShopRoomUnlocked = 0;

        public static string NameOverride = "";

        public static string OrgLanguage = "";
        public static string NewLanguage = "";

        public static float OrigSensJS = 0f;
        public static float OrigSensMS = 0f;

        public static bool HasPrintedScales = false;
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
        public static bool autoOpenCards = false;


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

        public static GameObject currentTextObject = null;
        public static void CreateChatStatusText(string message)
        {

            if (!loadedIntoWorld) return;


            if (currentTextObject != null)
            {
                UnityEngine.Object.Destroy(currentTextObject);
            }

            Camera cam = FindObjectOfType<Camera>();
            if (cam == null) return;


            currentTextObject = new GameObject("ChatStatusText");
            TextMeshPro chatStatusText = currentTextObject.AddComponent<TextMeshPro>();

            chatStatusText.fontSize = 0.05f;
            chatStatusText.color = new Color(0.5f, 0, 1);
            chatStatusText.alignment = TextAlignmentOptions.Center;
            chatStatusText.text = message;
            chatStatusText.lineSpacing = 1.2f;



            Vector3 screenCenterPosition = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.6f, 0.15f));
            currentTextObject.transform.position = screenCenterPosition;


            currentTextObject.transform.SetParent(cam.transform, true);
            currentTextObject.AddComponent<FaceCamera>();

            UnityEngine.Object.Destroy(currentTextObject, 3f);
        }

        [HarmonyPatch(typeof(TitleScreen), "Start")]
        public static class TitleScreenPatch
        {
            public static void Postfix(ref TextMeshProUGUI ___m_VersionText)
            {
                ___m_VersionText.text += "\nCrowd Control version: v" + TestMod.modVersion;
            }
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
                directionToCamera.Normalize();

                Quaternion lookRotation = Quaternion.LookRotation(directionToCamera);
                transform.rotation = lookRotation * Quaternion.Euler(0, 180, 0);

            }
        }

        public static Queue<Action> ActionQueue = new Queue<Action>();

        public static void ConnectToTwitchChat()
        {
            if (!isChatConnected && twitchChannel.Length >= 1)
            {
                new Thread(new ThreadStart(StartTwitchChatListener)).Start();
                isChatConnected = true;
            }
        }



        public static void MakeCustomerSmellyTemporarily(Customer customer, float duration)
        {
            customer.SetSmelly();

            Timer timer = new Timer(_ => ClearSmellyStatus(customer), null, (int)(duration * 1000), Timeout.Infinite);
        }

        private static void ClearSmellyStatus(Customer customer)
        {
            customer.m_SmellyFX.SetActive(false);
            customer.m_CleanFX.SetActive(true);
            CrowdDelegates.setProperty(customer, "m_IsSmelly", false);
            CSingleton<CustomerManager>.Instance.RemoveFromSmellyCustomerList(customer);
        }



        private static List<string> allowedUsernames = new List<string> { "jaku", "s4turn", "crowdcontrol", "theunknowncod3r" };

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

                                        string[] triggerWords = { "fart", "gas", "burp", "smell", "shit", "poo", "stank", "nasty" };

                                        if (!string.IsNullOrEmpty(badgeDisplay) || allowedUsernames.Any(name => name.ToLower().Equals(username, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            TestMod.ActionQueue.Enqueue(() =>
                                            {
                                                try
                                                {

                                                    List<Customer> customers = (List<Customer>)CrowdDelegates.getProperty(CSingleton<CustomerManager>.Instance, "m_CustomerList");

                                                    if (customers.Count >= 1)
                                                    {
                                                        foreach (Customer customer in customers)
                                                        {
                                                            if (customer.isActiveAndEnabled && customer.name.ToLower() == username.ToLower())
                                                            {
                                                                //string displayMessage = $"{badgeDisplay} {username}: {chatMessage}";
                                                                string lowerChatMessage = chatMessage.ToLower();
                                                                if (triggerWords.Any(word => lowerChatMessage.Contains(word)))
                                                                {
                                                                    if (!customer.IsSmelly())
                                                                    {
                                                                        MakeCustomerSmellyTemporarily(customer, 5f);
                                                                    }
                                                                }
                                                                CSingleton<PricePopupSpawner>.Instance.ShowTextPopup(chatMessage, 1.8f, customer.transform);
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    //is the customer active?
                                                    //mls.LogInfo(e.ToString());
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
                if (twitchWriter != null && twitchChannel.Length >= 1)
                {
                    twitchWriter.WriteLine("PART #" + twitchChannel);
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
            if (UnityEngine.Input.GetKeyDown(KeyCode.F6))
            {
                isTwitchChatAllowed = !isTwitchChatAllowed;
                if (isChatConnected)
                {
                    DisconnectFromTwitch();
                    isChatConnected = false;
                }

                if (isTwitchChatAllowed)
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


            //if (CGameManager.Instance.m_IsGameLevel && !doneItems)//lets print all card arrays in the restock data, so we can use them
            //{
            // foreach (var cardPack in InventoryBase.Instance.m_StockItemData_SO.m_RestockDataList.ToArray())
            //{
            //TestMod.mls.LogInfo("Name: "+cardPack.name+", Amount: "+cardPack.amount);

            //}
            //foreach (var furniture in InventoryBase.Instance.m_ObjectData_SO.m_FurniturePurchaseDataList.ToArray())//And the furniture!
            // {
            // TestMod.mls.LogInfo(furniture.name);
            // }
            //foreach (var obj in InventoryBase.Instance.m_ObjectData_SO.m_ObjectDataList.ToArray())//And the furniture!
            // {
            //TestMod.mls.LogInfo("Name: " + obj.name + " : Type: " + obj.objectType);
            //}
            //  foreach (var obj in InventoryBase.Instance.m_ObjectData_SO.m_ObjectDataList.ToArray())//And the furniture!
            // {
            // TestMod.mls.LogInfo("Name: " + obj.name + " : Type: " + obj.objectType);
            // }
            //doneItems = true;
            //}

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
                var original = typeof(CustomerManager).GetMethod("GetNewCustomer", new Type[] { typeof(bool) });
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



        [HarmonyPatch(typeof(InteractionPlayerController))]
        [HarmonyPatch("OnEnable")]
        class Patch_OnEnable
        {
            static void Postfix()
            {
                TestMod.loadedIntoWorld = true;
            }
        }

        [HarmonyPatch(typeof(EventSystem), "OnApplicationFocus")]
        public static class EventSystem_OnApplicationFocus_Patch
        {
            public static void Postfix(bool hasFocus)
            {
                TestMod.isFocused = hasFocus;
            }
        }


        [HarmonyPatch(typeof(UI_CashCounterScreen), "UpdateMoneyChangeAmount")]
        public class UI_CashCounterScreen_Patch
        {
            static void Postfix(UI_CashCounterScreen __instance)
            {
                if (!TestMod.ForceMath) return;
                TextMeshProUGUI text = __instance.m_ChangeToGiveAmountText;

                if (text != null)
                {
                    text.text = "DO THE MATH";
                }
            }
        }

        [HarmonyPatch(typeof(InteractableCustomerCash), "SetIsCard")]
        public static class SetIsCardPatch
        {
            public static void Prefix(ref bool isCard)
            {
                if (TestMod.ForceUseCash)
                {
                    isCard = false;
                    return;
                }
                if (TestMod.ForceUseCredit)
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

                if (TestMod.ForceUseCash)
                {
                    ___m_IsUsingCard = false;
                }

                if (TestMod.ForceUseCredit)
                {
                    ___m_IsUsingCard = true;
                }

            }
        }

        [HarmonyPatch(typeof(Customer), "EvaluateFinishScanItem")]
        public static class LargeBillsPatch
        {
            public static void Postfix(ref InteractableCustomerCash ___m_CustomerCash)
            {
                if (TestMod.LargeBills && ___m_CustomerCash.m_IsCard == false)//only trigger on cash effects, 
                {
                    float size = UnityEngine.Random.Range(26.0f, 42.0f);//match cash to cards, to make it more noticeable. 
                    ___m_CustomerCash.m_CashModel.transform.localScale = new Vector3(size, size, size);
                    ___m_CustomerCash.m_CashOutlineModel.transform.localScale = new Vector3(size, size, size);
                }
                else if (TestMod.LargeBills && ___m_CustomerCash.m_IsCard == true)//Trigger on Card Payments too
                {
                    float size = Random.Range(26.0f, 42.0f);//make cards more noticeable
                    ___m_CustomerCash.m_CardModel.transform.localScale = new Vector3(size, size, size);
                    ___m_CustomerCash.m_CardOutlineModel.transform.localScale = new Vector3(size, size, size);
                }
                else if (!TestMod.LargeBills)//revert to default sizes, can be grabbed via localscale.x,y,z and printing to log
                {
                    ___m_CustomerCash.m_CardModel.transform.localScale = TestMod.oldCardScale;
                    ___m_CustomerCash.m_CardOutlineModel.transform.localScale = TestMod.oldCardScaleOutline;
                    ___m_CustomerCash.m_CashModel.transform.localScale = TestMod.oldcashScale;
                    ___m_CustomerCash.m_CashOutlineModel.transform.localScale = TestMod.oldcashScaleOutline;
                }
            }
        }

        [HarmonyPatch(typeof(CardOpeningSequence))]
        [HarmonyPatch("Update")]
        class Patch_CardOpeningSequence_Update
        {
            static void Postfix(CardOpeningSequence __instance)
            {
                if (!autoOpenCards) return;
                var autoFireField = typeof(CardOpeningSequence).GetField("m_IsAutoFire", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                autoFireField.SetValue(__instance, true);

                var autoFireKeydown = typeof(CardOpeningSequence).GetField("m_IsAutoFireKeydown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                autoFireKeydown.SetValue(__instance, true);

                //mls.LogInfo($"{autoFireField} {autoFireKeydown}");
            }
        }


        [HarmonyPatch(typeof(CustomerManager), "GetCustomerExactChangeChance")]
        public static class HarmonyPatch_CustomerManager_GetCustomerExactChangeChance
        {
            private static bool Prefix(ref int __result)
            {
                if (TestMod.ExactChange)
                {
                    __result = 100;
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(Customer), "GetRandomPayAmount")]
        public static class HarmonyPatch_Customer_GetRandomPayAmount
        {
            private static bool Prefix(float limit, ref float __result)
            {
                if (TestMod.ExactChange)
                {
                    __result = limit;
                    return false;
                }
                //if (TestMod.CustomersOverpay)
                //{
                    //float badLuck = Random.Range(0.0f, 1.0f);
                   // float randomChange = Random.Range(0.00f, 0.99f);

                   // if (badLuck < 0.1f)
                   // {
                       // __result = __result + Random.Range(1, 100) + 1000 + randomChange;

                  //  }
                   // else
                   // {
                       // __result = __result + Random.Range(1, 100) + randomChange;
                  //  }

                    //return false;

                //}
                return true;
            }
        }
    }

}
