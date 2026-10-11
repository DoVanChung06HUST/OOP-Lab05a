/*************************************************
 * Ma sinh vien: 202418855
 * Ho ten: Do Van Chung
 *************************************************/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Lab05a;

public enum DeviceStatus { Active, UnderMaintenance, Retired }
public enum PrinterType { Laser, Inkjet }

public interface INetworkable
{
    string IpAddress { get; }
    bool IsConnected { get; }
    void Connect(string ipAddress);
    void Disconnect();
}

public abstract class Device
{
    public string DeviceId { get; }
    public string Name { get; }
    public int YearInUse { get; }
    public decimal PurchasePrice { get; }
    public int YearsInUse => DateTime.Now.Year - YearInUse;
    private DeviceStatus status;
    public DeviceStatus Status
    {
        get => status;
        set
        {
            if (!Enum.IsDefined(typeof(DeviceStatus), value))
                throw new ArgumentException("Trang thai khong hop le.");
            status = value;
        }
    }

    protected Device(string id, string name, int year, decimal price, DeviceStatus status)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Ma thiet bi rong.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ten thiet bi rong.");
        if (year < 1 || year > DateTime.Now.Year) throw new ArgumentOutOfRangeException(nameof(year));
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price));
        DeviceId = id.Trim();
        Name = name.Trim();
        YearInUse = year;
        PurchasePrice = price;
        Status = status;
    }

    public abstract decimal CalculateAnnualMaintenanceCost();
    public override string ToString() =>
        $"{DeviceId} | {Name} | {GetType().Name} | Nam: {YearInUse} | Gia: {PurchasePrice.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"))} dong | {Status}";
}

public class Computer : Device, INetworkable
{
    public int RamGB { get; }
    public string Processor { get; }
    public bool HasDedicatedGpu { get; }
    public string IpAddress { get; private set; } = string.Empty;
    public bool IsConnected => IpAddress.Length > 0;

    public Computer(string id, string name, int year, decimal price, int ramGB,
        string processor, bool hasDedicatedGpu, DeviceStatus status = DeviceStatus.Active)
        : base(id, name, year, price, status)
    {
        if (ramGB <= 0) throw new ArgumentOutOfRangeException(nameof(ramGB));
        if (string.IsNullOrWhiteSpace(processor)) throw new ArgumentException("CPU rong.");
        RamGB = ramGB;
        Processor = processor.Trim();
        HasDedicatedGpu = hasDedicatedGpu;
    }

    public override decimal CalculateAnnualMaintenanceCost()
    {
        decimal rate = 0.05m;
        if (HasDedicatedGpu) rate += 0.02m;
        if (YearsInUse > 5) rate += 0.01m;
        return PurchasePrice * rate;
    }

    public void Connect(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) throw new ArgumentException("IP rong.");
        if (IsConnected) throw new InvalidOperationException("Thiet bi dang ket noi.");
        IpAddress = ipAddress.Trim();
    }
    public void Disconnect() => IpAddress = string.Empty;
}

public class Printer : Device
{
    public PrinterType Type { get; }
    public int PrintedPages { get; }
    public bool IsColor { get; }
    // Nang luc mang phu thuoc vao lop thuc te, tranh co co bool mau thuan.
    public bool SupportsNetwork => this is INetworkable;

    public Printer(string id, string name, int year, decimal price, PrinterType type,
        int printedPages, bool isColor, DeviceStatus status = DeviceStatus.Active)
        : base(id, name, year, price, status)
    {
        if (!Enum.IsDefined(typeof(PrinterType), type)) throw new ArgumentException("Loai may in sai.");
        if (printedPages < 0) throw new ArgumentOutOfRangeException(nameof(printedPages));
        Type = type;
        PrintedPages = printedPages;
        IsColor = isColor;
    }

    public override decimal CalculateAnnualMaintenanceCost()
    {
        decimal cost = PurchasePrice * 0.04m;
        if (PrintedPages > 100000) cost += 500000m;
        if (IsColor) cost += 300000m;
        return cost;
    }
}

public class NetworkPrinter : Printer, INetworkable
{
    public string IpAddress { get; private set; } = string.Empty;
    public bool IsConnected => IpAddress.Length > 0;

    public NetworkPrinter(string id, string name, int year, decimal price, PrinterType type,
        int printedPages, bool isColor, DeviceStatus status = DeviceStatus.Active)
        : base(id, name, year, price, type, printedPages, isColor, status) { }

    public void Connect(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) throw new ArgumentException("IP rong.");
        if (IsConnected) throw new InvalidOperationException("Thiet bi dang ket noi.");
        IpAddress = ipAddress.Trim();
    }
    public void Disconnect() => IpAddress = string.Empty;
}

