using CustomRenderer.Views;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly:XamlCompilation(XamlCompilationOptions.Compile)]
namespace CustomRenderer
{
	public class App : Application
	{
        public static INavigation Navigation;
		public App ()
		{
			MainPage = new NavigationPage (new MainPageView());
            Navigation = MainPage.Navigation;
		}

		protected override void OnStart ()
		{
			// Handle when your app starts
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}
	}
}

