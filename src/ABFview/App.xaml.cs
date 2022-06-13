using System.Windows;

namespace ABFview;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private void Application_Startup(object sender, StartupEventArgs e)
    {
        MainWindow window = new();

        if (e.Args.Length == 1)
            window.LoadAbf(e.Args[0]);

        window.Show();
    }
}
