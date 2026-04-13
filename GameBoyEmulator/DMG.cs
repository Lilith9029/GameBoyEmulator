public class DMG
{
    public CPU _cpu;
    public MMU _mmu;
    public Timer _timer;

    private int _scanlineCycles = 0;

    public DMG(byte[] rom)
    {
        _mmu = new MMU(rom);
        _cpu = new CPU(_mmu);
        _timer = new Timer(_mmu);
    }

    /*public void Run()
    {
        while (true)
        {
            int cycles = _cpu.ExecuteInstruction();

            _timer.Tick(cycles);
            TickLY(cycles);
            HandleInperrupts();
        }
    }*/

    public void Run()
    {
        int instructionCount = 0;

        while (true)
        {
            // In ra 20 instruction đầu tiên
            if (instructionCount < 20)
            {
                Console.WriteLine($"PC=0x{_cpu.PC:X4} opcode=0x{_mmu.Read(_cpu.PC):X2}");
            }

            int cycles = _cpu.ExecuteInstruction();
            _timer.Tick(cycles);
            TickLY(cycles);
            HandleInperrupts();

            instructionCount++;
            
            // Dừng nếu quá lâu không thấy output
            if (instructionCount > 10_000_000)
            {
                Console.WriteLine($"[TIMEOUT] PC=0x{_cpu.PC:X4}");
                Console.WriteLine($"  AF=0x{_cpu.AF:X4} BC=0x{_cpu.BC:X4}");
                Console.WriteLine($"  DE=0x{_cpu.DE:X4} HL=0x{_cpu.HL:X4}");
                Console.WriteLine($"  SP=0x{_cpu.SP:X4} IME={_cpu._ime}");
                Console.WriteLine($"  IF=0x{_mmu.Read(0xFF0F):X2} IE=0x{_mmu.Read(0xFFFF):X2}");
                break;
            }

            // Dừng nếu có jump ra khỏi ROM
            ushort prevPC = _cpu.PC;

            // Bắt khi PC nhảy vào vùng không hợp lệ (không phải ROM, WRAM, HRAM)
            bool inROM = _cpu.PC < 0x8000;
            bool inWRAM = _cpu.PC >= 0xC000 && _cpu.PC < 0xE000;
            bool inHRAM = _cpu.PC >= 0xFF80 && _cpu.PC < 0xFFFF;

            if (!inROM && !inWRAM && !inHRAM)
            {
                Console.WriteLine($"[BAD PC] 0x{prevPC:X4} -> 0x{_cpu.PC:X4}  opcode=0x{_mmu.Read(prevPC):X2}");
                Console.WriteLine($"  AF=0x{_cpu.AF:X4} BC=0x{_cpu.BC:X4}");
                Console.WriteLine($"  DE=0x{_cpu.DE:X4} HL=0x{_cpu.HL:X4}");
                Console.WriteLine($"  SP=0x{_cpu.SP:X4}");
                break;
            }
        }
    }

    private void TickLY(int cycles)
    {
        _scanlineCycles += cycles;
        if (_scanlineCycles >= 456)
        {
            _scanlineCycles -= 456;
            byte ly = (byte)((_mmu.Read(0xFF44) + 1) % 154);
            _mmu.IncrementLY(ly);

            if (ly == 144)
            {
                byte iF = _mmu.Read(0xFF0F);
                _mmu.Write(0xFF0F, (byte)(iF | 0x01));
            }
        }
    }

    private void HandleInperrupts()
    {
        byte IF = _mmu.Read(0xFF0F);
        byte IE = _mmu.Read(0xFFFF);
        byte pending = (byte)(IF & IE & 0x1F);

        if (pending == 0) return;

        _cpu.UnHault();

        if (!_cpu._ime) return;

        _cpu._ime = false;

        ushort[] vectors = { 0x0040, 0x0048, 0x0050, 0x0058, 0x0060 };
        for (int i = 0; i < 5; i++)
        {
            if ((pending & (1 << i)) != 0)
            {
                _mmu.Write(0xFF0F, (byte)(IF & ~(1 << i)));
                _cpu.CallInterrupt(vectors[i]);
                return;
            }
        }
    }
}
