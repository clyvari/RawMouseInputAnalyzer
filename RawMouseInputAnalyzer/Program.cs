using Linearstar.Windows.RawInput;
using RawMouseInputAnalyzer;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

// Get the devices that can be handled with Raw Input.
var devices = RawInputDevice.GetDevices();

// Keyboards will be returned as a RawInputKeyboard.
var mouses = devices.OfType<RawInputMouse>();

// List them up.
foreach (var device in mouses)
    Console.WriteLine(
        $"{device.DeviceType} {device.VendorId:X4}:{device.ProductId:X4} {device.ProductName}, {device.ManufacturerName}");

// To begin catching inputs, first make a window that listens WM_INPUT.
var messages = new List<MessageEvent>();
var sw = new Stopwatch();
var window = new RawInputReceiverWindow(messages, sw);

var res = new List<MouseEvent>();


bool ShouldKillApp = false;

window.Input += (sender, e) =>
{
    var data = ((RawInputMouseData)e.Data).Mouse;
    var d = new MouseEvent(e.Data.Header.DeviceHandle.ToString(), data.LastX, data.LastY, sw.Elapsed.TotalMilliseconds);
    res.Add(d);
    Console.WriteLine(e.Data);
    //Console.WriteLine($"{data} {d} {res.Count}");
};

Console.CancelKeyPress += (sender, e) =>
{
    if(!ShouldKillApp)
    {
        e.Cancel = true;
        Application.Exit();
    }
};

try
{
    Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(2);
    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
    Thread.CurrentThread.Priority = ThreadPriority.Highest;

    Console.WriteLine("Ready !! press a key");
    Console.ReadKey(true);
    Console.WriteLine("Collecting data...");
    sw.Start();
    // Register the HidUsageAndPage to watch any device.
    RawInputDevice.RegisterDevice(HidUsageAndPage.Mouse,
        RawInputDeviceFlags.ExInputSink | RawInputDeviceFlags.NoLegacy, window.Handle);
    Application.Run();
}
finally
{
    ShouldKillApp = true;
    sw.Stop();
    RawInputDevice.UnregisterDevice(HidUsageAndPage.Mouse);
}

foreach (var m in messages)
{
    var data = ((RawInputMouseData)m.msg).Mouse;
    var d = new MouseEvent(m.msg.Header.DeviceHandle.ToString(), data.LastX, data.LastY, m.ts.TotalMilliseconds);
    res.Add(d);
    Console.WriteLine(m.msg);
}

Console.WriteLine(res.Count);
foreach (var a in res)
{
    Console.WriteLine(a);
}

var resPerDevice = res.ToLookup(x => x.Device);
foreach (var r in resPerDevice)
{
    var devRes = r.ToList();
    List<MouseTrack> mouseTrack = new(devRes.Count);
    var ticks = devRes[0].Milliseconds;
    MouseTrack prev = new(devRes[0].Device, devRes[0].DeltaX, devRes[0].DeltaY, 0);
    mouseTrack.Add(prev);
    for (int i = 1; i < devRes.Count; i++)
    {
        prev = new(devRes[i].Device, prev.X + devRes[i].DeltaX, prev.Y + devRes[i].DeltaY, devRes[i].Milliseconds - ticks);
        //ticks = res[i].Milliseconds;
        mouseTrack.Add(prev);
    }
    StringBuilder sb = new();
    sb.AppendLine(MouseTrack.CSVHeader);
    foreach (var a in mouseTrack)
    {
        sb.AppendLine(a.ToCSV());
        Console.WriteLine(a);
    }
    File.WriteAllText(@$"S:\mousetrack-{r.Key}.csv", sb.ToString());

    continue;

    List<MousePoint> mousePoints = new();
    MousePoint lastMp = new(mouseTrack[0].X, mouseTrack[0].Y);
    mousePoints.Add(lastMp);
    for (int i = 1; i < mouseTrack.Count; i++)
    {
        if ((lastMp.X, lastMp.Y) != (mouseTrack[i].X, mouseTrack[i].Y))
        {
            lastMp = new(mouseTrack[i].X, mouseTrack[i].Y);
            mousePoints.Add(lastMp);
        }
        else
        {
            lastMp.AddOccurence();
        }
    }

    foreach (var a in mousePoints)
    {
        Console.WriteLine(a);
    }

    Application.Run(new ResultView(mousePoints));
}

readonly record struct MessageEvent(RawInputData msg, TimeSpan ts);
readonly record struct MouseEvent(string Device, int DeltaX, int DeltaY, double Milliseconds);
readonly record struct MouseTrack(string Device, int X, int Y, double DeltaT)
{
    public static readonly string CSVHeader = $"{nameof(Device)};{nameof(X)};{nameof(Y)};{nameof(DeltaT)}";
    public string ToCSV() => $"{Device};{X};{Y};{DeltaT:0.####}";
}
record MousePoint(int X, int Y)
{
    public int Count { get; private set; } = 1;
    public void AddOccurence() => Count++;
}

class RawInputEventArgs : EventArgs
{
    public RawInputEventArgs(RawInputData data)
    {
        Data = data;
    }

    public RawInputData Data { get; }
}

sealed class RawInputReceiverWindow : NativeWindow
{
    private readonly List<MessageEvent> output;
    private readonly Stopwatch sw;

    public event EventHandler<RawInputEventArgs>? Input;

    public RawInputReceiverWindow(List<MessageEvent> output, Stopwatch sw)
    {
        CreateHandle(new CreateParams
        {
            X = 0,
            Y = 0,
            Width = 0,
            Height = 0,
            Style = 0x800000,
        });
        this.output = output;
        this.sw = sw;
    }

    protected override void WndProc(ref Message m)
    {
        const int WM_INPUT = 0x00FF;

        if (m.Msg == WM_INPUT)
        {
            var data = RawInputData.FromHandle(m.LParam);
            output.Add(new(data, sw.Elapsed));

            //Input?.Invoke(this, new RawInputEventArgs(data));
        }

        base.WndProc(ref m);
    }
}

