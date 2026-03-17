using GameBoyEmulator.Cartridge;

namespace GameBoyEmulator.Memory
{
    internal class Memory
    {
        CartridgeRom cartridge;
        byte[] wram;
        byte[] hram;

        public Memory(CartridgeRom cartridge)
        {
            this.cartridge = cartridge;
            wram = new byte[0x2000];
            hram = new byte[0x7F];
        }

        public byte ReadByte(ushort address)
        {
            if (address < 0x8000)
                return cartridge.ReadByte(address);
            if (address >= 0xC000 && address <= 0xDFFF)
                return wram[address - 0xC000];
            if (address >= 0xFF80 && address <= 0xFFFE)
                return hram[address - 0xFF80];
            return 0xFF;
        }

        public void WriteByte(ushort address, byte value)
        {

        }
    }
}
