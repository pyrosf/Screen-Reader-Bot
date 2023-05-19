using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AutoHotkey.Interop;

public static class Input
{
    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT point);
    

    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);


    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int x;
        public int y;
    }

    public static POINT GetMousePosition()
    {
        POINT pos;
        GetCursorPos(out pos);
        return pos;
    }
    public static Color GetPixelColor(int x, int y)
    {
        IntPtr desktopPtr = GetDC(IntPtr.Zero);
        uint color = GetPixel(desktopPtr, x, y);
        ReleaseDC(IntPtr.Zero, desktopPtr);

        // Extract the RGB components from the color value
        byte red = (byte)(color & 0xFF);
        byte green = (byte)((color >> 8) & 0xFF);
        byte blue = (byte)((color >> 16) & 0xFF);

        return Color.FromArgb(red, green, blue);
    }
    public static string ColorToHex(Color color)
    {
        return "#" + color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
    }
    

}

class Program
{
    
    static string _filePath = @"C:\Users\Public\Daybreak Game Company\Installed Games\EverQuest\Logs\eqlog_Glasyas_vaniki.txt";
    static long _fileSize = 0;
    static bool firsttime = true;
    static ConcurrentQueue<string> _lineQueue = new ConcurrentQueue<string>();

    static void PrintNewLines()
    {
        // Open the log file
        using (var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        {
            // Set the file position to the end of the previous file contents
            fileStream.Seek(_fileSize, SeekOrigin.Begin);

            // Read the new lines that were added to the file
            using (var streamReader = new StreamReader(fileStream))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {

                    Console.WriteLine(line);
                    if (firsttime)
                    { continue; }
                    _lineQueue.Enqueue(line);  // this prevents locking and ensures each line is put in the queue to be processed without impacting the file
                    
                }

                // Update the file size to the current position in the file
                _fileSize = fileStream.Position;
                firsttime = false;
            }
        }
    }

    [DllImport("user32.dll")]
    public static extern bool GetAsyncKeyState(int button);

    public static bool IsMouseButtonPressed(MouseButton button)
    {
        return GetAsyncKeyState((int)button);
    }

    public enum MouseButton
    {
        LeftMouseButton = 0x01,
        RightMouseButton = 0x02,
        MiddleMouseButton = 0x04
    }

    class DataObject
    {
        public int X { get; set; }
        public int Y { get; set; }
        public string Hex { get; set; }
        public int HP { get; set; }
        public string Notes { get; set; }
        public string Description { get; set; }
    }

    class Stats
    {
        public int Target_HP { get; set; }
        public int Target_HP1 { get; set; }
        public int Target_HP2 { get; set; }
        public int Target_HP3 { get; set; }
        public int Target_HP4 { get; set; }
        public int Target_HP5 { get; set; }
        public int Target_HP6 { get; set; }
        public int My_HP { get; set; }
        public int My_MP { get; set; }
        public int Casting { get; set; }
        public int Party1 { get; set; }
        public int Party2 { get; set;}
        public int Party3 { get; set;}
        public int Party4 { get; set;}
        public int Party5 { get; set;}
        public int Party6 { get; set;}




    }
     

