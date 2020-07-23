using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Interaction logic for ReviewImagesWindow.xaml
    /// </summary>
    public partial class ReviewImagesWindow : Window
    {
        private List<BitmapImage> images = new List<BitmapImage>();
        private int currentImgNum = 0;
        private int imgCount;
        public ReviewImagesWindow(List<String> imgPaths)
        {
            InitializeComponent();
            imgCount = imgPaths.Count;

            for (int i = 0; i < imgCount; i++)
            {
                //add a combobox item for each image with just it's number
                ComboBoxItem runImageNumber = new ComboBoxItem();
                runImageNumber.Content = i;
                imgNumberBox.Items.Add(runImageNumber);

                //make a bitmap image from each path and add it to images list
                /*BitmapImage bi = new BitmapImage(new Uri(imgPaths.ElementAt(i)));
                images.Add(bi);
                bi.Freeze();*/

                var bitmap = new BitmapImage();
                var stream = File.OpenRead(imgPaths.ElementAt(i));

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
                images.Add(bitmap);
                stream.Close();
                stream.Dispose();
            }

            imgNumberBox.SelectionChanged += boxSelection;
            imgNumberBox.SelectedIndex = 0;
        }
        

        private void nextBtn_Click(object sender, RoutedEventArgs e)
        {
            int newNum = (currentImgNum + 1) % imgCount;
            currentImg.Source = images.ElementAt(newNum);
            currentImgNum = newNum;
            imgNumberBox.SelectedIndex = newNum; 
        }

        private void prevBtn_Click(object sender, RoutedEventArgs e)
        {
            int newNum = (currentImgNum - 1) % imgCount;
            currentImg.Source = images.ElementAt(newNum);
            currentImgNum = newNum;
            imgNumberBox.SelectedIndex = newNum;
        }

        private void boxSelection(object sender, SelectionChangedEventArgs e)
        {
            int newNum = imgNumberBox.SelectedIndex;
            currentImg.Source = images.ElementAt(newNum);
            currentImgNum = newNum;
            imgNumberBox.SelectedIndex = newNum;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            currentImg.Source = null;
            images = null;
            GC.Collect();
        }
    }
}
