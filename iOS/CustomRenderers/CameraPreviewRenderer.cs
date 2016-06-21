using System;
using System.Diagnostics;
using System.Linq;
using AVFoundation;
using CameraPreviewSample.CustomRenderers;
using CameraPreviewSample.Infrastructure;
using CameraPreviewSample.iOS.CustomRenderers;
using CoreFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

[assembly: ExportRenderer(typeof(CameraPreview), typeof(CameraPreviewRenderer))]
namespace CameraPreviewSample.iOS.CustomRenderers
{
    public class CameraPreviewRenderer : ViewRenderer<CameraPreview, UICameraPreview>
    {
        UICameraPreview uiCameraPreview;

        protected override void OnElementChanged(ElementChangedEventArgs<CameraPreview> e) {
            base.OnElementChanged(e);


            if (Control == null) {
                uiCameraPreview = new UICameraPreview(e.NewElement);
                SetNativeControl(uiCameraPreview);
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
                Control.Release();
                Control.CaptureSession.Dispose();
                MessagingCenter.Unsubscribe<LifeCyclePayload>(Control, "");
                Control.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class UICameraPreview : UIView
    {
        AVCaptureVideoPreviewLayer previewLayer;
        CameraOptions cameraOptions;

        public event EventHandler<EventArgs> Tapped;

        public AVCaptureSession CaptureSession { get; private set; }

        public AVCaptureDeviceInput Input { get; set; }
        public AVCaptureVideoDataOutput Output { get; private set; }
        public OutputRecorder Recorder { get; set; }
        public DispatchQueue Queue { get; set; }
        public AVCaptureDevice MainDevice { get; private set; }

        private UIPinchGestureRecognizer Pinch;

        private CameraPreview Camera;
        private float MaxZoom;
        private float MinZoom = 1.0f;

        private bool _IsPreviewing;
        public bool IsPreviewing {
            get { return _IsPreviewing; }
            set {
                if (value) {
                    CaptureSession.StartRunning();
                }
                else {
                    CaptureSession.StopRunning();
                }
                _IsPreviewing = value;
            }
        }


        public UICameraPreview(CameraPreview camera) {
            cameraOptions = camera.Camera;
            _IsPreviewing = camera.IsPreviewing;
            Camera = camera;

            CaptureSession = new AVCaptureSession();
            previewLayer = new AVCaptureVideoPreviewLayer(CaptureSession) {
                Frame = Bounds,
                VideoGravity = AVLayerVideoGravity.ResizeAspectFill
            };
            Layer.AddSublayer(previewLayer);

            Initialize();

            MessagingCenter.Subscribe<LifeCyclePayload>(this, "", (p) => {
                switch (p.Status) {
                    case LifeCycle.OnSleep:
                        //Sleep状態になるときにリソース解放
                        Release();
                        break;
                    case LifeCycle.OnResume:
                        //Resume状態になるときに初期化
                        Initialize();
                        break;
                }
            });
        }

        public override void Draw(CGRect rect) {
            base.Draw(rect);
            previewLayer.Frame = rect;
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt) {
            base.TouchesBegan(touches, evt);
            OnTapped();
        }

        protected virtual void OnTapped() {
            var eventHandler = Tapped;
            if (eventHandler != null) {
                eventHandler(this, new EventArgs());
            }
        }

        public void Release() {
            CaptureSession.StopRunning();
            Recorder.Dispose();
            Queue.Dispose();
            CaptureSession.RemoveOutput(Output);
            CaptureSession.RemoveInput(Input);
            Output.Dispose();
            Input.Dispose();
            MainDevice.Dispose();
            this.RemoveGestureRecognizer(Pinch);
        }

        private void Initialize() {
            //Pinchジェスチャ登録
            SetPinchGesture();

            //デバイス設定
            var videoDevices = AVCaptureDevice.DevicesWithMediaType(AVMediaType.Video);
            var cameraPosition = (cameraOptions == CameraOptions.Front) ? AVCaptureDevicePosition.Front : AVCaptureDevicePosition.Back;
            MainDevice = videoDevices.FirstOrDefault(d => d.Position == cameraPosition);

            NSError device_error;
            MainDevice.LockForConfiguration(out device_error);
            if (device_error != null) {
                Console.WriteLine($"Error: {device_error.LocalizedDescription}");
                MainDevice.UnlockForConfiguration();
                return;
            }
            //フレームレート設定
            MainDevice.ActiveVideoMinFrameDuration = new CMTime(1, 24);
            MainDevice.UnlockForConfiguration();

            if (MainDevice == null) {
                return;
            }

            //max zoom
            MaxZoom = (float)Math.Min(MainDevice.ActiveFormat.VideoMaxZoomFactor, 6);

            //入力設定
            NSError error;
            Input = new AVCaptureDeviceInput(MainDevice, out error);
            CaptureSession.AddInput(Input);

            //出力設定
            Output = new AVCaptureVideoDataOutput();

            //フレーム処理用
            Queue = new DispatchQueue("myQueue");
            Output.AlwaysDiscardsLateVideoFrames = true;
            Recorder = new OutputRecorder() { Camera = Camera };
            Output.SetSampleBufferDelegate(Recorder, Queue);
            var vSettings = new AVVideoSettingsUncompressed();
            vSettings.PixelFormatType = CVPixelFormatType.CV32BGRA;
            Output.WeakVideoSettings = vSettings.Dictionary;

            CaptureSession.AddOutput(Output);

            if (IsPreviewing) {
                CaptureSession.StartRunning();
            }


        }

        private void SetPinchGesture() {
            nfloat lastscale = 1.0f;
            Pinch = new UIPinchGestureRecognizer((e) => {
                if (e.State == UIGestureRecognizerState.Changed) {
                    NSError device_error;
                    MainDevice.LockForConfiguration(out device_error);
                    if (device_error != null) {
                        Console.WriteLine($"Error: {device_error.LocalizedDescription}");
                        MainDevice.UnlockForConfiguration();
                        return;
                    }
                    var scale = e.Scale + (1 - lastscale);
                    var zoom = MainDevice.VideoZoomFactor * scale;
                    if (zoom > MaxZoom) zoom = MaxZoom;
                    if (zoom < MinZoom) zoom = MinZoom;
                    MainDevice.VideoZoomFactor = zoom;
                    MainDevice.UnlockForConfiguration();
                    lastscale = e.Scale;
                }
                else if (e.State == UIGestureRecognizerState.Ended) {
                    lastscale = 1.0f;
                }
            });
            this.AddGestureRecognizer(Pinch);

        }
    }
}

