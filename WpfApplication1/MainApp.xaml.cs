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
        public MainApp(KinectSensor sensor) : this()
        {
            kinect = sensor;

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
                #region 標準處理架構
                if (iaf == null)
                    return;

                UserInfo[] userInfos = new UserInfo[InteractionFrame.UserInfoArrayLength];
                iaf.CopyInteractionDataTo(userInfos);
                #endregion

                #region 擷取互動串流提供的資訊
                GetIDs(userInfos);

                var activeuser = (from u in userInfos
                                  where u.SkeletonTrackingId > 0
                                  select u).FirstOrDefault();

                if (activeuser != null)
                {
                    GetHands(activeuser);
                    GetHandEvent(activeuser);
                }
                #endregion
            }
        }

        private void GetIDs(UserInfo[] userInfos)
        {
            users.Text = "使用者IDs: ";
            foreach (var userInfo in userInfos)
            {
                if (userInfo.SkeletonTrackingId > 0)
                    users.Text += userInfo.SkeletonTrackingId + "  ";
            }
        }

        private void GetHands(UserInfo userInfo)
        {
            rhand.Text = "沒有發現使用者右手";
            lhand.Text = "沒有發現使用者左手";
            var hands = userInfo.HandPointers;
            if (hands.Count > 0)
            {               
                foreach (var hand in hands)
                {
                    if (hand.HandType == InteractionHandType.Right)
                    {
                        rhand.Text = FormatHandData(hand);
                    }
                    else if (hand.HandType == InteractionHandType.Left)
                    {
                        lhand.Text = FormatHandData(hand);
                    }
                }
            }
        }

        private static string FormatHandData(InteractionHandPointer hand)
        {
            string rawxloc = String.Format("X座標:{0:0.0}",hand.RawX) ;
            string rawyloc = String.Format(",Y座標:{0:0.0}", hand.RawY);
            string rawzloc = String.Format(",Z座標:{0:0.0}", hand.RawZ);
            //string xloc = String.Format(",校正X座標:{0:0.0}", hand.X);
            //string yloc = String.Format(",校正Y座標:{0:0.0}", hand.Y);

            return rawxloc + rawyloc + rawzloc;
        }

        private void GetHandEvent(UserInfo userInfo)
        {
            var hands = userInfo.HandPointers;
            if (hands.Count > 0)
            {
                foreach (var hand in hands)
                {
                    ParseHandEvent(hand);
                }
            }
        }

        private void ParseHandEvent(InteractionHandPointer hand)
        {
            if (hand.HandEventType == InteractionHandEventType.None)
                return;

            if (hand.HandType == InteractionHandType.Right)
            {
                if (hand.HandEventType == InteractionHandEventType.Grip)
                    status.Text = "右手握拳";
                else
                    status.Text = "右手張開";
            }
            else if (hand.HandType == InteractionHandType.Left)
            {
                if (hand.HandEventType == InteractionHandEventType.Grip)
                    status.Text = "左手握拳";
                else
                    status.Text = "左手張開";
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

                Skeleton[] skeletons = new Skeleton[skf.SkeletonArrayLength];
                skf.CopySkeletonDataTo(skeletons);
                var accelerometerReading = kinect.AccelerometerGetCurrentReading();
                interStream.ProcessSkeleton(skeletons, accelerometerReading, skf.Timestamp);
            }
        }
        
    }
}
