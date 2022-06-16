using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Util;
using Firebase.Messaging;
using WindowsAzure.Messaging;
using AndroidX.Core.App;

namespace xamarinforms_demo.Droid
{
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class MyFirebaseMessagingService : FirebaseMessagingService
    {
        const string TAG = "MyFirebaseMsgService";
        NotificationHub hub;
        public override void OnMessageReceived(RemoteMessage message)
        {
            Log.Debug(TAG, "From: " + message.From);
            if (message.GetNotification() != null)
            {
                //These is how most messages will be received
                Log.Debug(TAG, "Notification Message Body: " + message.GetNotification().Body);
                SendNotification(message.GetNotification().Body);
            }
            else
            {
                //Only used for debugging payloads sent from the Azure portal
                SendNotification(message.Data.Values.First());

            }
        }
        void SendNotification(string messageBody)
        {
            var intent = new Intent(this, typeof(MainActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);

            var notificationBuilder = new NotificationCompat.Builder(this, MainActivity.CHANNEL_ID);

            // edit Content Title for notification title
            notificationBuilder.SetContentTitle("Push Demo Android")
                        .SetSmallIcon(Resource.Drawable.ic_launcher)
                        .SetContentText(messageBody)
                        .SetAutoCancel(true)
                        .SetShowWhen(false)
                        .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManager.FromContext(this);

            notificationManager.Notify(0, notificationBuilder.Build());
        }
        public override void OnNewToken(string token)
        {
            Log.Debug(TAG, "FCM token: " + token);
            SendRegistrationToServer(token);
        }

        void SendRegistrationToServer(string token)
        {
            // Register with Notification Hubs
            hub = new NotificationHub(Constants.NotificationHubName,
                                        Constants.ListenConnectionString, this);

            var dt = DateTime.Now;
            var timeTag = dt.ToString("HH:mm:ss");

            var carVendor = "";
            var vendorType = "";
            var vendorLocation = "";
            var carGrade = "";
            switch (Convert.ToInt64(dt.Minute) % 10)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    carVendor = "TOYOTA";
                    vendorType = "JapaneseCar";
                    vendorLocation = "Aichi";
                    carGrade = "20";
                    break;
                case 4:
                case 5:
                case 6:
                    carVendor = "NISSAN";
                    vendorType = "JapaneseCar";
                    vendorLocation = "Kanagawa";
                    carGrade = "15";
                    break;
                case 7:
                case 8:
                case 9:
                    carVendor = "BMW";
                    vendorType = "ForeignCar";
                    vendorLocation = "Munich";
                    carGrade = "10";
                    break;
                default:
                    carVendor = "tmp1";
                    vendorType = "tmp2";
                    vendorLocation = "tmp3";
                    carGrade = "tmp4";
                    break;
            }

            var tags = new List<string>() { timeTag, carVendor, vendorType, vendorLocation, carGrade };
            var regID = hub.Register(token, tags.ToArray()).RegistrationId;

            Log.Debug(TAG, $"Successful registration of ID {regID}");
        }
    } 
}