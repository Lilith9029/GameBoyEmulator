namespace GameBoyEmulator;

internal class Program
{
    static void Main(string[] args)
    {
        /*byte[] rom = File.ReadAllBytes("Pokemon - Yellow Version - Special Pikachu Edition (USA, Europe) (CGB+SGB Enhanced).gb");*/
        byte[] rom = File.ReadAllBytes("11-op a,(hl).gb");
        DMG dmg = new DMG(rom);
        dmg.Run();
    }
}