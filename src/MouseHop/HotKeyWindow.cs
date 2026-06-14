using System.ComponentModel;
using System.Runtime.InteropServices;

namespace MouseHop;

internal sealed class HotKeyWindow : NativeWindow, IDisposable
{
    private const int HotKeyId = 1;
    private bool disposed;
    private bool registered;

    internal event EventHandler? HotKeyPressed;

    internal HotKeyWindow()
    {
        CreateHandle(new CreateParams());
    }

    internal void RegisterMoveHotKey()
    {
        if (registered)
        {
            return;
        }

        var success = NativeMethods.RegisterHotKey(
            Handle,
            HotKeyId,
            NativeMethods.ModControl | NativeMethods.ModAlt,
            (uint)Keys.M);

        if (!success)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Ctrl + Alt + M の登録に失敗しました。");
        }

        registered = true;
    }

    internal void UnregisterMoveHotKey()
    {
        if (!registered)
        {
            return;
        }

        NativeMethods.UnregisterHotKey(Handle, HotKeyId);
        registered = false;
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == NativeMethods.WmHotKey && m.WParam.ToInt32() == HotKeyId)
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        UnregisterMoveHotKey();
        DestroyHandle();
        disposed = true;
    }
}
