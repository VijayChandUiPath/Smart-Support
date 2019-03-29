using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using LSA_HANDLE = System.IntPtr;

namespace UiPath.Monitoring.Server.Logging
{
    class LocalSecurityPolicy
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    struct LSA_OBJECT_ATTRIBUTES
    {
        internal int Length;
        internal IntPtr RootDirectory;
        internal IntPtr ObjectName;
        internal int Attributes;
        internal IntPtr SecurityDescriptor;
        internal IntPtr SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct LSA_UNICODE_STRING
    {
        internal ushort Length;
        internal ushort MaximumLength;
        [MarshalAs(UnmanagedType.LPWStr)]
        internal string Buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct LSA_ENUMERATION_INFORMATION
    {
        internal IntPtr PSid;
    }


    /// <summary>
    /// Provides direct Win32 calls to the security related functions
    /// </summary>
    sealed class Win32Sec
    {

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        internal static extern uint LsaOpenPolicy(
        LSA_UNICODE_STRING[] SystemName,
        ref LSA_OBJECT_ATTRIBUTES ObjectAttributes,
        int AccessMask,
        out IntPtr PolicyHandle
        );

        [DllImport("advapi32", CharSet = CharSet.Unicode, SetLastError = true), SuppressUnmanagedCodeSecurityAttribute]
        internal static extern uint LsaEnumerateAccountsWithUserRight(
        LSA_HANDLE PolicyHandle,
        LSA_UNICODE_STRING[] UserRights,
        out IntPtr EnumerationBuffer,
        out int CountReturned
        );

        [DllImport("advapi32")]
        internal static extern int LsaNtStatusToWinError(int NTSTATUS);

        [DllImport("advapi32")]
        internal static extern int LsaClose(IntPtr PolicyHandle);

        [DllImport("advapi32")]
        internal static extern int LsaFreeMemory(IntPtr Buffer);

    }



    /// <summary>
    /// Provides a wrapper to the LSA classes
    /// </summary>
    public class LsaWrapper : IDisposable
    {
        enum Access : int
        {
            POLICY_READ = 0x20006,
            POLICY_ALL_ACCESS = 0x00F0FFF,
            POLICY_EXECUTE = 0X20801,
            POLICY_WRITE = 0X207F8
        }
        const uint STATUS_ACCESS_DENIED = 0xc0000022;
        const uint STATUS_INSUFFICIENT_RESOURCES = 0xc000009a;
        const uint STATUS_NO_MEMORY = 0xc0000017;
        const uint STATUS_NO_MORE_ENTRIES = 0xc000001A;
        const uint ERROR_NO_MORE_ITEMS = 2147483674;
        const uint ERROR_PRIVILEGE_DOES_NOT_EXIST = 3221225568;
        IntPtr lsaHandle;



        /// <summary>
        /// Creates a new LSA wrapper for the local machine
        /// </summary>
        public LsaWrapper()
            : this(Environment.MachineName)
        {

        }


        /// <summary>
        /// Creates a new LSA wrapper for the specified MachineName
        /// </summary>
        /// <param name="MachineName">The name of the machine that should be connected to</param>
        public LsaWrapper(string MachineName)
        {
            LSA_OBJECT_ATTRIBUTES lsaAttr;
            lsaAttr.RootDirectory = IntPtr.Zero;
            lsaAttr.ObjectName = IntPtr.Zero;
            lsaAttr.Attributes = 0;
            lsaAttr.SecurityDescriptor = IntPtr.Zero;
            lsaAttr.SecurityQualityOfService = IntPtr.Zero;
            lsaAttr.Length = Marshal.SizeOf(typeof(LSA_OBJECT_ATTRIBUTES));
            lsaHandle = IntPtr.Zero;
            LSA_UNICODE_STRING[] system = null;
            if (MachineName != null)
            {
                system = new LSA_UNICODE_STRING[1];
                system[0] = InitLsaString(MachineName);
            }
            uint ret = Win32Sec.LsaOpenPolicy(system, ref lsaAttr, (int)Access.POLICY_ALL_ACCESS, out lsaHandle);
            TestReturnValue(ret);
        }


        /// <summary>
        /// Reads the user accounts which have the specific privilege
        /// </summary>
        /// <param name="Privilege">The name of the privilege for which the accounts with this right should be enumerated</param>
        public List<String> ReadPrivilege(string Privilege)
        {
            LSA_UNICODE_STRING[] privileges = new LSA_UNICODE_STRING[1];
            privileges[0] = InitLsaString(Privilege);
            IntPtr buffer;
            int count = 0;
            uint ret = Win32Sec.LsaEnumerateAccountsWithUserRight(lsaHandle, privileges, out buffer, out count);
            List<String> Accounts = new List<String>();

            if (ret == 0)
            {
                LSA_ENUMERATION_INFORMATION[] LsaInfo = new LSA_ENUMERATION_INFORMATION[count];
                for (int i = 0, elemOffs = (int)buffer; i < count; i++)
                {
                    LsaInfo[i] = (LSA_ENUMERATION_INFORMATION)Marshal.PtrToStructure((IntPtr)elemOffs, typeof(LSA_ENUMERATION_INFORMATION));
                    elemOffs += Marshal.SizeOf(typeof(LSA_ENUMERATION_INFORMATION));
                    SecurityIdentifier SID = new SecurityIdentifier(LsaInfo[i].PSid);
                    Accounts.Add(ResolveAccountName(SID));
                }
                return Accounts;
            }
            TestReturnValue(ret);
            return Accounts;
        }


        /// <summary>
        /// Resolves the SID into it's account name. If the SID cannot be resolved the SDDL for the SID (for example "S-1-5-21-3708151440-578689555-182056876-1009") is returned.
        /// </summary>
        /// <param name="SID">The Security Identifier to resolve to an account name</param>
        /// <returns>An account name for example "NT AUTHORITY\LOCAL SERVICE" or SID in SDDL form</returns>
        private String ResolveAccountName(SecurityIdentifier SID)
        {
            try { return SID.Translate(typeof(NTAccount)).Value; }
            catch (Exception) { return SID.ToString(); }
        }


        /// <summary>
        /// Tests the return value from Win32 method calls
        /// </summary>
        /// <param name="ReturnValue">The return value from the a Win32 method call</param>
        private void TestReturnValue(uint ReturnValue)
        {
            if (ReturnValue == 0) return;
            if (ReturnValue == ERROR_PRIVILEGE_DOES_NOT_EXIST) { return; }
            if (ReturnValue == ERROR_NO_MORE_ITEMS) { return; }
            if (ReturnValue == STATUS_ACCESS_DENIED) { throw new UnauthorizedAccessException(); }
            if ((ReturnValue == STATUS_INSUFFICIENT_RESOURCES) || (ReturnValue == STATUS_NO_MEMORY)) { throw new OutOfMemoryException(); }
            throw new Win32Exception(Win32Sec.LsaNtStatusToWinError((int)ReturnValue));
        }


        /// <summary>
        /// Disposes of this LSA wrapper
        /// </summary>
        public void Dispose()
        {
            if (lsaHandle != IntPtr.Zero)
            {
                Win32Sec.LsaClose(lsaHandle);
                lsaHandle = IntPtr.Zero;
            }
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Occurs on destruction of the LSA Wrapper
        /// </summary>
        ~LsaWrapper()
        {
            Dispose();
        }


        /// <summary>
        /// Converts the specified string to an LSA string value
        /// </summary>
        /// <param name="Value"></param>
        static LSA_UNICODE_STRING InitLsaString(string Value)
        {
            if (Value.Length > 0x7ffe) throw new ArgumentException("String too long");
            LSA_UNICODE_STRING lus = new LSA_UNICODE_STRING();
            lus.Buffer = Value;
            lus.Length = (ushort)(Value.Length * sizeof(char));
            lus.MaximumLength = (ushort)(lus.Length + sizeof(char));
            return lus;
        }
    }


}
