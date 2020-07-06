using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PathMet_V2
{
    /// <summary>
    /// Interaction logic for UserMessageBox.xaml
    /// </summary>
    public partial class UserMessageBox : Window
    {
        Button basicBtn;

        public UserMessageBox(string msg, string caption) : this(msg, caption, "") { }

        public UserMessageBox(string msg, string caption, string type)
        {
            InitializeComponent();

            this.MinHeight = (SystemParameters.PrimaryScreenHeight * 0.4);
            this.MinWidth = (SystemParameters.PrimaryScreenWidth * 0.4);
            this.Height = 500;
            this.Width = 1000;
            this.Title = caption;
            messageBox.Text = msg;

            basicBtn = new Button();
            basicBtn.VerticalAlignment = VerticalAlignment.Center;
            basicBtn.FontSize = 16;
            basicBtn.Padding = new Thickness(40, 20, 40, 20);
            basicBtn.Margin = new Thickness(20);
            basicBtn.Click += yesResponse;

            switch (type)
            {
                case "error":
                    Button errbtn = basicBtn;
                    panel_Container.Background = System.Windows.Media.Brushes.LightCoral;
                    errbtn.Content = "OK";
                    errbtn.Click += closeWindow;
                    buttonsPanel.Children.Add(errbtn);
                    break;
                case "yesno":
                    Button yesBtn = basicBtn;
                    yesBtn.Content = "Yes";
                    yesBtn.Click += yesResponse;
                    buttonsPanel.Children.Add(yesBtn);

                    Button noBtn = basicBtn;
                    noBtn.Content = "No";
                    noBtn.Click -= yesResponse;
                    noBtn.Click += noResponse;
                    buttonsPanel.Children.Add(noBtn);
                    break;
                default:
                    Button okayBtn = basicBtn;
                    okayBtn.Content = "OK";
                    okayBtn.Click += closeWindow;
                    buttonsPanel.Children.Add(okayBtn);
                    break;
            }
        }


        void closeWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        void yesResponse(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        void noResponse(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

    }
}