public class Projector : Device
{
    public int BrightnessLumen { get; }
    public int LampHours { get; }
    public Projector(string id, string name, int year, decimal price, int brightnessLumen,
        int lampHours, DeviceStatus status = DeviceStatus.Active) : base(id, name, year, price, status)
    {
        if (brightnessLumen <= 0) throw new ArgumentOutOfRangeException(nameof(brightnessLumen));
        if (lampHours < 0) throw new ArgumentOutOfRangeException(nameof(lampHours));
        BrightnessLumen = brightnessLumen;
        LampHours = lampHours;
    }
    public override decimal CalculateAnnualMaintenanceCost() =>
        PurchasePrice * 0.03m + (LampHours > 3000 ? 1500000m : 0m);
}

public class LabRoom
{
    public string RoomId { get; }
    public string Name { get; }
    public int Capacity { get; }
    private readonly List<Device> devices = new List<Device>();
    // Khong cho ben ngoai sua danh sach va bo qua kiem tra cua AddDevice.
    public IReadOnlyList<Device> Devices => devices.AsReadOnly();
    public LabRoom(string id, string name, int capacity)
    {
        if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Ma phong rong.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Ten phong rong.");
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        RoomId = id.Trim();
        Name = name.Trim();
        Capacity = capacity;
    }
    public void AddDevice(Device device)
    {
        if (device is null) throw new ArgumentNullException(nameof(device));
        if (FindDevice(device.DeviceId) != null) throw new InvalidOperationException("Trung ma thiet bi.");
        if (devices.Count >= Capacity) throw new InvalidOperationException("Phong da day.");
        devices.Add(device);
    }
    public Device? FindDevice(string deviceId) => devices.Find(d =>
        string.Equals(d.DeviceId, deviceId?.Trim(), StringComparison.OrdinalIgnoreCase));
    public bool RemoveDevice(string deviceId)
    {
        Device? device = FindDevice(deviceId);
        return device != null && devices.Remove(device);
    }
    public decimal CalculateAnnualMaintenanceCost()
    {
        decimal total = 0;
        foreach (Device device in devices)
            total += device.CalculateAnnualMaintenanceCost(); // Goi da hinh.
        return total;
    }
    public List<Device> GetDevicesRequiringMaintenance() => devices.FindAll(d =>
        d.Status == DeviceStatus.UnderMaintenance || d.YearsInUse > 5);
}

public static class Program
{
    private static int passed;
    private static int failed;
    private static string Money(decimal value) => value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN"));
    private static void Check(string label, bool ok)
    {
        if (ok) passed++; else failed++;
        Console.WriteLine($"[{(ok ? "PASS" : "FAIL")}] {label}");
    }
    private static void Expect<T>(string label, Action action) where T : Exception
    {
        try { action(); Check(label, false); }
        catch (T) { Check(label, true); }
        catch (Exception ex) { Check(label + " (sai loai loi: " + ex.GetType().Name + ")", false); }
    }
    private static void PrintRoom(LabRoom room)
    {
        Console.WriteLine($"\n{room.RoomId} - {room.Name}");
        foreach (Device d in room.Devices)
            Console.WriteLine($"{d} | Bao tri: {Money(d.CalculateAnnualMaintenanceCost())} dong");
        Console.WriteLine($"Tong: {Money(room.CalculateAnnualMaintenanceCost())} dong/nam");
        Console.WriteLine("Can bao tri: " + string.Join(", ", room.GetDevicesRequiringMaintenance().Select(d => d.DeviceId)));
    }

    // Mac dinh minh hoa 7 thao tac trong de; --test chay kiem tra bo sung.
    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        if (args.Contains("--test")) { RunTests(); return; }

        int y = DateTime.Now.Year;
        var pc1 = new Computer("PC01", "May tinh 1", y - 6, 20000000m, 16, "Core i5", true);
        var pc2 = new Computer("PC02", "May tinh 2", y - 2, 12000000m, 8, "Core i3", false);
        var pr1 = new Printer("PR01", "May in 1", y - 3, 5000000m, PrinterType.Laser, 50000, false);
        var pr2 = new NetworkPrinter("PR02", "May in 2", y - 2, 8000000m,
            PrinterType.Inkjet, 120000, true, DeviceStatus.UnderMaintenance);
        var pj1 = new Projector("PJ01", "May chieu 1", y - 4, 15000000m, 3500, 3501);
        var room1 = new LabRoom("P101", "Phong thuc hanh 1", 3);
        var room2 = new LabRoom("P102", "Phong thuc hanh 2", 3);
        Console.WriteLine("LAB 05A - DO VAN CHUNG - 202418855");
        Console.WriteLine("\n1. THEM THIET BI VAO HAI PHONG");
        room1.AddDevice(pc1); room1.AddDevice(pr1); room1.AddDevice(pj1);
        room2.AddDevice(pc2); room2.AddDevice(pr2);
        Console.WriteLine("P101: da them PC01, PR01, PJ01.");
        Console.WriteLine("P102: da them PC02, PR02.");
        Console.WriteLine("PC01 co GPU roi; PR02 in mau, 120.000 trang; PJ01 da dung 3.501 gio.");

