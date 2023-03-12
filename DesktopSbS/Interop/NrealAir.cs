using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

/*
 * To run this code, please copy Resources/Dll/*.dll into exe folder.
 *   https://github.com/MSmithDev/AirAPI_Windows
 *   https://github.com/libusb/hidapi
 */

namespace DesktopSbS.Interop
{
    public struct Euler
    {
        public float x;
        public float y;
        public float z;
        private Euler(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public Euler(float[] array)
        {
            x = array[0];
            y = array[1];
            z = array[2];
        }
        public override string ToString() => $"({x}, {y}, {z})";
        public static Euler operator -(Euler a, Euler b)
            => new Euler(a.x - b.x, a.y - b.y, a.z - b.z);

    }

    public static class NrealAir
    {
        private const string dll = "AirAPI_Windows.dll";

        #region API

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        static extern int StartConnection();

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        static extern int StopConnection();

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr GetQuaternion();

        [DllImport(dll, CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr GetEuler();

        #endregion

        public static bool Connected { get; private set; }

        private static Euler euler_def = new Euler();

        private static Euler ReadEuler()
        {
            float[] EulerArray = new float[3];
            IntPtr EulerPtr = GetEuler();

            Marshal.Copy(EulerPtr, EulerArray, 0, 3);
            return new Euler(EulerArray);
        }

        public static Euler Euler
        {
            get
            {
                return ReadEuler() - euler_def;
            }
        }

        public static void Reset()
        {
            if (!Connected)
            {
                return;
            }

            euler_def = ReadEuler();
        }

        public static bool Start()
        {
            // Start the connection
            var res = StartConnection();
            if (res == 1)
            {
                Debug.WriteLine("Nreal Air connection started");
                Connected = true;
            }
            else
            {
                Debug.WriteLine("Nreal Air Connection failed");
                Connected = false;
            }
            return Connected;
        }

        public static void Stop()
        {
            StopConnection();
            Debug.WriteLine("Nreal Air connection stopped");
            Connected = false;
        }

        static float[] QuaternionArray = new float[4];
        static IntPtr QuaternionPtr;

        public static float[] Quaternion
        {
            get
            {
                QuaternionPtr = GetQuaternion();
                Marshal.Copy(QuaternionPtr, QuaternionArray, 0, 4);
                return QuaternionArray;
            }
        }
    }
}
