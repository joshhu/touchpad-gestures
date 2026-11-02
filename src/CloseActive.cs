// Resident hotkey helper:
//   Ctrl+Alt+W  closes the foreground window; in Chrome it closes the current tab (Ctrl+W).
//   Ctrl+Alt+C  opens Chrome; in Chrome it opens a new tab (Ctrl+T).
// Ctrl+Alt+W ignores the desktop and taskbar so it never pops up the shutdown dialog. Runs in the background (started from the Startup folder) and
// uses RegisterHotKey so the foreground window isn't disturbed, unlike .lnk hotkeys,
// which hand focus to the taskbar before launching.
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

static class CloseActive {
    [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
    [DllImport("user32.dll")] static extern int GetMessage(out MSG m, IntPtr h, uint min, uint max);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern IntPtr GetAncestor(IntPtr h, uint flags);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll")] static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);
    [DllImport("user32.dll")] static extern short GetAsyncKeyState(int vk);
    [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);

    [StructLayout(LayoutKind.Sequential)]
    struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam, lParam; public uint time; public int x, y; }

    const uint WM_HOTKEY = 0x0312, WM_CLOSE = 0x0010, KEYUP = 0x0002;
    const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_NOREPEAT = 0x4000;
    const byte VK_CONTROL = 0x11, VK_MENU = 0x12, VK_SHIFT = 0x10, VK_LWIN = 0x5B, VK_W = 0x57, VK_C = 0x43, VK_T = 0x54;
    const int ID_CLOSE = 1, ID_CHROME = 2;
    const string ChromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";

    static void Main() {
        bool fresh;
        using (new Mutex(true, "Local\\CloseActiveHotkey", out fresh)) {
            if (!fresh) return; // already running
            bool a = RegisterHotKey(IntPtr.Zero, ID_CLOSE, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_W);
            bool b = RegisterHotKey(IntPtr.Zero, ID_CHROME, MOD_CONTROL | MOD_ALT | MOD_NOREPEAT, VK_C);
            if (!a || !b) Log("RegisterHotKey failed: close=" + a + " chrome=" + b);
            if (!a && !b) return;
            MSG m;
            while (GetMessage(out m, IntPtr.Zero, 0, 0) > 0)
                if (m.message == WM_HOTKEY) {
                    try { if ((int)m.wParam == ID_CLOSE) Close(); else if ((int)m.wParam == ID_CHROME) Chrome(); }
                    catch (Exception e) { Log(e.ToString()); }
                }
        }
    }

    static bool Down(int vk) { return (GetAsyncKeyState(vk) & 0x8000) != 0; }

    static void Log(string msg) {
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CloseActive.log"), DateTime.Now + " " + msg + Environment.NewLine); } catch { }
    }

    static IntPtr Foreground() {
        IntPtr h = GetForegroundWindow();
        return h == IntPtr.Zero ? h : GetAncestor(h, 2 /* GA_ROOT */);
    }

    static string ProcName(IntPtr h) {
        uint pid; GetWindowThreadProcessId(h, out pid);
        try { return Process.GetProcessById((int)pid).ProcessName.ToLowerInvariant(); } catch { return ""; }
    }

    // Wait for the triggering hotkey to be released, force modifiers up, then send Ctrl+key.
    static void SendCtrl(byte key, byte trigger) {
        for (int i = 0; i < 50 && (Down(VK_MENU) || Down(VK_SHIFT) || Down(VK_LWIN) || Down(trigger)); i++) Thread.Sleep(10);
        foreach (byte vk in new[] { VK_MENU, VK_SHIFT, VK_LWIN, VK_CONTROL }) keybd_event(vk, 0, KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, 0, UIntPtr.Zero);
        keybd_event(key, 0, KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYUP, UIntPtr.Zero);
    }

    static void Chrome() {
        IntPtr h = Foreground();
        if (h != IntPtr.Zero && ProcName(h) == "chrome") SendCtrl(VK_T, VK_C);
        else Process.Start(ChromePath);
    }

    static void Close() {
        IntPtr h = Foreground();
        if (h == IntPtr.Zero) return;

        var cls = new StringBuilder(256);
        GetClassName(h, cls, cls.Capacity);
        switch (cls.ToString()) {
            case "Progman": case "WorkerW": case "Shell_TrayWnd": case "Shell_SecondaryTrayWnd": return;
        }

        if (ProcName(h) == "chrome") {
            SendCtrl(VK_W, VK_W);
        } else {
            PostMessage(h, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }
    }
}
