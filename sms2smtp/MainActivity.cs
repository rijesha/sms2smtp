using Android.App;
using Android.Widget;
using Android.OS;
using System.IO;
using System.Net.Mail;
using System;
using Android.Content;
using Android.Provider;
using Android.Telephony;
using Android.Net;
using System.Threading;
using Java.Text;

namespace sms2smtp
{
    [BroadcastReceiver]
    [IntentFilter(new[] { "android.net.conn.CONNECTIVITY_CHANGE" })]
    public class NetworkChangeReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Configuration.canSend = !intent.GetBooleanExtra(ConnectivityManager.ExtraNoConnectivity, false);
        }
    }

    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { "android.provider.Telephony.SMS_RECEIVED" })]
    class SmsListener : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Toast.MakeText(context, "sms2smtp: Sending SMS!", ToastLength.Long).Show();
            if (Telephony.Sms.Intents.SmsReceivedAction.Equals(intent.Action))
            {
                foreach (SmsMessage smsMessage in Telephony.Sms.Intents.GetMessagesFromIntent(intent))
                {
                    String messageBody = smsMessage.MessageBody;

                    Intent serviceStart = new Intent(context, typeof(SmsSenderService));
                    serviceStart.AddFlags(ActivityFlags.NewTask);
                    serviceStart.SetAction("My.Action");
                    serviceStart.PutExtra("time", new SimpleDateFormat("yyyy.MM.dd G 'at' HH:mm:ss").Format(smsMessage.TimestampMillis));
                    serviceStart.PutExtra("number", smsMessage.OriginatingAddress);
                    serviceStart.PutExtra("body", smsMessage.MessageBody);
                    context.StartService(serviceStart);

                }
            }
        }
    }

    [Service(Exported = true)]
    public class SmsSenderService : IntentService
    {

        public override void OnCreate()
        {
            base.OnCreate();
            if (!Configuration.isInitialized)
            {
                Configuration.Initialize();
            }
        }

        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
        }

        protected override void OnHandleIntent(Intent intent)
        {
            String time = intent.GetStringExtra("time");
            String number = intent.GetStringExtra("number");
            String body = intent.GetStringExtra("body");

            if (Configuration.isInitialized)
            {
                bool send_successful = false;
                while (!send_successful)
                {
                    try
                    {
                        MailMessage mail = new MailMessage();

                        mail.From = new MailAddress(Configuration.fromemail);
                        mail.To.Add(Configuration.toemail);
                        mail.Subject = "New SMS from " + number;
                        mail.Body = "On " + time + " you got a new sms from " + number + ":\n\n" + body;
                        Configuration.SmtpServer.Send(mail);
                        send_successful = true;
                    }
                    catch (Exception ex)
                    {
                        Thread.Sleep(5000);
                    }
                }
            }
        }
    }

    public static class Configuration
    {
        public static SmtpClient SmtpServer;
        public static string host = "";
        public static string port = "";
        public static string password = "";
        public static string fromemail = "";
        public static string toemail = "";
        public static string isSetup = "false";

        public static bool canSend = true;
        public static bool isInitialized = false;

        public static bool Initialize()
        {
            if (!isInitialized)
            {
                loadConfiguration();
                if (isSetup == "true")
                {
                    setupSMTP();
                    isInitialized = true;
                }
            }
            return isInitialized;
        }

        public static void loadConfiguration()
        {
            //retreive
            var prefs = Application.Context.GetSharedPreferences("smtp_settings", FileCreationMode.Private);
            host = prefs.GetString("host", "");
            port = prefs.GetString("port", "");
            fromemail = prefs.GetString("fromemail", "");
            toemail = prefs.GetString("toemail", "");
            password = prefs.GetString("password", "");
            isSetup = prefs.GetString("isSetup", "false");
        }

        public static void saveConfiguration()
        {
            //store
            isSetup = "true";
            var prefs = Application.Context.GetSharedPreferences("smtp_settings", FileCreationMode.Private);
            var prefEditor = prefs.Edit();
            prefEditor.PutString("host", host);
            prefEditor.PutString("port", port);
            prefEditor.PutString("fromemail", fromemail);
            prefEditor.PutString("toemail", toemail);
            prefEditor.PutString("password", password);
            prefEditor.PutString("isSetup", isSetup);

            prefEditor.Commit();
        }

        public static void setupSMTP()
        {
            SmtpServer = new SmtpClient(host);
            SmtpServer.Port = Convert.ToInt32(port);
            SmtpServer.Host = host;
            SmtpServer.EnableSsl = true;
            SmtpServer.UseDefaultCredentials = false;
            SmtpServer.Credentials = new System.Net.NetworkCredential(fromemail, password);
        }

        public static void sendTestEmail()
        {
            try
            {
                MailMessage mail = new MailMessage();

                mail.From = new MailAddress(fromemail);
                mail.To.Add(toemail);
                mail.Subject = "sms2smtp test email";
                mail.Body = "Successful test email";

                SmtpServer.Send(mail);
            }
            catch 
            {

            }
        }
    }

    [Activity(Label = "sms2smtp", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.myButton);
            EditText host_txt = FindViewById<EditText>(Resource.Id.smtp_host);
            EditText port_txt = FindViewById<EditText>(Resource.Id.smtp_port);
            EditText email_from_txt = FindViewById<EditText>(Resource.Id.smtp_email);
            EditText password_txt = FindViewById<EditText>(Resource.Id.smtp_password);
            EditText email_to_field_txt = FindViewById<EditText>(Resource.Id.email_to_field);

            TextView configured_box = FindViewById<TextView>(Resource.Id.configured_box);
            Configuration.Initialize();
            if (Configuration.isInitialized)
            {
                host_txt.Text = Configuration.host;
                port_txt.Text = Configuration.port;
                password_txt.Text = Configuration.password;
                email_from_txt.Text = Configuration.fromemail;
                email_to_field_txt.Text = Configuration.toemail;

                configured_box.Text = "CONFIGURED";
            }
            button.Click += delegate {
                Configuration.host = host_txt.Text;
                Configuration.port = port_txt.Text;
                Configuration.password = password_txt.Text;
                Configuration.fromemail = email_from_txt.Text;
                Configuration.toemail = email_to_field_txt.Text;
                Configuration.setupSMTP();
                Configuration.saveConfiguration();
                Configuration.sendTestEmail();

                configured_box.Text = "CONFIGURED";
            };
        }
    }
}

