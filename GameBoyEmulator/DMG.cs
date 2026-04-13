public class DMG
{
    public CPU _cpu;
    public MMU _mmu;
    public Timer _timer;
    public PPU _ppu;
    public RendererDMG _renderer;

    private int _scanlineCycles = 0;

    public DMG(byte[] rom)
    {
        _mmu = new MMU(rom);
        _cpu = new CPU(_mmu);
        _timer = new Timer(_mmu);
        _ppu = new PPU(_mmu);
        _renderer = new RendererDMG();
    }

    public void Run()
    {
        const double TargetFrameTime = 1000.0 / 59.73; // ~16.75ms
        const int CyclesPerFrame = 70224;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (_renderer.HandleEvents())
        {
            long startTime = sw.ElapsedMilliseconds;
            int cyclesThisFrame = 0;

            while (cyclesThisFrame < CyclesPerFrame)
            {
                int cycles = _cpu.ExecuteInstruction();
                _timer.Tick(cycles);
                _ppu.Tick(cycles);
                HandleInperrupts();

                cyclesThisFrame += cycles;
            }

            _renderer.Render(_ppu.Framebuffer);

            while (sw.ElapsedMilliseconds - startTime < TargetFrameTime)
            {
                System.Threading.Thread.Sleep(0);
            }
        }

        _renderer.Destroy();
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
