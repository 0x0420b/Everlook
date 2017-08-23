﻿//
//  RenderableActorInstance.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
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
using Everlook.Viewport.Camera;
using Everlook.Viewport.Rendering.Interfaces;
using OpenTK;

namespace Everlook.Viewport.Rendering.Core
{
	/// <summary>
	/// Represents an instanced renderable. This class acts as a proxied instance of a given actor with its own
	/// transform.
	/// </summary>
	public class RenderableActorInstance : IActor
	{
		/// <inheritdoc />
		public bool IsStatic { get; }

		/// <inheritdoc />
		public bool IsInitialized { get; set; }

		/// <inheritdoc />
		public ProjectionType Projection { get; }

		/// <inheritdoc />
		public Transform ActorTransform { get; set; }

		private readonly IActor Target;
		private readonly Transform DefaultTransform;

		/// <summary>
		/// Initializes a new instance of the <see cref="RenderableActorInstance"/> class.
		/// </summary>
		/// <param name="target">The target actor to act as an instance of.</param>
		public RenderableActorInstance(IActor target)
			: this(target, target.ActorTransform)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RenderableActorInstance"/> class.
		/// </summary>
		/// <param name="target">The target actor to act as an instance of.</param>
		/// <param name="transform">The transform of the instance.</param>
		public RenderableActorInstance(IActor target, Transform transform)
		{
			if (target == null)
			{
				throw new ArgumentNullException(nameof(target));
			}

			if (transform == null)
			{
				throw new ArgumentNullException(nameof(transform));
			}

			this.Target = target;
			this.DefaultTransform = target.ActorTransform;

			this.ActorTransform = transform;
		}

		/// <inheritdoc />
		public void Initialize()
		{
			this.IsInitialized = true;
		}

		/// <inheritdoc />
		public void Render(Matrix4 viewMatrix, Matrix4 projectionMatrix, ViewportCamera camera)
		{
			this.Target.ActorTransform = this.ActorTransform;
			this.Target.Render(viewMatrix, projectionMatrix, camera);
			this.Target.ActorTransform = this.DefaultTransform;
		}

		/// <summary>
		/// This method does nothing, and should not be called. The source actor should be disposed instead.
		/// </summary>
		public void Dispose()
		{
			throw new NotSupportedException("A renderable instance should not be disposed. Dispose the source actor instead.");
		}
	}
}
