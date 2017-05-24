//
//  AudioSource.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2016 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using OpenTK.Audio;

namespace Everlook.Audio
{
	/// <summary>
	/// Manages the audio context of the application, and handles audio sources within it.
	/// </summary>
	public class AudioManager : IDisposable
	{
		/// <summary>
		/// Gets the singleton instance of the <see cref="AudioManager"/>.
		/// </summary>
		public static readonly AudioManager Instance = new AudioManager();

		private readonly AudioContext Context;

		/// <summary>
		/// Initializes the singleton instance of the <see cref="AudioManager"/> class.
		/// </summary>
		protected AudioManager()
		{
			this.Context = new AudioContext();
			this.Context.MakeCurrent();
		}

		/// <summary>
		/// Disposes the audio manager.
		/// </summary>
		public void Dispose()
		{
			this.Context.Dispose();
		}
	}
}