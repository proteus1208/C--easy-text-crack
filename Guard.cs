using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace AllowedMac;

static class Guard
{
    const byte Mask = 0xA7;

    [DllImport("kernel32.dll")]
    static extern bool IsDebuggerPresent();

    [DllImport("kernel32.dll")]
    static extern bool CheckRemoteDebuggerPresent(IntPtr process, ref bool present);

    public static string BeginMarker => Reveal("hIqQrb7GqLCDnXJtf1FcJTQlHhXg4vDD0cLY1eY=");

    public static string EndMarker => Reveal("hIqQrb7GrLKLjWRkd0FGITMlABHg6erJ3La8t4X+DBkq");

    public static void RefuseDebug()
    {
        if (Debugger.IsAttached || Debugger.IsLogging() || IsDebuggerPresent())
            Environment.Exit(0);

        var remote = false;
        CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref remote);
        if (remote)
            Environment.Exit(0);
    }

    public static string Digest(string mac)
    {
        var hex = new string(mac.Where(Uri.IsHexDigit).ToArray()).ToUpperInvariant();
        return Hash(Key(1), Encoding.ASCII.GetBytes(hex));
    }

    public static string Sign(string digest) =>
        Hash(Key(2), Encoding.ASCII.GetBytes(digest));

    static string Hash(byte[] key, byte[] data)
    {
        var buffer = new byte[key.Length + data.Length];
        Buffer.BlockCopy(key, 0, buffer, 0, key.Length);
        Buffer.BlockCopy(data, 0, buffer, key.Length, data.Length);
        return Convert.ToHexString(SHA256.HashData(buffer)).ToLowerInvariant();
    }

    static byte[] Key(int which)
    {
        var mixed = which == 1
            ? new byte[] { 0xFF, 0xAD, 0x3B, 0x62, 0x96, 0x28, 0x57, 0xEE, 0x04, 0xCC, 0x25, 0x4B }
            : new byte[] { 0x74, 0x02, 0xEE, 0x57, 0x39, 0xC0, 0xBB, 0x1F, 0xD6, 0x7B, 0xAC, 0x2A };
        var mask = which == 1 ? (byte)0x3C : (byte)0x5A;
        for (var i = 0; i < mixed.Length; i++)
            mixed[i] ^= mask;
        return mixed;
    }

    static string Reveal(string packed)
    {
        var raw = Convert.FromBase64String(packed);
        for (var i = 0; i < raw.Length; i++)
            raw[i] ^= (byte)(Mask ^ ((i * 13) & 0xFF));
        return Encoding.UTF8.GetString(raw);
    }
}
