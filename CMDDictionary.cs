using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace SpmManagmentTool
{
    public class CMDDictionary
    {
        public const string _terminalcommand = "TerminalMessage/TerminalCommand/";
        public const string _terminalresponse = "TerminalMessage/TerminalResponse/";
        public const string _terminalevent = "TerminalMessage/TerminalEvent/";
        public static Dictionary<string, string> _spmdictionary_core = new Dictionary<string, string>();
        public static Dictionary<string, string> _spmdictionary_cardreader = new Dictionary<string, string>();
        public static Dictionary<string, string> _spmdictionary_printer = new Dictionary<string, string>();
        public static Dictionary<string, string> _spmdictionary_screen = new Dictionary<string, string>();
        public static Dictionary<string, string> _spmdictionary_keypad = new Dictionary<string, string>();
        public static Dictionary<string, string> _spmdictionary_securitymodule = new Dictionary<string, string>();
        public static Dictionary<string, string> _spmdictionary_emvmodule = new Dictionary<string, string>();
        public static Dictionary<string, string> _spmdictionary_contactlessreader = new Dictionary<string, string>();
        public static Dictionary<string, string> _spmdictionary_barcodereader = new Dictionary<string, string>();
        public CMDDictionary()
        {
            //BuildImageBin();
        }

        public void BuildDictionaries()
        {
            StreamReader _srd = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "\\Commands.txt");
            string _temp = string.Empty;
            try
            {
                while (true)
                {
                    _temp = _srd.ReadLine();
                    if (null != _temp && _temp.Contains(','))
                    {
                        switch (_temp.Split(',')[0].ToLower())
                        {
                            case "core":
                                _spmdictionary_core.Add(_temp.Split(',')[1], _terminalcommand + _temp.Split(',')[2].Trim());
                                break;
                            case "screen":
                                _spmdictionary_screen.Add(_temp.Split(',')[1], _terminalcommand + _temp.Split(',')[2].Trim());
                                break;
                            case "cardreader":
                                _spmdictionary_cardreader.Add(_temp.Split(',')[1], _terminalcommand + _temp.Split(',')[2].Trim());
                                break;
                            case "keypad":
                                _spmdictionary_keypad.Add(_temp.Split(',')[1], _terminalcommand + _temp.Split(',')[2].Trim());
                                break;
                            case "printer":
                                _spmdictionary_printer.Add(_temp.Split(',')[1], _terminalcommand + _temp.Split(',')[2].Trim());
                                break;
                            case "securemodule":
                                _spmdictionary_securitymodule.Add(_temp.Split(',')[1], _terminalcommand + _temp.Split(',')[2].Trim());
                                break;
                            case "emvmodule":
                                _spmdictionary_emvmodule.Add(_temp.Split(',')[1], _terminalcommand + _temp.Split(',')[2].Trim());
                                break;
                            case "contactless":
                                _spmdictionary_contactlessreader.Add(_temp.Split(',')[1], _terminalcommand + _temp.Split(',')[2].Trim());
                                break;
                            case "barcodereader":
                                _spmdictionary_barcodereader.Add(_temp.Split(',')[1], _terminalcommand + _temp.Split(',')[2].Trim());
                                break;
                        }
                    }
                    else
                        break;
                }
            }
            catch (IOException ex)
            {

            }
            catch (Exception ex)
            {

            }
        }

        public void BuildImageBin()
        {
            try
            {
                string val = string.Empty;
                MemoryStream ms = new MemoryStream();
                System.Drawing.Image _myimage = System.Drawing.Image.FromFile("C:\\Projects\\SPMTool\\SpmManagmentTool\\untitled.bmp");
                _myimage.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
                for (int i = 0; i < ms.Length - 1; i++)
                {
                    val += String.Format("{0:X2}",ms.ToArray()[i].ToString());
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}
