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
        private double angle = 0;
        public MainWindow()
        {
            InitializeComponent();

            textBox.Text = "D:\\TestPhoto";
            listTabItem.Focus();

            Title = "PhotoViewer";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Height = SystemParameters.PrimaryScreenHeight - 100;
            Width = SystemParameters.PrimaryScreenWidth - 50;

            MinHeight = 500;
            MinWidth = 930;

            mainGridAnim = new DoubleAnimation()
            {
                From = 0.0,
                To = 1.0,
                Duration = new Duration(TimeSpan.FromSeconds(3))
            };
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
            imageBox.Source = getBitmapImage(files[index]);
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            stackPanel.BeginAnimation(OpacityProperty, mainGridAnim);
        }

        private void showButton_Click(object sender, RoutedEventArgs e)
        {
            if (textBox.Text.Length != 0)
            {
                listBoxWithPictures.Items.Clear();

                try
                {
                    files = Directory.GetFiles(textBox.Text).ToList();
                    files.RemoveAll(NotAPicture);

                    BackgroundWorker bgw = new BackgroundWorker();
                    bgw.WorkerReportsProgress = true;
                    bgw.DoWork += bgw_DoWork;
                    bgw.ProgressChanged += bgw_ProgressChanged;
                    bgw.RunWorkerCompleted += bgw_RunWorkerCompleted;
                    bgw.RunWorkerAsync();

                    tabControl.BeginAnimation(OpacityProperty, mainGridAnim);
                }
                catch (DirectoryNotFoundException)
                {
                    MessageBox.Show("Error! Directory wasn't found!");
                    textBox.Clear();
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

        private TransformedBitmap getRotatedTransformedBitmapFromImageSource(ImageSource imageSource, double angle)
        {
            TransformedBitmap transBit = new TransformedBitmap();

            transBit.BeginInit();
            transBit.Source = imageSource as BitmapImage;
            transBit.Source ??= imageSource as TransformedBitmap;
            transBit.Transform = new RotateTransform(angle);
            transBit.EndInit();

            return transBit;
        }

        private bool NotAPicture(string s)
        {
            return !(s.EndsWith("jpg") || s.EndsWith("png"));
        }

        private void savePicture(BitmapSource imageSource, string path)
        {
            var frame = BitmapFrame.Create(imageSource);
            BitmapEncoder encoder = getEncoder(System.IO.Path.GetExtension(path));

            if (encoder != null)
            {
                encoder.Frames.Add(frame);

                try
                {
                    using (FileStream fs = new FileStream(path, FileMode.Create))
                    {
                        encoder.Save(fs);
                    }
                }
                catch(Exception)
                {
                    MessageBox.Show("Error! Image can't be saved!");
                }
            }
            else MessageBox.Show("Pizdec programme!");
        }

        private BitmapEncoder getEncoder(string extention)
        {
            switch (extention)
            {
                case ".jpg":
                    return new JpegBitmapEncoder();
                case ".png":
                    return new PngBitmapEncoder();
                case ".tiff":
                    return new TiffBitmapEncoder();
                case ".gif":
                    return new GifBitmapEncoder();
                default: return null;
            }
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
                angle = 0;
            }
        }
        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            if (files != null && files.Count != 0)
            {
                if (angle % 360 != 0 &&
                    MessageBox.Show("Do you want to apply changes?", "Questing", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    savePicture(imageBox.Source as TransformedBitmap, files[index]);
                angle = 0;
                
                index = index > 0 ? index - 1 : files.Count - 1;
                imageBox.Source = getBitmapImage(files[index]);
            }
        }
        private void forwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (files != null && files.Count != 0)
            {
                if (angle % 360 != 0 &&
                    MessageBox.Show("Do you want to apply changes?", "Questing", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    savePicture(imageBox.Source as TransformedBitmap, files[index]);
                angle = 0;

                index = (index + 1) % files.Count;
                imageBox.Source = getBitmapImage(files[index]);
            }
        }
        private void rotateLButton_Click(object sender, RoutedEventArgs e)
        {
            if (files != null && files.Count != 0)
            {
                imageBox.Source = getRotatedTransformedBitmapFromImageSource(imageBox.Source, -90);
                angle += -90;
            }
        }

        private void rotateRButton_Click(object sender, RoutedEventArgs e)
        {
            if (files != null && files.Count != 0)
            {
                imageBox.Source = getRotatedTransformedBitmapFromImageSource(imageBox.Source, 90);
                angle += 90;
            }
        }

        /*private void imageTabItem_LostFocus(object sender, RoutedEventArgs e)
        {
            if((stackPanel.IsFocused || listTabItem.IsFocused) && angle % 360 != 0)
            {
                imageTabItem.Focus();
                if(MessageBox.Show("Do you want to apply changes?", "Questing", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    savePicture(imageBox.Source as TransformedBitmap, files[index]);
                    angle = 0;
                }
            }
        }*/

        // Methods to listBox
        private void listBoxWithPictures_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            string path = (listBoxWithPictures.SelectedItem as PictureListItem).Path;
            index = files.IndexOf(path);
            imageBox.Source = getBitmapImage(path);
            imageTabItem.Focus();
            angle = 0;
        }

        private void openMenuItem_Click(object sender, RoutedEventArgs e)
        {
            string path = (listBoxWithPictures.SelectedItem as PictureListItem).Path;
            index = files.IndexOf(path);
            imageBox.Source = getBitmapImage(path);
            imageTabItem.Focus();
            angle = 0;
        }

        private void deleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show("Delete this picture?", "Questing", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                int target = index;
                index = (index + 1) % files.Count;

                imageBox.Source = files.Count - 1 == 0 ? null : getBitmapImage(files[index]);

                listBoxWithPictures.Items.RemoveAt(target);
                File.Delete(files[target]);
                files.RemoveAt(target);
                angle = 0;
            }
        }
    }

    class PictureListItem
    {
        public string Path { get; set; }
    }
}