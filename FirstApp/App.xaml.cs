namespace FirstApp;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // No Shell: a single page needs no navigation infrastructure (faster start, smaller app)
        var page = new MainPage();
        var window = new Window(page) { Title = "Resistor Calculator" };
        window.Stopped += (_, _) => page.SaveState();
        return window;
    }
}
