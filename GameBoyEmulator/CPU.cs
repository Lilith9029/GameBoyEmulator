public class CPU
{
    // 8-bit registers
    public byte A, B, C, D, E, H, L;
    private byte _f;
    public byte F
    {
        get => _f;
        set => _f = (byte)(value & 0xF0);
        // Flags register: Z, N, H, C (Bit 7-4), lower 4 bits are always 0
    }

    public ushort PC; // Program Counter
    public ushort SP; // Stack Pointer

    public bool _ime; // Interrupt Master Enable
    private bool _halted; // CPU halted state

    private MMU _mmu;

    public ushort AF
    {
        get => (ushort)((A << 8) | F);
        set { A = (byte)(value >> 8); F = (byte)(value & 0xFF); }
    }

    public ushort BC
    {
        get => (ushort)((B << 8) | C);
        set { B = (byte)(value >> 8); C = (byte)(value & 0xFF); }
    }

    public ushort DE
    {
        get => (ushort)((D << 8) | E);
        set { D = (byte)(value >> 8); E = (byte)(value & 0xFF); }
    }

    public ushort HL
    {
        get => (ushort)((H << 8) | L);
        set { H = (byte)(value >> 8); L = (byte)(value & 0xFF); }
    }

    public bool FlagZ
    {
        get => (F & 0x80) != 0;
        set { if (value) F |= 0x80; else F &= 0x7F; }
    }

    public bool FlagN
    {
        get => (F & 0x40) != 0;
        set { if (value) F |= 0x40; else F &= 0xBF; }
    }

    public bool FlagH
    {
        get => (F & 0x20) != 0;
        set { if (value) F |= 0x20; else F &= 0xDF; }
    }

    public bool FlagC
    {
        get => (F & 0x10) != 0;
        set { if (value) F |= 0x10; else F &= 0xEF; }
    }

    public CPU(MMU mmu)
    {
        A = 0x01;
        F = 0xB0; // Initial flags: Z=1, N=0, H=1, C=1
        BC = 0x0013;
        DE = 0x00D8;
        HL = 0x014D;
        PC = 0x0100; // Start of the program after the header
        SP = 0xFFFE; // Initial stack pointer

        _mmu = mmu;
    }

    private byte Fetch()
    {
        return _mmu.Read(PC++);
    }

    private ushort Fetch16()
    {
        byte lo = Fetch();
        byte hi = Fetch();
        return (ushort)(hi << 8 | lo);
    }

    public int ExecuteNext()
    {
        byte opcode = _mmu.Read(PC++);

        switch (opcode)
        {
            case 0x02: // LD (BC), A
                return LD_rr_A(BC);
            case 0x06: // LD B, n8
                return LD_r_n(ref B);
            case 0x0A: // LD A, (BC)
                return LD_A_rr(BC);
            case 0x0E: // LD C, n8
                return LD_r_n(ref C);
            case 0x12: // LD (DE), A
                return LD_rr_A(DE);
            case 0x16: // LD D, n8
                return LD_r_n(ref D);
            case 0x1A: // LD A, (DE)
                return LD_A_rr(DE);
            case 0x1E: // LD E, n8
                return LD_r_n(ref E);
            case 0x22: // LD (HL+), A
                return LD_HLI_A();
            case 0x26: // LD H, n8
                return LD_r_n(ref H);
            case 0x2A: // LD A, (HL+)
                return LD_A_HLI();
            case 0x2E: // LD L, n8
                return LD_r_n(ref L);
            case 0x32: // LD (HL-), A
                return LD_HLD_A();
            case 0x36: // LD (HL), n8
                return LD_HL_n();
            case 0x3A: // LD A, (HL-)
                return LD_A_HLD();
            case 0x3E: // LD A, n8
                return LD_r_n(ref A);
            case 0x40: // LD B, B
                return LD_r_r(ref B, B);
            case 0x41: // LD B, C
                return LD_r_r(ref B, C);
            case 0x42: // LD B, D
                return LD_r_r(ref B, D);
            case 0x43: // LD B, E
                return LD_r_r(ref B, E);
            case 0x44: // LD B, H
                return LD_r_r(ref B, H);
            case 0x45: // LD B, L
                return LD_r_r(ref B, L);
            case 0x46: // LD B, (HL)
                return LD_r_HL(ref B);
            case 0x47: // LD B, A
                return LD_r_r(ref B, A);
            case 0x48: // LD C, B
                return LD_r_r(ref C, B);
            case 0x49: // LD C, C
                return LD_r_r(ref C, C);
            case 0x4A: // LD C, D
                return LD_r_r(ref C, D);
            case 0x4B: // LD C, E
                return LD_r_r(ref C, E);
            case 0x4C: // LD C, H
                return LD_r_r(ref C, H);
            case 0x4D: // LD C, L
                return LD_r_r(ref C, L);
            case 0x4E: // LD C, (HL)
                return LD_r_HL(ref C);
            case 0x4F: // LD C, A
                return LD_r_r(ref C, A);
            case 0x50: // LD D, B
                return LD_r_r(ref D, B);
            case 0x51: // LD D, C
                return LD_r_r(ref D, C);
            case 0x52: // LD D, D
                return LD_r_r(ref D, D);
            case 0x53: // LD D, E
                return LD_r_r(ref D, E);
            case 0x54: // LD D, H
                return LD_r_r(ref D, H);
            case 0x55: // LD D, L
                return LD_r_r(ref D, L);
            case 0x56: // LD D, (HL)
                return LD_r_HL(ref D);
            case 0x57: // LD D, A
                return LD_r_r(ref D, A);
            case 0x58: // LD E, B
                return LD_r_r(ref E, B);
            case 0x59: // LD E, C
                return LD_r_r(ref E, C);
            case 0x5A: // LD E, D
                return LD_r_r(ref E, D);
            case 0x5B: // LD E, E
                return LD_r_r(ref E, E);
            case 0x5C: // LD E, H
                return LD_r_r(ref E, H);
            case 0x5D: // LD E, L
                return LD_r_r(ref E, L);
            case 0x5E: // LD E, (HL)
                return LD_r_HL(ref E);
            case 0x5F: // LD E, A
                return LD_r_r(ref E, A);
            case 0x60: // LD H, B
                return LD_r_r(ref H, B);
            case 0x61: // LD H, C
                return LD_r_r(ref H, C);
            case 0x62: // LD H, D
                return LD_r_r(ref H, D);
            case 0x63: // LD H, E
                return LD_r_r(ref H, E);
            case 0x64: // LD H, H
                return LD_r_r(ref H, H);
            case 0x65: // LD H, L
                return LD_r_r(ref H, L);
            case 0x66: // LD H, (HL)
                return LD_r_HL(ref H);
            case 0x67: // LD H, A
                return LD_r_r(ref H, A);
            case 0x68: // LD L, B
                return LD_r_r(ref L, B);
            case 0x69: // LD L, C
                return LD_r_r(ref L, C);
            case 0x6A: // LD L, D
                return LD_r_r(ref L, D);
            case 0x6B: // LD L, E
                return LD_r_r(ref L, E);
            case 0x6C: // LD L, H
                return LD_r_r(ref L, H);
            case 0x6D: // LD L, L
                return LD_r_r(ref L, L);
            case 0x6E: // LD L, (HL)
                return LD_r_HL(ref L);
            case 0x6F: // LD L, A
                return LD_r_r(ref L, A);
            case 0x70: // LD (HL), B
                return LD_HL_r(B);
            case 0x71: // LD (HL), C
                return LD_HL_r(C);
            case 0x72: // LD (HL), D
                return LD_HL_r(D);
            case 0x73: // LD (HL), E
                return LD_HL_r(E);
            case 0x74: // LD (HL), H
                return LD_HL_r(H);
            case 0x75: // LD (HL), L
                return LD_HL_r(L);
            case 0x77: // LD (HL), A
                return LD_HL_r(A);
            case 0x78: // LD A, B
                return LD_r_r(ref A, B);
            case 0x79: // LD A, C
                return LD_r_r(ref A, C);
            case 0x7A: // LD A, D
                return LD_r_r(ref A, D);
            case 0x7B: // LD A, E
                return LD_r_r(ref A, E);
            case 0x7C: // LD A, H
                return LD_r_r(ref A, H);
            case 0x7D: // LD A, L
                return LD_r_r(ref A, L);
            case 0x7E: // LD A, (HL)
                return LD_r_HL(ref A);
            case 0x7F: // LD A, A
                return LD_r_r(ref A, A);
            case 0xE0: // LDH (a8), A
                return LDH_a8_A();
            case 0xE2: // LDH (C), A
                return LDH_C_A();
            case 0xEA: // LD (a16), A
                return LD_a16_A();
            case 0xF0: // LDH A, (a8)
                return LDH_A_a8();
            case 0xF2: // LDH A, (C)
                return LDH_A_C();
            case 0xFA: // LD A, (a16)
                return LD_A_a16();
            default:
                Console.WriteLine($"Unknown opcode: 0x{opcode:X2} at PC: 0x{PC - 1:X4}");
                return 4;
        }
    }

    // 8-bit load instructions
    private int LD_r_n(ref byte r)
    {
        byte n8 = Fetch();
        r = n8;
        return 8;
    }

    private int LD_r_r(ref byte r1, byte r2)
    {
        r1 = r2;
        return 4;
    }

    private int LD_r_HL(ref byte r)
    {
        r = _mmu.Read(HL);
        return 8;
    }
    private int LD_HL_r(byte r)
    {
        _mmu.Write(HL, r);
        return 8;
    }

    private int LD_HL_n() // LD (HL), n8 -> 0x36
    {
        byte n8 = Fetch();
        _mmu.Write(HL, n8);
        return 12;
    }

    private int LD_rr_A(ushort r)
    {
        _mmu.Write(r, A);
        return 8;
    }

    private int LD_A_rr(ushort r)
    {
        A = _mmu.Read(r);
        return 8;
    }

    private int LD_HLI_A() // LD (HL+), A -> 0x22
    {
        _mmu.Write(HL, A);
        HL++;
        return 8;
    }

    private int LD_HLD_A() // LD (HL-), A -> 0x32
    {
        _mmu.Write(HL, A);
        HL--;
        return 8;
    }

    private int LD_A_HLI() // LD A, (HL+) -> 0x2A
    {
        A = _mmu.Read(HL);
        HL++;
        return 8;
    }

    private int LD_A_HLD() // LD A, (HL-) -> 0x3A
    {
        A = _mmu.Read(HL);
        HL--;
        return 8;
    }

    private int LDH_a8_A() // LDH (a8), A -> 0xE0
    {
        _mmu.Write((ushort)(0xFF00 + Fetch()), A);
        return 12;
    }

    private int LDH_A_a8() // LDH A, (a8) -> 0xF0
    {
        A = _mmu.Read((ushort)(0xFF00 + Fetch()));
        return 12;
    }

    private int LDH_C_A() // LDH (C), A -> 0xE2
    {
        _mmu.Write((ushort)(0xFF00 + C), A);
        return 8;
    }

    private int LDH_A_C() // LDH A, (C) -> 0xF2
    {
        A = _mmu.Read((ushort)(0xFF00 + C));
        return 8;
    }

    private int LD_a16_A()
    {
        _mmu.Write(Fetch16(), A);
        return 16;
    }

    private int LD_A_a16()
    {
        A = _mmu.Read(Fetch16());
        return 16;
    }
}
