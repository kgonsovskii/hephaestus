﻿using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AxMSTSCLib;
using MSTSCLib;

namespace SharpRDP
{
    public class Client
    {
        private const int WaitChop = 1000;

        private void Wait(double chops)
        {
            Task.Delay((int)(WaitChop * chops)).GetAwaiter().GetResult();
        }
        
        private Dictionary<string, Code> keycode;
        private IMsRdpClientNonScriptable keydata;
        private int LogonErrorCode { get; set; }
        private int DisconnectCode { get; set; }
        private string cmd;
        private string execwith;
        private string target;
        private string runtype;
        private bool isdrive;
        private bool takeover;
        private bool networkauth;
        private enum LogonErrors : uint
        {
            ARBITRATION_CODE_BUMP_OPTIONS = 0xFFFFFFFB,
            ARBITRATION_CODE_CONTINUE_LOGON = 0xFFFFFFFE,
            ARBITRATION_CODE_CONTINUE_TERMINATE = 0xFFFFFFFD,
            ARBITRATION_CODE_NOPERM_DIALOG = 0xFFFFFFFA,
            ARBITRATION_CODE_REFUSED_DIALOG = 0xFFFFFFF9,
            ARBITRATION_CODE_RECONN_OPTIONS = 0xFFFFFFFC,
            ERROR_CODE_ACCESS_DENIED = 0xFFFFFFFF,
            LOGON_FAILED_BAD_PASSWORD = 0x0,
            LOGON_FAILED_OTHER = 0x2,
            LOGON_FAILED_UPDATE_PASSWORD = 0x1,
            LOGON_WARNING = 0x3,
            STATUS_ACCOUNT_RESTRICTION = 0xC000006E,
            STATUS_LOGON_FAILURE = 0xC000006D,
            STATUS_PASSWORD_MUST_CHANGE = 0xC0000224
        }
        private enum DisconnectReasons : uint
        {
            disconnectReasonAtClientWinsockFDCLOSE = 0x904,
            disconnectReasonByServer = 0x3,
            disconnectReasonClientDecompressionError = 0xC08,
            disconnectReasonConnectionTimedOut = 0x108,
            disconnectReasonDecryptionError = 0xC06,
            disconnectReasonDNSLookupFailed = 0x104,
            disconnectReasonDNSLookupFailed2 = 0x508,
            disconnectReasonEncryptionError = 0xB06,
            disconnectReasonGetHostByNameFailed = 0x604,
            disconnectReasonHostNotFound = 0x208,
            disconnectReasonInternalError = 0x408,
            disconnectReasonInternalSecurityError = 0x906,
            disconnectReasonInternalSecurityError2 = 0xA06,
            disconnectReasonInvalidEncryption = 0x506,
            disconnectReasonInvalidIP = 0x804,
            disconnectReasonInvalidServerSecurityInfo = 0x606,
            disconnectReasonInvalidSecurityData = 0x406,
            disconnectReasonInvalidIPAddr = 0x308,
            disconnectReasonLicensingFailed = 0x808,
            disconnectReasonLicensingTimeout = 0x908,
            disconnectReasonLocalNotError = 0x1,
            disconnectReasonNoInfo = 0x0,
            disconnectReasonOutOfMemory = 0x106,
            disconnectReasonOutOfMemory2 = 0x206,
            disconnectReasonOutOfMemory3 = 0x306,
            disconnectReasonRemoteByUser = 0x2,
            disconnectReasonServerCertificateUnpackErr = 0x706,
            disconnectReasonSocketConnectFailed = 0x204,
            disconnectReasonSocketRecvFailed = 0x404,
            disconnectReasonTimeoutOccurred = 0x704,
            disconnectReasonTimerError = 0x608,
            disconnectReasonWinsockSendFailed = 0x304,
            SSL_ERR_ACCOUNT_DISABLED = 0xB07,
            SSL_ERR_ACCOUNT_EXPIRED = 0xE07,
            SSL_ERR_ACCOUNT_LOCKED_OUT = 0xD07,
            SSL_ERR_ACCOUNT_RESTRICTION = 0xC07,
            SSL_ERR_CERT_EXPIRED = 0x1B07,
            SSL_ERR_DELEGATION_POLICY = 0x1607,
            SSL_ERR_FRESH_CRED_REQUIRED_BY_SERVER = 0x2107,
            SSL_ERR_LOGON_FAILURE = 0x807,
            SSL_ERR_NO_AUTHENTICATING_AUTHORITY = 0x1807,
            SSL_ERR_NO_SUCH_USER = 0xA07,
            SSL_ERR_PASSWORD_EXPIRED = 0xF07,
            SSL_ERR_PASSWORD_MUST_CHANGE = 0x1207,
            SSL_ERR_POLICY_NTLM_ONLY = 0x1707,
            SSL_ERR_SMARTCARD_CARD_BLOCKED = 0x2207,
            SSL_ERR_SMARTCARD_WRONG_PIN = 0x1C07
        }