        Console.WriteLine("\n2. THU THEM THIET BI TRUNG MA");
        try
        {
            room2.AddDevice(new Computer("PC02", "May trung ma", y, 10000000m, 8, "CPU", false));
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine("Them PC02 vao P102: " + ex.Message);
        }
        Console.WriteLine($"P102 van co {room2.Devices.Count} thiet bi.");

        LabRoom[] rooms = { room1, room2 };
        Console.WriteLine("\n3. DANH SACH THIET BI TUNG PHONG");
        foreach (LabRoom room in rooms)
        {
            Console.WriteLine($"{room.RoomId} - {room.Name} ({room.Devices.Count}/{room.Capacity}):");
            foreach (Device device in room.Devices)
            {
                Console.WriteLine("  " + device);
                Console.WriteLine("  Phi bao tri: " + Money(device.CalculateAnnualMaintenanceCost()) + " dong/nam");
            }
        }
        Console.WriteLine("\n4. TONG CHI PHI BAO TRI MOI PHONG");
        foreach (LabRoom room in rooms)
            Console.WriteLine($"{room.RoomId}: {Money(room.CalculateAnnualMaintenanceCost())} dong/nam");

        Console.WriteLine("\n5. THIET BI CAN BAO TRI");
        foreach (LabRoom room in rooms)
        {
            Console.WriteLine(room.RoomId + ":");
            List<Device> maintenance = room.GetDevicesRequiringMaintenance();
            if (maintenance.Count == 0) Console.WriteLine("  Khong co.");
            foreach (Device device in maintenance)
            {
                string reason = device.Status == DeviceStatus.UnderMaintenance
                    ? "dang o trang thai UnderMaintenance" : "da su dung tren 5 nam";
                Console.WriteLine($"  {device.DeviceId} - {device.Name}: {reason}.");
            }
        }
        Console.WriteLine("\n6. KET NOI MANG CHO CAC THIET BI HO TRO");
        Device[] all = { pc1, pc2, pr1, pr2, pj1 };
        List<INetworkable> networks = all.OfType<INetworkable>().ToList();
        int host = 10;
        foreach (INetworkable network in networks)
        {
            network.Connect("192.168.1." + host++);
            Console.WriteLine($"{((Device)network).DeviceId}: da ket noi {network.IpAddress}");
        }
        Console.WriteLine("\n7. DUYET VA NGAT MANG QUA INetworkable");
        foreach (INetworkable network in networks)
        {
            string id = ((Device)network).DeviceId;
            Console.WriteLine($"{id}: IsConnected = {network.IsConnected}, IP = {network.IpAddress}");
            network.Disconnect();
            Console.WriteLine($"{id}: sau ngat, IsConnected = {network.IsConnected}, IP = \"{network.IpAddress}\"");
        }
        Console.WriteLine("\nKet thuc chuong trinh.");
    }

    private static void RunTests()
    {
        Console.OutputEncoding = Encoding.UTF8;
        int y = DateTime.Now.Year;
        var pc1 = new Computer("PC01", "May tinh 1", y - 6, 20000000m, 16, "Core i5", true);
        var pc2 = new Computer("PC02", "May tinh 2", y - 2, 12000000m, 8, "Core i3", false);
        var pr1 = new Printer("PR01", "May in 1", y - 3, 5000000m, PrinterType.Laser, 50000, false);
        var pr2 = new NetworkPrinter("PR02", "May in 2", y - 2, 8000000m,
            PrinterType.Inkjet, 120000, true, DeviceStatus.UnderMaintenance);
        var pj1 = new Projector("PJ01", "May chieu 1", y - 4, 15000000m, 3500, 3501);
        var room1 = new LabRoom("P101", "Phong thuc hanh 1", 3);
        var room2 = new LabRoom("P102", "Phong thuc hanh 2", 3);
        room1.AddDevice(pc1); room1.AddDevice(pr1); room1.AddDevice(pj1);
        room2.AddDevice(pc2); room2.AddDevice(pr2);
        Console.WriteLine("LAB 05A - DO VAN CHUNG - 202418855");
        PrintRoom(room1); PrintRoom(room2);

        Console.WriteLine("\nKIEM THU PHONG VA CHI PHI");
        Check("PC01 = 1.600.000", pc1.CalculateAnnualMaintenanceCost() == 1600000m);
        Check("PC02 = 600.000", pc2.CalculateAnnualMaintenanceCost() == 600000m);
        Check("PR01 = 200.000", pr1.CalculateAnnualMaintenanceCost() == 200000m);
        Check("PR02 = 1.120.000", pr2.CalculateAnnualMaintenanceCost() == 1120000m);
        Check("PJ01 = 1.950.000", pj1.CalculateAnnualMaintenanceCost() == 1950000m);
        Check("Tong P101 = 3.750.000", room1.CalculateAnnualMaintenanceCost() == 3750000m);
        Check("Tong P102 = 1.720.000", room2.CalculateAnnualMaintenanceCost() == 1720000m);
        Expect<InvalidOperationException>("Trung ma (doi tuong khac)", () => room2.AddDevice(
            new Computer("pc02", "May trung ma", y, 10000000m, 8, "CPU", false)));
        Expect<ArgumentNullException>("Khong them null", () => room2.AddDevice(null!));
        Expect<InvalidOperationException>("Vuot suc chua", () => room1.AddDevice(pc2));
        Check("P101 can bao tri PC01", room1.GetDevicesRequiringMaintenance().SequenceEqual(new Device[] { pc1 }));
        Check("P102 can bao tri PR02", room2.GetDevicesRequiringMaintenance().SequenceEqual(new Device[] { pr2 }));
        Check("Tim thay PC02", ReferenceEquals(room2.FindDevice("PC02"), pc2));
        Check("Tim ma khong ton tai", room2.FindDevice("XXX") == null);
        Check("Xoa ma khong ton tai", !room2.RemoveDevice("XXX"));
        Check("Xoa PC02", room2.RemoveDevice("PC02") && room2.FindDevice("PC02") == null);
        Check("Thiet bi van ton tai sau khi xoa khoi phong", pc2.CalculateAnnualMaintenanceCost() == 600000m);
        room2.AddDevice(pc2);

        Console.WriteLine("\nDUYET QUA INTERFACE INetworkable");
        Device[] all = { pc1, pc2, pr1, pr2, pj1 };
        List<INetworkable> networks = all.OfType<INetworkable>().ToList();
        Check("Co 3 thiet bi mang", networks.Count == 3);
        Check("Nang luc mang cua hai may in", !pr1.SupportsNetwork && pr2.SupportsNetwork);
        int host = 10;
        foreach (INetworkable network in networks)
        {
            Expect<ArgumentException>("Tu choi IP rong", () => network.Connect(" "));
            string ip = "192.168.1." + host++;
            network.Connect(ip);
            Console.WriteLine($"{network.GetType().Name}: {network.IpAddress}");
            Check("Ket noi thanh cong", network.IsConnected && network.IpAddress == ip);
            Expect<InvalidOperationException>("Khong ket noi lap", () => network.Connect("192.168.1.99"));
            network.Disconnect();
            Check("Ngat mang va xoa IP", !network.IsConnected && network.IpAddress == string.Empty);
        }

        Console.WriteLine("\nDU LIEU SAI VA CAC NGUONG BIEN");
        Expect<ArgumentException>("Ma rong", () => new Computer(" ", "PC", y, 1m, 8, "CPU", false));
        Expect<ArgumentOutOfRangeException>("Gia bang 0", () => new Computer("X", "PC", y, 0m, 8, "CPU", false));
        Expect<ArgumentOutOfRangeException>("Gia am", () => new Computer("X", "PC", y, -1m, 8, "CPU", false));
        Expect<ArgumentOutOfRangeException>("Nam tuong lai", () => new Computer("X", "PC", y + 1, 1m, 8, "CPU", false));
        Check("Dung 5 nam: khong phu thu", new Computer("X", "PC", y - 5, 10000000m, 8, "CPU", false).CalculateAnnualMaintenanceCost() == 500000m);
        Check("Tren 5 nam: them 1%", new Computer("X", "PC", y - 6, 10000000m, 8, "CPU", false).CalculateAnnualMaintenanceCost() == 600000m);
        Check("Dung 100.000 trang", new Printer("X", "PR", y, 5000000m, PrinterType.Laser, 100000, false).CalculateAnnualMaintenanceCost() == 200000m);
        Check("100.001 trang", new Printer("X", "PR", y, 5000000m, PrinterType.Laser, 100001, false).CalculateAnnualMaintenanceCost() == 700000m);
        Check("Dung 3.000 gio", new Projector("X", "PJ", y, 10000000m, 3000, 3000).CalculateAnnualMaintenanceCost() == 300000m);
        Check("3.001 gio", new Projector("X", "PJ", y, 10000000m, 3000, 3001).CalculateAnnualMaintenanceCost() == 1800000m);
        var empty = new LabRoom("E", "Phong rong", 1);
        Check("Phong rong", empty.CalculateAnnualMaintenanceCost() == 0m && empty.GetDevicesRequiringMaintenance().Count == 0);
        Console.WriteLine($"\nKET QUA: {passed} PASS, {failed} FAIL");
        Environment.ExitCode = failed == 0 ? 0 : 1;
    }
}
