using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace SpmManagmentTool
{
    public class SPMManagerTcpClient
    {
        #region variables
        public delegate void SetControlsValue(object control, object[] values);
        private SetControlsValue _setcontrolvalue;
        private TextBox _datareceivedbox;
        private Label _connectionstatuslbl, _loadstatuslbl, _loadAppsKeysStatusLbl;
        private Button[] _spmidbtns;
        private ProgressBar _pgbPrompt, _pgbAppsKeys;
        private bool _issocketopen = false;
        internal StringDictionary _availablespms = new StringDictionary();
        internal ManualResetEvent _loadPromptFinishedEvent;
        internal ManualResetEvent _loadAppsKeysFinishedEvent;
        internal enum ProgressBarSetOptions { Min = 1, Max, Current };
        private int loadcount = 0;
        internal static int totalpromptscount = 0, totalAppsKeysItemCount = 0, totalContatclessAppsKeysItemCount=0;
        internal Socket _socket = null;
        internal string _optipaddress = System.Configuration.ConfigurationManager.AppSettings["OPT_IP_address"];
        internal int _optport = int.Parse(System.Configuration.ConfigurationManager.AppSettings["OPT port"]);

        #endregion

        #region constructor
        public SPMManagerTcpClient(SetControlsValue setcontrol, TextBox tb, Label[] lbls, Button[] btns, ProgressBar[] pgbs)
        {
            try
            {
                this._setcontrolvalue = setcontrol;
                _datareceivedbox = tb;
                _connectionstatuslbl = lbls[0];
                _loadstatuslbl = lbls[1];
                _loadAppsKeysStatusLbl = lbls[2];
                _spmidbtns = new Button[btns.Length];
                for (int i = 0; i < btns.Length; i++)
                {
                    _spmidbtns[i] = btns[i];
                }
                _pgbPrompt = pgbs[0];
                _pgbAppsKeys = pgbs[1];
                _loadPromptFinishedEvent = new ManualResetEvent(false);
                _loadAppsKeysFinishedEvent = new ManualResetEvent(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString()); 
            }
        }

         #endregion

        #region methods

        public void SetUpConnectionToOPT()
        {
            if (_optipaddress == string.Empty)
            {
                IPAddress[] _ipaddresses = Dns.GetHostAddresses(Dns.GetHostName());
                if (_ipaddresses.Length > 0)
                {
                    foreach (IPAddress ipaddress in _ipaddresses)
                    {
                        if (ipaddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                           && ipaddress.ToString() != "127.0.0.1")
                        {
                            _optipaddress = ipaddress.ToString();
                            //System.Configuration.Configuration _config = System.Configuration.ConfigurationManager.OpenExeConfiguration(System.Configuration.ConfigurationUserLevel.None);
                            //_config.AppSettings.Settings["OPT_IP_address"].Value = _optipaddress;
                            //_config.Save();
                            //System.Configuration.ConfigurationManager.RefreshSection("appSettings");
                            break;
                        }
                    }
                }
            }
            while (true)
            {
                try
                {
                    if (null == _socket || !_issocketopen || !_socket.Connected)
                    {
                        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        _socket.Connect(new IPEndPoint(IPAddress.Parse(_optipaddress), _optport));
                        _issocketopen = true;
                        _availablespms.Clear();
                        loadcount = 0;
                        ThreadPool.QueueUserWorkItem(new WaitCallback(this.readStream));
                        _setcontrolvalue.BeginInvoke(_connectionstatuslbl, new object[] { "Connected to OPT." }, new AsyncCallback(readStream), null);
                    }
                }
                catch (System.TimeoutException ex)
                {
                    _setcontrolvalue.Invoke(_connectionstatuslbl, new object[] { "OPT is not running or none of the SPMs is connected. Connect time out." });
                    foreach (Button btn in _spmidbtns)
                    {
                        btn.Dispatcher.BeginInvoke(Window1._setbuttonborderdele, new object[] { btn, Brushes.Gray });
                    }
                }
                catch (Exception ex)
                {
                    foreach (System.Collections.DictionaryEntry de in _availablespms)
                    {
                        _spmidbtns[int.Parse(de.Key.ToString()) - 1].Dispatcher.BeginInvoke(Window1._setbuttonborderdele, new object[] { _spmidbtns[int.Parse(de.Key.ToString()) - 1], Brushes.Black });
                        _spmidbtns[int.Parse(de.Key.ToString()) - 1].Dispatcher.BeginInvoke(Window1._setBtnStatusDele, new object[] { _spmidbtns[int.Parse(de.Key.ToString()) - 1], false });
                    }
                    _availablespms.Clear();
                    _setcontrolvalue.Invoke(_connectionstatuslbl, new object[] { ex.Message });
                }
                finally
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public void writeStream(string data)
        {
            try
            {
                if(null != _socket && _socket.Connected && _issocketopen)
                    _socket.Send(Encoding.ASCII.GetBytes(data));
            }
            catch (SocketException e)
            {
                CloseSocket();
            }
        }


        public void readStream(object obj)
        {
            string _receivedtemp;
            byte[] _readbuffer = new byte[4069];
            byte[] _data = null;
            int _readlength = 0;
            string[] _onlinespm;
            string[] _receivedOnlineSpmEvents;

            while (true)
            {
                if (null != _socket && _socket.Connected && _issocketopen)
                {
                    try
                    {
                        _readlength = _socket.Receive(_readbuffer);
                        if (_readlength <= 0)
                        {
                            Thread.Sleep(100);
                            continue;
                        }
                        else
                        {
                            _data = new byte[_readlength];
                            Array.Copy(_readbuffer, _data, _readlength);
                            _receivedtemp = Encoding.ASCII.GetString(_data);
                            //_setcontrolvalue.Invoke(_datareceivedbox, new object[] { temp });
                            _receivedOnlineSpmEvents = _receivedtemp.Split(new string[] { "{LF}" }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (string temp in _receivedOnlineSpmEvents)
                            {
                                _setcontrolvalue.Invoke(_datareceivedbox, new object[] { temp });
                                if (temp.Contains("spmonline"))
                                {
                                    _onlinespm = temp.Split(':');
                                    foreach (string s in _onlinespm)
                                    {
                                        if (System.Text.RegularExpressions.Regex.IsMatch(s, "\\d{1,2}") && !_availablespms.ContainsKey(s))
                                        {
                                            _availablespms.Add(s, null);
                                            _spmidbtns[int.Parse(_onlinespm[1]) - 1].Dispatcher.BeginInvoke(Window1._setbuttonborderdele, new object[] { _spmidbtns[int.Parse(_onlinespm[1]) - 1], Brushes.Red });
                                            _spmidbtns[int.Parse(_onlinespm[1]) - 1].Dispatcher.BeginInvoke(Window1._setBtnStatusDele, new object[] { _spmidbtns[int.Parse(_onlinespm[1]) - 1], true });

                                        }
                                    }
                                }

                                if (temp.Contains("TotalPrompts"))
                                {
                                    int.TryParse(temp.Split(':')[1], out totalpromptscount);
                                }
                                if (temp.Contains("TotalAppsKeysItems"))
                                {
                                    int.TryParse(temp.Split(':')[1], out totalAppsKeysItemCount);
                                }
                                if (temp.Contains("TotalContactlessAppsKeysItems"))
                                {
                                    int.TryParse(temp.Split(':')[1], out totalContatclessAppsKeysItemCount);
                                }
                                if (temp.Contains("SetPromptDoneOnSpm"))//Finish loading prompt for one SPM unit.
                                {
                                    _setcontrolvalue.Invoke(_loadstatuslbl, new object[] { "The Spm " + temp.Replace("SetPromptDoneOnSpm", string.Empty) + " has prompts loaded successfully." });
                                    Window1._ispromptloadsucess = true;
                                    _loadPromptFinishedEvent.Set();
                                    loadcount = 0;
                                    Thread.Sleep(10);
                                    _loadPromptFinishedEvent.Reset();

                                }
                                if (temp.Contains("NeedLoadContactlessAppKeys"))
                                {
                                    _setcontrolvalue(_pgbAppsKeys, new object[] { totalAppsKeysItemCount + totalContatclessAppsKeysItemCount - 1, ProgressBarSetOptions.Max });
                                }
                                if (temp.Contains("NoContactlessAppKeys"))
                                {
                                    _setcontrolvalue(_pgbAppsKeys, new object[] { totalAppsKeysItemCount - 1, ProgressBarSetOptions.Max });
                                }
                                if (temp.Contains("LoadAppsKeysDoneOnSpm") || temp.Contains("LoadContactlessAppsKeysDoneOnSpm"))
                                {
                                    _setcontrolvalue.Invoke(_loadAppsKeysStatusLbl, new object[] { "The Spm " + temp.Replace("LoadAppsKeysDoneOnSpm", string.Empty) + " has Applications Keys loaded successfully." });
                                    Window1._isAppsKeysLoadSuccess = true;
                                    _loadAppsKeysFinishedEvent.Set();
                                    loadcount = 0;
                                    Thread.Sleep(10);
                                    _loadAppsKeysFinishedEvent.Reset();
                                }

                                if (temp.Contains("SetPromptDetailsResponse"))
                                {
                                    _setcontrolvalue.Invoke(_pgbPrompt, new object[] { loadcount++, ProgressBarSetOptions.Current });
                                }
                                if (temp.Contains("DownloadApplicationPublicKeysResponse")
                                    || temp.Contains("DownloadApplicationResponse")
                                    || temp.Contains("DownloadContactlessPublicKeysResponse")
                                    || temp.Contains("DownloadContactlessApplicationResponse")
                                    )
                                {
                                    _setcontrolvalue.Invoke(_pgbAppsKeys, new object[] { loadcount++, ProgressBarSetOptions.Current });
                                }

                                if (temp.Contains("Socket exception or network problem on SPM READER"))
                                {
                                    _setcontrolvalue.Invoke(_connectionstatuslbl, new object[] { temp });
                                    _spmidbtns[int.Parse(temp.Split(':')[1]) - 1].Dispatcher.BeginInvoke(Window1._setbuttonborderdele, new object[] { _spmidbtns[int.Parse(temp.Split(':')[1]) - 1], Brushes.Gray });
                                    Window1._ispromptloadsucess = false;
                                    _loadPromptFinishedEvent.Set();
                                    loadcount = 0;
                                }
                            }
                        }
                    }
                    catch (SocketException e)
                    {
                        writeStream("socketclosed");
                        CloseSocket();
                        _setcontrolvalue.Invoke(_connectionstatuslbl, new object[] { string.Format("ERROR: {0}", e.Message) });
                        break;
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        Thread.Sleep(50);
                    }
                }
                Thread.Sleep(50);
            }
        }

        
        public void CloseSocket()
        {
            _issocketopen = false;
            _socket.Close();
        }
        #endregion
    }
}
