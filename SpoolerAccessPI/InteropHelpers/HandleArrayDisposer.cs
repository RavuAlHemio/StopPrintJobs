using System;
using System.Collections.Generic;

namespace SpoolerAccessPI
{
	public class HandleArrayDisposer : IDisposable
    {
		public delegate void CloseHandleFunc(IntPtr handle);

		public List<IntPtr> Handles { get; private set; }
		public CloseHandleFunc CloseHandleFunction { get; private set; }
		private bool IsDisposed { get; set; }

		public HandleArrayDisposer(CloseHandleFunc closeHandleFunction)
        {
			Handles = new List<IntPtr>();
			CloseHandleFunction = closeHandleFunction;
			IsDisposed = false;
        }

		public HandleArrayDisposer(CloseHandleFunc closeHandleFunction, IEnumerable<IntPtr> handles)
		{
			Handles = new List<IntPtr>(handles);
			CloseHandleFunction = closeHandleFunction;
			IsDisposed = false;
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				// if (disposing) { /* dispose managed resources */ }

				foreach (var handle in Handles)
				{
					CloseHandleFunction(handle);
				}
				Handles.Clear();
			}
			IsDisposed = true;
		}
    }
}

