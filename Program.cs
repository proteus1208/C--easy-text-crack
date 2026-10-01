namespace AllowedMac;

static class Program
{
    [STAThread]
    static void Main()
    {
        Guard.RefuseDebug();
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
