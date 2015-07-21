using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Devices.Gpio;

namespace BlinkyLED
{
    public sealed partial class MainPage : Page
    {
        private const int LED_PIN = 5;
        private GpioPin pin;
        private GpioPinValue pinValue;
        private DispatcherTimer timer;
        private SolidColorBrush orangeBrush = new SolidColorBrush(Windows.UI.Colors.Orange);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        public MainPage()
        {
            InitializeComponent();

            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromMilliseconds(500);
            this.timer.Tick += Timer_Tick;
            this.timer.Start();

            if (pin != null)
            {
                timer.Start();
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            if (pinValue == GpioPinValue.High)
            {
                pinValue = GpioPinValue.Low;
                pin.Write(pinValue);
                LED.Fill = orangeBrush;
            }
            else
            {
                // null exception if GPIO not initialized 
                pinValue = GpioPinValue.High;
                pin.Write(pinValue);
                LED.Fill = grayBrush;
            }
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();
            pin = gpio.OpenPin(LED_PIN);

            if (gpio == null)
            {
                pin = null;
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            if (pin == null)
            {
                GpioStatus.Text = "There were problems initializing the GPIO pin.";
                return;
            }

            pinValue = GpioPinValue.High;
            pin.Write(pinValue);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            GpioStatus.Text = "GPIO pin initialized correctly.";
        }
    }
}
