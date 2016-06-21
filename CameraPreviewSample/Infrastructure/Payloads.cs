using Xamarin.Forms;

namespace CameraPreviewSample.Infrastructure
{
    //CustomRendererと通信するためのコンテナ
	public class LifeCyclePayload
	{
		public LifeCycle Status { get; set; }
	}
	public enum LifeCycle
	{
		OnStart,
		OnSleep,
		OnResume
	}
}

