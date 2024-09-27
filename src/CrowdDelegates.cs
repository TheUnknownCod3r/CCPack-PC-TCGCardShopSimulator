
using DG.Tweening;
using DG.Tweening.Core.Easing;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using TMPro;
using UnityEngine;


namespace BepinControl
{
    public delegate CrowdResponse CrowdDelegate(ControlClient client, CrowdRequest req);



    public class CrowdDelegates
    {
        public static System.Random rnd = new System.Random();
        public static int maxBoxCount = 100;

        public static CrowdResponse ToggleLights(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    LightManager.Instance.ToggleShopLight();
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse SpawnCustomer(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            LightManager LM = CSingleton<LightManager>.Instance;
            CustomerManager CM = CustomerManager.Instance;
            bool hasDayEnded = (bool)getProperty(LM, "m_HasDayEnded");
            if (!CPlayerData.m_IsShopOnceOpen || hasDayEnded) return new CrowdResponse(id: req.GetReqID(), status: CrowdResponse.Status.STATUS_RETRY, message: "Store is Closed");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    CM.GetNewCustomer();
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse SpawnCustomerSmelly(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            LightManager LM = CSingleton<LightManager>.Instance;
            CustomerManager CM = CustomerManager.Instance;
            bool hasDayEnded = (bool)getProperty(LM, "m_HasDayEnded");
            if (!CPlayerData.m_IsShopOnceOpen || hasDayEnded) return new CrowdResponse(id: req.GetReqID(), status: CrowdResponse.Status.STATUS_RETRY, message: "Store is Closed");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    Customer Smelly = CM.GetNewCustomer();
                    if (Smelly != null) Smelly.SetSmelly();
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }

        public static CrowdResponse AllSmellyCustomers(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            List<Customer> customers = (List<Customer>)getProperty(CSingleton<CustomerManager>.Instance, "m_CustomerList");
            CustomerManager customerManager = CSingleton<CustomerManager>.Instance;
            TestMod.mls.LogInfo($"Customers?");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {

                    if (customers == null)
                    {
                        TestMod.mls.LogInfo("Customer list not found.");
                        return;
                    }

                    // Loop through the customer list and add each customer to the smelly customer list
                    foreach (Customer customer in customers)
                    {
                        if (customer.isActiveAndEnabled)
                        {
                            TestMod.mls.LogInfo($"Customer?" + customer.name);
                            customerManager.AddToSmellyCustomerList(customer);
                            customer.SetSmelly();
                        }
                    }

                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse TeleportPlayer(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;

            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {


                    Transform pos = CSingleton<InteractionPlayerController>.Instance.transform;
                    TestMod.mls.LogInfo($"Player POS: {pos.position}");
                    Vector3 teleportPosition = new Vector3();

                    teleportPosition = new Vector3(15.00f, 0.06f, -1.46f);
                    CSingleton<InteractionPlayerController>.Instance.m_WalkerCtrl.transform.position = teleportPosition;


                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse ForceMath(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;


            if (TimedThread.isRunning(TimedType.FORCE_MATH)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            UI_CashCounterScreen ui_CashCounterScreen = CSingleton<UI_CashCounterScreen>.Instance;
            TextMeshProUGUI text = (TextMeshProUGUI)getProperty(ui_CashCounterScreen, "m_ChangeToGiveAmountText");
            text.text = "DO THE MATH";

            new Thread(new TimedThread(req.GetReqID(), TimedType.FORCE_MATH, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }
        public static CrowdResponse ForcePaymentType(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;


            if (TimedThread.isRunning(TimedType.FORCE_CASH)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            if (TimedThread.isRunning(TimedType.FORCE_CARD)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            if (!CPlayerData.m_IsShopOnceOpen) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            InteractionPlayerController player = CSingleton<InteractionPlayerController>.Instance;
            if (player.m_CurrentGameState != EGameState.CashCounterState) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");//Better state check, still runs if the player leaves the checkout, but only starts if there
            
            List<Customer> cust = (List<Customer>)getProperty(CSingleton<CustomerManager>.Instance, "m_CustomerList");
            foreach(Customer c in cust)
            {
                if(c.m_CustomerCash.gameObject.activeSelf == true)
                {
                    if(TestMod.ForceUseCredit) c.m_CustomerCash.m_IsCard = true;
                    if (TestMod.ForceUseCash) c.m_CustomerCash.m_IsCard = false;
                }
            }
            string paymentType = req.code.Split('_')[1];

            if (paymentType == "cash") new Thread(new TimedThread(req.GetReqID(), TimedType.FORCE_CASH, dur * 1000).Run).Start();
            if (paymentType == "card") new Thread(new TimedThread(req.GetReqID(), TimedType.FORCE_CARD, dur * 1000).Run).Start();

            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse InvertX(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;
            TestMod.mls.LogInfo($"running");

            if (TimedThread.isRunning(TimedType.INVERT_X)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            new Thread(new TimedThread(req.GetReqID(), TimedType.INVERT_X, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }
        public static CrowdResponse SetLanguage(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;

            SettingScreen SS = CSingleton<SettingScreen>.Instance;
            string currentLanguage = LocalizationManager.CurrentLanguage;

            string language = req.code.Split('_')[1];
            string newLanguage = "";
            switch (language)
            {
                case "english":
                    {
                        newLanguage = "English";
                        break;
                    }
                case "french":
                    {
                        newLanguage = "France";
                        break;
                    }
                case "german":
                    {
                        newLanguage = "Germany";
                        break;
                    }
                case "italian":
                    {
                        newLanguage = "Italian";
                        break;
                    }
                case "spanish":
                    {
                        newLanguage = "Spanish";
                        break;
                    }
                case "portuguese":
                    {
                        newLanguage = "Portuguese";
                        break;
                    }
                case "chineset":
                    {
                        newLanguage = "ChineseT";
                        break;
                    }
                case "chineses":
                    {
                        newLanguage = "ChineseS";
                        break;
                    }
                case "korean":
                    {
                        newLanguage = "Korean";
                        break;
                    }
                case "japanese":
                    {
                        newLanguage = "Japanese";
                        break;
                    }
                case "russian":
                    {
                        newLanguage = "Russian";
                        break;
                    }
                case "hindi":
                    {
                        newLanguage = "Hindi";
                        break;
                    }
                case "thai":
                    {
                        newLanguage = "Thai";
                        break;
                    }
                case "arabic":
                    {
                        newLanguage = "Arabic";
                        break;
                    }
                case "dutch":
                    {
                        newLanguage = "Dutch";
                        break;
                    }
            };


            if (currentLanguage == newLanguage) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "");
            if (TimedThread.isRunning(TimedType.SET_LANGUAGE)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            TestMod.NewLanguage = newLanguage;
            TestMod.OrgLanguage = currentLanguage;

            new Thread(new TimedThread(req.GetReqID(), TimedType.SET_LANGUAGE, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);

        }
        public static CrowdResponse InvertY(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;

            if (TimedThread.isRunning(TimedType.INVERT_Y)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            new Thread(new TimedThread(req.GetReqID(), TimedType.INVERT_Y, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse HighFOV(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;


            if (TimedThread.isRunning(TimedType.HIGH_FOV)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            if (TimedThread.isRunning(TimedType.LOW_FOV)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            new Thread(new TimedThread(req.GetReqID(), TimedType.HIGH_FOV, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse LowFOV(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if (req.duration > 0) dur = req.duration / 1000;

            if (TimedThread.isRunning(TimedType.HIGH_FOV)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");
            if (TimedThread.isRunning(TimedType.LOW_FOV)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");

            new Thread(new TimedThread(req.GetReqID(), TimedType.LOW_FOV, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }

        public static CrowdResponse WorkersSpeedy(ControlClient client, CrowdRequest req)
        {
            int dur = 30;
            if(req.duration > 0) dur = req.duration / 1000;

            if (TimedThread.isRunning(TimedType.WORKERS_FAST)) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "");


            new Thread(new TimedThread(req.GetReqID(), TimedType.WORKERS_FAST, dur * 1000).Run).Start();
            return new TimedResponse(req.GetReqID(), dur * 1000, CrowdResponse.Status.STATUS_SUCCESS);
        }
        public static CrowdResponse GiveMoney(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            int amount = 0;
            string[] enteredText = req.code.Split('_');
            try
            {
                amount = int.Parse(enteredText[1]);
            }
            catch
            {
                return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "WHERES THE MONEY");

            }
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    CSingleton<GameUIScreen>.Instance.AddCoin(amount, true);
                });

            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse TakeMoney(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            int amount = 0;
            string[] enteredText = req.code.Split('_');
            try
            {
                amount = int.Parse(enteredText[1]);
            }
            catch
            {
                return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "WHERES THE MONEY");

            }
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    CSingleton<GameUIScreen>.Instance.ReduceCoin(amount, true);
                });

            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse ShopControls(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            string[] enteredText = req.code.Split('_');
            try
            {
                if (enteredText[0] == "close" && CPlayerData.m_IsShopOpen == true)
                {
                    TestMod.ActionQueue.Enqueue(() =>
                    {
                        CPlayerData.m_IsShopOpen = false;
                        InteractableOpenCloseSign.FindFirstObjectByType<InteractableOpenCloseSign>().m_CloseShopMesh.SetActive(true);
                        InteractableOpenCloseSign.FindFirstObjectByType<InteractableOpenCloseSign>().m_OpenShopMesh.SetActive(false);
                        CPlayerData.m_IsShopOnceOpen = false;
                    });
                }
                else if (enteredText[0] == "open" && CPlayerData.m_IsShopOpen == false)
                {
                    TestMod.ActionQueue.Enqueue(() =>
                    {
                        CPlayerData.m_IsShopOpen = true;
                        InteractableOpenCloseSign.FindFirstObjectByType<InteractableOpenCloseSign>().m_CloseShopMesh.SetActive(false);
                        InteractableOpenCloseSign.FindFirstObjectByType<InteractableOpenCloseSign>().m_OpenShopMesh.SetActive(true);
                        CPlayerData.m_IsShopOnceOpen = true;
                    });
                }
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse UpgradeWarehouse(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            if (CPlayerData.m_UnlockWarehouseRoomCount == 8 || CPlayerData.m_IsWarehouseRoomUnlocked == false) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Storage Is Unlocked");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    UnlockRoomManager.Instance.StartUnlockNextWarehouseRoom();
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_FAILURE;
            }
            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse UpgradeStore(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            if (CPlayerData.m_UnlockRoomCount == 20) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Storage Is Unlocked");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    UnlockRoomManager.Instance.StartUnlockNextRoom();
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_FAILURE;
            }
            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse UnlockWarehouse(ControlClient client, CrowdRequest req)
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";

            if (CPlayerData.m_IsWarehouseRoomUnlocked == true) return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_RETRY, "Storage Is Unlocked");
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    TestMod.isWarehouseUnlocked = true;
                    UnlockRoomManager.Instance.SetUnlockWarehouseRoom(true);
                });
            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_FAILURE;
            }
            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static CrowdResponse GiveItem(ControlClient client, CrowdRequest req) //https://pastebin.com/BVEACvGA item list
        {
            CrowdResponse.Status status = CrowdResponse.Status.STATUS_SUCCESS;
            string message = "";
            var item = "";
            RestockData item2 = null;
            string[] enteredText = req.code.Split('_');
            if(enteredText.Length > 0)
            try
            {
                    if (enteredText.Length == 5) item = string.Join(" ", enteredText[1], enteredText[2], enteredText[3], enteredText[4]);
                    else if (enteredText.Length == 4) item = string.Join(" ", enteredText[1], enteredText[2], enteredText[3]);//playmat, Plushie
                    else if (enteredText.Length == 3) item = string.Join(enteredText[1], enteredText[2]);//single items like Freshener
                    else item = enteredText[1];
                    item2 = CSingleton<InventoryBase>.Instance.m_StockItemData_SO.m_RestockDataList.Find(z => z.name.ToLower().Contains(item.ToLower()));//Item bools
            }
            catch
            {
                return new CrowdResponse(req.GetReqID(), CrowdResponse.Status.STATUS_FAILURE, "WHERES THE MONEY");

            }
            try
            {
                TestMod.ActionQueue.Enqueue(() =>
                {
                    if(item2.isBigBox) RestockManager.SpawnPackageBoxItem(item2.itemType, 64, item2.isBigBox);
                    else RestockManager.SpawnPackageBoxItem(item2.itemType, 32, item2.isBigBox);
                });

            }
            catch (Exception e)
            {
                TestMod.mls.LogInfo($"Crowd Control Error: {e.ToString()}");
                status = CrowdResponse.Status.STATUS_RETRY;
            }

            return new CrowdResponse(req.GetReqID(), status, message);
        }
        public static void setProperty(System.Object a, string prop, System.Object val)
        {
            var f = a.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (f == null)
            {
                TestMod.mls.LogInfo($"Field {prop} not found in {a.GetType()}");
                return;
            }

            f.SetValue(a, val);
        }

        public static System.Object getProperty(System.Object a, string prop)
        {
            var f = a.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (f == null)
            {
                TestMod.mls.LogInfo($"Field {prop} not found in {a.GetType()}");
                return null;
            }

            return f.GetValue(a);
        }

        public static void setSubProperty(System.Object a, string prop, string prop2, System.Object val)
        {
            var f = a.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic);
            var f2 = f.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic);
            f2.SetValue(f, val);
        }

        public static void callSubFunc(System.Object a, string prop, string func, System.Object val)
        {
            callSubFunc(a, prop, func, new object[] { val });
        }

        public static void callSubFunc(System.Object a, string prop, string func, System.Object[] vals)
        {
            var f = a.GetType().GetField(prop, BindingFlags.Instance | BindingFlags.NonPublic);


            var p = f.GetType().GetMethod(func, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
            p.Invoke(f, vals);

        }

        public static void callFunc(System.Object a, string func, System.Object val)
        {
            callFunc(a, func, new object[] { val });
        }

        public static void callFunc(System.Object a, string func, System.Object[] vals)
        {
            var p = a.GetType().GetMethod(func, BindingFlags.Instance | BindingFlags.NonPublic);
            p.Invoke(a, vals);

        }

        public static System.Object callAndReturnFunc(System.Object a, string func, System.Object val)
        {
            return callAndReturnFunc(a, func, new object[] { val });
        }

        public static System.Object callAndReturnFunc(System.Object a, string func, System.Object[] vals)
        {
            var p = a.GetType().GetMethod(func, BindingFlags.Instance | BindingFlags.NonPublic);
            return p.Invoke(a, vals);

        }

    }
}
