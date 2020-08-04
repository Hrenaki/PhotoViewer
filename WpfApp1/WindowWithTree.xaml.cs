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
    /// Логика взаимодействия для WindowWithTree.xaml
    /// </summary>
    public partial class WindowWithTree : Window
    {
        private string root;
        private SolidColorBrush fileBrush = new SolidColorBrush { Color = Colors.Green },
            directoryBrush = new SolidColorBrush { Color = Colors.Red };
        private Window parent;

        public WindowWithTree(string root, Window parent)
        {
            InitializeComponent();
            this.root = root;
            this.parent = parent;

            if (root.Length != 0)
            {
                try
                {                    
                    TreeViewItem rootTreeViewItem, item;

                    SolidColorBrush dirBrush = new SolidColorBrush(Colors.Red),
                        fileBrush = new SolidColorBrush(Colors.Green);

                    string[] splittedRoot = root.Split('\\');
                    rootTreeViewItem = new TreeViewItem() { Header = splittedRoot[0], Foreground = directoryBrush};
                    item = rootTreeViewItem;

                    for(int i = 1; i < splittedRoot.Length; i++)
                    {
                        item.Items.Add(new TreeViewItem() { Header = splittedRoot[i], Foreground = directoryBrush });
                        item = item.Items[0] as TreeViewItem;
                    }
                    
                    treeView.Items.Add(rootTreeViewItem);
                } 
                catch (Exception)
                {
                    MessageBox.Show("Tree building is FAILED! Check the path and try again.");
                }
            }
            else MessageBox.Show("Tree building is FAILED! Check the path!");

            //Configuring window
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void getDirectoriesAndFiles(string path, TreeViewItem root)
        {
            bool flag = false;
            int i;

            try
            {
                foreach (var file in Directory.GetFiles(path).ToList())
                {
                    TreeViewItem item = new TreeViewItem() { Header = System.IO.Path.GetFileName(file), Foreground = fileBrush };

                    if (root.Items.Count != 0)
                        for (i = 0; i < root.Items.Count; i++)
                            if ((root.Items[i] as TreeViewItem).Header as string == item.Header as string)
                            {
                                flag = true;
                                i = root.Items.Count;
                            }

                    if (!flag)
                        root.Items.Add(item);

                    flag = false;
                }
            }
            catch (Exception) { }

            flag = false;
            try
            {
                foreach (var directory in Directory.GetDirectories(path).ToList())
                {
                    TreeViewItem item = new TreeViewItem() { Header = getDirectoryName(directory, '\\'), Foreground = directoryBrush };
                    
                    if(root.Items.Count != 0)
                        for(i = 0; i < root.Items.Count; i++)
                            if((root.Items[i] as TreeViewItem).Header as string == item.Header as string)
                            {
                                flag = true;
                                i = root.Items.Count;
                            }

                    if (!flag)
                        root.Items.Add(item);
                    flag = false;
                }
            }
            catch (Exception) { }

            root.Items.SortDescriptions.Clear();
            root.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
        }
        
        private void treeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(treeView.SelectedItem != null)
            {
                TreeViewItem least = treeView.SelectedItem as TreeViewItem;
                List<string> splittedPath = new List<string>() { least.Header as string};
                TreeViewItem temp = least;
                 
                int i = 1;
                while(temp.Parent as TreeViewItem != null)
                {
                    splittedPath.Add((temp.Parent as TreeViewItem).Header as string);
                    temp = temp.Parent as TreeViewItem;
                    i++;
                }

                string path = splittedPath[splittedPath.Count - 1];

                for(i = splittedPath.Count - 2; i >= 0; i--)
                    path += "\\" + splittedPath[i];
                
                getDirectoriesAndFiles(path, treeView.SelectedItem as TreeViewItem);
            }          
        }
        private void Window_OnLoaded(object sender, RoutedEventArgs e)
        {
            TreeViewItem least = treeView.Items[0] as TreeViewItem;
            least.IsExpanded = true;

            string[] splittedPath = root.Split('\\');

            if(splittedPath[1] != "")
                while (least.Items.Count != 0)
                {
                    least = least.Items.GetItemAt(0) as TreeViewItem;
                    least.IsExpanded = true;
                }

            getDirectoriesAndFiles(root, least);
        }

        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            if (treeView.SelectedItem != null)
            {
                TreeViewItem least = treeView.SelectedItem as TreeViewItem;
                List<string> splittedPath = new List<string>() { least.Header as string };

                TreeViewItem temp = least;
                int i = 1;
                while (temp.Parent as TreeViewItem != null)
                {
                    splittedPath.Add((temp.Parent as TreeViewItem).Header as string);
                    temp = temp.Parent as TreeViewItem;
                    i++;
                }

                string path = splittedPath[splittedPath.Count - 1];

                if (path.EndsWith('\\'))
                    path = path.Remove(path.Length - 1);

                for (i = splittedPath.Count - 2; i >= 0; i--)
                    path += '\\' + splittedPath[i];

                (parent as MainWindow).setTextBoxText(path);

                Close();
            }
        }

        private string getDirectoryName(string path, char separator)
        {
            string[] splittedPath = path.Split(separator);
            return splittedPath[splittedPath.Length - 1];
        }
    }
}