        public void CreateRdpConnection(string server, string user, string domain, string password, string command, string execw, string runelevated, bool condrive, bool tover, bool nla)
        {
            keycode = new Dictionary<String, Code>();
            KeyCodes();
            runtype = runelevated;
            isdrive = condrive;
            cmd = command;
            target = server;
            execwith = execw;
            takeover = tover;
            networkauth = nla;

            void ProcessTaskThread()
            {
                var form = new Form();
                form.Opacity = 0;
                form.Visible = false;
                form.WindowState = FormWindowState.Minimized;
                form.ShowInTaskbar = false;
                form.FormBorderStyle = FormBorderStyle.None;
                form.Width = Screen.PrimaryScreen.WorkingArea.Width;
                form.Height = Screen.PrimaryScreen.WorkingArea.Height;
                form.Load += (sender, args) =>
                {
                    var rdpConnection = new AxMsRdpClient9NotSafeForScripting();
                    form.Controls.Add(rdpConnection);
                    var rdpC = rdpConnection.GetOcx() as IMsRdpClientNonScriptable5;
                    IMsRdpExtendedSettings rdpc2 = rdpConnection.GetOcx() as IMsRdpExtendedSettings;
                    rdpC.AllowPromptingForCredentials = false;
                    rdpC.AllowCredentialSaving = false;
                    rdpConnection.Server = server;
                    rdpConnection.Domain = domain;
                    rdpConnection.UserName = user;
                    rdpConnection.AdvancedSettings9.allowBackgroundInput = 1;
                    rdpConnection.AdvancedSettings9.BitmapPersistence = 0;
                    if(condrive == true)
                    {
                        rdpConnection.AdvancedSettings5.RedirectDrives = true;
                    }
                    if (password != string.Empty || user != string.Empty)
                    {
                        rdpConnection.UserName = user;
                        rdpConnection.AdvancedSettings9.ClearTextPassword = password;
                    }
                    else
                    {
                        rdpc2.set_Property("RestrictedLogon", true);
                        rdpc2.set_Property("DisableCredentialsDelegation", true);
                    }
                    rdpConnection.AdvancedSettings9.EnableCredSspSupport = true;
                    if(networkauth == true)
                    {
                        rdpC.NegotiateSecurityLayer = true;
                    }
                    if (true)
                    {
                        rdpConnection.OnDisconnected += RdpConnectionOnOnDisconnected;
                        rdpConnection.OnLoginComplete += RdpConnectionOnOnLoginComplete;
                        rdpConnection.OnLogonError += RdpConnectionOnOnLogonError;
                    }
                    rdpConnection.Connect();
                    rdpConnection.Enabled = false;
                    rdpConnection.Dock = DockStyle.Fill;
                    Application.Run(form);
                };
                form.Show();
            }

            var rdpClientThread = new Thread(ProcessTaskThread) { IsBackground = true };
            rdpClientThread.SetApartmentState(ApartmentState.STA);
            rdpClientThread.Start();
            while (rdpClientThread.IsAlive)
            {
                Task.Delay(500).GetAwaiter().GetResult();
            }
        }

        private void RdpConnectionOnOnLogonError(object sender, IMsTscAxEvents_OnLogonErrorEvent e)
        {
            LogonErrorCode = e.lError;
            var errorstatus = Enum.GetName(typeof(LogonErrors), (uint)LogonErrorCode);
            Program.Log("[-] Logon Error           :  {0} - {1}", LogonErrorCode, errorstatus);
            Wait(1);

            if(LogonErrorCode == -5 && takeover == true)
            {
                // it doesn't go to the logon event, so this has to be done here
                var rdpSession = (AxMsRdpClient9NotSafeForScripting)sender;
                Wait(1);
                keydata = (IMsRdpClientNonScriptable)rdpSession.GetOcx();
                Program.Log("[+] Another user is logged on, asking to take over session");
                SendElement("Tab");
                Wait(1);
                SendElement("Enter+down");
                Wait(1);
                SendElement("Enter+up");
                Wait(1);
                Program.Log("[+] Sleeping for 30 seconds");
                Wait(31);
                Marshal.ReleaseComObject(rdpSession);
                Marshal.ReleaseComObject(keydata);
            }
            else if (LogonErrorCode != -2)
            {
                Environment.Exit(0);
            }
        }

