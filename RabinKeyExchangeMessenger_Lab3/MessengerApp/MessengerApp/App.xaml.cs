using Microsoft.Maui.Controls;
using Microsoft.Maui.LifecycleEvents;

namespace MessengerApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }


    }
}
