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
        F = 0xB0; // Initial flags: Z=1, N=0, H=1, C=1 (0x00 for DMG, 0xB0 for CGB)
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

    private ushort PopWord()
    {
        byte lo = _mmu.Read(SP++);
        byte hi = _mmu.Read(SP++);
        return (ushort)(hi << 8 | lo);
    }

    private void PushPC()
    {
        _mmu.Write(--SP, (byte)(PC >> 8)); // High byte
        _mmu.Write(--SP, (byte)(PC & 0xFF)); // Low byte
    }

    public int ExecuteNext()
    {
        byte opcode = Fetch();
        
        switch (opcode)
        {
            case 0x00:
                return NOP();
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
    private int LD_HL_r(ref byte r)
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

    // 16-bit load instructions 
    private int LD_rr_nn(Action<ushort> rr)
    {
        rr(Fetch16());
        return 12;
    }

    private int LD_a16_rr(ushort rr) // LD (a16), rr -> 0x09
    {
        ushort addr = Fetch16();
        _mmu.Write(addr, (byte)(rr & 0xFF)); // Low byte
        _mmu.Write((ushort)(addr + 1), (byte)(rr >> 8)); // High byte
        return 20;
    }

    private int LD_SP_HL() // LD SP, HL -> 0xF9
    {
        SP = HL;
        return 8;
    }

    private int POP_rr(ref byte r1, ref byte r2)
    {
        r2 = _mmu.Read(SP++); // Low byte
        r1 = _mmu.Read(SP++); // High byte
        return 12;
    }

    private int POP_AF() // POP AF -> 0xF1
    {
        F = (byte)(_mmu.Read(SP++) & 0xF0); // Low byte (F) - only upper 4 bits are used
        A = _mmu.Read(SP++); // High byte
        return 12;
    }

    private int PUSH_rr(ref byte r1, ref byte r2)
    {
        _mmu.Write(--SP, r1); // High byte
        _mmu.Write(--SP, r2); // Low byte
        return 16;
    }

    private int PUSH_AF() // PUSH AF -> 0xF5
    {
        _mmu.Write(--SP, A); // High byte
        _mmu.Write(--SP, (byte)(F & 0xF0)); // Low byte
        return 16;
    }

    private int LD_HL_SP_e8() // LD HL, SP+e8 -> 0xF8
    {
        sbyte e8 = (sbyte)Fetch();
        int result = SP + e8;
        FlagZ = false;
        FlagN = false;
        FlagH = ((SP ^ e8 ^ result) & 0x10) != 0;
        FlagC = ((SP ^ e8 ^ result) & 0x100) != 0;
        return 12;
    }

    //  Jumps / calls instructions
    // JP (Jump) instructions
    private int JP_NZ_a16() // JP NZ, a16 -> 0xC2
    {
        if (!FlagZ) { PC = Fetch16(); return 16; }
        PC += 2;
        return 12;
    }

    private int JP_a16() // JP a16 -> 0xC3
    {
        PC = Fetch16();
        return 16;
    }

    private int JP_Z_a16() // JP Z, a16 -> 0xCA
    {
        if (FlagZ) { PC = Fetch16(); return 16; }
        PC += 2;
        return 12;
    }

    private int JP_NC_a16() // JP NC, a16 -> 0xD2
    {
        if (!FlagC) { PC = Fetch16(); return 16; }
        PC += 2;
        return 12;
    }

    private int JP_C_a16() // JP C, a16 -> 0xDA
    {
        if (FlagC) { PC = Fetch16(); return 16; }
        PC += 2;
        return 12;
    }

    private int JP_HL() // JP HL -> 0xE9
    {
        PC = HL;
        return 4;
    }

    // JR (Relative Jump) instructions
    private int JR_e8() // JR e8 -> 0x18
    {
        PC += (ushort)(sbyte)Fetch();
        return 12;
    }

    private int JR_NZ_e8() // JR NZ, e8 -> 0x20
    {
        if (!FlagZ) { PC += (ushort)(sbyte)Fetch(); return 12; }
        PC++;
        return 8;
    }

    private int JR_Z_e8() // JR Z, e8 -> 0x28
    {
        if (FlagZ) { PC += (ushort)(sbyte)Fetch(); return 12; }
        PC++;
        return 8;
    }

    private int JR_NC_e8() // JR NC, e8 -> 0x30
    {
        if (!FlagC) { PC += (ushort)(sbyte)Fetch(); return 12; }
        PC++;
        return 8;
    }

    private int JR_C_e8() // JR C, e8 -> 0x38
    {
        if (FlagC) { PC += (ushort)(sbyte)Fetch(); return 12; }
        PC++;
        return 8;
    }

    // CALL instructions
    private int CALL_NZ_a16() // CALL NZ, a16 -> 0xC4
    {
        ushort addr = Fetch16();
        if (!FlagZ) { PushPC(); PC = addr; return 24; }
        return 12;
    }

    private int CALL_Z_a16() // CALL Z, a16 -> 0xCC
    {
        ushort addr = Fetch16();
        if (FlagZ) { PushPC(); PC = addr; return 24; }
        return 12;
    }

    private int CALL_a16() // CALL a16 -> 0xCD
    {
        ushort addr = Fetch16();
        PushPC();
        PC = addr;
        return 24;
    }

    private int CALL_NC_a16() // CALL NC, a16 -> 0xD4
    {
        ushort addr = Fetch16();
        if (!FlagC) { PushPC(); PC = addr; return 24; }
        return 12;
    }

    private int CALL_C_a16() // CALL C, a16 -> 0xDA
    {
        ushort addr = Fetch16();
        if (FlagC) { PushPC(); PC = addr; return 24; }
        return 12;
    }

    // RET/RETI instructions
    private int RET_NZ() // RET NZ -> 0xC0
    {
        if (!FlagZ) { PC = PopWord(); return 20; }
        return 8;
    }

    private int RET_Z() // RET Z -> 0xC8
    {
        if (FlagZ) { PC = PopWord(); return 20; }
        return 8;
    }

    private int RET() // RET -> 0xC9
    {
        PC = PopWord();
        return 16;
    }

    private int RET_NC() // RET NC -> 0xD0
    {
        if (!FlagC) { PC = PopWord(); return 20; }
        return 8;
    }

    private int RET_C() // RET C -> 0xD8
    {
        if (FlagC) { PC = PopWord(); return 20; }
        return 8;
    }

    private int RETI() // RETI -> 0xD9
    {
        PC = PopWord();
        _ime = true; // Enable interrupts after returning
        return 16;
    }

    // 8-bit arithmetic / logical instructions
    // ADD (Add) instructions
    private int ADD_A_r(byte r)
    {
        int result = A + r;
        FlagZ = (byte)result == 0;
        FlagN = false;
        FlagH = ((A ^ r ^ result) & 0x10) != 0;
        FlagC = result > 0xFF;
        A = (byte)result;
        return 4;
    }

    private int ADD_A_HL() // ADD A, (HL) -> 0x86
    {
        byte value = _mmu.Read(HL);
        int result = A + value;
        FlagZ = (byte)result == 0;
        FlagN = false;
        FlagH = ((A ^ value ^ result) & 0x10) != 0;
        FlagC = result > 0xFF;
        A = (byte)result;
        return 8;

    }

    private int ADD_A_n8() // ADD A, n8 -> 0xC6
    {
        byte n8 = Fetch();
        int result = A + n8;
        FlagZ = (byte)result == 0;
        FlagN = false;
        FlagH = ((A ^ n8 ^ result) & 0x10) != 0;
        FlagC = result > 0xFF;
        A = (byte)result;
        return 8;
    }

    // ADC (Add with Carry) instructions
    private int ADC_A_r(byte r)
    {
        int carry = FlagC ? 1 : 0;
        int result = A + r + carry;
        FlagZ = (byte)result == 0;
        FlagN = false;
        FlagH = ((A & 0x0F) + (r & 0x0F) + carry) > 0x0F;
        FlagC = result > 0xFF;
        A = (byte)result;
        return 4;
    }

    private int ADC_A_HL() // ADC A, (HL) -> 0x8E
    {
        byte value = _mmu.Read(HL);
        int carry = FlagC ? 1 : 0;
        int result = A + value + carry;
        FlagZ = (byte)result == 0;
        FlagN = false;
        FlagH = ((A & 0x0F) + (value & 0x0F) + carry) > 0x0F;
        FlagC = result > 0xFF;
        A = (byte)result;
        return 8;
    }

    private int ADC_A_n8() // ADC A, n8 -> 0xCE
    {
        byte n8 = Fetch();
        int carry = FlagC ? 1 : 0;
        int result = A + n8 + carry;
        FlagZ = (byte)result == 0;
        FlagN = false;
        FlagH = ((A & 0x0F) + (n8 & 0x0F) + carry) > 0x0F;
        FlagC = result > 0xFF;
        A = (byte)result;
        return 8;
    }

    // SUB (Subtract) instructions
    private int SUB_A_r(byte r)
    {
        int result = A - r;
        FlagZ = (byte)result == 0;
        FlagN = true;
        FlagH = (A & 0x0F) < (r & 0x0F);
        FlagC = A < r;
        A = (byte)result;
        return 4;
    }

    private int SUB_A_HL() // SUB A, (HL) -> 0x96
    {
        byte value = _mmu.Read(HL);
        int result = A - value;
        FlagZ = (byte)result == 0;
        FlagN = true;
        FlagH = (A & 0x0F) < (value & 0x0F);
        FlagC = A < value;
        A = (byte)result;
        return 8;
    }

    private int SUB_A_n8() // SUB A, n8 -> 0xD6
    {
        byte n8 = Fetch();
        int result = A - n8;
        FlagZ = (byte)result == 0;
        FlagN = true;
        FlagH = (A & 0x0F) < (n8 & 0x0F);
        FlagC = A < n8;
        A = (byte)result;
        return 8;
    }

    // SBC (Subtract with Carry) instructions
    private int SBC_A_r(byte r)
    {
        int carry = FlagC ? 1 : 0;
        int result = A - r - carry;
        FlagZ = (byte)result == 0;
        FlagN = true;
        FlagH = (A & 0x0F) < (r & 0x0F) + carry;
        FlagC = (int)A - r - carry < 0;
        A = (byte)result;
        return 4;
    }

    private int SBC_A_HL() // SBC A, (HL) -> 0x9E
    {
        byte value = _mmu.Read(HL);
        int carry = FlagC ? 1 : 0;
        int result = A - value - carry;
        FlagZ = (byte)result == 0;
        FlagN = true;
        FlagH = (A & 0x0F) < (value & 0x0F) + carry;
        FlagC = (int)A - value - carry < 0;
        A = (byte)result;
        return 8;
    }

    private int SBC_A_n8() // SBC A, n8 -> 0xDE
    {
        byte n8 = Fetch();
        int carry = FlagC ? 1 : 0;
        int result = A - n8 - carry;
        FlagZ = (byte)result == 0;
        FlagN = true;
        FlagH = (A & 0x0F) < (n8 & 0x0F) + carry;
        FlagC = (int)A - n8 - carry < 0;
        A = (byte)result;
        return 8;
    }

    // AND (inclusive AND) instructions
    private int AND_A_r(byte r)
    {
        A &= r;
        FlagZ = A == 0;
        FlagN = false;
        FlagH = true;
        FlagC = false;
        return 4;
    }

    private int AND_A_HL() // AND A, (HL) -> 0xA6
    {
        A &= _mmu.Read(HL);
        FlagZ = A == 0;
        FlagN = false;
        FlagH = true;
        FlagC = false;
        return 8;
    }

    private int AND_A_n8() // AND A, n8 -> 0xE6
    {
        A &= Fetch();
        FlagZ = A == 0;
        FlagN = false;
        FlagH = true;
        FlagC = false;
        return 8;
    }

    // XOR (exclusive OR) instructions
    private int XOR_A_r(byte r)
    {
        A ^= r;
        FlagZ = A == 0;
        FlagN = false;
        FlagH = false;
        FlagC = false;
        return 4;
    }

    private int XOR_A_HL() // XOR A, (HL) -> 0xAE
    {
        A ^= _mmu.Read(HL);
        FlagZ = A == 0;
        FlagN = false;
        FlagH = false;
        FlagC = false;
        return 8;
    }

    private int XOR_A_n8() // XOR A, n8 -> 0xEE
    {
        A ^= Fetch();
        FlagZ = A == 0;
        FlagN = false;
        FlagH = false;
        FlagC = false;
        return 8;
    }

    // OR (inclusive OR) instructions
    private int OR_A_r(byte r)
    {
        A |= r;
        FlagZ = A == 0;
        FlagN = false;
        FlagH = false;
        FlagC = false;
        return 4;
    }

    private int OR_A_HL() // OR A, (HL) -> 0xB6
    {
        A |= _mmu.Read(HL);
        FlagZ = A == 0;
        FlagN = false;
        FlagH = false;
        FlagC = false;
        return 8;
    }

    private int OR_A_n8() // OR A, n8 -> 0xF6
    {
        A |= Fetch();
        FlagZ = A == 0;
        FlagN = false;
        FlagH = false;
        FlagC = false;
        return 8;
    }

    // CP (Compare) instructions
    private int CP_A_r(byte r)
    {
        int result = A - r;
        FlagZ = (byte)result == 0;
        FlagN = true;
        FlagH = (A & 0x0F) < (r & 0x0F);
        FlagC = A < r;
        return 4;
    }

    private int CP_A_HL() // CP A, (HL) -> 0xBE
    {
        byte value = _mmu.Read(HL);
        int result = A - value;
        FlagZ = (byte)result == 0;
        FlagN = true;
        FlagH = (A & 0x0F) < (value & 0x0F);
        FlagC = A < value;
        return 8;
    }

    private int CP_A_n8() // CP A, n8 -> 0xFE
    {
        byte n8 = Fetch();
        int result = A - n8;
        FlagZ = (byte)result == 0;
        FlagN = true;
        FlagH = (A & 0x0F) < (n8 & 0x0F);
        FlagC = A < n8;
        return 8;
    }

    // INC (Increment) instructions
    private int INC_r(ref byte r)
    {
        FlagH = (r & 0x0F) == 0x0F;
        r++;
        FlagZ = r == 0;
        FlagN = false;
        return 4;
    }

    private int INC_HL() // INC (HL) -> 0x34
    {
        byte value = _mmu.Read(HL);
        FlagH = (value & 0x0F) == 0x0F;
        value++;
        _mmu.Write(HL, value);
        FlagZ = value == 0;
        FlagN = false;
        return 12;
    }

    // DEC (Decrement) instructions
    private int DEC_r(ref byte r)
    {
        FlagH = (r & 0x0F) == 0x00;
        r--;
        FlagZ = r == 0;
        FlagN = true;
        return 4;
    }

    private int DEC_HL() // DEC (HL) -> 0x35
    {
        byte value = _mmu.Read(HL);
        FlagH = (value & 0x0F) == 0x00;
        value--;
        _mmu.Write(HL, value);
        FlagZ = value == 0;
        FlagN = true;
        return 12;
    }

    // DAA, CPL, SCF, CCF
    private int DAA()
    {
        int adjustment = 0;
        bool newCarry = FlagC;

        if (!FlagC) // before ADD, ADC
        {
            if (FlagH || (A & 0x0F) > 0x09) adjustment |= 0x06;
            if (FlagC || A > 0x99)
            {
                adjustment |= 0x60;
                newCarry = true;
            } 
        }
        else // before SUB, SBC, CP
        {
            if (FlagH) adjustment |= 0x06;
            if (FlagC) adjustment |= 0x60;
        }

        if (FlagN) A -= (byte)adjustment;
        else A += (byte)adjustment;

        FlagZ = A == 0;
        FlagH = false;
        FlagC = newCarry;

        return 4;
    }

    private int CPL()
    {
        A = (byte)~A;
        FlagN = true;
        FlagH = true;
        return 4;
    }

    private int SCF()
    {
        FlagN = false;
        FlagH = false;
        FlagC = true;
        return 4;
    }

    private int CCF()
    {
        FlagN = false;
        FlagH = false;
        FlagC = !FlagC;
        return 4;
    }

    // 16-bit arithmetic / logical instructions
    private int INC_rr(Func<ushort> get, Action<ushort> set)
    {
        ushort value = get();
        value++;
        set(value);
        // set((ushort)(get() + 1));
        return 8; 
    }

    private int ADD_HL_rr(ushort rr)
    {
        int result = HL + rr;
        FlagN = false;
        FlagH = ((HL & 0x0FFF) + (rr & 0x0FFF)) > 0x0FFF;
        FlagC = result > 0xFFFF;
        HL = (ushort)result;
        return 8;
    }

    private int DEC_rr(Func<ushort> get, Action<ushort> set)
    {
        ushort value = get();
        value--;
        set(value);
        // set((ushort)(get() - 1));
        return 8;
    }

    private int ADD_SP_e8()
    {
        sbyte e8 = (sbyte)Fetch();
        int result = SP + e8;
        FlagZ = false;
        FlagN = false;
        FlagH = ((SP & 0x0F) + (e8 & 0x0F)) > 0x0F;
        FlagC = ((SP & 0xFF) + (e8 & 0xFF)) > 0xFF;
        SP = (ushort)result;
        return 16;
    }

    // Misc / control instructions
    private int NOP()
    {
        return 4;   
    }

    private int STOP()
    {
        PC++;
        return 4;
    }

    private int HALT()
    {
        _halted = true;
        return 4;
    }

    private int CB_PREFIX()
    {
        byte cbOpcode = Fetch();
        Console.WriteLine($"CB opcode not implemented: 0x{cbOpcode:X2}");
        return 8;
    }

    private int DI()
    {
        _ime = false;
        return 4;
    }

    private int EI()
    {
        _ime = true;
        return 4;
    }   

    // 8-bit shift, rotate and bit instructions
}
