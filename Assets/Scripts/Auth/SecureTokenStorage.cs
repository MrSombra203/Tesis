using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GestionInteligente.Auth
{
    public static class SecureTokenStorage
    {
        private const uint UiForbidden = 0x1;

        public static byte[] Proteger(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            var bytes = Encoding.UTF8.GetBytes(token);
            try { return Transform(bytes, proteger: true); }
            finally { Array.Clear(bytes, 0, bytes.Length); }
        }

        public static string Desproteger(byte[] tokenCifrado)
        {
            if (tokenCifrado == null || tokenCifrado.Length == 0) return null;
            var bytes = Transform(tokenCifrado, proteger: false);
            try { return Encoding.UTF8.GetString(bytes); }
            finally { Array.Clear(bytes, 0, bytes.Length); }
        }

        private static byte[] Transform(byte[] input, bool proteger)
        {
            var inputPointer = Marshal.AllocHGlobal(input.Length);
            IntPtr outputPointer = IntPtr.Zero;
            try
            {
                Marshal.Copy(input, 0, inputPointer, input.Length);
                var inputBlob = new DataBlob { Size = input.Length, Data = inputPointer };
                DataBlob outputBlob;
                var succeeded = proteger
                    ? Native.CryptProtectData(ref inputBlob, "GestionInteligente refresh token", IntPtr.Zero,
                        IntPtr.Zero, IntPtr.Zero, UiForbidden, out outputBlob)
                    : Native.CryptUnprotectData(ref inputBlob, IntPtr.Zero, IntPtr.Zero,
                        IntPtr.Zero, IntPtr.Zero, UiForbidden, out outputBlob);

                if (!succeeded)
                    throw new InvalidOperationException(
                        $"Windows no pudo {(proteger ? "cifrar" : "descifrar")} el token (código {Marshal.GetLastWin32Error()}).");

                outputPointer = outputBlob.Data;
                var result = new byte[outputBlob.Size];
                Marshal.Copy(outputBlob.Data, result, 0, outputBlob.Size);
                return result;
            }
            finally
            {
                Marshal.Copy(new byte[input.Length], 0, inputPointer, input.Length);
                Marshal.FreeHGlobal(inputPointer);
                if (outputPointer != IntPtr.Zero) Native.LocalFree(outputPointer);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DataBlob
        {
            public int Size;
            public IntPtr Data;
        }

        private static class Native
        {
            [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptProtectData(
                ref DataBlob dataIn, string description, IntPtr optionalEntropy, IntPtr reserved,
                IntPtr prompt, uint flags, out DataBlob dataOut);

            [DllImport("Crypt32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CryptUnprotectData(
                ref DataBlob dataIn, IntPtr description, IntPtr optionalEntropy, IntPtr reserved,
                IntPtr prompt, uint flags, out DataBlob dataOut);

            [DllImport("Kernel32.dll")]
            internal static extern IntPtr LocalFree(IntPtr memory);
        }
    }
}
