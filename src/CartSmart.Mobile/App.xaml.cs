namespace CartSmart.Mobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // AppShell is resolved here (after InitializeComponent has merged Application.Resources)
        // rather than injected via the constructor, because AppShell's XAML uses StaticResource
        // lookups (e.g. PageBackground) that resolve against Application.Current.Resources —
        // which isn't populated yet while App's own constructor is still running.
        var shell = _services.GetRequiredService<AppShell>();
        return new Window(shell);
    }
}
