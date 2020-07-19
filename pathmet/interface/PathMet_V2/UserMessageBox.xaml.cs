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


            switch (type)
            {
                case "error":
                    Button errbtn = new Button
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 16,
                        Padding = new Thickness(40, 20, 40, 20),
                        Margin = new Thickness(20)
                    };
                    errbtn.Click += yesResponse;
                    panel_Container.Background = System.Windows.Media.Brushes.LightCoral;
                    errbtn.Content = "OK";
                    errbtn.Click += closeWindow;
                    buttonsPanel.Children.Add(errbtn);
                    break;

                case "yesno":
                    Button yesBtn = new Button
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 16,
                        Padding = new Thickness(40, 20, 40, 20),
                        Margin = new Thickness(20),
                        Content = "Yes"
                    };
                    yesBtn.Click += yesResponse;
                    buttonsPanel.Children.Add(yesBtn);

                    Button noBtn = new Button
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 16,
                        Padding = new Thickness(40, 20, 40, 20),
                        Margin = new Thickness(20),
                        Content = "No"
                    };
                    noBtn.Click -= yesResponse;
                    noBtn.Click += noResponse;
                    buttonsPanel.Children.Add(noBtn);
                    break;

                default:
                    Button okayBtn = new Button
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 16,
                        Padding = new Thickness(40, 20, 40, 20),
                        Margin = new Thickness(20),
                        Content = "OK"
                    };
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
