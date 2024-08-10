
namespace appcalorie
{
    public partial class MainPage : ContentPage
    {


        public MainPage()
        {
            InitializeComponent();
        }

        private async void OnSwiped(object sender, SwipedEventArgs e)
        {
            if (e.Direction == SwipeDirection.Up)
            {
                // Navigate to HomePage
                await Navigation.PushAsync(new HomePage());
            }
        }
    }

}
