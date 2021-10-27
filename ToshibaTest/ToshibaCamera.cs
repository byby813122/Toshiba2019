using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing.Imaging;

using System.Drawing.Imaging;

using Teli.TeliCamAPI.NET;
using Teli.TeliCamAPI.NET.Utility;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace ToshibaTest
{
    class ToshibaCamera
    {
        private CameraSystem cameraSystem;
        private CameraDevice cameraDevice;

        #region Property
        private Bitmap image;
        public Bitmap Image
        {
            get => image;
            set
            {
                image = value;
            }
        }

        private string cameraState;
        private int maxPayloadSize;

        public string CameraState
        {
            get => cameraState;
            set
            {
                cameraState = value;
            }
        }
        #endregion

        public ToshibaCamera()
        {
            //cameraSystem = new CameraSystem();
            //cameraDevice = new CameraDevice();
            //Initial();
        }

        public void Initial()
        {
            CamSystemInfo sysInfo = null;
            cameraSystem.GetInformation(ref sysInfo);

            int camNum;
            cameraSystem.GetNumOfCameras(out camNum);

            CameraInfoEx camInfo = null;
            for (int i = 0; i < camNum; i++)
            {
                cameraSystem.GetCameraInformationEx(i, ref camInfo);
            }

            cameraSystem.CreateDeviceObject(0, ref this.cameraDevice); //Creat camera instance that is detected first
            cameraDevice.Open(); //Open camera

            SetFeature();
            SetSoftTriggerMode();
            OpenStream();
            StartAcquisition();
        }


        private void SetFeature()
        {
            #region ExposureTimeControl / ExposureAuto.
            // Set ExposureTimeControl "Manual", in this sample code.
            // The feature name differs depending on the camera.
            // Therefore, this sample attempt to set multiple features and ignores the error.
            cameraDevice.genApi.SetEnumStrValue("ExposureTimeControl", "Manual");
            cameraDevice.genApi.SetEnumStrValue("ExposureAuto", "Off");
            #endregion

            #region GainAuto.
            // Set GainAuto off.
            // The feature name differs depending on the camera.
            // Therefore, this sample attempt to set multiple features and ignores the error.
            cameraDevice.genApi.SetEnumStrValue("GainAuto", "Off"); //Set value
            #endregion

            #region AcquisitionFrameRateControl.
            // Set AcquisitionFrameRateControl NoSpecify.
            // The feature name differs depending on the camera.
            // Therefore, this sample attempt to set multiple features and ignores the error.
            cameraDevice.genApi.SetEnumStrValue("AcquisitionFrameRateControl", "NoSpecify"); //Set value
            #endregion
        }

        private void SetSoftTriggerMode()
        {
            // This application uses software one shot trigger mode. 
            //   TriggerMode : On.
            //   TriggerSequence : TriggerSequence0 (External edge mode). This feature does not exist in GigE Vision camera.
            //   TriggerSource   : Software.

            #region Set TriggerMode "On", in this sample code.
            cameraDevice.genApi.SetEnumStrValue("TriggerMode", "On"); //Set value
            #endregion

            #region Set TriggerSequence TriggerSequence0.
            if (this.cameraDevice.CamType == CameraType.TypeU3v)
            {
                // Trigger sequence must be set to TriggerSequence0 in software trigger mode.
                //觸發序列必須在軟件觸發模式下設置為Triggerequence0。
                cameraDevice.genApi.SetEnumStrValue("TriggerSequence", "TriggerSequence0"); //Set value
            }
            #endregion

            #region Set TriggerSource software.
            cameraDevice.genApi.SetEnumStrValue("TriggerSource", "Software"); //Set value
            #endregion
        }

        private void StartAcquisition()
        {
            cameraDevice.camStream.Start();
        }

        public void GrabOnce()
        {
            cameraDevice.genApi.ExecuteCommand("TriggerSoftware"); //Execute
        }

        public void Terminate()
        {
            StopGrab();
            CloseStream();
            cameraDevice.Close();
        }

        private void StopGrab()
        {
            cameraDevice.camStream.Stop();
        }

        private void CloseStream()
        {
            cameraDevice.camStream.Close();
        }

        private void OpenStream()
        {
            #region Open the stream interface
            cameraDevice.camStream.Open(out this.maxPayloadSize);
            #endregion

            #region Add a new event handler
            cameraDevice.camStream.ImageAcquired += new ImageAcquiredEventHandler(camStream_ImageAcquired);
            #endregion
        }

        private void camStream_ImageAcquired(CameraStream sender, ImageAcquiredEventArgs e)
        {
            if (e.ImageInfo.PixelFormat == CameraPixelFormat.Mono8 || e.ImageInfo.PixelFormat == CameraPixelFormat.BayerBG8 ||
                e.ImageInfo.PixelFormat == CameraPixelFormat.BayerGB8 || e.ImageInfo.PixelFormat == CameraPixelFormat.BayerGR8 ||
                e.ImageInfo.PixelFormat == CameraPixelFormat.BayerRG8)
            {
                // Make 8bit graysacle bitmap
                //製作8位灰色的位圖
                Image = new Bitmap(e.ImageInfo.SizeX, e.ImageInfo.SizeY, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

                // Set palette in bitmap
                //在位圖設置調色板
                System.Drawing.Imaging.ColorPalette monoPalette = Image.Palette;
                for (int i = 0; i <= 255; i++)
                {
                    monoPalette.Entries[i] = System.Drawing.Color.FromArgb(i, i, i);
                }
                Image.Palette = monoPalette;
            }
            else if (e.ImageInfo.PixelFormat == CameraPixelFormat.RGB8)
            {
                // Make 24bit RGB bitmap
                //製作24位RGB位圖
                Image = new Bitmap(e.ImageInfo.SizeX, e.ImageInfo.SizeY, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            }
        }


        private ImageSource ToBitmapSource(Bitmap bitmap)
        {
            MemoryStream stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Bmp);
            stream.Position = 0;
            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            return bitmapImage;
        }

    }
}
