using CameraPreviewSample.Infrastructure;
using Xamarin.Forms;

namespace CameraPreviewSample
{
    public partial class App : Application
    {
        public App() {
            InitializeComponent();

            var tabPage = new TabbedPage();

            tabPage.Children.Add(new NavigationPage (new CameraPreviewSamplePage()){ Title = "tab1" });
            tabPage.Children.Add(new NavigationPage(new ContentPage()) { Title = "tab2" });


            MainPage = tabPage;
        }

        protected override void OnStart() {
            // Handle when your app starts
        }

        protected override void OnSleep() {
            //CustomRendererのリソース解放処理を発行
            MessagingCenter.Send<LifeCyclePayload>(
                new LifeCyclePayload { Status = LifeCycle.OnSleep }, "");

        }

        protected override void OnResume() {
            //CustomRendererのリソース初期化処理を発行
            MessagingCenter.Send<LifeCyclePayload>(
                new LifeCyclePayload { Status = LifeCycle.OnResume }, "");
        }
    }
}

