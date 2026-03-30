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
            case 0x01: // LD BC, n16
                return LD_rr_nn(val => BC = val);
            case 0x02: // LD (BC), A
                return LD_rr_A(BC);
            case 0x03: // INC BC
                return INC_rr(() => BC, val => BC = val);
            case 0x04: // INC B
                return INC_r(ref B);
            case 0x05: // DEC B
                return DEC_r(ref B);
            case 0x06: // LD B, n8
                return LD_r_n(ref B);
            case 0x07: // RLCA
                return RLCA();
            case 0x08: // LD (a16), SP
                return LD_a16_rr(SP);
            case 0x09: // ADD HL, BC
                return ADD_HL_rr(BC);
            case 0x0A: // LD A, (BC)
                return LD_A_rr(BC);
            case 0x0B: // DEC BC
                return DEC_rr(() => BC, val => BC = val);
            case 0x0C: // INC C
                return INC_r(ref C);
            case 0x0D: // DEC C
                return DEC_r(ref C);
            case 0x0E: // LD C, n8
                return LD_r_n(ref C);
            case 0x0F: // RRCA
                return RRCA();
            case 0x10: // STOP
                return STOP();
            case 0x11: // LD DE, n16
                return LD_rr_nn(val => DE = val);
            case 0x12: // LD (DE), A
                return LD_rr_A(DE);
            case 0x13: // INC DE
                return INC_rr(() => DE, val => DE = val);
            case 0x14: // INC D
                return INC_r(ref D);
            case 0x15: // DEC D
                return DEC_r(ref D);
            case 0x16: // LD D, n8
                return LD_r_n(ref D);
            case 0x17: // RLA
                return RLA();
            case 0x18: // JR e8
                return JR_e8();
            case 0x19: // ADD HL, DE
                return ADD_HL_rr(DE);
            case 0x1A: // LD A, (DE)
                return LD_A_rr(DE);
            case 0x1B: // DEC DE
                return DEC_rr(() => DE, val => DE = val);
            case 0x1C: // INC E
                return INC_r(ref E);
            case 0x1D: // DEC E
                return DEC_r(ref E);
            case 0x1E: // LD E, n8
                return LD_r_n(ref E);
            case 0x1F: // RRA
                return RRA();
            case 0x20: // JR NZ, e8
                return JR_NZ_e8();
            case 0x21: // LD HL, n16
                return LD_rr_nn(val => HL = val);
            case 0x22: // LD (HL+), A
                return LD_HLI_A();
            case 0x23: // INC HL
                return INC_rr(() => HL, val => HL = val);
            case 0x24: // INC H
                return INC_r(ref H);
            case 0x25: // DEC H
                return DEC_r(ref H);
            case 0x26: // LD H, n8
                return LD_r_n(ref H);
            case 0x27: // DAA
                return DAA();
            case 0x28: // JR Z, e8
                return JR_Z_e8();
            case 0x29: // ADD HL, HL
                return ADD_HL_rr(HL);
            case 0x2A: // LD A, (HL+)
                return LD_A_HLI();
            case 0x2B: // DEC HL
                return DEC_rr(() => HL, val => HL = val);
            case 0x2C: // INC L
                return INC_r(ref L);
            case 0x2D: // DEC L
                return DEC_r(ref L);
            case 0x2E: // LD L, n8
                return LD_r_n(ref L);
            case 0x2F: // CPL
                return CPL();
            case 0x30: // JR NC, e8
                return JR_NC_e8();
            case 0x31: // LD SP, n16
                return LD_rr_nn(val => SP = val);
            case 0x32: // LD (HL-), A
                return LD_HLD_A();
            case 0x33: // INC SP
                return INC_rr(() => SP, val => SP = val);
            case 0x34: // INC (HL)
                return INC_HL();
            case 0x35: // DEC (HL)
                return DEC_HL();
            case 0x36: // LD (HL), n8
                return LD_HL_n();
            case 0x37: // SCF
                return SCF();
            case 0x38: // JR C, e8
                return JR_C_e8();
            case 0x39: // ADD HL, SP
                return ADD_HL_rr(SP);
            case 0x3A: // LD A, (HL-)
                return LD_A_HLD();
            case 0x3B: // DEC SP
                return DEC_rr(() => SP, val => SP = val);
            case 0x3C: // INC A
                return INC_r(ref A);
            case 0x3D: // DEC A
                return DEC_r(ref A);
            case 0x3E: // LD A, n8
                return LD_r_n(ref A);
            case 0x3F: // CCF
                return CCF();
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
                return LD_HL_r(ref B);
            case 0x71: // LD (HL), C
                return LD_HL_r(ref C);
            case 0x72: // LD (HL), D
                return LD_HL_r(ref D);
            case 0x73: // LD (HL), E
                return LD_HL_r(ref E);
            case 0x74: // LD (HL), H
                return LD_HL_r(ref H);
            case 0x75: // LD (HL), L
                return LD_HL_r(ref L);
            case 0x76: // HALT
                return HALT();
            case 0x77: // LD (HL), A
                return LD_HL_r(ref A);
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
            case 0x80: // ADD A, B
                return ADD_A_r(B);
            case 0x81: // ADD A, C
                return ADD_A_r(C);
            case 0x82: // ADD A, D
                return ADD_A_r(D);
            case 0x83: // ADD A, E
                return ADD_A_r(E);
            case 0x84: // ADD A, H
                return ADD_A_r(H);
            case 0x85: // ADD A, L
                return ADD_A_r(L);
            case 0x86: // ADD A, (HL)
                return ADD_A_HL();
            case 0x87: // ADD A, A
                return ADD_A_r(A);
            case 0x88: // ADC A, B
                return ADC_A_r(B);
            case 0x89: // ADC A, C
                return ADC_A_r(C);
            case 0x8A: // ADC A, D
                return ADC_A_r(D);
            case 0x8B: // ADC A, E
                return ADC_A_r(E);
            case 0x8C: // ADC A, H
                return ADC_A_r(H);
            case 0x8D: // ADC A, L
                return ADC_A_r(L);
            case 0x8E: // ADC A, (HL)
                return ADC_A_HL();
            case 0x8F: // ADC A, A
                return ADC_A_r(A);
            case 0x90: // SUB A, B
                return SUB_A_r(B);
            case 0x91: // SUB A, C
                return SUB_A_r(C);
            case 0x92: // SUB A, D
                return SUB_A_r(D);
            case 0x93: // SUB A, E
                return SUB_A_r(E);
            case 0x94: // SUB A, H
                return SUB_A_r(H);
            case 0x95: // SUB A, L
                return SUB_A_r(L);
            case 0x96: // SUB A, (HL)
                return SUB_A_HL();
            case 0x97: // SUB A, A
                return SUB_A_r(A);
            case 0x98: // SBC A, B
                return SBC_A_r(B);
            case 0x99: // SBC A, C
                return SBC_A_r(C);
            case 0x9A: // SBC A, D
                return SBC_A_r(D);
            case 0x9B: // SBC A, E
                return SBC_A_r(E);
            case 0x9C: // SBC A, H
                return SBC_A_r(H);
            case 0x9D: // SBC A, L
                return SBC_A_r(L);
            case 0x9E: // SBC A, (HL)
                return SBC_A_HL();
            case 0x9F: // SBC A, A
                return SBC_A_r(A);
            case 0xA0: // AND A, B
                return AND_A_r(B);
            case 0xA1: // AND A, C
                return AND_A_r(C);
            case 0xA2: // AND A, D
                return AND_A_r(D);
            case 0xA3: // AND A, E
                return AND_A_r(E);
            case 0xA4: // AND A, H
                return AND_A_r(H);
            case 0xA5: // AND A, L
                return AND_A_r(L);
            case 0xA6: // AND A, (HL)
                return AND_A_HL();
            case 0xA7: // AND A, A
                return AND_A_r(A);
            case 0xA8: // XOR A, B
                return XOR_A_r(B);
            case 0xA9: // XOR A, C
                return XOR_A_r(C);
            case 0xAA: // XOR A, D
                return XOR_A_r(D);
            case 0xAB: // XOR A, E
                return XOR_A_r(E);
            case 0xAC: // XOR A, H
                return XOR_A_r(H);
            case 0xAD: // XOR A, L
                return XOR_A_r(L);
            case 0xAE: // XOR A, (HL)
                return XOR_A_HL();
            case 0xAF: // XOR A, A
                return XOR_A_r(A);
            case 0xB0: // OR A, B
                return OR_A_r(B);
            case 0xB1: // OR A, C
                return OR_A_r(C);
            case 0xB2: // OR A, D
                return OR_A_r(D);
            case 0xB3: // OR A, E
                return OR_A_r(E);
            case 0xB4: // OR A, H
                return OR_A_r(H);
            case 0xB5: // OR A, L
                return OR_A_r(L);
            case 0xB6: // OR A, (HL)
                return OR_A_HL();
            case 0xB7: // OR A, A
                return OR_A_r(A);
            case 0xB8: // CP A, B
                return CP_A_r(B);
            case 0xB9: // CP A, C
                return CP_A_r(C);
            case 0xBA: // CP A, D
                return CP_A_r(D);
            case 0xBB: // CP A, E
                return CP_A_r(E);
            case 0xBC: // CP A, H
                return CP_A_r(H);
            case 0xBD: // CP A, L
                return CP_A_r(L);
            case 0xBE: // CP A, (HL)
                return CP_A_HL();
            case 0xBF: // CP A, A
                return CP_A_r(A);
            case 0xC0: // RET NZ
                return RET_NZ();
            case 0xC1: // POP BC
                return POP_rr(ref B, ref C);
            case 0xC2: // JP NZ, a16
                return JP_NZ_a16();
            case 0xC3: // JP a16
                return JP_a16();
            case 0xC4: // CALL NZ, a16
                return CALL_NZ_a16();
            case 0xC5: // PUSH BC
                return PUSH_rr(ref B, ref C);
            case 0xC6: // ADD A, n8
                return ADD_A_n8();
            case 0xC7: // RST 00H
                return RST(0x00);
            case 0xC8: // RET Z
                return RET_Z();
            case 0xC9: // RET
                return RET();
            case 0xCA: // JP Z, a16
                return JP_Z_a16();
            case 0xCB: // CB prefix for extended instructions
                return CB_PREFIX(); // Placeholder for CB-prefixed instructions
            case 0xCC: // CALL Z, a16
                return CALL_Z_a16();
            case 0xCD: // CALL a16
                return CALL_a16();
            case 0xCE: // ADC A, n8
                return ADC_A_n8();
            case 0xCF: // RST 08H
                return RST(0x08);
            case 0xD0: // RET NC
                return RET_NC();
            case 0xD1: // POP DE
                return POP_rr(ref D, ref E);
            case 0xD2: // JP NC, a16
                return JP_NC_a16();
            case 0xD3: // NOT USED
                // Not used in the original Game 
            case 0xD4: // CALL NC, a16
                return CALL_NC_a16();
            case 0xD5: // PUSH DE
                return PUSH_rr(ref D, ref E);
            case 0xD6: // SUB A, n8
                return SUB_A_n8();
            case 0xD7: // RST 10H
                return RST(0x10);
            case 0xD8: // RET C
                return RET_C();
            case 0xD9: // RETI
                return RETI();
            case 0xDA: // JP C, a16
                return JP_C_a16();
            case 0xDB: // NOT USED
                // Not used in the original Game 
            case 0xDC: // CALL C, a16
                return CALL_C_a16();
            case 0xDD: // NOT USED
                // Not used in the original Game 
            case 0xDE: // SBC A, n8
                return SBC_A_n8();
            case 0xDF: // RST 18H
                return RST(0x18);
            case 0xE0: // LDH (a8), A
                return LDH_a8_A();
            case 0xE1: // POP HL
                return POP_rr(ref H, ref L);
            case 0xE2: // LDH (C), A
                return LDH_C_A();
            case 0xE3: // NOT USED
                // Not used in the original Game 
            case 0xE4: // NOT USED
                // Not used in the original Game 
            case 0xE5: // PUSH HL
                return PUSH_rr(ref H, ref L);
            case 0xE6: // AND A, n8
                return AND_A_n8();
            case 0xE7: // RST 20H
                return RST(0x20);
            case 0xE8: // ADD SP, e8
                return ADD_SP_e8();
            case 0xE9: // JP HL
                return JP_HL();
            case 0xEA: // LD (a16), A
                return LD_a16_A();
            case 0xEB: // NOT USED
                // Not used in the original Game 
            case 0xEC: // NOT USED
                // Not used in the original Game 
            case 0xED: // NOT USED
                // Not used in the original Game 
            case 0xEE: // XOR A, n8
                return XOR_A_n8();
            case 0xEF: // RST 28H
                return RST(0x28);
            case 0xF0: // LDH A, (a8)
                return LDH_A_a8();
            case 0xF1: // POP AF
                return POP_AF();
            case 0xF2: // LDH A, (C)
                return LDH_A_C();
            case 0xF3: // DI
                return DI();
            case 0xF4: // NOT USED
                // Not used in the original Game 
            case 0xF5: // PUSH AF
                return PUSH_AF();
            case 0xF6: // OR A, n8
                return OR_A_n8();
            case 0xF7: // RST 30H
                return RST(0x30);
            case 0xF8: // LD HL, SP+e8
                return LD_HL_SP_e8();
            case 0xF9: // LD SP, HL
                return LD_SP_HL();
            case 0xFA: // LD A, (a16)
                return LD_A_a16();
            case 0xFB: // EI
                return EI();
            case 0xFC: // NOT USED
                // Not used in the original Game 
            case 0xFD: // NOT USED
                // Not used in the original Game 
            case 0xFE: // CP A, n8
                return CP_A_n8();
            case 0xFF: // RST 38H
                return RST(0x38);
            default:
                Console.WriteLine($"Invalid opcode: 0x{opcode:X2} at PC: 0x{PC - 1:X4}");
                _halted = true; // Halt on invalid opcode
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

    // RST (Restart) instructions
    private int RST(ushort vector)
    {
        PushPC();
        PC = vector;
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
    private int RLCA()
    {
        FlagC = (A & 0x80) != 0;
        A = (byte)((A << 1) | (A >> 7));
        FlagZ = false;
        FlagN = false;
        FlagH = false;
        return 4;
    }

    private int RRCA()
    {
        FlagC = (A & 0x01) != 0;
        A = (byte)((A >> 1) | (A << 7));
        FlagZ = false;
        FlagN = false;
        FlagH = false;
        return 4;
    }

    private int RLA()
    {
        byte oldCarry = (byte)(FlagC ? 1 : 0);
        FlagC = (A & 0x80) != 0;
        A = (byte)((A << 1) | oldCarry);
        FlagZ = false;
        FlagN = false;
        FlagH = false;
        return 4;
    }

    private int RRA()
    {
        byte oldCarry = (byte)(FlagC ? 1 : 0);
        FlagC = (A & 0x01) != 0;
        A = (byte)((A >> 1) | (oldCarry << 7));
        FlagZ = false;
        FlagN = false;
        FlagH = false;
        return 4;
    }
}
