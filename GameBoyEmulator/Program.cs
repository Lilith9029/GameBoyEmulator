namespace GameBoyEmulator;

internal class Program
{
    static void Main(string[] args)
    {
        byte[] rom = File.ReadAllBytes("01-special.gb");
        DMG dmg = new DMG(rom);
        dmg.Run();
    }
}