        private void RdpConnectionOnOnLoginComplete(object sender, EventArgs e)
        {
            var rdpSession = (AxMsRdpClient9NotSafeForScripting)sender;
            Program.Log("[+] Connected to          :  {0}", target);
            Wait(1);
            keydata = (IMsRdpClientNonScriptable)rdpSession.GetOcx();

            if (LogonErrorCode == -2)
            {
                Program.Log("[+] User not currently logged in, creating new session");
                Wait(10);
            }

            string privinfo = "non-elevated";
            if (runtype != string.Empty)
            {
                privinfo = "elevated";
            }

            Program.Log("[+] Execution priv type   :  {0}", privinfo);
            Wait(1);

            try
            {


                SendElement("Win+R+down");
                Wait(1);
                SendElement("Win+R+up");
                Wait(1);


                if (execwith == "cmd")
                {
                    RunConsole("cmd.exe");
                }
                else if (execwith == "powershell" || execwith == "ps")
                {
                    RunConsole("powershell.exe");
                }
                else
                {
                    RunRun();
                }
            }
            catch (Exception exception)
            {
                Program.completed = true;
                Program.error = true;
                Program.Report();
                Program.Log("HANLDED: " + exception.Message);
                try
                {
                    rdpSession.Disconnect();
                }
                catch (Exception e1)
                {
                }
                return;
            }
            Program.Log("Finish keys");
            Thread.Sleep(1000);


            if (!Program.IsLocal())
            {
                Program.Log("[+] Disconnecting from    :  {0}", target);
                rdpSession.Disconnect();
            }
            else
            {
                Program.Report();
                Program.Log("[+] No disconnecting    :  {0}", target);
            }
        }

        private void RdpConnectionOnOnDisconnected(object sender, IMsTscAxEvents_OnDisconnectedEvent e)
        {
            DisconnectCode = e.discReason;
            var dire = Enum.GetName(typeof(DisconnectReasons), (uint)DisconnectCode);
            Program.Log("[+] Connection closed     :  {0}", target);
            if (e.discReason != 1)
            {
                Program.Log("[-] Disconnection Reason  :  {0} - {1}", DisconnectCode, dire);
            }

            Program.completed = true;
            Program.Report();
            Environment.Exit(0);
        }

        private void RunRun()
        {
            if(runtype == "taskmgr")
            {
                Program.Log("[+] Running task manager");
                Wait(1);
                SendText("taskmgr");
                Wait(1);

                Wait(1);
                SendElement("Enter+down");
                Wait(1);
                SendElement("Enter+up");

                SendElement("Alt+F");
                Wait(1);

                SendElement("Enter+down");
                Wait(1);
                SendElement("Enter+up");
                Wait(1);
            }

            Program.Log("[+] Executing {0}", cmd.ToLower());
            SendText(cmd.ToLower());
            Wait(1);

            if (runtype == "taskmgr")
            {
                SendElement("Tab");
                Wait(1);
                SendElement("Space");
                Wait(1);
            }

            if(runtype == "winr")
            {
                //Currently bugged - does not run elevated
                SendElement("Ctrl+Shift+down");
                Wait(1);
                SendElement("Enter+down");
                Wait(1);
                SendElement("Enter+up");
                Wait(1);
                SendElement("Ctrl+Shift+up");
                Wait(1);
            }
            else
            {
                SendElement("Enter+down");
                Wait(1);
                SendElement("Enter+up");
                Wait(1);
            }

            if (isdrive == true)
            {
                SendElement("Left");
                Wait(1);
                SendElement("Enter+down");
                Wait(1);
                SendElement("Enter+up");
            }
            
            if (runtype == "winr")
            {
                SendElement("Left");
                Wait(1);
                SendElement("Enter+down");
                Wait(1);
                SendElement("Enter+up");
            }
            if (runtype == "taskmgr")
            {
                Wait(1);
                SendElement("Alt+F4");
            }
        }

