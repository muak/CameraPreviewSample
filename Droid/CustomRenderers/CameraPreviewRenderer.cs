using System;
using System.Collections.Generic;
using Android.Content;
using Android.Hardware;
using Android.Views;
using CameraPreviewSample.CustomRenderers;
using CameraPreviewSample.Droid.CustomRenderers;
using CameraPreviewSample.Infrastructure;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;


[assembly: ExportRenderer(typeof(CameraPreview), typeof(CameraPreviewRenderer))]
namespace CameraPreviewSample.Droid.CustomRenderers
{
    public class CameraPreviewRenderer : ViewRenderer<CameraPreview, DroidCameraPreview>
    {
        DroidCameraPreview cameraPreview;

        protected override void OnElementChanged(ElementChangedEventArgs<CameraPreview> e) {
            base.OnElementChanged(e);

            if (Control == null) {
                cameraPreview = new DroidCameraPreview(Context, e.NewElement);
                SetNativeControl(cameraPreview);
            }

            if (e.OldElement != null) {
            }
            if (e.NewElement != null) {
            }
        }

        protected override void OnElementPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            base.OnElementPropertyChanged(sender, e);
            if (this.Element == null || this.Control == null)
                return;

            // PCL側の変更をプラットフォームに反映
            if (e.PropertyName == nameof(Element.IsPreviewing)) {
                Control.IsPreviewing = Element.IsPreviewing;
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) {
                if (Control.Preview != null) {
                    Control.Release();
                }

                MessagingCenter.Unsubscribe<LifeCyclePayload>(Control, "");
            }
            base.Dispose(disposing);
        }
    }


    public sealed class DroidCameraPreview : ViewGroup, ISurfaceHolderCallback
    {
        SurfaceView surfaceView;
        ISurfaceHolder holder;
        Camera.Size previewSize;
        IList<Camera.Size> supportedPreviewSizes;
        Camera camera;
        Context context;
        byte[] Buff;

        private PinchListener pinchlistener;
        private ScaleGestureDetector scaleGestureDetector;
        private bool surfaceCreated;

        public CameraPreviewCallback PreviewCallback { get; set; }
        public CameraPreview FormsCameraPreview { get; set; }

        bool _IsPreviewing;
        public bool IsPreviewing {
            get {
                return _IsPreviewing;
            }
            set {
                if (value) {
                    StartPreview();
                }
                else {
                    StopPreview();
                }
                _IsPreviewing = value;
            }
        }

        public Camera Preview {
            get { return camera; }
            set {
                camera = value;
                if (camera != null) {
                    supportedPreviewSizes = Preview.GetParameters().SupportedPreviewSizes;
                    RequestLayout();
                }
            }
        }

        public DroidCameraPreview(Context context, CameraPreview formsCameraPreview)
            : base(context) {
            FormsCameraPreview = formsCameraPreview;
            surfaceView = new SurfaceView(context);
            AddView(surfaceView);

            _IsPreviewing = FormsCameraPreview.IsPreviewing;
            holder = surfaceView.Holder;
            holder.AddCallback(this);

            this.context = context;

            MessagingCenter.Subscribe<LifeCyclePayload>(this, "", (p) => {
                switch (p.Status) {
                    case LifeCycle.OnSleep:
                        if (surfaceCreated) {
                            //Sleepの時にSurfaceViewが生成されていればリソース解放
                            Release();
                        }
                        break;
                    case LifeCycle.OnResume:
                        if (surfaceCreated) {
                            //Resumeの時にSurfaceViewが生成されていればリソース初期化
                            Initialize();
                        }
                        break;
                }
            });

        }

        void StopPreview() {
            if (Preview != null) {
                Preview.AddCallbackBuffer(null);
                Preview.SetPreviewCallbackWithBuffer(null);
                Preview.StopPreview();
            }
        }
        void StartPreview() {
            if (Preview != null) {
                Preview.StartPreview();
                Preview.SetPreviewCallbackWithBuffer(PreviewCallback);  //画面移動するとコールバックが無効になるので再セット
                Preview.AddCallbackBuffer(Buff);
            }
        }


        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec) {
            int width = ResolveSize(SuggestedMinimumWidth, widthMeasureSpec);
            int height = ResolveSize(SuggestedMinimumHeight, heightMeasureSpec);
            SetMeasuredDimension(width, height);
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b) {
            var msw = MeasureSpec.MakeMeasureSpec(r - l, MeasureSpecMode.Exactly);
            var msh = MeasureSpec.MakeMeasureSpec(b - t, MeasureSpecMode.Exactly);

            surfaceView.Measure(msw, msh);
            surfaceView.Layout(0, 0, r - l, b - t);
        }

        public override bool OnTouchEvent(MotionEvent e) {
            //ピンチジェスチャー追加
            return scaleGestureDetector.OnTouchEvent(e);
        }

        public void SurfaceCreated(ISurfaceHolder holder) {
            surfaceCreated = true;
        }

        public void SurfaceChanged(ISurfaceHolder holder, Android.Graphics.Format format, int width, int height) {
            if (Preview == null) {
                Initialize();
            }
        }

        public void SurfaceDestroyed(ISurfaceHolder holder) {
            if (Preview != null) {
                Release();
            }
            surfaceCreated = false;
        }

        public void Release() {

            Preview.StopPreview();
            PreviewCallback.Dispose();
            Preview.AddCallbackBuffer(null);
            Preview.SetPreviewCallbackWithBuffer(null);
            pinchlistener.Dispose();
            scaleGestureDetector.Dispose();

            Preview.Release();
            Preview = null;

        }

        public void Initialize() {

            Preview = Camera.Open((int)FormsCameraPreview.Camera);

            //Portrait固定
            Preview.SetDisplayOrientation(90);

            var parameters = Preview.GetParameters();


            //プレビューサイズ設定
            if (supportedPreviewSizes != null) {
                previewSize = GetOptimalPreviewSize(supportedPreviewSizes, surfaceView.Width, surfaceView.Height);
            }
            parameters.SetPreviewSize(previewSize.Width, previewSize.Height);

            //フレームレート設定
            parameters.SetPreviewFpsRange(10000, 24000);


            Preview.SetParameters(parameters);
            RequestLayout();

            //フレーム処理用バッファの作成
            int size = previewSize.Width * previewSize.Height * Android.Graphics.ImageFormat.GetBitsPerPixel(Android.Graphics.ImageFormat.Nv21) / 8;
            Buff = new byte[size];
            //フレーム処理用のコールバック生成
            PreviewCallback = new CameraPreviewCallback { CameraPreview = FormsCameraPreview, Buff = Buff };

            Preview.SetPreviewCallbackWithBuffer(PreviewCallback);
            Preview.AddCallbackBuffer(Buff);

            //ピンチジェスチャー登録処理
            pinchlistener = new PinchListener { camera = Preview, PreviewCallback = PreviewCallback, buff = Buff };
            scaleGestureDetector = new ScaleGestureDetector(context, pinchlistener);

            Preview.SetPreviewDisplay(holder);

            if (IsPreviewing) {
                StartPreview();
            }

        }

        class PinchListener : ScaleGestureDetector.SimpleOnScaleGestureListener
        {
            public Camera camera { get; set; }
            public byte[] buff { get; set; }
            public CameraPreviewCallback PreviewCallback { get; set; }

            public override bool OnScale(ScaleGestureDetector detector) {

                var param = camera.GetParameters();

                if (Math.Abs(detector.ScaleFactor - 1.0f) < 0.01f) {
                    return base.OnScale(detector);
                }
                if (detector.ScaleFactor > 1.0) {
                    param.Zoom += (int)Math.Round(2.0 * detector.ScaleFactor, 0);

                    if (param.Zoom == 0) {
                        param.Zoom = 2;
                    }
                    if (param.Zoom > param.MaxZoom) {
                        param.Zoom = param.MaxZoom;
                    }
                }
                else {
                    //param.Zoom -= 3;
                    param.Zoom -= (int)Math.Round(4.0 * detector.ScaleFactor, 0);
                    if (param.Zoom < 0) {
                        param.Zoom = 0;
                    }
                }

                camera.SetParameters(param);

                return base.OnScale(detector);
            }
            public override bool OnScaleBegin(ScaleGestureDetector detector) {
                camera.AddCallbackBuffer(null);
                camera.SetPreviewCallbackWithBuffer(null);
                return base.OnScaleBegin(detector);
            }
            public override void OnScaleEnd(ScaleGestureDetector detector) {
                camera.SetPreviewCallbackWithBuffer(PreviewCallback);
                camera.AddCallbackBuffer(buff);
                base.OnScaleEnd(detector);
            }
        }

        private Camera.Size GetOptimalPreviewSize(IList<Camera.Size> sizes, int w, int h) {
            double AspectTolerance = 0.1;
            double targetRatio = (double)w / h;

            if (sizes == null) {
                return null;
            }

            Camera.Size optimalSize = null;
            double minDiff = double.MaxValue;

            int targetHeight = h;

            foreach (Camera.Size size in sizes) {
                double ratio = (double)size.Height / size.Width;    //Portraitは縦横逆


                if (Math.Abs(ratio - targetRatio) > AspectTolerance)
                    continue;
                if (Math.Abs(size.Width - targetHeight) < minDiff) {
                    optimalSize = size;
                    minDiff = Math.Abs(size.Width - targetHeight);
                }

            }

            if (optimalSize == null) {
                minDiff = double.MaxValue;
                foreach (Camera.Size size in sizes) {
                    if (Math.Abs(size.Width - targetHeight) < minDiff) {
                        optimalSize = size;
                        minDiff = Math.Abs(size.Width - targetHeight);
                    }
                }
            }

            return optimalSize;
        }


    }
}