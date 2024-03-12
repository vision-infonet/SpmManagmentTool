using System;
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
using System.Windows.Shapes;

namespace SpmManagmentTool
{
    /// <summary>
    /// Interaction logic for LogIn.xaml
    /// </summary>
    public partial class LogIn : Window
    {
        #region variables
        public delegate void LogResult(bool success);
        public LogResult logresultdelegate;
        private bool logSuccess = false;
        private bool wasLogin = false;
        #endregion

        #region constructor
        public LogIn(LogResult logresult, bool wasLogin)
        {
            InitializeComponent();
            if (wasLogin)
            {
                this.wasLogin = wasLogin;
                lblPassword.Visibility = Visibility.Hidden;
                psbPassword.Visibility = Visibility.Hidden;
                btnGoToHome.Visibility = Visibility.Visible;
                lblSuccess.Visibility = Visibility.Visible;
                lblSuccess.Content = "You logged in already!";
            }
            else
                psbPassword.Focus();
            this.logresultdelegate = logresult;
        }
        #endregion
        #region events
        private void FocusOnLoginBtn(object sender, KeyEventArgs e)
        {
            if (ValidatePassword(psbPassword.Password))
            {
                this.logSuccess = true;
                psbPassword.Clear();
                psbPassword.IsEnabled = false;
                btnGoToHome.Visibility = Visibility.Visible;
                lblSuccess.Visibility = Visibility.Visible;
                lblSuccess.Content = "SUCCESS!";
                btnGoToHome.Focus();
            }
            else
                psbPassword.Focus();
        }

        private void Button_GoToHome_Click(object sender, RoutedEventArgs e)
        {
            if(!wasLogin)
                logresultdelegate.Invoke(logSuccess);
            this.Close();
        }
        #endregion

        #region methods
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
        #endregion
    }
}
