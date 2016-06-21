using Xamarin.Forms;

namespace CameraPreviewSample
{
    public partial class CameraPreviewSamplePage : ContentPage
    {
        public CameraPreviewSamplePage() {
            InitializeComponent();


            this.Disappearing += (sender, e) => {
                //画面が非表示の時はプレビューを止める
                this.CameraPreview.IsPreviewing = false;
            };

            this.Appearing += (sender, e) => {
                //画面が表示されたらプレビューを開始する
                this.CameraPreview.IsPreviewing = true;
            };
        }

        async void Handle_Clicked(object sender, System.EventArgs e) {
            await Navigation.PushAsync(new ContentPage { Title = "空のページ" });
        }
    }
}

