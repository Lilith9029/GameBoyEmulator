public class PPU
{
    private MMU _mmu;
    private byte[] _framebuffer = new byte[160 * 144 * 3];
    private int _cycles = 0;
    private int _mode = 2; // OAM

    public byte SCX;
    public byte SCY;

    private static readonly (byte r, byte g, byte b)[] _color = {
        (224, 248, 208), // White
        (136, 192, 112), // Light Gray
        (52, 104, 86),  // Dark Gray
        (8, 24, 32)      // Black
    };

    public byte[] Framebuffer => _framebuffer;
    public bool FrameReady { get; private set; }

    public PPU(MMU mmu)
    {
        _mmu = mmu;
    }

    public void Tick(int cycles)
    {
        _cycles += cycles;
        byte ly = _mmu.Read(0xFF44);

        switch (_mode)
        {
            case 2: // OAM
                if (_cycles >= 80)
                {
                    _cycles -= 80;
                    _mode = 3;
                }
                break;
            case 3: // Drawing
                if (_cycles >= 172)
                {
                    _cycles -= 172;
                    RenderScanline(ly);
                    _mode = 0;
                }
                break;
            case 0: // HBlank
                if (_cycles >= 204)
                {
                    _cycles -= 204;
                    _mmu.IncrementLY((byte)((ly + 1)));
                    ly++;

                    if (ly == 144)
                    {
                        _mode = 1; // VBlank
                        _mmu.RequestInterrupt(0); // VBlank interrupt
                        FrameReady = true;
                    }
                    else
                    {
                        _mode = 2;
                    }
                }
                break;
            case 1: // VBlank
                if (_cycles >= 456)
                {
                    _cycles -= 456;
                    _mmu.IncrementLY((byte)((ly + 1)));
                    ly++;

                    if (ly == 154)
                    {
                        _mmu.IncrementLY(0);
                        _mode = 2;
                        FrameReady = false;
                    }
                }
                break;

        }
    }

    private void RenderScanline(byte ly)
    {
        RenderBackground(ly);
    }

    private void RenderBackground(byte ly)
    {
        byte lcdc = _mmu.Read(0xFF40);
        if ((lcdc & 0x01) == 0) return;

        byte scx = _mmu.Read(0xFF43);
        byte scy = _mmu.Read(0xFF42);
        byte bgp = _mmu.Read(0xFF47);

        int[] palette = {
            (bgp & 0x03),
            (bgp >> 2) & 0x03,
            (bgp >> 4) & 0x03,
            (bgp >> 6) & 0x03
        };

        ushort tilemapBase = (lcdc & 0x08) != 0 ? (ushort)0x9C00 : (ushort)0x9800;
        bool isUnsignedMode = (lcdc & 0x10) != 0;
        int bgY = (ly + scy) % 256;
        int tileY = bgY / 8;
        int tileRowOffset = (bgY % 8) * 2;

        for (int x = 0; x < 160; x++)
        {
            int bgX = (x + scx) % 256;
            int tileX = bgX / 8;

            int titleIndex = tileY * 32 + tileX;
            byte tileNumber = _mmu.Read((ushort)(tilemapBase + titleIndex));

            ushort tileDataAddr = isUnsignedMode
                ? (ushort)(0x8000 + tileNumber * 16)
                : (ushort)(0x9000 + (sbyte)tileNumber * 16);

            byte tileLow = _mmu.Read((ushort)(tileDataAddr + tileRowOffset));
            byte tileHigh = _mmu.Read((ushort)(tileDataAddr + tileRowOffset + 1));

            byte bitIndex = (byte)(7 - (bgX % 8));
            int colorBit = ((tileHigh >> bitIndex) & 1) << 1 | ((tileLow >> bitIndex) & 1);

            var color = _color[palette[colorBit]];

            int index = (ly * 160 + x) * 3;
            _framebuffer[index] = color.r;
            _framebuffer[index + 1] = color.g;
            _framebuffer[index + 2] = color.b;
        }
    }
}