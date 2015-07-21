using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using Windows.Devices.Gpio;

namespace ShiftRegister
{
    public sealed partial class MainPage : Page
    {
        // constants for controlling the time interval for clocking in serial data to the shift register
        private const double TIME_INTERVAL = 100; // value in miliseconds
        private const double TIME_DELAY = 1;

        // The 74HC595N has five input pins that are used to control the device
        // Shift Register Clock (SRCLK): the clock for the serial input to the shift register
        private const int SRCLK_PIN = 0; // GPIO 0 is pin 27 on the RPI2
        private GpioPin shiftRegisterClock;

        //Serial input (SER): the serial data input to the shift register used in conjunction with SRCLK
        private const int SER_PIN = 1; // GPIO 1 is pin 28 on the RPI2
        private GpioPin serial;

        // Storage Register Clock (RCLK): the clock for clocking data from the serial input to the parallel output of the shift register
        private const int RCLK_PIN = 5; // GPIO 5 is pin 29 on the RPI2
        private GpioPin registerClock;

        // Output Enable (OE): when set low, each of the eight shift register outputs are set high/low dending on the binary value in the storage register
        private const int OE_PIN = 6; // GPIO 6 is pin 31 on the RPI2
        private GpioPin outputEnable;

        // Storage Register Clock (SRCLK): the clock for clcking the current 8 bits of data from the serial input register to the storage register
        private const int SRCLR_PIN = 12; // GPIO 12 is pin 32 on the RPI2
        private GpioPin ShiftRegisterClear;

        private DispatcherTimer timer;
        private byte pinMask = 0x01;
        private bool areLEDsInverted = true;

        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        public MainPage()
        {
            this.InitializeComponent();

            // register for the unloaded event so we can clean up upon exit
            Unloaded += MainPage_Unloaded;

            InitializeSystem();
        }

        private void InitializeSystem()
        {
            // initialzing the GPIO pins
            var gpio = GpioController.GetDefault();

            // throw an error if there is no GPIO controller
            if (gpio == null)
            {
                GpioStatus.Text = "There is no GPIO controller on this device";
                return;
            }

            // setup the onboard GPIO pins that control the shift register
            shiftRegisterClock = gpio.OpenPin(SRCLK_PIN);
            serial = gpio.OpenPin(SER_PIN);
            registerClock = gpio.OpenPin(RCLK_PIN);
            outputEnable = gpio.OpenPin(OE_PIN);
            ShiftRegisterClear = gpio.OpenPin(SRCLR_PIN);

            // throw an error if the pin wasn't initialized properly
            if (shiftRegisterClock == null || serial == null || registerClock == null || outputEnable == null || ShiftRegisterClear == null)
            {
                GpioStatus.Text = "There were problems initializing the GPIO pins";
                return;
            }

            // reset the pins to a known state
            shiftRegisterClock.Write(GpioPinValue.Low);
            shiftRegisterClock.SetDriveMode(GpioPinDriveMode.Output);

            serial.Write(GpioPinValue.Low);
            serial.SetDriveMode(GpioPinDriveMode.Output);

            registerClock.Write(GpioPinValue.Low);
            registerClock.SetDriveMode(GpioPinDriveMode.Output);

            outputEnable.Write(GpioPinValue.Low);
            outputEnable.SetDriveMode(GpioPinDriveMode.Output);

            ShiftRegisterClear.Write(GpioPinValue.Low);
            ShiftRegisterClear.SetDriveMode(GpioPinDriveMode.Output);

            // With the shiftRegisterClear GPIO set to low, a rising edge on the register clock will clear (set all bits to 0) the shift register
            registerClock.Write(GpioPinValue.High);

            // typically when bit-banging a serial signal out, a delay is needed between setting the output
            // value and sending a rising or falling edge on the clock. However, the setup and hold 
            // times for this shift register at 5v are in the nanoseconds so we can cheat a bit here
            // by not adding a delay before driving the register clock low
            registerClock.Write(GpioPinValue.Low);
            GpioStatus.Text = "GPIO pin initialized correctly.";

            try
            {
                timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(TIME_INTERVAL);
                timer.Tick += Timer_Tick;
                timer.Start();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Exception: {0}", e.Message);
                return;
            }
        }

        private void SendDataBit()
        {
            if ((pinMask & 0x80) > 0)
            {
                serial.Write(GpioPinValue.High);
                shiftRegisterClock.Write(GpioPinValue.High);
                shiftRegisterClock.Write(GpioPinValue.Low);
                registerClock.Write(GpioPinValue.High);
                registerClock.Write(GpioPinValue.Low);
            }
            else
            {
                serial.Write(GpioPinValue.Low);
                shiftRegisterClock.Write(GpioPinValue.High);
                shiftRegisterClock.Write(GpioPinValue.Low);
                registerClock.Write(GpioPinValue.High);
                registerClock.Write(GpioPinValue.Low);
            }

            pinMask <<= 1;
            if (areLEDsInverted)
            {
                if (pinMask == 0)
                {
                    pinMask = 0x01;
                }
            }
            else
            {
                pinMask |= 0x01;
                if (pinMask == 0xFF)
                {
                    pinMask &= 0xFE;
                }
            }
        }

        private void ToggleButtonClicked(object sender, RoutedEventArgs e)
        {
            pinMask ^= 0xFF;
            if (areLEDsInverted)
            {
                areLEDsInverted = false;
                ToggleButton.Background = grayBrush;
            }
            else
            {
                areLEDsInverted = true;
                ToggleButton.Background = redBrush;
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            SendDataBit();
        }

        private void Delay_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (timer == null)
            {
                return;
            }
            if (e.NewValue == Delay.Minimum)
            {
                DelayText.Text = e.NewValue + "ms";
                timer.Stop();
            }
            else
            {
                DelayText.Text = e.NewValue + "ms";
                timer.Interval = TimeSpan.FromMilliseconds(e.NewValue);
                timer.Start();
            }
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            // cleanup
            shiftRegisterClock.Dispose();
            serial.Dispose();
            registerClock.Dispose();
            outputEnable.Dispose();
            ShiftRegisterClear.Dispose();
        }
    }
}