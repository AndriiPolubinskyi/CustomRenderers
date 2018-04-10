using MvvmHelpers;
using Xamarin.Forms;

namespace CustomRenderer.ViewModels
{
    public abstract class BaseViewModelExt : BaseViewModel
    {
        protected INavigation Navigation { get { return App.Navigation; } }

        public virtual void OnShow()
        {
            // do nothing
        }

        public virtual bool BackButtonPressed()
        {
            return true;
        }
    }

}
