using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;
using System.Timers;
using System.Windows.Forms;
using Microsoft.Kinect.Toolkit.Interaction;

namespace WpfApplication1
{
    public class DummyInteractionClient : IInteractionClient
    {
        public InteractionInfo GetInteractionInfoAtLocation(
            int skeletonTrackingId, InteractionHandType handType, 
            double x, double y)
        {
            var result = new InteractionInfo();
            result.IsGripTarget = true;
            result.IsPressTarget = true;
            result.PressAttractionPointX = 0.5;
            result.PressAttractionPointY = 0.5;
            result.PressTargetControlId = 1;
     
            return result;
        }
    }

    public partial class MainApp : Window
    {
        KinectSensor kinect;
        InteractionStream interStream;
        Skeleton[] skeletons; 
        UserInfo[] userInfos; 
        public MainApp(KinectSensor sensor) : this()
        {
            kinect = sensor;
            skeletons = new Skeleton[kinect.SkeletonStream.FrameSkeletonArrayLength];
            userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];

            kinect.DepthStream.Range = DepthRange.Near;
            kinect.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
            kinect.DepthFrameReady += kinect_DepthFrameReady;

            kinect.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
            kinect.SkeletonStream.EnableTrackingInNearRange = true;
            kinect.SkeletonStream.Enable();
            kinect.SkeletonFrameReady += kinect_SkeletonFrameReady;
  
            interStream = new InteractionStream(kinect, new DummyInteractionClient());
            interStream.InteractionFrameReady += interStream_InteractionFrameReady;
           
            kinect.Start();
        }
        public MainApp()
        {
            InitializeComponent();
        }

        void interStream_InteractionFrameReady(object sender, InteractionFrameReadyEventArgs e)
        {
            using (var iaf = e.OpenInteractionFrame())
            {
                if (iaf == null)
                    return;

                iaf.CopyInteractionDataTo(userInfos);

                users.Text = "找到的使用者ID有: ";
                foreach (var userInfo in userInfos)
                {
                    if( userInfo.SkeletonTrackingId > 0 )
                        users.Text += userInfo.SkeletonTrackingId + ",";
                }
            }
        }

        void kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {

                if (depthFrame == null)
                    return;

                interStream.ProcessDepth(depthFrame.GetRawPixelData(), depthFrame.Timestamp);
            }
        }

        void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skf = e.OpenSkeletonFrame())
            {
                if (skf == null)
                    return;

                skf.CopySkeletonDataTo(skeletons);
                var accelerometerReading = kinect.AccelerometerGetCurrentReading();
                interStream.ProcessSkeleton(skeletons, accelerometerReading, skf.Timestamp);
            }
        }
        



    }
}
