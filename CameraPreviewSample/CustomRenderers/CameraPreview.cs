using Xamarin.Forms;

namespace CameraPreviewSample.CustomRenderers
{
    public class CameraPreview : View
	{
		public static readonly BindableProperty CameraProperty = BindableProperty.Create(
			propertyName: "Camera",
			returnType: typeof(CameraOptions),
			declaringType: typeof(CameraPreview),
			defaultValue: CameraOptions.Rear);

		public CameraOptions Camera {
			get { return (CameraOptions)GetValue(CameraProperty); }
			set { SetValue(CameraProperty, value); }
		}

		public static readonly BindableProperty IsPreviewingProperty = BindableProperty.Create(
			propertyName: "IsPreviewing",
			returnType: typeof(bool),
			declaringType: typeof(CameraPreview),
			defaultValue: false);

		public bool IsPreviewing {
			get { return (bool)GetValue(IsPreviewingProperty); }
			set { SetValue(IsPreviewingProperty, value); }
		}

        public static readonly BindableProperty HogeProperty = BindableProperty.Create(
            propertyName: "Hoge",
            returnType: typeof(object),
            declaringType: typeof(CameraPreview),
            defaultValue: null);

        public object Hoge {
            get { return (object)GetValue(HogeProperty); }
            set { SetValue(HogeProperty, value); }
        }
	}


	public enum CameraOptions
	{
		Rear,
		Front
	}

}

