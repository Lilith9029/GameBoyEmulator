using System.Text;

namespace GameBoyEmulator.Cartridge
{
    internal class CartridgeRom
    {
        byte[] rom;

        public CartridgeRom(string path)
        {
            rom = System.IO.File.ReadAllBytes(path);
        }

        public string GetTitle()
        {
            return Encoding.ASCII.GetString(rom, 0x0134, 16).TrimEnd('\0');
        }

        public byte GetCartridgeType()
        {
            return rom[0x0147];
        }

        public byte GetRomSize()
        {
            return rom[0x0148];
        }

        public byte GetRamSize()
        {
            return rom[0x0149];
        }

        public byte ReadByte(ushort address)
        {
            if (address < 0x8000)
                return rom[address];
            return 0xFF;
        }

        public void WriteByte(ushort address, byte value)
        {
            // ROM is read-only, so ignore writes
        }
    }   
}
