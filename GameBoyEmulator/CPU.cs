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
        set { if (value) F |= 0x80; else F &= 0x7F; } // Set OR clear bit 7
    }

    public bool FlagN
    {
        get => (F & 0x40) != 0;
        set { if (value) F |= 0x40; else F &= 0xBF; } // Set OR clear bit 6
    }

    public bool FlagH
    {
        get => (F & 0x20) != 0;
        set { if (value) F |= 0x20; else F &= 0xDF; } // Set OR clear bit 5
    }

    public bool FlagC
    {
        get => (F & 0x10) != 0;
        set { if (value) F |= 0x10; else F &= 0xEF; } // Set OR clear bit 4
    }

    public CPU()
    {
        A = 0x01;
        F = 0xB0; // Initial flags: Z=1, N=0, H=1, C=0
        BC = 0x0013;
        DE = 0x00D8;
        HL = 0x014D;
        PC = 0x0100; // Start of the program after the header
        SP = 0xFFFE; // Initial stack pointer
    }
}