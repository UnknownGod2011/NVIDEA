using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace Nvidea.Core.Security;

public interface ILocalStateProtector
{
    byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose);
    byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose);
}

public readonly record struct LocalStatePayload(byte[] Plaintext, bool WasProtected);

/// <summary>
/// Stable, versioned envelope around protected local state. The protector owns cryptography;
/// this codec only provides format/version detection so legacy plaintext can be migrated safely.
/// </summary>
public static class LocalStateEnvelope
{
    private static readonly byte[] Header = Encoding.ASCII.GetBytes("NVIDEA-STATE-V1\n");

    public static byte[] Encode(ReadOnlySpan<byte> plaintext, ILocalStateProtector protector, string purpose)
    {
        ArgumentNullException.ThrowIfNull(protector);
        ValidatePurpose(purpose);

        var protectedData = protector.Protect(plaintext, purpose);
        var base64 = Convert.ToBase64String(protectedData);
        return Encoding.UTF8.GetBytes(Encoding.ASCII.GetString(Header) + base64);
    }

    public static LocalStatePayload Decode(ReadOnlySpan<byte> persisted, ILocalStateProtector protector, string purpose)
    {
        ArgumentNullException.ThrowIfNull(protector);
        ValidatePurpose(purpose);

        if (!persisted.StartsWith(Header))
            return new LocalStatePayload(persisted.ToArray(), false);

        var encoded = Encoding.UTF8.GetString(persisted[Header.Length..]).Trim();
        if (string.IsNullOrWhiteSpace(encoded))
            throw new InvalidDataException("Protected local-state envelope is empty.");

        byte[] ciphertext;
        try
        {
            ciphertext = Convert.FromBase64String(encoded);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Protected local-state envelope contains invalid base64.", ex);
        }

        try
        {
            return new LocalStatePayload(protector.Unprotect(ciphertext, purpose), true);
        }
        catch (CryptographicException ex)
        {
            throw new InvalidDataException("Protected local state could not be decrypted for this user/device context.", ex);
        }
    }

    public static bool HasProtectedHeader(ReadOnlySpan<byte> persisted) => persisted.StartsWith(Header);

    private static void ValidatePurpose(string purpose)
    {
        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentException("A non-empty local-state protection purpose is required.", nameof(purpose));
    }
}

/// <summary>
/// Windows CurrentUser DPAPI boundary implemented directly over CryptProtectData/CryptUnprotectData.
/// No reusable encryption key is stored in NVIDEA files. Purpose-derived optional entropy prevents
/// ciphertext from one durable store from being transplanted into a different store type.
/// </summary>
public sealed class WindowsDpapiLocalStateProtector : ILocalStateProtector
{
    private const uint CryptprotectUiForbidden = 0x1;

    public byte[] Protect(ReadOnlySpan<byte> plaintext, string purpose)
    {
        EnsureWindows();
        return Transform(plaintext, purpose, protect: true);
    }

    public byte[] Unprotect(ReadOnlySpan<byte> protectedData, string purpose)
    {
        EnsureWindows();
        return Transform(protectedData, purpose, protect: false);
    }

    private static byte[] Transform(ReadOnlySpan<byte> input, string purpose, bool protect)
    {
        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentException("A non-empty local-state protection purpose is required.", nameof(purpose));

        var inputBytes = input.ToArray();
        var entropyBytes = SHA256.HashData(Encoding.UTF8.GetBytes("NVIDEA/local-state/" + purpose));
        var inputBlob = default(DataBlob);
        var entropyBlob = default(DataBlob);
        var outputBlob = default(DataBlob);
        IntPtr description = IntPtr.Zero;

        try
        {
            inputBlob = Allocate(inputBytes);
            entropyBlob = Allocate(entropyBytes);

            bool ok;
            if (protect)
            {
                ok = CryptProtectData(
                    ref inputBlob,
                    null,
                    ref entropyBlob,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptprotectUiForbidden,
                    out outputBlob);
            }
            else
            {
                ok = CryptUnprotectData(
                    ref inputBlob,
                    out description,
                    ref entropyBlob,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptprotectUiForbidden,
                    out outputBlob);
            }

            if (!ok)
                throw new CryptographicException(Marshal.GetLastWin32Error());

            var result = new byte[outputBlob.Size];
            if (outputBlob.Size > 0)
                Marshal.Copy(outputBlob.Data, result, 0, outputBlob.Size);
            return result;
        }
        finally
        {
            FreeHGlobal(ref inputBlob, zero: true);
            FreeHGlobal(ref entropyBlob, zero: true);
            FreeLocal(ref outputBlob, zero: true);
            if (description != IntPtr.Zero)
                LocalFree(description);
            CryptographicOperations.ZeroMemory(inputBytes);
            CryptographicOperations.ZeroMemory(entropyBytes);
        }
    }

    private static DataBlob Allocate(byte[] bytes)
    {
        if (bytes.Length == 0)
            return default;

        var pointer = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, pointer, bytes.Length);
        return new DataBlob { Size = bytes.Length, Data = pointer };
    }

    private static void FreeHGlobal(ref DataBlob blob, bool zero)
    {
        if (blob.Data == IntPtr.Zero)
            return;

        ZeroNativeBuffer(blob, zero);
        Marshal.FreeHGlobal(blob.Data);
        blob = default;
    }

    private static void FreeLocal(ref DataBlob blob, bool zero)
    {
        if (blob.Data == IntPtr.Zero)
            return;

        ZeroNativeBuffer(blob, zero);
        LocalFree(blob.Data);
        blob = default;
    }

    private static void ZeroNativeBuffer(DataBlob blob, bool zero)
    {
        if (!zero || blob.Size <= 0)
            return;

        var zeros = new byte[blob.Size];
        Marshal.Copy(zeros, 0, blob.Data, blob.Size);
    }

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("Windows DPAPI local-state protection is only available on Windows.");
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Size;
        public IntPtr Data;
    }

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob pDataIn,
        string? szDataDescr,
        ref DataBlob pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        uint dwFlags,
        out DataBlob pDataOut);

    [DllImport("crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob pDataIn,
        out IntPtr ppszDataDescr,
        ref DataBlob pOptionalEntropy,
        IntPtr pvReserved,
        IntPtr pPromptStruct,
        uint dwFlags,
        out DataBlob pDataOut);

    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr hMem);
}
