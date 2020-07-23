using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Windows.Media.Animation;
using System.ComponentModel;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> files;
        private int index = 0;
        private readonly DoubleAnimation mainGridAnim;
        public MainWindow()
        {
            InitializeComponent();

            textBox.Text = "D:\\TestPhoto";
            //textBox.Focus();
            listTabItem.Focus();
             
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Title = "PhotoViewer";
            Height = SystemParameters.PrimaryScreenHeight - 100;
            Width = SystemParameters.PrimaryScreenWidth - 50;

            MinHeight = 500;
            MinWidth = 930;

            mainGridAnim = new DoubleAnimation();

            mainGridAnim.From = 0.0;
            mainGridAnim.To = 1.0;
            mainGridAnim.Duration = new Duration(TimeSpan.FromSeconds(3)); 
        }

        private void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            Stopwatch sw = new Stopwatch();

            sw.Start();

            for(int i = 0; i < files.Count; i++)
                (sender as BackgroundWorker).ReportProgress(Convert.ToInt32((double)i * 100 / files.Count) + 5, new PictureListItem { Path = files[i] });

            sw.Stop();
            e.Result = sw.ElapsedTicks.ToString();
        }

        private void bgw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            statsProgBar.Value = e.ProgressPercentage;
            listBoxWithPictures.Items.Add(e.UserState);
        }

        private void bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statsTextBlock.Text = "Done in " + e.Result + " ticks. Loaded pics : " + files.Count;
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            mainGrid.BeginAnimation(Grid.OpacityProperty, mainGridAnim);
        }

        private void showButton_Click(object sender, RoutedEventArgs e)
        {
            string path = textBox.Text;
            if (path.Length != 0)
            {
                try
                {
                    files = Directory.GetFiles(path).ToList();
                    files.RemoveAll(NotAPicture);

                    BackgroundWorker bgw = new BackgroundWorker();
                    bgw.WorkerReportsProgress = true;
                    bgw.DoWork += bgw_DoWork;
                    bgw.ProgressChanged += bgw_ProgressChanged;
                    bgw.RunWorkerCompleted += bgw_RunWorkerCompleted;
                    bgw.RunWorkerAsync();
                }
                catch (DirectoryNotFoundException ex)
                {
                    MessageBox.Show("Error! Directory wasn't found!");
                    textBox.Text = "";
                    textBox.Focus();
                }
            }
            else
            {
                MessageBox.Show("Error! Path can't be empty");
                textBox.Focus();
            }
        }

        //Supportive functions
        private BitmapImage getBitmapImage(string path)
        {
            BitmapImage bit = new BitmapImage();
            bit.BeginInit();
            bit.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read);
            bit.CacheOption = BitmapCacheOption.OnLoad;
            bit.EndInit();

            bit.StreamSource.Dispose();

            return bit;
        }

        private BitmapImage getRotatedBitmapImage(string path, Rotation e)
        {
            BitmapImage bit = new BitmapImage();
            bit.BeginInit();
            bit.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read);
            bit.CacheOption = BitmapCacheOption.OnLoad;
            bit.Rotation = e;
            bit.EndInit();

            bit.StreamSource.Dispose();

            return bit;
        }

        private bool NotAPicture(string s)
        {
            return !(s.EndsWith("jpg") || s.EndsWith("png"));
        }

        // OnClick methods to buttons in controlPanel (from left to right)
        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (files != null && MessageBox.Show("Delete this picture?", "Questing", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                int target = index;
                index = (index + 1) % files.Count;

                imageBox.Source = files.Count - 1 == 0 ? null : getBitmapImage(files[index]);

                listBoxWithPictures.Items.RemoveAt(target);
                File.Delete(files[target]);
                files.RemoveAt(target);  
            }
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            if (files != null && files.Count != 0)
            {
                index = index > 0 ? index - 1 : files.Count - 1;
                imageBox.Source = getBitmapImage(files[index]);
            }
        }

        private void forwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (files != null && files.Count != 0)
            {
                index = (index + 1) % files.Count;
                imageBox.Source = getBitmapImage(files[index]);
            }
        }

        private void rotateLButton_Click(object sender, RoutedEventArgs e)
        {
            if (files != null && files.Count != 0)
            {
                TransformedBitmap transBit = new TransformedBitmap();

                transBit.BeginInit();
                transBit.Source = imageBox.Source as BitmapImage;
                transBit.Source ??= imageBox.Source as TransformedBitmap;
                transBit.Transform = new RotateTransform(-90);
                transBit.EndInit();

                imageBox.Source = transBit;
            }
        }

        private void rotateRButton_Click(object sender, RoutedEventArgs e)
        {
            if (files != null && files.Count != 0)
            {
                TransformedBitmap transBit = new TransformedBitmap();

                transBit.BeginInit();
                transBit.Source = imageBox.Source as BitmapImage;
                transBit.Source ??= imageBox.Source as TransformedBitmap;
                transBit.Transform = new RotateTransform(90);
                transBit.EndInit();

                imageBox.Source = transBit;
            }
        }

        // Methods to listBox in "listTabItem"
        private void listBoxWithPictures_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            string path = (listBoxWithPictures.SelectedItem as PictureListItem).Path;
            index = files.IndexOf(path);
            imageBox.Source = getBitmapImage(path);
            imageTabItem.Focus();
        }
    }

    class PictureListItem
    {
        //public BitmapImage Source { get; set; }
        public string Path { get; set; }
    }
}