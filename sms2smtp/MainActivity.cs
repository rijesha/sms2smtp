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

//things to do
//queue up unsent messages
//have app turn on at startup
//read sms messages
//save password securely
namespace sms2smtp
{
    [BroadcastReceiver]
    [IntentFilter(new[] { "android.net.conn.CONNECTIVITY_CHANGE" })]
    public class NetworkChangeReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Configuration.canSend = !intent.GetBooleanExtra(ConnectivityManager.ExtraNoConnectivity, false);
            Console.WriteLine("NETWORK CHANGED ");
            Console.WriteLine(Configuration.canSend);
            Console.WriteLine("****************************************");
        }
    }

    [BroadcastReceiver]
    [IntentFilter(new[] { Intent.ActionBootCompleted, "android.intent.action.QUICKBOOT_POWERON" })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            //Toast.MakeText(context, "Received intent!", ToastLength.Long).Show();
            Intent serviceStart = new Intent(context, typeof(SmsSenderService));
            serviceStart.AddFlags(ActivityFlags.NewTask);
            serviceStart.SetAction("My.Action");
            context.StartService(serviceStart);
        }
    }

    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { "android.provider.Telephony.SMS_RECEIVED" })]
    class SmsListener : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            Toast.MakeText(context, "Received intent!", ToastLength.Long).Show();
            if (Telephony.Sms.Intents.SmsReceivedAction.Equals(intent.Action))
            {
                foreach (SmsMessage smsMessage in Telephony.Sms.Intents.GetMessagesFromIntent(intent))
                {
                    String messageBody = smsMessage.MessageBody;
                    System.Diagnostics.Debug.WriteLine(messageBody);
                }
            }
            Intent serviceStart = new Intent(context, typeof(SmsSenderService));
            serviceStart.AddFlags(ActivityFlags.NewTask);
            serviceStart.SetAction("My.Action");
            context.StartService(serviceStart);
            /*
            Intent serviceStart = new Intent(context, typeof(MainActivity));
            serviceStart.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(serviceStart);*/
        }
    }

    [Service(Exported = true)]
    public class SmsSenderService : Service
    {

        public override void OnCreate()
        {
            base.OnCreate();
            Configuration.Initialize();
        }

        public override IBinder OnBind(Intent intent)
        {
            throw new NotImplementedException();
        }

        public override StartCommandResult OnStartCommand(Android.Content.Intent intent, StartCommandFlags flags, int startId)
        {
            // This method executes on the main thread of the application.
            if (Configuration.isInitialized)
            {
                Configuration.sendTestEmail();
            }

            return StartCommandResult.Sticky;
        }


    }

    public static class Configuration
    {
        public static SmtpClient SmtpServer;
        //tannkkfussgrwhga
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
                retrievedata();
                if (isSetup == "true")
                {
                    setupSMTP();
                    isInitialized = true;
                }
            }
            return isInitialized;
        }

        public static void retrievedata()
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

        public static void saveprefs()
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
            catch (Exception ex)
            {
                //System.Diagnostics.Debug.WriteLine("---------###----------------");
                System.Diagnostics.Debug.WriteLine(ex);
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
                Configuration.saveprefs();
                Configuration.sendTestEmail();

                configured_box.Text = "CONFIGURED";
            };
        }
    }
}

