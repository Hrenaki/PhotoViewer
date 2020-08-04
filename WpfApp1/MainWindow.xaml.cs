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
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Runtime.Caching;

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
        private readonly ColorAnimation colorAnimation;

        private double angle = 0;
        BackgroundWorker bgw;

        public MainWindow()
        {
            InitializeComponent();

            textBox.Text = "D:\\TestPhoto";
            listTabItem.Focus();

            Opacity = 1;
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
                Duration = new Duration(TimeSpan.FromSeconds(4))
            };

            colorAnimation = new ColorAnimation()
            {
                From = Colors.Black,
                To = Colors.White,
                Duration = new Duration(TimeSpan.FromSeconds(4))
            };
            colorAnimation.Completed += colorAnimation_Completed;

            bgw = new BackgroundWorker();
            bgw.WorkerReportsProgress = true;
            bgw.DoWork += bgw_DoWork;
            bgw.ProgressChanged += bgw_ProgressChanged;
            bgw.RunWorkerCompleted += bgw_RunWorkerCompleted;
        }

        private void colorAnimation_Completed(object sender, EventArgs e)
        {
            stackPanel.BeginAnimation(OpacityProperty, mainGridAnim);
        }

        //BackgroundWorker "bgw" methods
        private void bgw_DoWork(object sender, DoWorkEventArgs e)
        {
            Stopwatch sw = new Stopwatch();

            ListBox listBox = (e.Argument as List<object>)[0] as ListBox;
            List<string> source = (e.Argument as List<object>)[1] as List<string>;

            sw.Start();
            for (int i = 0; i < source.Count; i++)
                (sender as BackgroundWorker).ReportProgress(Convert.ToInt32(i * 100.0 / source.Count) + 3,
                    new List<object> { listBox, new PictureListItem { Path = source[i] } });
            sw.Stop();
            e.Result = new List<object>() { sw.ElapsedTicks.ToString(), listBox };
        }
        private void bgw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            statsProgBar.Value = e.ProgressPercentage;
            ((e.UserState as List<object>)[0] as ListBox).Items.Add((e.UserState as List<object>)[1] as PictureListItem);
        }

        private void bgw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            statsTextBlock.Text = "Done in " + ((e.Result as List<object>)[0] as string) + " ticks. Loaded pics : " + files.Count;

            ListBox listBox = (e.Result as List<object>)[1] as ListBox;
            listBox.UpdateLayout();
            (listBox.Parent as TabItem).Focus();

            imageBox.Source = files.Count == 0 ? null : getBitmapImage(files[0]);
        }
        //-------------------------------------------------------------------------------------------------------------------------
        
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            SolidColorBrush animatedBrush = new SolidColorBrush();
            mainGrid.Background = animatedBrush;

            BeginAnimation(OpacityProperty, mainGridAnim);
            animatedBrush.BeginAnimation(SolidColorBrush.ColorProperty, colorAnimation);
        }

        private void showButton_Click(object sender, RoutedEventArgs e)
        {

            if (textBox.Text.Length != 0)
            {
                if (!System.IO.Path.HasExtension(textBox.Text))
                {
                    if (files != null)
                        files.Clear();

                    index = 0;

                    try
                    {
                        files = Directory.GetFiles(textBox.Text).ToList();
                        files.RemoveAll(NotAPicture);

                        updateListBox(listBoxWithPictures, files);
                        tabControl.BeginAnimation(OpacityProperty, mainGridAnim);

                        listTabItem.IsEnabled = true;
                        listTabItem.Opacity = 1;

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
                    if (NotAPicture(textBox.Text) && MessageBox.Show("Do you want to open the file with associated app?", "Questing",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            Process.Start(new ProcessStartInfo(textBox.Text) { UseShellExecute = true} );
                    else
                    {
                        tabControl.BeginAnimation(OpacityProperty, mainGridAnim);
                        imageBox.Source = getBitmapImage(textBox.Text);
                        imageTabItem.Focus();

                        listTabItem.IsEnabled = false;
                        listTabItem.Opacity = 0;
                    }
                }
            }
            else
            {
                MessageBox.Show("Error! Path can't be empty");
                textBox.Focus();
            }
        }
        private void treeButton_Click(object sender, RoutedEventArgs e)
        {
            if (textBox.Text != "")
            {
                WindowWithTree treeWin = new WindowWithTree(textBox.Text, this);
                treeWin.Show();
            }
        }

        //Supportive functions
        public void setTextBoxText(string text)
        {
            textBox.Text = text;
        }
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

        private BitmapImage getBitmapImage(string path, int height, int width)
        {
            BitmapImage bit = new BitmapImage();
            bit.BeginInit();
            bit.StreamSource = new FileStream(path, FileMode.Open, FileAccess.Read);
            bit.CacheOption = BitmapCacheOption.OnLoad;
            bit.DecodePixelHeight = height;
            bit.DecodePixelWidth = width;
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

        private void updateListBox(ListBox listBox, List<string> source)
        {
            listBox.Items.Clear();
            statsProgBar.Value = 0;

            List<object> data = new List<object>() { listBox, source };
            bgw.RunWorkerAsync(data);
        }

        private void refreshPictureListItem(ListBox listBox, int index)
        {
            listBox.Items.RemoveAt(index);
            listBox.Items.Insert(index, new PictureListItem { Path = files[index] });
            listBox.UpdateLayout();          
        }
        //-------------------------------------------------------------------------------------------------------------------------

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
                {
                    savePicture(imageBox.Source as TransformedBitmap, files[index]);
                    refreshPictureListItem(listBoxWithPictures, index);
                }
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
                {
                    savePicture(imageBox.Source as TransformedBitmap, files[index]);
                    refreshPictureListItem(listBoxWithPictures, index);
                }
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
        //-------------------------------------------------------------------------------------------------------------------------

        /*private void imageTabItem_LostFocus(object sender, RoutedEventArgs e)
        {
            if((textBox.IsFocused || listTabItem.IsFocused || showbtn.IsFocused) && angle % 360 != 0)
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

        
        //-------------------------------------------------------------------------------------------------------------------------
    }

    class PictureListItem
    {
        public string Path { get; set; }
    }
}