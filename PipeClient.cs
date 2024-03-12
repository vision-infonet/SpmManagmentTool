using System;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace SpmManagmentTool
{
    class PipeClient
    {
        #region variables
        public delegate void SetControlsValue(object control, object[] values);
        private SetControlsValue _setcontrolvalue;
        private NamedPipeClientStream _spmmanagerpipe = null;
        private StreamWriter sw;
        private StreamReader sr;
        private TextBox _datareceivedbox;
        private Label _connectionstatuslbl, _loadstatuslbl;
        private Button[] _spmidbtns;
        private ProgressBar _pgb;
        private bool _ispipeopen = false;
        private System.Threading.Timer _timer;
        internal StringDictionary _availablespms = new StringDictionary();
        internal ManualResetEvent _loadPromptFinishedEvent;
        internal enum ProgressBarSetOptions { Min = 1, Max, Current };
        private int loadcount = 0;
        internal static int totalpromptscount;
        #endregion

        #region constructor
        public PipeClient(SetControlsValue setcontrol, TextBox tb, Label lbl, Label lbl2, Button[] btns, ProgressBar pgb)
        {
            try
            {
                this._setcontrolvalue = setcontrol;
                _datareceivedbox = tb;
                _connectionstatuslbl = lbl;
                _loadstatuslbl = lbl2;
                _spmidbtns = new Button[btns.Length];
                for (int i = 0; i < btns.Length; i++)
                {
                    _spmidbtns[i] = btns[i];
                }
                _pgb = pgb;
                _loadPromptFinishedEvent = new ManualResetEvent(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString()); 
            }
        }
        #endregion

        #region methods

        public void SetUpConnectionToPipeServer()
        {
            while (true)
            {
                try
                {
                    if (null == _spmmanagerpipe || !_ispipeopen || !_spmmanagerpipe.IsConnected)
                    {
                        //if (bool.Parse(System.Configuration.ConfigurationSettings.AppSettings["IsRemote"].ToString()))
                        //{
                        //    _spmmanagerpipe = new NamedPipeClientStream(System.Configuration.ConfigurationSettings.AppSettings["ServerNameRemote"].ToString(),
                        //                    "spmmanagerpipe", PipeDirection.InOut, PipeOptions.Asynchronous, System.Security.Principal.TokenImpersonationLevel.Impersonation);
                        //}
                        //else
                        _spmmanagerpipe = new NamedPipeClientStream(System.Configuration.ConfigurationSettings.AppSettings["ServerName"].ToString(),
                                        "spmmanagerpipe", PipeDirection.InOut, PipeOptions.Asynchronous);
                        _spmmanagerpipe.Connect(1000);
                        _ispipeopen = true;
                        _availablespms.Clear();
                        sw = new StreamWriter((NamedPipeClientStream)_spmmanagerpipe);
                        sr = new StreamReader((NamedPipeClientStream)_spmmanagerpipe);
                        sw.AutoFlush = true;
                        _timer = new Timer(new TimerCallback(PollingPipe), null, 0, 1000);
                        _setcontrolvalue.BeginInvoke(_connectionstatuslbl, new object[]{"Connected to OPT."}, new AsyncCallback(readStream), null);
                    }
                }
                catch (System.TimeoutException ex)
                {
                    _setcontrolvalue.Invoke(_connectionstatuslbl, new object[]{"OPT is not running or none of the SPMs is connected. Connect time out."});
                    foreach (Button btn in _spmidbtns)
                    {
                        btn.Dispatcher.BeginInvoke(Window1._setbuttonborderdele, new object[] {btn, Brushes.Gray });
                    }
                    _spmmanagerpipe.Dispose();
                }
                catch (Exception ex)
                {
                    _setcontrolvalue.Invoke(_connectionstatuslbl, new object[]{ex.Message});
                }
                finally
                {
                    Thread.Sleep(1000);
                }
            }
        }

        public  void writeStream(string data)
        {
            try
            {
                sw.WriteLine(data);
                sw.Flush();
            }
            catch (IOException e)
            {
                ClosePipe();
            }
            catch (Exception ex)
            {
            }
        }

        public  void readStream(object obj)
        {
            string temp;
            string[] _onlinespm;
            
            while (true)
            {
                if (null != sr && _spmmanagerpipe.IsConnected)
                {
                    try
                    {
                        while ((temp = sr.ReadLine()) != null)
                        {
                            _setcontrolvalue.Invoke(_datareceivedbox, new object[]{temp});
                            if (temp.Contains("spmonline"))
                            {
                                _onlinespm = temp.Split(':');
                                foreach (string s in _onlinespm)
                                {
                                    if (System.Text.RegularExpressions.Regex.IsMatch(s, "\\d{1,2}") && !_availablespms.ContainsKey(s) )
                                    {
                                        _availablespms.Add(s, null);
                                        _spmidbtns[int.Parse(_onlinespm[1]) - 1].Dispatcher.BeginInvoke(Window1._setbuttonborderdele, new object[] { _spmidbtns[int.Parse(_onlinespm[1]) - 1], Brushes.Red });
                                       
                                    }
                                }
                            }
                            if (temp.Contains("TotalPrompts"))
                            {
                                totalpromptscount = int.Parse(temp.Split(':')[1]);
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
                            if (temp.Contains("SetPromptDetailsResponse"))
                            {
                                _setcontrolvalue.Invoke(_pgb, new object[] { loadcount++, ProgressBarSetOptions.Current}); 
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
                    catch (IOException e)
                    {
                        writeStream("pipeclosed");
                        ClosePipe();
                        _setcontrolvalue.Invoke(_connectionstatuslbl, new object[]{string.Format("ERROR: {0}", e.Message)});
                        break;
                    }
                    catch (Exception ex)
                    {
                        writeStream("pipeclosed");
                        ClosePipe();
                    }
                    finally
                    {
                        Thread.Sleep(50);
                    }
                }
                Thread.Sleep(50);
            }
        }

        private void PollingPipe(object obj)
        {
            try
            {
                this.writeStream("");
            }
            catch (IOException ex)
            {
                ClosePipe();
            }
        }
        public void ClosePipe()
        {
            _ispipeopen = false;
            _spmmanagerpipe.Close();
        }
        #endregion
    }

     
}
