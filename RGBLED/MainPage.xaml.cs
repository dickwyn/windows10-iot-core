using System;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;

namespace RGBLED
{
    public sealed partial class MainPage : Page
    {
        private GpioPin redpin;
        private GpioPin bluepin;
        private GpioPin greenpin;
        private int LEDStatus = 0;
        private const int REDLED_PIN = 5;
        private const int BLUELED_PIN = 6;
        private const int GREENLED_PIN = 13;
        private DispatcherTimer timer;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush blueBrush = new SolidColorBrush(Windows.UI.Colors.Blue);
        private SolidColorBrush greenBrush = new SolidColorBrush(Windows.UI.Colors.Green);

        public MainPage()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            timer.Start();

            Unloaded += MainPage_Unloaded;

            InitGpio();
        }

        private void InitGpio()
        {
            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                redpin = null;
                bluepin = null;
                greenpin = null;
                GpioStatus.Text = "There is no GPIO controller on this device";
                return;
            }

            redpin = gpio.OpenPin(REDLED_PIN);
            bluepin = gpio.OpenPin(BLUELED_PIN);
            greenpin = gpio.OpenPin(GREENLED_PIN);

            redpin.Write(GpioPinValue.High);
            redpin.SetDriveMode(GpioPinDriveMode.Output);
            bluepin.Write(GpioPinValue.High);
            bluepin.SetDriveMode(GpioPinDriveMode.Output);
            greenpin.Write(GpioPinValue.High);
            greenpin.SetDriveMode(GpioPinDriveMode.Output);

            GpioStatus.Text = "GPIO red/green/blue pin initialized correctly";
        }


        private void MainPage_Unloaded(object sender, object args)
        {
            redpin.Dispose();
            bluepin.Dispose();
            greenpin.Dispose();
        }


        private void FlipLED()
        {
            if (LEDStatus == 0)
            {
                LEDStatus = 1;
                if (redpin != null && bluepin != null && greenpin != null)
                {
                    redpin.Write(GpioPinValue.High);
                    bluepin.Write(GpioPinValue.Low);
                    greenpin.Write(GpioPinValue.Low);
                }
                LED.Fill = redBrush;
            }
            else if (LEDStatus == 1)
            {
                LEDStatus = 2;
                if (redpin != null && bluepin != null && greenpin != null)
                {
                    redpin.Write(GpioPinValue.Low);
                    bluepin.Write(GpioPinValue.High);
                    greenpin.Write(GpioPinValue.Low);
                }
                LED.Fill = greenBrush;
            }
            else
            {
                LEDStatus = 0;
                if (redpin != null && bluepin != null && greenpin != null)
                {
                    redpin.Write(GpioPinValue.Low);
                    bluepin.Write(GpioPinValue.Low);
                    greenpin.Write(GpioPinValue.High);
                }
                LED.Fill = greenBrush;
            }
        }


        private void TurnOffLED()
        {
            if (LEDStatus == 1)
            {
                FlipLED();
            }
        }


        private void Timer_Tick(object sender, object e)
        {
            FlipLED();
        }


        private void Delay_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (timer == null)
            {
                return;
            }
            if (e.NewValue == Delay.Minimum)
            {
                DelayText.Text = "Stopped";
                timer.Stop();
                TurnOffLED();
            }
            else
            {
                DelayText.Text = e.NewValue + "ms";
                timer.Interval = TimeSpan.FromMilliseconds(e.NewValue);
                timer.Start();
            }
        }
    }
}

