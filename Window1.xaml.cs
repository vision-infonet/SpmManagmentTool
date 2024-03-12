using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;

namespace SpmManagmentTool
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        #region variables
        private delegate void SetControlValueDelegate(string val);
        public delegate void SetButtonStyle(Button btn, Style style);
        internal SetButtonStyle _setButtonStyleDele;
        public delegate void SetButtonStatus(Button btn, bool isOnline);
        internal static SetButtonStatus _setBtnStatusDele;
        public delegate void LoadPromptDele(string spmid);
        internal LoadPromptDele _loadpromptdele;
        public delegate void LoadAllPromptsDele();
        internal LoadAllPromptsDele _loadallpromptsdele;
        public delegate void LoadAppsKeysdele(string spmid);
        internal LoadAppsKeysdele _loadAppsKeysdele;
        public delegate void LoadAllAppsKeysdele();
        internal LoadAllAppsKeysdele _loadAllAppsKeysdele;
        private SPMManagerTcpClient _spmmanagertcpclient;
        private Thread _runtcplcientthread;
        private CMDDictionary _cmddictionary;
        private string _currentcategory = string.Empty;
        private string _currentcmd = string.Empty;
        private string _currentspm = string.Empty;
        private Button[] _spmIDbtns, _menubtns;
        private const int PROMPTLOADINGTIMEOUT = 300000; //Timeout for load prompts; one spm usually takes 2-3 min to load prompts. we set it 10 min as timeout.
        internal static bool _ispromptloadsucess = false;
        internal static bool _isAppsKeysLoadSuccess = false;
        internal delegate void SetProgressbarDelegate(double value, SPMManagerTcpClient.ProgressBarSetOptions option);
        internal delegate void SetButtonBorder(Button btn, SolidColorBrush scb);
        internal static SetButtonBorder _setbuttonborderdele;
        private LogIn _mylogin;
        private bool _isLogin = false;
        private Style _btnStyle = new Style();
        private Style _btnHighLightStyle = new Style();
        private Style _btnLoadSuccessStyle = new Style();
        private Style _btnLoadFailStyle = new Style();
        private BitmapImage _imgOn, _imgOff;

        #endregion

        #region constructor
        public Window1()
        {
            InitializeComponent();
            InitializeMyComponent();
            _cmddictionary = new CMDDictionary();
            _cmddictionary.BuildDictionaries();
            _spmIDbtns = new Button[] { Spm1Btn, Spm2Btn, Spm3Btn, Spm4Btn, Spm5Btn, Spm6Btn, Spm7Btn, Spm8Btn, Spm9Btn,
                Spm10Btn, Spm11Btn, Spm12Btn, Spm13Btn, Spm14Btn, Spm15Btn, Spm16Btn, Spm17Btn, Spm18Btn, Spm19Btn, Spm20Btn, Spm21Btn, Spm22Btn, Spm23Btn, Spm24Btn };
            _menubtns = new Button[] {CoreBtn, CardReaderBtn, PrinterBtn, ScreenBtn, EmvModBtn, SecModBtn, KeypadBtn, ContactlessBtn, BarcodeReaderBtn};
            _loadpromptdele = LoadPrompt;
            _loadAppsKeysdele = LoadAppsKeys;
            _setButtonStyleDele = SetBtnStyle;
            _setBtnStatusDele = SetBtnStatus;
            _setbuttonborderdele = SetBtnBorder;
            _loadallpromptsdele = LoadAllPrompts;
            _loadAllAppsKeysdele = LoadAllAppsKeys;
            _runtcplcientthread = new Thread(new ThreadStart(IniSocketClient));
            _runtcplcientthread.Start();
            
            LoginActive(false);
        }
        #endregion

        #region methods
        private void InitializeMyComponent()
        {
            _btnStyle = (Style)Application.Current.FindResource("NormalButton");
            _btnHighLightStyle = (Style)Application.Current.FindResource("HightLightButton");
            _btnLoadSuccessStyle = (Style)Application.Current.FindResource("LoadSuccessButton");
            _btnLoadFailStyle = (Style)Application.Current.FindResource("LoadFailButton");
            _imgOn = new BitmapImage(new Uri(@"img\16_activate.gif", UriKind.Relative));
            _imgOff = new BitmapImage(new Uri(@"img\16_deactivate.gif", UriKind.Relative));
            lstboxCommands.SelectionChanged += new SelectionChangedEventHandler(lstboxCommands_SelectionChanged);
            if (System.IO.File.Exists(@"C:\NextGenFuel\OPTService\InfoNetLib\Files\EMVPublicKeys.txt")
                && System.IO.File.Exists(@"C:\NextGenFuel\OPTService\InfoNetLib\Files\EMVApplications.txt"))
            {
                lblNote.Foreground = Brushes.Green;
                lblNote.Content = "Files EMVApplications.txt and EMVPublicKeys.txt are exist under:"
                                + @"C:\NextGenFuel\OPTService\InfoNetLib\Files\";
            }
            else
            {
                lblNote.Foreground = Brushes.Red;
                lblNote.Content = "Loading EMV Applications and Public Keys needs two files,  EMVApplications.txt and EMVPublicKeys.txt,"
                                + " are currently missing.\n located under:" + @"C:\NextGenFuel\OPTService\InfoNetLib\Files\";
                ckbEnableLoadAppsKeysSingle.IsEnabled = false;
                ckbEnableLoadAppsKeysAll.IsEnabled = false;
            }
        }

        public void IniSocketClient()
        {
            _spmmanagertcpclient = new SPMManagerTcpClient(this.SetControlValue, DataReceiveTxt,
                                                            new Label[]{ connectionsatusLbl2, lblLoadStatus,lblLoadAppsKeysStatus},
                                                            _spmIDbtns, new ProgressBar[]{pgbLoadPrompt, pgbLoadAppsKeys});
            _spmmanagertcpclient.SetUpConnectionToOPT();
        }
        private void LoginActive(bool enable)
        {
            _isLogin = enable;
            SendBtn.IsEnabled = enable;
            ckbEnableLoadPromptAll.IsEnabled = enable;
            ckbEnableLoadPromptSingle.IsEnabled = enable;
            foreach (Button _btn in _menubtns)
                _btn.IsEnabled = enable;
            foreach (Button _btn in _spmIDbtns)
                _btn.IsEnabled = enable;
            //cmdlistCBbox.IsEnabled = enable;
            SetCmdTxt.IsEnabled = enable;
            DataReceiveTxt.IsEnabled = enable;
            tabGeneralCommands.IsEnabled = enable;
            tabAppPubKeyLoader.IsEnabled = enable;
            tabSPMPromptLoader.IsEnabled = enable;
            if (!enable)
                tabAllTabs.SelectedIndex = 0;
        }

        //this is generic function for most of control that has text display in it.
        public void SetControlValue(object obj, object[] myparams)
        {
            try
            {
                if ((Label)obj == connectionsatusLbl2)
                    connectionsatusLbl2.Dispatcher.Invoke(new SetControlValueDelegate(SetConnLableValue), myparams);
            }
            catch { }
            try
            {
                if ((TextBox)obj == DataReceiveTxt)
                    DataReceiveTxt.Dispatcher.Invoke(new SetControlValueDelegate(SetDataReceiveBox), myparams);
            }
            catch { }
            try
            {
                if ((ProgressBar)obj == pgbLoadPrompt)
                    pgbLoadPrompt.Dispatcher.Invoke(new SetProgressbarDelegate(SetPromptProgressBar), myparams);
            }
            catch { }
            try
            {
                if ((ProgressBar)obj == pgbLoadAppsKeys)
                    pgbLoadAppsKeys.Dispatcher.Invoke(new SetProgressbarDelegate(SetAppkKeysProgressBar), myparams);
            }
            catch { }
            try
            {
                if ((Label)obj == lblLoadStatus)
                    lblLoadStatus.Dispatcher.Invoke(new SetControlValueDelegate(SetLoadStatusLabelValue), myparams);
            }
            catch { }
            try
            {
                if ((Label)obj == lblLoadAppsKeysStatus)
                    lblLoadAppsKeysStatus.Dispatcher.Invoke(new SetControlValueDelegate(SetLoadAppsKeysStatusLabelValue), myparams);
            }
            catch { }
        }

        private void SetConnLableValue(string val)
        {
            connectionsatusLbl2.Content = val;
        }

        private void SetLoadStatusLabelValue(string val)
        {
            lblLoadStatus.Content = val;
        }

        private void SetLoadAppsKeysStatusLabelValue(string val)
        {
            lblLoadAppsKeysStatus.Content = val;
        }

        private void SetDataReceiveBox(string val)
        {

            DataReceiveTxt.Text += val + "\n";
            DataReceiveTxt.ScrollToEnd();
            //DataReceiveTxt.Text = val;
        }

        private void BuildCommandsListBox(Dictionary<string, string> _collection)
        {
            lstboxCommands.Items.Clear();
            foreach (KeyValuePair<string, string> kv in _collection)
            {
                lstboxCommands.Items.Add(kv.Key);
            }
        }

        private void SendBtn_Click(object sender, RoutedEventArgs e)
        {
            GetCurrentListBoxCmdFromDictionary();
            if (null != lstboxCommands.SelectedValue && lstboxCommands.SelectedValue.ToString().Length > 0 && _currentcmd.Length > 0)
            {
                if (_spmmanagertcpclient._availablespms.ContainsKey(_currentspm))
                    _spmmanagertcpclient.writeStream(_currentcmd + "{PARAMS}" + "SpmId=" + _currentspm + SetCmdTxt.Text.Replace("\r\n", string.Empty));
 
                else if(_currentspm == "")
                    SetDataReceiveBox("Please choose a spm unit from above buttons");
                else
                    SetDataReceiveBox("The Spm " + _currentspm + " is not online");
            }
        }

        private void GetCurrentListBoxCmdFromDictionary()
        {

            if (null != lstboxCommands.SelectedValue && lstboxCommands.SelectedValue.ToString().Length > 0)
            {
                switch (_currentcategory)
                {
                    case "core":
                        _currentcmd = CMDDictionary._spmdictionary_core[lstboxCommands.SelectedValue.ToString()];
                        break;
                    case "cardreader":
                        _currentcmd = CMDDictionary._spmdictionary_cardreader[lstboxCommands.SelectedValue.ToString()];
                        break;
                    case "printer":
                        _currentcmd = CMDDictionary._spmdictionary_printer[lstboxCommands.SelectedValue.ToString()];
                        break;
                    case "screen":
                        _currentcmd = CMDDictionary._spmdictionary_screen[lstboxCommands.SelectedValue.ToString()];
                        break;
                    case "emvmoduel":
                        _currentcmd = CMDDictionary._spmdictionary_emvmodule[lstboxCommands.SelectedValue.ToString()];
                        break;
                    case "securemoduel":
                        _currentcmd = CMDDictionary._spmdictionary_securitymodule[lstboxCommands.SelectedValue.ToString()];
                        break;
                    case "keypad":
                        _currentcmd = CMDDictionary._spmdictionary_keypad[lstboxCommands.SelectedValue.ToString()];
                        break;
                    case "contactless":
                        _currentcmd = CMDDictionary._spmdictionary_contactlessreader[lstboxCommands.SelectedValue.ToString()];
                        break;
                    case "barcodereader":
                        _currentcmd = CMDDictionary._spmdictionary_barcodereader[lstboxCommands.SelectedValue.ToString()];
                        break;
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _spmmanagertcpclient.writeStream("socketclosed");
            _spmmanagertcpclient.CloseSocket();
            _runtcplcientthread.Abort();
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            DataReceiveTxt.Text = string.Empty;
        }

        private void ResetBtnBgd(Button btn, Button[] btns)
        {
            foreach (Button mybtn in btns)
            {
                mybtn.Dispatcher.Invoke(_setButtonStyleDele, new object[] { mybtn, _btnStyle });
                ////mybtn.Dispatcher.Invoke(_setbuttonbackgrounddele, new object[] { mybtn, Brushes.White });
            }
            if (null != btn)
                //btn.Dispatcher.Invoke(_setbuttonbackgrounddele, new object[] { btn, Brushes.LightSteelBlue });
                btn.Dispatcher.Invoke(_setButtonStyleDele, new object[] { btn, _btnHighLightStyle });
        }
        
        private void SetBtnStyle(Button btn, Style style)
        {
            btn.Style = style;
        }

        private void SetBtnBorder(Button btn, SolidColorBrush scb)
        {
            btn.BorderBrush = scb;
        }
        private void SetBtnStatus(Button btn, bool isOnline)
        {
            if (isOnline)
            {
                ((Label)this.FindName(btn.Name + "LblStatus")).Content = "ON";
                ((Image)this.FindName(btn.Name + "ImgStatus")).Source = _imgOn;
            }
            else
            {
                ((Label)this.FindName(btn.Name + "LblStatus")).Content = "OFF";
                ((Image)this.FindName(btn.Name + "ImgStatus")).Source = _imgOff;
            }
        }

        private void LoadAllPrompts()
        {
            for (int i = 1; i <= _spmIDbtns.Length; i++)
            {
                _loadpromptdele.Invoke(i.ToString());
            }
        }

        private void LoadAllAppsKeys()
        {
            for (int i = 1; i <= _spmIDbtns.Length; i++)
            {
                _loadAppsKeysdele.Invoke(i.ToString());
            }
        }

        /// <summary>
        /// Load message into a SPM unit
        /// </summary>
        /// <param name="spmid"></param>
        internal void LoadPrompt(string spmid)
        {
            _ispromptloadsucess = false;
            ResetPromptProgressbar();
            if (spmid == "")
                SetControlValue(DataReceiveTxt, new object[]{"Please choose a spm unit from above buttons"});
            //else if (_spmmanagerpipe._availablespms.ContainsKey(spmid))
            else if (_spmmanagertcpclient._availablespms.ContainsKey(spmid))
            {
                _spmmanagertcpclient.writeStream("LoadPromtByManager{PARAMS}" + "SpmId=" + spmid);
                SetControlValue(lblLoadStatus, new object[]{"Loading SPM " + spmid + " ..."});
                if (_spmmanagertcpclient._loadPromptFinishedEvent.WaitOne(PROMPTLOADINGTIMEOUT))
                {
                    if (_ispromptloadsucess)
                    {
                        //_spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setbuttonbackgrounddele,
                        //    new object[] { _spmIDbtns[int.Parse(spmid) - 1], Brushes.LightGreen });
                        _spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setButtonStyleDele,
                            new object[] { _spmIDbtns[int.Parse(spmid) - 1], _btnLoadSuccessStyle });
                        //SetControlValue(lblLoadStatus, new object[] { "Load SPM " + spmid + " success." });
                    }
                    else
                    {
                        //_spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setbuttonbackgrounddele,
                        //   new object[] { _spmIDbtns[int.Parse(spmid) - 1], Brushes.Red });
                        _spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setButtonStyleDele,
                           new object[] { _spmIDbtns[int.Parse(spmid) - 1], _btnLoadFailStyle });
                        SetControlValue(lblLoadStatus, new object[] { "Load SPM " + spmid + " fail." });
                    }
                }
                else
                {
                    SetControlValue(DataReceiveTxt, new object[]{"Time out for loading the Spm " + spmid
                        + ". the spm unit may have network issue or other promblems while loading prompts"});
                    //_spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setbuttonbackgrounddele, 
                    //    new object[] { _spmIDbtns[int.Parse(spmid) - 1], Brushes.Red });
                    _spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setButtonStyleDele,
                       new object[] { _spmIDbtns[int.Parse(spmid) - 1], _btnLoadFailStyle });
                }
            }
            else
            {
                SetControlValue(DataReceiveTxt, new object[]{"The Spm " + spmid + " is not online"});
                SetControlValue(lblLoadStatus, new object[] { "The Spm " + spmid + " is not online" });
                //_spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setbuttonbackgrounddele, new object[] { _spmIDbtns[int.Parse(spmid) - 1], Brushes.Red });
                _spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setButtonStyleDele,
                      new object[] { _spmIDbtns[int.Parse(spmid) - 1], _btnLoadFailStyle });
                _spmmanagertcpclient._loadPromptFinishedEvent.Set();
            }
            ResetPromptProgressbar();
            _spmmanagertcpclient._loadPromptFinishedEvent.Reset();
        }

        private void LoadAppsKeys(string spmid)
        {
            _isAppsKeysLoadSuccess = false;
            //ResetAppsKeysProgressbar(SPMManagerTcpClient.totalAppsKeysItemCount);
            if (spmid == "")
                SetControlValue(DataReceiveTxt, new object[]{"Please choose a spm unit from above buttons"});
            else if (_spmmanagertcpclient._availablespms.ContainsKey(spmid))
            {
                _spmmanagertcpclient.writeStream("LoadAppsKeysByManager{PARAMS}" + "SpmId=" + spmid);
                SetControlValue(lblLoadAppsKeysStatus, new object[]{"Loading Applications & Public Keys into SPM " + spmid + " ..."});
                if (_spmmanagertcpclient._loadAppsKeysFinishedEvent.WaitOne(PROMPTLOADINGTIMEOUT))
                {
                    if (_isAppsKeysLoadSuccess)
                    {
                        _spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setButtonStyleDele,
                            new object[] { _spmIDbtns[int.Parse(spmid) - 1], _btnLoadSuccessStyle });
                    }
                    else
                    {
                        _spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setButtonStyleDele,
                           new object[] { _spmIDbtns[int.Parse(spmid) - 1], _btnLoadFailStyle });
                        SetControlValue(lblLoadAppsKeysStatus, new object[] { "Load SPM " + spmid + " fail." });
                    }
                }
                else
                {
                    SetControlValue(DataReceiveTxt, new object[]{"Time out for loading the Spm " + spmid
                        + ". the spm unit may have network issue or other promblems while loading prompts"});
                    _spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setButtonStyleDele,
                       new object[] { _spmIDbtns[int.Parse(spmid) - 1], _btnLoadFailStyle });
                }
                ResetAppsKeysProgressbar(0);
            }
            else
            {
                SetControlValue(DataReceiveTxt, new object[]{"The Spm " + spmid + " is not online"});
                SetControlValue(lblLoadAppsKeysStatus, new object[] { "The Spm " + spmid + " is not online" });
                _spmIDbtns[int.Parse(spmid) - 1].Dispatcher.BeginInvoke(_setButtonStyleDele, new object[] { _spmIDbtns[int.Parse(spmid) - 1], _btnLoadFailStyle });
                _spmmanagertcpclient._loadAppsKeysFinishedEvent.Set();
            }
            
            _spmmanagertcpclient._loadAppsKeysFinishedEvent.Reset();
        }

        private void ResetPromptProgressbar()
        {
            SetControlValue(pgbLoadPrompt, new object[] { 0, SPMManagerTcpClient.ProgressBarSetOptions.Current });
            SetControlValue(pgbLoadPrompt, new object[] { 0, SPMManagerTcpClient.ProgressBarSetOptions.Min });
            SetControlValue(pgbLoadPrompt, new object[] { SPMManagerTcpClient.totalpromptscount - 1, SPMManagerTcpClient.ProgressBarSetOptions.Max });
        }
        private void ResetAppsKeysProgressbar(int totalcount)
        {
            SetControlValue(pgbLoadAppsKeys, new object[] { 0, SPMManagerTcpClient.ProgressBarSetOptions.Current });
            SetControlValue(pgbLoadAppsKeys, new object[] { 0, SPMManagerTcpClient.ProgressBarSetOptions.Min });
            //SetControlValue(pgbLoadAppsKeys, new object[] { SPMManagerTcpClient.totalAppsKeysItemCount - 1, SPMManagerTcpClient.ProgressBarSetOptions.Max });//temp
            SetControlValue(pgbLoadAppsKeys, new object[] { totalcount, SPMManagerTcpClient.ProgressBarSetOptions.Max });//temp
        }

        private void enableSendButton(bool enable)
        {
            SendBtn.IsEnabled = enable;
        }

        private bool ValidatePassword(string password)
        {
            if ((password.ToUpper() == ("INFONET" + ((DateTime.Now.Day
                + int.Parse(DateTime.Now.Year.ToString().Substring(2))
                + DateTime.Now.Month) * 2)))
                || (password.ToUpper() == ("IFICC") + DateTime.Now.Day.ToString().PadLeft(2, '0'))
                || (password.ToUpper() == ("ICC") + DateTime.Now.Day.ToString().PadLeft(2, '0')))
            {
                return true;
            }
            else
                return false;
        }

        private void SetPromptProgressBar(double value, SPMManagerTcpClient.ProgressBarSetOptions setoption)
        {
            switch (setoption)
            {
                case SPMManagerTcpClient.ProgressBarSetOptions.Current:
                    pgbLoadPrompt.Value = value;
                    break;
                case SPMManagerTcpClient.ProgressBarSetOptions.Max:
                    pgbLoadPrompt.Maximum = value;
                    break;
                case SPMManagerTcpClient.ProgressBarSetOptions.Min:
                    pgbLoadPrompt.Minimum = value;
                    break;
            }
        }

        private void SetAppkKeysProgressBar(double value, SPMManagerTcpClient.ProgressBarSetOptions setoption)
        {
            switch (setoption)
            {
                case SPMManagerTcpClient.ProgressBarSetOptions.Current:
                    pgbLoadAppsKeys.Value = value;
                    break;
                case SPMManagerTcpClient.ProgressBarSetOptions.Max:
                    pgbLoadAppsKeys.Maximum = value;
                    break;
                case SPMManagerTcpClient.ProgressBarSetOptions.Min:
                    pgbLoadAppsKeys.Minimum = value;
                    break;
            }
        }
        #endregion

        #region events
        private void loadPromtAllSpmBtn_Click(object sender, RoutedEventArgs e)
        {
            _loadallpromptsdele.BeginInvoke(null, null);
        }

        private void loadPromtSingleSpmBtn_Click(object sender, RoutedEventArgs e)
        {
            _loadpromptdele.BeginInvoke(_currentspm, null, null);
        }
        

        private void loadAppKeysSingleSpmBtn_Click(object sender, RoutedEventArgs e)
        {
            _loadAppsKeysdele.BeginInvoke(_currentspm, null, null);
        }

        private void loadAppsKeysAllSpmBtn_Click(object sender, RoutedEventArgs e)
        {
            _loadAllAppsKeysdele.BeginInvoke(null, null);
        }

        private void ckbEnableLoadAppsKeysSingle_Checked(object sender, RoutedEventArgs e)
        {
            if ((bool)ckbEnableLoadAppsKeysSingle.IsChecked)
            {
                loadAppKeysSingleSpmBtn.IsEnabled = true;
                enableSendButton(false);
                ckbEnableLoadAppsKeysAll.IsChecked = false;
                loadAppsKeysAllSpmBtn.IsEnabled = false;
            }
            else
            {
                loadAppKeysSingleSpmBtn.IsEnabled = false;
                enableSendButton(true);
            }
        }

        private void ckbEnableLoadAppsKeysAll_Checked(object sender, RoutedEventArgs e)
        {
            ResetBtnBgd(null, _spmIDbtns);
            _currentspm = string.Empty;
            if ((bool)ckbEnableLoadAppsKeysAll.IsChecked)
            {
                loadAppsKeysAllSpmBtn.IsEnabled = true;
                enableSendButton(false);
                ckbEnableLoadAppsKeysSingle.IsChecked = false;
                loadAppKeysSingleSpmBtn.IsEnabled = false;
            }
            else
            {
                loadAppsKeysAllSpmBtn.IsEnabled = false;
                enableSendButton(true);
            }
        }

        private void CoreBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentcategory = "core";
            ResetBtnBgd(CoreBtn, _menubtns);
            BuildCommandsListBox(CMDDictionary._spmdictionary_core);
        }

        private void CardReaderBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentcategory = "cardreader";
            ResetBtnBgd(CardReaderBtn, _menubtns);
            BuildCommandsListBox(CMDDictionary._spmdictionary_cardreader);
        }

        private void PrinterBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentcategory = "printer";
            ResetBtnBgd(PrinterBtn, _menubtns);
            BuildCommandsListBox(CMDDictionary._spmdictionary_printer);
        }

        private void ScreenBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentcategory = "screen";
            ResetBtnBgd(ScreenBtn, _menubtns);
            BuildCommandsListBox(CMDDictionary._spmdictionary_screen);
        }
        private void EmvModBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentcategory = "emvmoduel";
            ResetBtnBgd(EmvModBtn, _menubtns);
            BuildCommandsListBox(CMDDictionary._spmdictionary_emvmodule);
        }

        private void SecModBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentcategory = "securemoduel";
            ResetBtnBgd(SecModBtn, _menubtns);
            BuildCommandsListBox(CMDDictionary._spmdictionary_securitymodule);
        }
        private void KeypadBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentcategory = "keypad";
            ResetBtnBgd(KeypadBtn, _menubtns);
            BuildCommandsListBox(CMDDictionary._spmdictionary_keypad);
        }
        private void ContactlessBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentcategory = "contactless";
            ResetBtnBgd(ContactlessBtn, _menubtns);
            BuildCommandsListBox(CMDDictionary._spmdictionary_contactlessreader);
        }
        private void BarcodeReaderBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentcategory = "barcodereader";
            ResetBtnBgd(BarcodeReaderBtn, _menubtns);
            BuildCommandsListBox(CMDDictionary._spmdictionary_barcodereader);
        }

        private void ckbEnableLoadPromptAll_Checked(object sender, RoutedEventArgs e)
        {
            ResetBtnBgd(null, _spmIDbtns);
            _currentspm = string.Empty;
            if ((bool)ckbEnableLoadPromptAll.IsChecked)
            {
                loadPromtAllSpmBtn.IsEnabled = true;
                enableSendButton(false);
                ckbEnableLoadPromptSingle.IsChecked = false;
                loadPromtSingleSpmBtn.IsEnabled = false;
            }
            else
            {
                loadPromtAllSpmBtn.IsEnabled = false;
                enableSendButton(true);
            }

        }
        private void ckbEnableLoadPromptSingle_Checked(object sender, RoutedEventArgs e)
        {
            //ResetBtnBgd(null, _spmIDbtns);
            //_currentspm = string.Empty;
            if ((bool)ckbEnableLoadPromptSingle.IsChecked)
            {
                loadPromtSingleSpmBtn.IsEnabled = true;
                enableSendButton(false);
                ckbEnableLoadPromptAll.IsChecked = false;
                loadPromtAllSpmBtn.IsEnabled = false;
            }
            else
            {
                loadPromtSingleSpmBtn.IsEnabled = false;
                enableSendButton(true);
            }
        }

        #region SPM SELECTION
        private void Spm1Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "1";
            ResetBtnBgd(Spm1Btn, _spmIDbtns);
        }

        private void Spm2Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "2";
            ResetBtnBgd(Spm2Btn, _spmIDbtns);
        }

        private void Spm3Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "3";
            ResetBtnBgd(Spm3Btn, _spmIDbtns);
        }

        private void Spm4Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "4";
            ResetBtnBgd(Spm4Btn, _spmIDbtns);
        }

        private void Spm5Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "5";
            ResetBtnBgd(Spm5Btn, _spmIDbtns);
        }

        private void Spm6Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "6";
            ResetBtnBgd(Spm6Btn, _spmIDbtns);
        }

        private void Spm7Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "7";
            ResetBtnBgd(Spm7Btn, _spmIDbtns);
        }

        private void Spm8Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "8";
            ResetBtnBgd(Spm8Btn, _spmIDbtns);
        }

        private void Spm9Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "9";
            ResetBtnBgd(Spm9Btn, _spmIDbtns);
        }

        private void Spm10Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "10";
            ResetBtnBgd(Spm10Btn, _spmIDbtns);
        }

        private void Spm11Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "11";
            ResetBtnBgd(Spm11Btn, _spmIDbtns);
        }

        private void Spm12Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "12";
            ResetBtnBgd(Spm12Btn, _spmIDbtns);
        }

        private void Spm13Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "13";
            ResetBtnBgd(Spm13Btn, _spmIDbtns);
        }

        private void Spm14Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "14";
            ResetBtnBgd(Spm14Btn, _spmIDbtns);
        }

        private void Spm15Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "15";
            ResetBtnBgd(Spm15Btn, _spmIDbtns);
        }

        private void Spm16Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "16";
            ResetBtnBgd(Spm16Btn, _spmIDbtns);
        }
        private void Spm17Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "17";
            ResetBtnBgd(Spm17Btn, _spmIDbtns);
        }
        private void Spm18Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "18";
            ResetBtnBgd(Spm18Btn, _spmIDbtns);
        }

        private void Spm19Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "19";
            ResetBtnBgd(Spm19Btn, _spmIDbtns);
        }

        private void Spm20Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "20";
            ResetBtnBgd(Spm20Btn, _spmIDbtns);
        }

        private void Spm21Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "21";
            ResetBtnBgd(Spm21Btn, _spmIDbtns);
        }

        private void Spm22Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "22";
            ResetBtnBgd(Spm22Btn, _spmIDbtns);
        }

        private void Spm23Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "23";
            ResetBtnBgd(Spm23Btn, _spmIDbtns);
        }

        private void Spm24Btn_Click(object sender, RoutedEventArgs e)
        {
            _currentspm = "24";
            ResetBtnBgd(Spm24Btn, _spmIDbtns);
        }
        #endregion

        private void MenuItem_Click_Login(object sender, RoutedEventArgs e)
        {
            _mylogin = new LogIn(this.LoginActive, this._isLogin);
            _mylogin.Left = 400;
            _mylogin.Top = 400;
            _mylogin.Show();
        }

        private void MenuItem_Click_LogOut(object sender, RoutedEventArgs e)
        {
            LoginActive(false);
        }

        

        void lstboxCommands_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SetCmdTxt.Text = string.Empty;
            GetCurrentListBoxCmdFromDictionary();
            if (_currentcmd.Length > 0)
            {
                switch (_currentcmd)
                {
                    #region terminal property
                    case "TerminalMessage/TerminalCommand/SetDateTime":
                        break;
                    case "TerminalMessage/TerminalCommand/DisplaySecurePrompt":
                        SetCmdTxt.Text = "PromptId=1\r\n"
                                           + "MinLength=4\r\n"
                                           + "MaxLength=12\r\n"
                                           + "EntryType=Numeric\r\n"
                                           + "SecurLevel=Unencrypted\r\n"
                                           + "Hidden=false";
                        break;
                    case "TerminalMessage/TerminalCommand/EnableFunctionKeys":
                        SetCmdTxt.Text = "functionL1=Function1\r\n"
                                           + "functionL2=Function2\r\n"
                                           + "functionL3=Function3\r\n"
                                           + "functionL4=Function4\r\n"
                                           + "functionR1=Function5\r\n"
                                           + "functionR2=Function6\r\n"
                                           + "functionR3=Function7\r\n"
                                           + "functionR4=Function8\r\n";
                        break;
                    case "TerminalMessage/TerminalCommand/DisplayCommand/SetMessageWindowProperties":
                        SetCmdTxt.Text = "ForeColor=000000FF\r\n"
                                           + "BackColor=FFFFFFFF\r\n"
                                           + "VerticalAlignment=Middle\r\n"
                                           + "HorizontalAlignment=Left\r\n"
                                           + "Top=0\r\n"
                                           + "Left=0\r\n"
                                           + "Width=320\r\n"
                                           + "Height=240\r\n"
                                           + "Name=Courier New\r\n"
                                           + "Size=14\r\n"
                                           + "Style=0\r\n";
                        break;
                    case "TerminalMessage/TerminalCommand/DisplayCommand/SetDataEntryWindowProperties":
                        SetCmdTxt.Text = "ForeColor=FF000000\r\n"
                                           + "BackColor=FFFFFFFF\r\n"
                                           + "LocationTop=240\r\n"
                                           + "LocationLeft=0\r\n"
                                           + "SizeWidth=320\r\n"
                                           + "SizeHeight=240\r\n"
                                           + "FontName=Tahoma\r\n"
                                           + "FontSize=14\r\n"
                                           + "FontStyle=0\r\n";
                        break;
                    #endregion
                    #region beeper
                    case "TerminalMessage/TerminalCommand/BeeperCommand/Beep":
                        SetCmdTxt.Text = "OnTime=100\r\n"
                                           + "OffTime=100\r\n"
                                           + "NumBeeps=3\r\n";
                        break;
                    #endregion
                    #region display property
                    case "TerminalMessage/TerminalCommand/DisplayCommand/SetPromptDetails":
                        SetCmdTxt.Text = "PromptId=200\r\n"
                                           + "PromptText=\\r\\n\\r\\n\\r\\n\r\n"
                                           + "Description=Idle Prompt\r\n"
                                           + "Top=0\r\n"
                                           + "Left=0\r\n"
                                           + "Height=240\r\n"
                                           + "Width=240\r\n";
                        break;
                    #endregion
                    #region display command
                    case "TerminalMessage/TerminalCommand/DisplayCommand/DisplayPrompt":
                        SetCmdTxt.Text = "PromptId=200\r\n"
                                        + "SubstitueParameter1=\r\n"
                                        + "SubstitueParameter2=\r\n"
                                        + "SubstitueParameter3=\r\n"
                                        + "SubstitueParameter4=\r\n"
                                        + "SubstitueParameter5=\r\n"
                                        + "SubstitueParameter6=\r\n"
                                        ;
                        break;
                    case "TerminalMessage/TerminalCommand/DisplayCommand/DisplayPromptImmediate":
                        SetCmdTxt.Text = "AdditionalText1=\r\n"
                                           + "AdditionalText2=\r\n"
                                           + "AdditionalText3=\r\n"
                                           + "AdditionalText4=\r\n";
                        break;
                    case "TerminalMessage/TerminalCommand/DisplayCommand/GetPromptDetails":
                        SetCmdTxt.Text = "PromptId=1";
                        break;
                    case "TerminalMessage/TerminalCommand/DisplayCommand/SetCurrentLanguage":
                        SetCmdTxt.Text = "Language=en";
                        break;
                    #endregion
                    #region keypad
                    case "TerminalMessage/TerminalCommand/KeypadCommand/EnableKeypadFunctionKeys":
                        SetCmdTxt.Text = "Function1=FuncL1 ReturnValue1=41\r\n"
                                           + "Function2=FuncL2 ReturnValue2=30\r\n"
                                           + "Function3=FuncL3 ReturnValue3=20\r\n"
                                           + "Function4=FuncL4 ReturnValue4=10\r\n"
                                           + "Function5=FuncR1 ReturnValue5=44\r\n"
                                           + "Function6=FuncR2 ReturnValue6=34\r\n"
                                           + "Function7=FuncR3 ReturnValue7=24\r\n"
                                           + "Function8=FuncR4 ReturnValue8=14";
                        break;
                    #endregion
                    #region Print
                    case "TerminalMessage/TerminalCommand/PrinterCommand/Print":
                        SetCmdTxt.Text = "Text1=\r\n"
                                       + "Text2=\r\n"
                                       + "Text3=\r\n"
                                       + "Text4=\r\n"
                                       + "Text5=\r\n"
                                       + "Text6=\r\n";
                        break;
                    #endregion
                    #region chipcard
                    case "TerminalMessage/TerminalCommand/ChipCardReaderCommand/OpenChipCardReader":
                        SetCmdTxt.Text = "AllowMagStripeFallback=true";
                        break;
                    #endregion
                    #region Secure module
                    case "TerminalMessage/TerminalCommand/SecurityModuleCommand/CalculateMAC":
                        SetCmdTxt.Text = "MACSOURCEVALUE=454C67755663873A";
                        break;
                    case "TerminalMessage/TerminalCommand/SecurityModuleCommand/VerifyMAC":
                        SetCmdTxt.Text = "MACSOURCEVALUE=\r\n"
                                           + "MACValue=856FF7E5\r\n"
                                           + "KSN=0000000001";
                        break;
                    case "TerminalMessage/TerminalCommand/SecurityModuleCommand/SetWorkingKey":
                        SetCmdTxt.Text = "KeyType=PIN\r\n"
                                           + "WorkingKey=454C67755663873A";
                        break;
                    case "TerminalMessage/TerminalCommand/SecurityModuleCommand/GetWorkingKeyStatistics":
                        SetCmdTxt.Text = "KeyType=PIN";
                        break;
                    case "TerminalMessage/TerminalCommand/SecurityModuleCommand/DownloadFile":
                        //SetCmdTxt.Text = "File=";
                        break;
                    #endregion
                    #region Emv module
                    case "TerminalMessage/TerminalCommand/EMVModuleCommand/InitiateTransaction":
                        SetCmdTxt.Text = "EmvDataElement=E1 38 C6 01 FF";
                        break;
                    case "TerminalMessage/TerminalCommand/EMVModuleCommand/ContinueTransaction":
                        SetCmdTxt.Text = "EmvDataElement=C101FF";
                        break;
                    case "TerminalMessage/TerminalCommand/EMVModuleCommand/DownloadApplication":
                        SetCmdTxt.Text = "Name=Visa\r\n"
                                           + "Rid=A000000003\r\n"
                                           + "Pix=1010\r\n"
                                           + "Version=84\r\n"
                                           + "FloorLimit=10000\r\n"
                                           + "ThresholdValue=500\r\n"
                                           + "TargetPercentage=30\r\n"
                                           + "MaxTargetPercentage=90\r\n"
                                           + "MaxTransactionValue=5000\r\n"
                                           + "MaxNoCVMTransactionValue=3000\r\n"
                                           + "ApprovalAmount=100\r\n"
                                           + "PromptForAccountType=false";
                        break;
                    case "TerminalMessage/TerminalCommand/EMVModuleCommand/DownloadApplicationPublicKeys":
                        SetCmdTxt.Text = "Rid=A000000003\r\n"
                                           + "SignAlgorithm=01\r\n"
                                           + "Number=92\r\n"
                                           + "Exponent=03\r\n"
                                           + "Modulus=\r\n"
                                           + "HashAlgorithm=01\r\n"
                                           + "HashValue=";
                        break;
                    case "TerminalMessage/TerminalCommand/EMVModuleCommand/SetTimeout":
                        SetCmdTxt.Text = "TimeoutValue=60";
                        break;
                    case "TerminalMessage/TerminalCommand/EMVModuleCommand/SetApplicationExclusionList":
                        SetCmdTxt.Text = "App=A0000002771010 ExcludeAll=true\r\n"
                                       + "App=A0000000050001 Exclude=A0000000043060";
                        break;
                    #endregion
                    default:
                        break;
                }
            }
        }

        private void MenuItem_MouseDoubleClick_Login(object sender, MouseButtonEventArgs e)
        {
            LoginActive(true);
        }


        #endregion


    }
}
