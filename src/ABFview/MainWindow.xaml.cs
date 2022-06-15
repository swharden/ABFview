using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace ABFview;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    AbfSharp.ABF Abf;

    int CurrentSweep = 0;

    private string Version => Assembly.GetEntryAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            .InformationalVersion;

    public MainWindow()
    {
        InitializeComponent();
        HideAllOptionalGroupBoxes();

        Title = $"ABFview {Version}";

        string demoAbfFolder = System.IO.Path.GetFullPath("../../../../../dev/abfs/");
        string demoAbfFile = System.IO.Path.Combine(demoAbfFolder, "ic-ramp-ap.abf");
        if (System.IO.File.Exists(demoAbfFile))
            LoadAbf(demoAbfFile);
    }

    public void LoadAbf(string filePath)
    {
        try
        {
            Abf = new AbfSharp.ABF(filePath);
            Title = $"ABFview {Version} - {System.IO.Path.GetFileName(filePath)}";
            SetSweep(0);
            string initialViewMode = IsGapFree(Abf) ? "gapfree" : "sweep";
            SetViewMode(initialViewMode);
        }
        catch
        {
            Abf = null;
            Title = $"ABFview {Version} - ABF ERROR";
            wpfPlot1.Plot.Clear();
        }

    }

    private void SetSweep(int sweepNumber = 1)
    {
        CurrentSweep = sweepNumber;
        EphysPlot.Sweep(wpfPlot1.Plot, Abf, sweepNumber, (bool)cbDerivative.IsChecked);
        wpfPlot1.Render();
        lblSweep.Content = $"{sweepNumber + 1} of {Abf.Header.SweepCount}";
    }

    private void SetStack()
    {
        if (!double.TryParse(tbVertSep.Text, out double yOffset))
            yOffset = 0;

        EphysPlot.Stack(wpfPlot1.Plot, Abf, yOffset, (bool)cbDerivative.IsChecked);
        wpfPlot1.Render();
    }

    private void SetFull()
    {
        EphysPlot.Full(wpfPlot1.Plot, Abf, (bool)cbDerivative.IsChecked);
        wpfPlot1.Render();
    }

    private void btnSweepPrevious_Click(object sender, RoutedEventArgs e)
    {
        SetSweep(Math.Max(0, CurrentSweep - 1));
    }

    private void btnSweepNext_Click(object sender, RoutedEventArgs e)
    {
        SetSweep(Math.Min(Abf.Header.SweepCount - 1, CurrentSweep + 1));
    }

    private void btnSweepFirst_Click(object sender, RoutedEventArgs e)
    {
        SetSweep(0);
    }

    private void btnSweepLast_Click(object sender, RoutedEventArgs e)
    {
        SetSweep(Abf.Header.SweepCount - 1);
    }

    private void cbDelta_CheckChanged(object sender, RoutedEventArgs e)
    {
        double x1 = wpfPlot1.Plot.GetAxisLimits().XMin;
        double x2 = wpfPlot1.Plot.GetAxisLimits().XMax;
        SetViewMode();
        wpfPlot1.Plot.SetAxisLimits(xMin: x1, xMax: x2);
        wpfPlot1.Render();
    }

    private void HideAllOptionalGroupBoxes()
    {
        gbSweepNav.Visibility = Visibility.Collapsed;
        gbStackSettings.Visibility = Visibility.Collapsed;
        gbView.Visibility = Visibility.Collapsed;
        gbExport.Visibility = Visibility.Collapsed;
    }

    private void SetViewMode(string viewMode = null)
    {
        if (wpfPlot1 is null)
            return;

        if (viewMode is null)
            viewMode = cbView.Text;

        HideAllOptionalGroupBoxes();

        switch (viewMode)
        {
            case "sweep":
                SetSweep();
                gbSweepNav.Visibility = Visibility.Visible;
                gbView.Visibility = Visibility.Visible;
                gbExport.Visibility = Visibility.Visible;
                break;

            case "stack":
                SetStack();
                gbStackSettings.Visibility = Visibility.Visible;
                gbView.Visibility = Visibility.Visible;
                gbExport.Visibility = Visibility.Visible;
                break;

            case "full":
                SetFull();
                gbView.Visibility = Visibility.Visible;
                gbExport.Visibility = Visibility.Visible;
                break;

            case "gapfree":
                SetFull();
                gbExport.Visibility = Visibility.Visible;
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
        Microsoft.Win32.OpenFileDialog openFileDlg = new();
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
        if (Abf is null)
            return;

        string previousAbf = GetPathAdjacentAbf(Abf.Path, nextAbf: false);
        if (previousAbf != null)
            LoadAbf(previousAbf);
    }

    private void btnNextAbf_Click(object sender, RoutedEventArgs e)
    {
        if (Abf is null)
            return;

        string nextAbf = GetPathAdjacentAbf(Abf.Path, nextAbf: true);
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

    public static bool IsGapFree(AbfSharp.ABF abf)
    {
        const int GAP_FREE_MODE = 3;
        return abf.Header.nOperationMode == GAP_FREE_MODE;
    }

    private void btnExportCsv_Click(object sender, RoutedEventArgs e)
    {
        Microsoft.Win32.SaveFileDialog saveFileDialog = new()
        {
            FileName = System.IO.Path.GetFileNameWithoutExtension(Abf.Path) + ".csv",
            Filter = "Comma separated values (*.csv)|*.csv",
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            EphysPlot.SaveCSV(Abf, saveFileDialog.FileName);
        }
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            LoadAbf(files[0]);
        }
    }
}
