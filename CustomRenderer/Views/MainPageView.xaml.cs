using CustomRenderer.ViewModels;
using Xamarin.Forms;

namespace CustomRenderer.Views
{
    public partial class MainPageView : ContentPage
	{
		public MainPageView()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);
            BindingContext = new MainPageViewModel();
		}
	}
}

