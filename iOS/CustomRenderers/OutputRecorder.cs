using System;
using AVFoundation;
using CoreGraphics;
using CoreMedia;
using CoreVideo;
using UIKit;
using Xamarin.Forms;
using CameraPreviewSample.CustomRenderers;

namespace CameraPreviewSample.iOS.CustomRenderers
{

    public class OutputRecorder : AVCaptureVideoDataOutputSampleBufferDelegate
    {

        private UIImage GetImageFromSampleBuffer(CMSampleBuffer sampleBuffer) {

            // Get a pixel buffer from the sample buffer
            using (var pixelBuffer = sampleBuffer.GetImageBuffer() as CVPixelBuffer) {
                // Lock the base address

                pixelBuffer.Lock(CVPixelBufferLock.None);

                // Prepare to decode buffer
                var flags = CGBitmapFlags.PremultipliedFirst | CGBitmapFlags.ByteOrder32Little;

                // Decode buffer - Create a new colorspace
                using (var cs = CGColorSpace.CreateDeviceRGB()) {

                    // Create new context from buffer
                    using (var context = new CGBitmapContext(pixelBuffer.BaseAddress,
                                             pixelBuffer.Width,
                                             pixelBuffer.Height,
                                             8,
                                             pixelBuffer.BytesPerRow,
                                             cs,
                                             (CGImageAlphaInfo)flags)) {

                        // Get the image from the context
                        using (var cgImage = context.ToImage()) {

                            // Unlock and return image
                            pixelBuffer.Unlock(CVPixelBufferLock.None);
                            return UIImage.FromImage(cgImage);
                        }
                    }
                }
            }
        }

        public CameraPreview Camera { get; set; }
        private long FrameCount = 1;

        public override void DidOutputSampleBuffer(
            AVCaptureOutput captureOutput,
            CMSampleBuffer sampleBuffer,
            AVCaptureConnection connection) {

            try {
                // ここでフレーム画像を取得していろいろしたり
                //var image = GetImageFromSampleBuffer (sampleBuffer);

                //PCLプロジェクトとのやりとりやら
                Camera.Hoge = (object)(FrameCount++.ToString());

                //変更した画像をプレビューに反映させたりする

                //これがないと"Received memory warning." で落ちたり、画面の更新が止まったりする
                GC.Collect();  //  "Received memory warning." 回避

            }
            catch (Exception e) {
                Console.WriteLine("Error sampling buffer: {0}", e.Message);
            }
        }
    }
}

