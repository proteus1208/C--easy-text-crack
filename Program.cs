namespace AllowedMac;

static class Program
{
    [STAThread]
    static void Main()
    {
        Guard.RefuseDebug();
        Run();
        LoadProfile();
    }

    public static void Run()
    {
    }

    static void LoadProfile()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
