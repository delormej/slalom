using Android.App;
using Android.Widget;
using Android.OS;
using Android.Hardware;
using Android.Content;

namespace SlalomTracker.Android
{
    [Activity(Label = "SlalomTracker.Android", MainLauncher = true, Icon = "@mipmap/icon")]
    public class MainActivity : Activity, ISensorEventListener
    {
        static readonly object _syncLock = new object();
        SensorManager _sensorManager;
        TextView _sensorTextView;

        int count = 1;

        // TYPE_GYROSCOPE

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _sensorManager = (SensorManager)GetSystemService(Context.SensorService);
            _sensorTextView = FindViewById<TextView>(Resource.Id.accelerometer_text);

            // Get our button from the layout resource,
            // and attach an event to it
            Button button = FindViewById<Button>(Resource.Id.myButton);

            button.Click += delegate { button.Text = string.Format("{0} clicks!", count++); };
        }

        protected override void OnResume()
        {
            base.OnResume();
            _sensorManager.RegisterListener(this,
                                            _sensorManager.GetDefaultSensor(SensorType.Gyroscope),
                                            SensorDelay.Ui);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _sensorManager.UnregisterListener(this);
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
            // We don't want to do anything here.
        }

        public void OnSensorChanged(SensorEvent e)
        {
            lock (_syncLock)
            {
                _sensorTextView.Text = string.Format("x={0:f}, y={1:f}, z={2:f}\n", e.Values[0], e.Values[1], e.Values[2]);
            }
        }
    }
}


//protected void StartSensor()
//{
//    SensorManager sensorManager = this.ApplicationContext.GetSystemService(SensorService) as SensorManager;

//    if (sensorManager == null)
//    {
//        System.Diagnostics.Debug.Write("Can't get sensor manager.");
//        return;
//    }

//    Sensor gyro = sensorManager.GetDefaultSensor(SensorType.Gyroscope);
//}
