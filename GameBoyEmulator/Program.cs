namespace GameBoyEmulator;

internal class Program
{
    static void Main(string[] args)
    {
        byte[] rom = File.ReadAllBytes("03-op sp,hl.gb");
        DMG dmg = new DMG(rom);
        dmg.Run();
    }
}