        private void RunConsole(string consoletype)
        {
            if (runtype == "taskmgr")
            {
                Program.Log("[+] Executing task manager");
                Wait(1);
                SendText("taskmgr");
                Wait(3);

                Wait(1);
                SendElement("Enter+down");
                Wait(1);
                SendElement("Enter+up");

                SendElement("Alt+F");
                Wait(1);

                SendElement("Enter+down");
                Wait(1);
                SendElement("Enter+up");
                Wait(1);
            }

            Program.Log("[+] Executing {0} from {1}", cmd.ToLower(), consoletype);
            SendText(consoletype);
            Wait(1);

            if (runtype == "taskmgr")
            {
                SendElement("Tab");
                Wait(1);
                SendElement("Space");
                Wait(1);
            }

            if (runtype == "winr")
            {
                //Currently bugged - does not run elevated
                SendElement("Ctrl+Shift+down");
                Wait(1);
                SendElement("Enter+down");
                Wait(1);
                SendElement("Enter+up");
                Wait(1);
                SendElement("Ctrl+Shift+up");
                Wait(1);
            }
            else
            {
                SendElement("Enter+down");
                Wait(1);
                SendElement("Enter+up");
                Wait(1);
            }

            Wait(1);
            SendText(cmd.ToLower());

            Wait(1);

            SendElement("Enter+down");
            Wait(1);
            SendElement("Enter+up");

            Wait(1);
            SendText("exit");

            SendElement("Enter+down");
            Thread.Sleep(500);
            SendElement("Enter+up");

            if(runtype == "taskmgr")
            {
                Wait(1);
                SendElement("Alt+F4");
                Wait(1);
            }
        }

        public void SendText(String text)
        {
            foreach (var t in text)
            {
                var symbol = t.ToString();
                keydata.SendKeys(keycode[symbol].length, ref keycode[symbol].bools[0], ref keycode[symbol].ints[0]);
                Wait(0.05);
            }
        }

        private void SendElement(String curchars)
        {
            var current = keycode[curchars];
            keydata.SendKeys(current.length, ref current.bools[0], ref current.ints[0]);
            Wait(0.05);
        }

