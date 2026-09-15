/*
TShock, a server mod for Terraria
Copyright (C) 2011-2019 Pryaxis & TShock Contributors

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using Terraria;
using Terraria.IO;
using TerrariaApi.Server;

namespace TShockAPI
{
	class SaveManager : IDisposable
	{
		// Singleton
		private static readonly SaveManager instance = new SaveManager();
		private SaveManager()
		{
			_saveThread = new Thread(SaveWorker);
			_saveThread.Name = "TShock SaveManager Worker";
			_saveThread.Start();
		}
		public static SaveManager Instance { get { return instance; } }

		private readonly object _saveLock = new();
		private readonly Queue<SaveTask> _saveQueue = new();
		private readonly Thread _saveThread;
		private int _pending;

		/// <summary>
		/// SaveWorld event handler which notifies users that the server may lag
		/// </summary>
		public void OnSaveWorld(WorldSaveEventArgs args)
		{
			if (TShock.Config.Settings.AnnounceSave)
			{
				// Protect against internal errors causing save failures
				// These can be caused by an unexpected error such as a bad or out of date plugin
				try
				{
					TShock.Utils.Broadcast(GetString("Saving world..."), Color.Yellow);
				}
				catch (Exception ex)
				{
					TShock.Log.Error("World saved notification failed");
					TShock.Log.Error(ex.ToString());
				}
			}
		}

		/// <summary>
		/// Saves the map data
		/// </summary>
		/// <param name="wait">wait for all pending saves to finish (default: true)</param>
		/// <param name="resetTime">reset the last save time counter (default: false)</param>
		/// <param name="direct">use the realsaveWorld method instead of saveWorld event (default: false)</param>
		public void SaveWorld(bool wait = true, bool resetTime = false, bool direct = false)
		{
			EnqueueTask(new SaveTask(resetTime, direct));
			if (!wait)
				return;

			lock (_saveLock)
			{
				while (_pending > 0)
					Monitor.Wait(_saveLock);
			}
		}

		/// <summary>
		/// Processes any outstanding saves, shutsdown the save thread and returns
		/// </summary>
		public void Dispose()
		{
			EnqueueTask(null);
			_saveThread.Join();
		}

		private void EnqueueTask(SaveTask task)
		{
			lock (_saveLock)
			{
				_saveQueue.Enqueue(task);
				if (task is not null)
					_pending++;
				Monitor.Pulse(_saveLock);
			}
		}

		private void SaveWorker()
		{
			while (true)
			{
				SaveTask task;
				lock (_saveLock)
				{
					while (_saveQueue.Count == 0)
						Monitor.Wait(_saveLock);
					task = _saveQueue.Dequeue();
				}
				if (task is null)
					return;

				try
				{
					if (task.direct)
					{
						OnSaveWorld(new WorldSaveEventArgs());
						WorldFile.SaveWorld(task.resetTime);
					}
					else
						WorldFile.SaveWorld(task.resetTime);

					if (TShock.Config.Settings.AnnounceSave)
						TShock.Utils.Broadcast(GetString("World saved."), Color.Yellow);

					TShock.Log.Info(GetString("World saved at ({0})", Main.worldPathName));
				}
				catch (Exception e)
				{
					TShock.Log.Error("World saved failed");
					TShock.Log.Error(e.ToString());
				}
				finally
				{
					lock (_saveLock)
					{
						_pending--;
						Monitor.PulseAll(_saveLock);
					}
				}
			}
		}

		class SaveTask
		{
			public bool resetTime { get; set; }
			public bool direct { get; set; }
			public SaveTask(bool resetTime, bool direct)
			{
				this.resetTime = resetTime;
				this.direct = direct;
			}

			public override string ToString()
			{
				return GetString("resetTime {0}, direct {1}", resetTime, direct);
			}
		}
	}
}
