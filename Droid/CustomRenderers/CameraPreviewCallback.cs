using System;
using System.IO;
using Android.Graphics;
using CameraPreviewSample.CustomRenderers;
using Xamarin.Forms;
using static Android.Hardware.Camera;

namespace CameraPreviewSample.Droid.CustomRenderers
{
    public class CameraPreviewCallback : Java.Lang.Object, IPreviewCallback
    {


        private long FrameCount = 1;
        public CameraPreview CameraPreview { get; set;}
        public byte[] Buff { get; set;}

        public void OnPreviewFrame(byte[] data, Android.Hardware.Camera camera) {

            //ここでフレーム画像データを加工したり情報を取得したり

            //PCLプロジェクトとのやりとりやら
            CameraPreview.Hoge = (object)(this.FrameCount++.ToString());

            //変更した画像をプレビューに反映させたりする

            //次のバッファをセット
            camera.AddCallbackBuffer(Buff);
        }

    }
}