        private void KeyCodes()
        {
            keycode["Esc"] = new Code(new[] { false, true }, new[] { 0x01 });
            keycode["Enter+down"] = new Code(new[] { false }, new[] { 0x1c });
            keycode["Enter+up"] = new Code(new[] { true }, new[] { 0x1c });
            keycode["Win"] = new Code(new[] { false, true }, new[] { 0x15b });
            keycode["Down"] = new Code(new[] { false, true }, new[] { 0x150 });
            keycode["Right"] = new Code(new[] { false, true }, new[] { 0x14d });
            keycode["Left"] = new Code(new[] { false, true }, new[] { 0x14b });
            keycode["Alt"] = new Code(new[] { false, true }, new[] { 0x38 });
            keycode["Shift"] = new Code(new[] { false, true }, new[] { 0x2a });
            keycode["Space"] = new Code(new[] { false, true }, new[] { 0x39 });
            keycode["Tab"] = new Code(new[] { false, true }, new[] { 0x0f });

            keycode["Calc"] = new Code(new[] { false, true }, new[] { 0x121, 0x121 });
            keycode["Paste"] = new Code(new[] { false, true }, new[] { 0x10a, 0x10a });

            keycode["1"] = new Code(new[] { false, true }, new[] { 0x02 });
            keycode["2"] = new Code(new[] { false, true }, new[] { 0x03 });
            keycode["3"] = new Code(new[] { false, true }, new[] { 0x04 });
            keycode["4"] = new Code(new[] { false, true }, new[] { 0x05 });
            keycode["5"] = new Code(new[] { false, true }, new[] { 0x06 });
            keycode["6"] = new Code(new[] { false, true }, new[] { 0x07 });
            keycode["7"] = new Code(new[] { false, true }, new[] { 0x08 });
            keycode["8"] = new Code(new[] { false, true }, new[] { 0x09 });
            keycode["9"] = new Code(new[] { false, true }, new[] { 0x0a });
            keycode["0"] = new Code(new[] { false, true }, new[] { 0x0b });
            keycode["-"] = new Code(new[] { false, true }, new[] { 0x0c });

            keycode["a"] = new Code(new[] { false, true }, new[] { 0x1e });
            keycode["b"] = new Code(new[] { false, true }, new[] { 0x30 });
            keycode["c"] = new Code(new[] { false, true }, new[] { 0x2e });
            keycode["d"] = new Code(new[] { false, true }, new[] { 0x20 });
            keycode["e"] = new Code(new[] { false, true }, new[] { 0x12 });
            keycode["f"] = new Code(new[] { false, true }, new[] { 0x21 });
            keycode["g"] = new Code(new[] { false, true }, new[] { 0x22 });
            keycode["h"] = new Code(new[] { false, true }, new[] { 0x23 });
            keycode["i"] = new Code(new[] { false, true }, new[] { 0x17 });
            keycode["j"] = new Code(new[] { false, true }, new[] { 0x24 });
            keycode["k"] = new Code(new[] { false, true }, new[] { 0x25 });
            keycode["l"] = new Code(new[] { false, true }, new[] { 0x26 });
            keycode["m"] = new Code(new[] { false, true }, new[] { 0x32 });
            keycode["n"] = new Code(new[] { false, true }, new[] { 0x31 });
            keycode["o"] = new Code(new[] { false, true }, new[] { 0x18 });
            keycode["p"] = new Code(new[] { false, true }, new[] { 0x19 });
            keycode["q"] = new Code(new[] { false, true }, new[] { 0x10 });
            keycode["r"] = new Code(new[] { false, true }, new[] { 0x13 });
            keycode["s"] = new Code(new[] { false, true }, new[] { 0x1f });
            keycode["t"] = new Code(new[] { false, true }, new[] { 0x14 });
            keycode["u"] = new Code(new[] { false, true }, new[] { 0x16 });
            keycode["v"] = new Code(new[] { false, true }, new[] { 0x2f });
            keycode["w"] = new Code(new[] { false, true }, new[] { 0x11 });
            keycode["x"] = new Code(new[] { false, true }, new[] { 0x2d });
            keycode["y"] = new Code(new[] { false, true }, new[] { 0x15 });
            keycode["z"] = new Code(new[] { false, true }, new[] { 0x2c });
            keycode[" "] = new Code(new[] { false, true }, new[] { 0x39 });

            keycode[","] = new Code(new[] { false, true }, new[] { 0x33 });
            keycode["."] = new Code(new[] { false, true }, new[] { 0x34 });
            keycode["/"] = new Code(new[] { false, true }, new[] { 0x35 });
            keycode["["] = new Code(new[] { false, true }, new[] { 0x1a });
            keycode["]"] = new Code(new[] { false, true }, new[] { 0x1b });
            keycode["\\"] = new Code(new[] { false, true }, new[] { 0x2b });
            keycode[";"] = new Code(new[] { false, true }, new[] { 0x27 });
            keycode["'"] = new Code(new[] { false, true }, new[] { 0x28 });

            keycode["\""] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x28 });
            keycode[":"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x27 });
            keycode["|"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x2b });
            keycode["&"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x08 });
            keycode["%"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x06 });
            keycode["("] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x0a });
            keycode[")"] = new Code(new[] { false, false, true, true }, new[] { 0x2a, 0x0b });

            keycode["Win+R+down"] = new Code(new[] { false, false }, new[] { 0x15b, 0x13 });
            keycode["Win+R+up"] = new Code(new[] { true, true }, new[] { 0x15b, 0x13 });
            keycode["Win+D"] = new Code(new[] { false, false, true, true }, new[] { 0x15b, 0x20 });
            keycode["Alt+Shift"] = new Code(new[] { false, false, true, true }, new[] { 0x38, 0x2a });
            keycode["Alt+Space"] = new Code(new[] { false, false, true, true }, new[] { 0x38, 0x39 });
            keycode["Ctrl+Shift"] = new Code(new[] { false, false, true, true }, new[] { 0x1d, 0x2a });
            keycode["Alt+F4"] = new Code(new[] { false, false, true, true }, new[] { 0x38, 0x3e });
            keycode["Ctrl+V"] = new Code(new[] { false, false, true, true }, new[] { 0x1d, 0x2f });
            keycode["Alt+F"] = new Code(new[] { false, false, true, true }, new[] { 0x38, 0x21 });

            keycode["Ctrl+Shift+down"] = new Code(new[] { false, false }, new[] { 0x1d, 0x2a });
            keycode["Ctrl+Shift+up"] = new Code(new[] { true, true }, new[] { 0x1d, 0x2a });
        }
    }
}