    static List<DataObject> ReadCSVFile(string filePath)
    {
        List<DataObject> dataObjects = new List<DataObject>();

        try
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                string headerLine = reader.ReadLine();
                if (headerLine != null)
                {
                    string[] headers = headerLine.Split(',');

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            string[] values = line.Split(',');

                            DataObject dataObject = new DataObject();

                            // Assuming the CSV columns are in the same order as the object properties
                            for (int i = 0; i < values.Length; i++)
                            {
                                string value = values[i];
                                switch (headers[i].ToLower())
                                {
                                    case "x":
                                        dataObject.X = int.Parse(value);
                                        break;
                                    case "y":
                                        dataObject.Y = int.Parse(value);
                                        break;
                                    case "hex":
                                        dataObject.Hex = value;
                                        break;
                                    case "hp":
                                        dataObject.HP = int.Parse(value);
                                        break;
                                    case "notes":
                                        dataObject.Notes = value;
                                        break;
                                    case "description":
                                        dataObject.Description = value;
                                        break;
                                    default:
                                        // Handle unknown headers/columns
                                        break;
                                }
                            }

                            dataObjects.Add(dataObject);
                        }
                    }
                }
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"File '{filePath}' not found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }

        return dataObjects;
    }



    static void Main()
    {

        

        // setting up AutoHotKey
        var ahk = AutoHotkeyEngine.Instance;
        ahk.ExecRaw("SetKeyDelay, 2");
        
        
        // Modes!
        bool AutoTarget = false;
        bool AutoSit = false;
        bool Auction = false;


        Random random = new Random();

        // This file has all the pixle data
        string filePath2 = @"C:\temp\EQTestFiles\Data.csv";
        List<DataObject> dataObjects = ReadCSVFile(filePath2);
        List<DataObject> Target1 = new List<DataObject>();
        List<DataObject> Target2 = new List<DataObject>();
        List<DataObject> Target3 = new List<DataObject>();
        List<DataObject> Target4 = new List<DataObject>();
        List<DataObject> Target5 = new List<DataObject>();
        List<DataObject> Target6 = new List<DataObject>();
        List<DataObject> HP = new List<DataObject>();
        List<DataObject> TargetHP = new List<DataObject>();
        List<DataObject> MP = new List<DataObject>();
        List<DataObject> ActiveCasting = new List<DataObject>();
        List<DataObject> Party1 = new List<DataObject>();
        List<DataObject> Party2 = new List<DataObject>();
        List<DataObject> Party3 = new List<DataObject>();

        Stats EQStats = new Stats();
        // Read in the Data File

        foreach (DataObject obj in dataObjects)
        {
            Console.WriteLine($"X: {obj.X}, Y: {obj.Y}, Hex: {obj.Hex}, HP: {obj.HP}, Notes: {obj.Notes}, Description: {obj.Description}");
            if (obj.Description == "Target 1") { Target1.Add(obj); }
            if (obj.Description == "Target 2") { Target2.Add(obj); }
            if (obj.Description == "Target 3") { Target3.Add(obj); }
            if (obj.Description == "Target 4") { Target4.Add(obj); }
            if (obj.Description == "Target 5") { Target5.Add(obj); }
            if (obj.Description == "Target 6") { Target6.Add(obj); }
            if (obj.Description == "My HP") { HP.Add(obj); }
            if (obj.Description == "Target HP") { TargetHP.Add(obj); }
            if (obj.Description == "My MP") { MP.Add(obj); }
            if (obj.Description == "Casting") { ActiveCasting.Add(obj); }
            if (obj.Description == "Party 1") { Party1.Add(obj); }
            if (obj.Description == "Party 2") { Party2.Add(obj); }
            if (obj.Description == "Party 3") { Party3.Add(obj); }
        }

        bool stopPrinting = false;

        bool OutOfRange = false;
        bool ManaRegen = false;
        bool backup = false;
        Int64 counter = 0;
        Int64 nextcast = 0;


        Thread printThread = new Thread(() =>
        {
            while (!stopPrinting)
            {
                foreach (DataObject obj in Target1)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Target_HP1 = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.Target_HP1 = -1;
                    }
                }
                foreach (DataObject obj in Target2)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Target_HP2 = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.Target_HP2 = -1;
                    }
                }
                foreach (DataObject obj in Target3)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Target_HP3 = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.Target_HP3 = -1;
                    }

                }
                foreach (DataObject obj in Target4)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Target_HP4 = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.Target_HP4 = -1;
                    }
                }
                foreach (DataObject obj in Target5)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Target_HP5 = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.Target_HP5 = -1;
                    }
                }
                foreach (DataObject obj in Target6)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Target_HP6 = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.Target_HP6 = -1;
                    }
                }
                foreach (DataObject obj in HP)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.My_HP = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.My_HP = -1;
                    }
                }
                foreach (DataObject obj in TargetHP)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Target_HP = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.Target_HP = -1;
                    }
                }
                foreach (DataObject obj in MP)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.My_MP = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.My_MP = -1;
                    }

                }
                foreach (DataObject obj in ActiveCasting)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Casting = 1;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.Casting = -1;
                    }

                }
                foreach (DataObject obj in Party1)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Party1 = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.Party1 = -1;
                    }
                }
                foreach (DataObject obj in Party2)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Party2 = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.Party2 = -1;
                    }
                }
                foreach (DataObject obj in Party3)
                {
                    Color pixelColor = Input.GetPixelColor(obj.X, obj.Y);
                    string hexColor = Input.ColorToHex(pixelColor);
                    if (hexColor == obj.Hex)
                    {
                        EQStats.Party3 = obj.HP;
                        break;
                    }
                    if (hexColor != obj.Hex && obj.HP == 5)
                    {
                        EQStats.Party3 = -1;
                    }

                }





                Console.Clear();
                Console.WriteLine($"Target_HP: {EQStats.Target_HP}");
                Console.WriteLine($"Target_HP1: {EQStats.Target_HP1}");
                Console.WriteLine($"Target_HP2: {EQStats.Target_HP2}");
                Console.WriteLine($"Target_HP3: {EQStats.Target_HP3}");
                Console.WriteLine($"Target_HP4: {EQStats.Target_HP4}");
                Console.WriteLine($"Target_HP5: {EQStats.Target_HP5}");
                Console.WriteLine($"Target_HP6: {EQStats.Target_HP6}");
                Console.WriteLine($"My_HP: {EQStats.My_HP}");
                Console.WriteLine($"My_MP: {EQStats.My_MP}");
                Console.WriteLine($"Casting: {EQStats.Casting}");
                Console.WriteLine($"Party 1: {EQStats.Party1}");
                Console.WriteLine($"Party 2: {EQStats.Party2}");
                Console.WriteLine($"Party 3: {EQStats.Party3}");

                if (EQStats.Casting == 1)  // Check to see if currently casting, if so do nothing
                {
                    Thread.Sleep( 500 );
                    continue;
                }

                //Process Log Here

                //ahk.ExecRaw("Send {s down}\r\nSleep 1000\r\nSend {s up}");  // How to send multiline code

                foreach (string Qline in _lineQueue)
                {
                    string queueline = "";
                    bool isRemoved = _lineQueue.TryDequeue(out queueline);
                    string line = Qline.Substring(27);
                    if (line.Contains("Your target is out of range, get closer!") || line.Contains("You cannot see your target."))
                    {
                        OutOfRange = true;
                    }
                    if (line.Contains("YOU for") && line.Contains("points of damage."))
                    {
                        //ahk.ExecRaw("Send {s down}\r\nSleep 1000\r\nSend {s up}");
                        backup = true;
                    }

                }
                if (OutOfRange)   // if given the out of range error, clear the error 
                {
                    ahk.ExecRaw("SendEvent,{Esc}");  // This wont stop a current cast
                    Thread.Sleep(random.Next(50, 100));
                    OutOfRange = false;
                    RandomMove();
                    

                }
                if (backup)
                {
                    ahk.ExecRaw("Send {s down}\r\nSleep 500\r\nSend {s up}");
                    backup = false;
                    ManaRegen = false;
                }
                if (EQStats.My_MP <= 5 && ManaRegen == false)
                {
                    ManaRegen = true;
                    ahk.ExecRaw("SendEvent,-");

                    

                }
                if (EQStats.My_HP <= 60 && EQStats.My_HP != -1)  // heal myself
                {
                    ahk.ExecRaw("SendEvent,{F1}");
                    Thread.Sleep(100);
                    ahk.ExecRaw("SendEvent,2");
                    Thread.Sleep(2000);
                    ahk.ExecRaw("SendEvent,{Esc}");  // Clear Target
                    Thread.Sleep(random.Next(50, 100));
                }
                if (EQStats.Target_HP1 >= 0)
                {
                    ahk.ExecRaw("SendEvent,+{1}");  // Target The Mob
                    Thread.Sleep(random.Next(300, 600));
                }
                if (EQStats.Target_HP1 == -1 && EQStats.Target_HP == -1 && ManaRegen == false)  // auto target code
                {
                    ahk.ExecRaw("SendEvent,{Tab}");  // Target The Mob
                    Thread.Sleep(random.Next(30, 60));

                }
                if (EQStats.Target_HP <= 100 && ManaRegen == false && AutoTarget)  // open fire code
                {
                    if (counter > nextcast)
                    {
                        ahk.ExecRaw("SendEvent,{3}");  // Open Fire!
                        Thread.Sleep(random.Next(50, 100)); // dont chain cast!!     
                        nextcast = counter + 6;
                    }
                }
                if (EQStats.My_MP >= 80)
                {
                    ManaRegen = false;
                }
                if (EQStats.Party1 >= 0 && EQStats.Party1 < 100) 
                {
                    ahk.ExecRaw("SendEvent,{F2}");
                    Thread.Sleep(300);
                    ahk.ExecRaw("SendEvent,2");
                    Thread.Sleep(2000);  // casting time
                    ahk.ExecRaw("SendEvent,{Esc}");  // Clear Target
                    Thread.Sleep(random.Next(50, 100));

                }
                if (EQStats.Party2 >= 0 && EQStats.Party2 < 100)
                {
                    ahk.ExecRaw("SendEvent,{F3}");
                    Thread.Sleep(300);
                    ahk.ExecRaw("SendEvent,2");
                    Thread.Sleep(2000);  // casting time
                    ahk.ExecRaw("SendEvent,{Esc}");  // Clear Target
                    Thread.Sleep(random.Next(50, 100));

                }
                if (EQStats.Party3 >= 0 && EQStats.Party3 < 100)
                {
                    ahk.ExecRaw("SendEvent,{F4}");
                    Thread.Sleep(300);
                    ahk.ExecRaw("SendEvent,2");
                    Thread.Sleep(2000);  // casting time
                    ahk.ExecRaw("SendEvent,{Esc}");  // Clear Target
                    Thread.Sleep(random.Next(50, 100));

                }



                counter++;
                Thread.Sleep( random.Next(251,750) );
            }
        });
        Thread ReadThread = new Thread(() =>
        {
            while (!stopPrinting)
            {
                // Print out the current contents of the file
                PrintNewLines();
                

                // Create a new FileSystemWatcher object to monitor the log file
                using (var watcher = new FileSystemWatcher(Path.GetDirectoryName(_filePath), Path.GetFileName(_filePath)))
                {
                    // Set the notification filter to watch for changes in LastWrite time
                    watcher.NotifyFilter = NotifyFilters.LastWrite;

                    // Start watching for changes
                    watcher.EnableRaisingEvents = true;

                    // Loop indefinitely
                    while (true)
                    {
                        // Wait for a change notification
                        var result = watcher.WaitForChanged(WatcherChangeTypes.Changed);

                        // Check if the file was changed
                        if (result.ChangeType == WatcherChangeTypes.Changed)
                        {
                            // Print out the new lines that were added to the file
                            PrintNewLines();
                        }
                    }
                }
                
            }

        });
        printThread.Start();
        ReadThread.Start();
        // Wait for the user to press Enter to stop printing
        Console.ReadLine();

        stopPrinting = true; // Set the flag to stop the printing loop
        printThread.Join(); // Wait for the printThread to finish
        ReadThread.Join();
        Console.WriteLine("Printing stopped.");

    }
    static void RandomMove()
    {
        var ahk = AutoHotkeyEngine.Instance;
        ahk.ExecRaw("SetKeyDelay, 2");
        // Create an instance of the Random class
        Random random = new Random();

        // Generate a random number between 0 and 2 (inclusive)
        int randomNumber = random.Next(4);

        // Use a switch statement or if-else statements to perform actions based on the random number
        switch (randomNumber)
        {
            case 0:
                // Action 1
                ahk.ExecRaw("Send {a down}\r\nSleep 250\r\nSend {a up}");
                // TODO: Add your code for Action 1 here
                break;
            case 1:
                // Action 2
                ahk.ExecRaw("Send {d down}\r\nSleep 250\r\nSend {d up}");
                // TODO: Add your code for Action 2 here
                break;
            case 2:
                // Action 3
                // do nothing
                // TODO: Add your code for Action 3 here
                break;
            case 3:
                // Action 3
                // do nothing
                // TODO: Add your code for Action 3 here
                break;
        }
    }
}
