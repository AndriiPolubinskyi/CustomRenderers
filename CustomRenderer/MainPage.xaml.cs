using System;
using Xamarin.Forms;

namespace CustomRenderer
{
	public partial class MainPage : ContentPage
	{
		public MainPage ()
		{
			InitializeComponent ();
            NavigationPage.SetHasNavigationBar(this, false);
		}

		async void OnTakePhotoButtonClicked (object sender, EventArgs e)
		{
			await Navigation.PushAsync (new CameraPage ());
		}
	}
}

