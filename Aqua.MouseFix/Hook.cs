using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Aqua.MouseFix
{
    public class Hook
	{
		[DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);
		[DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int UnhookWindowsHookEx(int idHook);
		[DllImport("user32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
		private static extern int CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);

		public Hook()
		{
			Start();
		}

		public Hook(bool installMouseHook)
		{
			Start(installMouseHook);
		}

		~Hook()
		{
			Stop(true, false);
		}

		public void Start(bool installMouseHook = true)
		{
			if (_hMouseHook != 0 || !installMouseHook) return;
			_mouseHookProcedure = MouseHookProc;
			_hMouseHook = SetWindowsHookEx(WhMouseLl, _mouseHookProcedure, Marshal.GetHINSTANCE(Assembly.GetExecutingAssembly().GetModules()[0]), 0);
			
			if (_hMouseHook != 0) return;
			var lastWin32Error = Marshal.GetLastWin32Error();
			Stop(true, false);
			throw new Win32Exception(lastWin32Error);
		}

		public void Stop(bool uninstallMouseHook = true, bool throwExceptions = true)
		{
			if (_hMouseHook != 0 && uninstallMouseHook)
			{
				int num = UnhookWindowsHookEx(_hMouseHook);
				_hMouseHook = 0;
				if (num == 0 && throwExceptions)
				{
					throw new Win32Exception(Marshal.GetLastWin32Error());
				}
			}
		}
		
		public event Action Blocked;
		
		private DateTime _lastDownL;
		private DateTime _lastUpL;
		private DateTime _lastDownR;
		private DateTime _lastUpR;
		private double _globalDelta = 25.0;
		private bool _nextUpLBlocked;
		private bool _nextUpRBlocked;

		private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
		{
			bool flag = false;
			if (nCode >= 0)
			{
				// right click
				if (wParam == WmRbuttondown)
				{
					_lastDownR = DateTime.UtcNow;
					if (_lastUpR != DateTime.MinValue)
					{
						if ((_lastDownR - _lastUpR).TotalMilliseconds < _globalDelta)
						{
							flag = true;
							_nextUpRBlocked = true;
						}
					}
				}
				if (wParam == WmRbuttonup)
				{
					_lastUpR = DateTime.UtcNow;
					if (_nextUpRBlocked)
					{
						flag = true;
						_nextUpRBlocked = false;
					}
				}
				
				// left click
				
				if (wParam == WmLbuttondown)
				{
					_lastDownL = DateTime.UtcNow;
					if (_lastUpL != DateTime.MinValue)
					{
						if ((_lastDownL - _lastUpL).TotalMilliseconds < _globalDelta)
						{
							flag = true;
							_nextUpLBlocked = true;
						}
					}
				}
				if (wParam == WmLbuttonup)
				{
					_lastUpL = DateTime.UtcNow;
					if (_nextUpLBlocked)
					{
						flag = true;
						_nextUpLBlocked = false;
					}
				}

				if (flag)
				{
					Blocked?.Invoke();
				}
				
				// for debug
				// switch (wParam)
				// {
				// 	case 513:
				// 		
				// 		Console.WriteLine($"WM_LBUTTONDOWN" + (flag?"BLOCKED":""));
				// 		break;
				// 	case 514:
				// 		Console.WriteLine("WM_LBUTTONUP" + (flag?"BLOCKED":""));
				// 		break;
				// 	case 516:
				// 		Console.WriteLine("WM_RBUTTONDOWN" + (flag?"BLOCKED":""));
				// 		break;
				// 	case 517:
				// 		Console.WriteLine("WM_RBUTTONUP" + (flag?"BLOCKED":""));
				// 		break;
				// }
			}

			var result = !flag ? CallNextHookEx(_hMouseHook, nCode, wParam, lParam) : 1;
			return result;
		}

		private const int WhMouseLl = 14;
		private const int WmLbuttondown = 513;
		private const int WmRbuttondown = 516;
		private const int WmLbuttonup = 514;
		private const int WmRbuttonup = 517;

		private int _hMouseHook;

		private static HookProc _mouseHookProcedure;

		private delegate int HookProc(int nCode, int wParam, IntPtr lParam);
	}
}