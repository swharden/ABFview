using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace ABFview
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            string demoAbfFolder = System.IO.Path.GetFullPath("../../../../../dev/abfs/");
            string demoAbfFile = System.IO.Path.Combine(demoAbfFolder, "ic-ramp-ap.abf");

            if (IntPtr.Size != 4)
                throw new Exception("Must build as 32-bit (x86)");

            gbSweepNav.Visibility = Visibility.Collapsed;
            gbStackSettings.Visibility = Visibility.Collapsed;
            gbView.Visibility = Visibility.Collapsed;
            btnNextAbf.Visibility = Visibility.Collapsed;
            btnPreviousAbf.Visibility = Visibility.Collapsed;

            wpfPlot1.Plot.AxisAuto();
            wpfPlot1.Render();

            LoadAbf(demoAbfFile);
        }

        AbfSharp.ABF abf;
        private void LoadAbf(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                return;

            if (abf != null)
            {
                Debug.WriteLine($"Closing old ABF...");
            }

            Debug.WriteLine($"Loading ABF: {filePath}");
            abf = new AbfSharp.ABF(filePath);
            Title = $"ABFview {System.IO.Path.GetFileName(filePath)}";
            SetViewMode("sweep");
            SetSweep(0);

            gbView.Visibility = Visibility.Visible;
            btnNextAbf.Visibility = Visibility.Visible;
            btnPreviousAbf.Visibility = Visibility.Visible;
        }

        int currentSweep = 0;

        private void SetSweep(int sweepNumber = 1)
        {
            currentSweep = sweepNumber;
            EphysPlot.Sweep(wpfPlot1.Plot, abf, sweepNumber, (bool)cbDerivative.IsChecked);
            wpfPlot1.Render();
            lblSweep.Content = $"{sweepNumber + 1} of {abf.Header.SweepCount}";
        }

        private void SetStack()
        {
            if (!double.TryParse(tbVertSep.Text, out double yOffset))
                yOffset = 0;

            EphysPlot.Stack(wpfPlot1.Plot, abf, yOffset, (bool)cbDerivative.IsChecked);
            wpfPlot1.Render();
        }

        private void SetFull()
        {
            EphysPlot.Full(wpfPlot1.Plot, abf, (bool)cbDerivative.IsChecked);
            wpfPlot1.Render();
        }

        private void btnSweepPrevious_Click(object sender, RoutedEventArgs e)
        {
            SetSweep(Math.Max(0, currentSweep - 1));
        }

        private void btnSweepNext_Click(object sender, RoutedEventArgs e)
        {
            SetSweep(Math.Min(abf.Header.SweepCount - 1, currentSweep + 1));
        }

        private void btnSweepFirst_Click(object sender, RoutedEventArgs e)
        {
            SetSweep(0);
        }

        private void btnSweepLast_Click(object sender, RoutedEventArgs e)
        {
            SetSweep(abf.Header.SweepCount - 1);
        }

        private void cbDelta_CheckChanged(object sender, RoutedEventArgs e)
        {
            double x1 = wpfPlot1.Plot.GetAxisLimits().XMin;
            double x2 = wpfPlot1.Plot.GetAxisLimits().XMax;
            SetViewMode();
            wpfPlot1.Plot.SetAxisLimits(xMin: x1, xMax: x2);
            wpfPlot1.Render();
        }

        private void SetViewMode(string viewMode = null)
        {
            if (wpfPlot1 is null)
                return;

            if (viewMode is null)
                viewMode = cbView.Text;

            // start by hiding all custom panels
            gbSweepNav.Visibility = Visibility.Collapsed;
            gbStackSettings.Visibility = Visibility.Collapsed;

            // reveal relevant panels only
            switch (viewMode)
            {
                case "sweep":
                    SetSweep();
                    gbSweepNav.Visibility = Visibility.Visible;
                    break;

                case "stack":
                    SetStack();
                    gbStackSettings.Visibility = Visibility.Visible;
                    break;

                case "full":
                    SetFull();
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void cbView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string viewMode = ((sender as ComboBox).SelectedItem as ComboBoxItem).Content as string;
            if (wpfPlot1 != null && viewMode != null)
                SetViewMode(viewMode);
        }

        private void btnLoadAbf_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDlg = new Microsoft.Win32.OpenFileDialog();
            openFileDlg.DefaultExt = ".abf";
            openFileDlg.Filter = "ABF Files|*.abf";
            if (openFileDlg.ShowDialog() == true)
            {
                string filePath = System.IO.Path.GetFullPath(openFileDlg.FileName);
                LoadAbf(filePath);
            }
        }

        private void tbVertSep_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetViewMode();
        }

        private string GetPathAdjacentAbf(string pathAbf, bool nextAbf = true)
        {
            pathAbf = System.IO.Path.GetFullPath(pathAbf);
            string abfFolder = System.IO.Path.GetDirectoryName(pathAbf);

            string[] abfsInFolder = System.IO.Directory.GetFiles(abfFolder, "*.abf");
            Array.Sort(abfsInFolder);
            for (int i = 0; i < abfsInFolder.Length; i++)
                abfsInFolder[i] = abfsInFolder[i].ToUpper();

            int index = Array.IndexOf(abfsInFolder, pathAbf.ToUpper());

            if (nextAbf)
                index += 1;
            else
                index -= 1;

            if ((index >= 0) && (index < abfsInFolder.Length))
                return abfsInFolder[index];
            else
                return null;
        }

        private void btnPreviousAbf_Click(object sender, RoutedEventArgs e)
        {
            string previousAbf = GetPathAdjacentAbf(abf.Path, nextAbf: false);
            if (previousAbf != null)
                LoadAbf(previousAbf);
        }

        private void btnNextAbf_Click(object sender, RoutedEventArgs e)
        {
            string nextAbf = GetPathAdjacentAbf(abf.Path, nextAbf: true);
            if (nextAbf != null)
                LoadAbf(nextAbf);
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files[0].ToUpper().EndsWith("ABF"))
                    LoadAbf(files[0]);
            }
        }
    }
}
