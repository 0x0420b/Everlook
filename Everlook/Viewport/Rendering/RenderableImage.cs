﻿//
//  RenderableImage.cs
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
using System.Collections.Generic;
using Everlook.Viewport.Camera;
using Everlook.Viewport.Rendering.Core;
using Everlook.Viewport.Rendering.Interfaces;
using Everlook.Viewport.Rendering.Shaders;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Warcraft.Core.Extensions;
using Warcraft.Core.Structures;

namespace Everlook.Viewport.Rendering
{
	/// <summary>
	/// Represents a renderable 2D image, and contains common functionality required to render one.
	/// </summary>
	public abstract class RenderableImage : IRenderable, IActor, IDefaultCameraPositionProvider
	{
		/// <summary>
		/// Gets a reference to the global shader cache.
		/// </summary>
		protected static RenderCache Cache => RenderCache.Instance;

		/// <summary>
		/// Gets or sets a value indicating whether this object has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

		/// <summary>
		/// Gets a value indicating whether this instance uses static rendering; that is,
		/// a single frame is rendered and then reused. Useful as an optimization for images.
		/// </summary>
		/// <value>true</value>
		/// <c>false</c>
		public bool IsStatic => true;

		/// <summary>
		/// Gets or sets the model transformation of the image. Used for moving and zooming.
		/// </summary>
		public Transform ActorTransform { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether or not the current renderable has been initialized.
		/// </summary>
		public bool IsInitialized { get; set; }

		/// <summary>
		/// Gets the native OpenGL ID for the vertex buffer.
		/// </summary>
		protected Buffer<Vector2> VertexBuffer { get; private set; }

		/// <summary>
		/// Gets the native OpenGL ID for the UV coordinate buffer.
		/// </summary>
		protected Buffer<Vector2> UVBuffer { get; private set; }

		/// <summary>
		/// Gets the native OpenGL ID for the vertex index buffer.
		/// </summary>
		protected Buffer<ushort> VertexIndexBuffer { get; private set; }

		/// <summary>
		/// Gets or sets the native OpenGL ID for the image on the GPU.
		/// </summary>
		protected Texture2D Texture { get; set; }

		/// <summary>
		/// Gets or sets the native OpenGL ID for the unlit 2D shader.
		/// </summary>
		protected Plain2DShader Shader { get; set; }

		/// <summary>
		/// Gets or sets the path to the encapsulated texture in the package group.
		/// </summary>
		protected string TexturePath { get; set; }

		/// <summary>
		/// Gets the projection method to use for this renderable object. Typically, this is Orthographic
		/// or Perspective.
		/// </summary>
		public ProjectionType Projection => ProjectionType.Orthographic;

		/// <summary>
		/// Gets the default camera position for this renderable.
		/// </summary>
		public Vector3 DefaultCameraPosition => new Vector3(0.0f, 0.0f, 1.0f);

		/// <summary>
		/// Gets or sets a value indicating whether or not the red channel should be rendered.
		/// </summary>
		public bool RenderRedChannel { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether or not the green channel should be rendered.
		/// </summary>
		public bool RenderGreenChannel { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether or not the blue channel should be rendered.
		/// </summary>
		public bool RenderBlueChannel { get; set; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether or not the alpha channel should be rendered.
		/// </summary>
		public bool RenderAlphaChannel { get; set; } = true;

		/// <summary>
		/// Gets a vector based on <see cref="RenderRedChannel"/>, <see cref="RenderGreenChannel"/>,
		/// <see cref="RenderBlueChannel"/> and <see cref="RenderAlphaChannel"/> that is multiplied with the final
		/// texture sampling to mask out channels.
		/// TODO: Investigate whether or not this is a problem memory-wise, new vector created every frame
		/// </summary>
		public Vector4 ChannelMask => new Vector4
		(
			this.RenderRedChannel ? 1.0f : 0.0f,
			this.RenderGreenChannel ? 1.0f : 0.0f,
			this.RenderBlueChannel ? 1.0f : 0.0f,
			this.RenderAlphaChannel ? 1.0f : 0.0f
		);

		/// <summary>
		/// Gets the number of mipmap levels for this image.
		/// </summary>
		public uint MipCount => GetNumReasonableMipLevels();

		/// <summary>
		/// TODO: Put this in Warcraft.Core instead
		/// </summary>
		/// <returns>The number of reasonable mipmap levels.</returns>
		private uint GetNumReasonableMipLevels()
		{
			uint smallestXRes = GetResolution().X;
			uint smallestYRes = GetResolution().Y;

			uint mipLevels = 0;
			while (smallestXRes > 1 && smallestYRes > 1)
			{
				// Bisect the resolution using the current number of mip levels.
				smallestXRes = smallestXRes / (uint)Math.Pow(2, mipLevels);
				smallestYRes = smallestYRes / (uint)Math.Pow(2, mipLevels);

				++mipLevels;
			}

			return mipLevels.Clamp<uint>(0, 15);
		}

		/// <summary>
		/// Initializes the required data for rendering.
		/// </summary>
		public void Initialize()
		{
			ThrowIfDisposed();

			if (this.IsInitialized)
			{
				return;
			}

			this.VertexBuffer = GenerateVertices();
			this.VertexIndexBuffer = GenerateVertexIndexes();
			this.UVBuffer = GenerateTextureCoordinates();

			// Use cached textures whenever possible
			this.Texture = LoadTexture();

			// Use cached shaders whenever possible
			this.Shader = Cache.GetShader(EverlookShader.Plain2D) as Plain2DShader;

			this.ActorTransform = new Transform(
				new Vector3(0.0f, 0.0f, 0.0f),
				Quaternion.FromAxisAngle(Vector3.UnitX, 0.0f),
				new Vector3(1.0f, 1.0f, 1.0f));

			this.IsInitialized = true;
		}

		/// <summary>
		/// Loads or creates a cached texture from the global texture cache using the path the image
		/// was constructed with as a key.
		/// </summary>
		/// <returns>A texture object.</returns>
		protected abstract Texture2D LoadTexture();

		/// <inheritdoc />
		public void Render(Matrix4 viewMatrix, Matrix4 projectionMatrix, ViewportCamera camera)
		{
			ThrowIfDisposed();

			if (!this.IsInitialized)
			{
				return;
			}

			this.Shader.Enable();

			// Render the object
			// Send the vertices to the shader
			GL.EnableVertexAttribArray(0);
			this.VertexBuffer.Bind();
			GL.VertexAttribPointer(
				0,
				2,
				VertexAttribPointerType.Float,
				false,
				0,
				0);

			// Send the UV coordinates to the shader
			GL.EnableVertexAttribArray(1);
			this.UVBuffer.Bind();
			GL.VertexAttribPointer(
				1,
				2,
				VertexAttribPointerType.Float,
				false,
				0,
				0);

			// Set the channel mask
			this.Shader.SetChannelMask(this.ChannelMask);

			// Set the texture ID as a uniform sampler in unit 0
			this.Shader.SetTexture(this.Texture);

			// Set the model view matrix
			Matrix4 modelViewProjection = this.ActorTransform.GetModelMatrix() * viewMatrix * projectionMatrix;

			// Send the model matrix to the shader
			this.Shader.SetMVPMatrix(modelViewProjection);

			// Finally, draw the image
			this.VertexIndexBuffer.Bind();
			GL.DrawElements(BeginMode.Triangles, 6, DrawElementsType.UnsignedShort, 0);

			// Release the attribute arrays
			GL.DisableVertexAttribArray(0);
			GL.DisableVertexAttribArray(1);
		}

		/// <summary>
		/// Gets the resolution of the encapsulated image.
		/// </summary>
		/// <returns>The resolution of the image.</returns>
		protected abstract Resolution GetResolution();

		/// <summary>
		/// Generates the four corner vertices of the encapsulated image.
		/// </summary>
		/// <returns>The vertex buffer.</returns>
		protected Buffer<Vector2> GenerateVertices()
		{
			// Generate vertex positions
			uint halfWidth = GetResolution().X / 2;
			uint halfHeight = GetResolution().Y / 2;

			List<Vector2> vertexPositions = new List<Vector2>
			{
				new Vector2(-halfWidth, halfHeight),
				new Vector2(halfWidth, halfHeight),
				new Vector2(-halfWidth, -halfHeight),
				new Vector2(halfWidth, -halfHeight)
			};

			// Buffer the generated vertices in the GPU
			return new Buffer<Vector2>(BufferTarget.ArrayBuffer, BufferUsageHint.StaticDraw)
			{
				Data = vertexPositions.ToArray()
			};
		}

		/// <summary>
		/// Generates a vertex index buffer for the four corner vertices.
		/// </summary>
		/// <returns>The vertex index buffer.</returns>
		protected static Buffer<ushort> GenerateVertexIndexes()
		{
			// Generate vertex indexes
			List<ushort> vertexIndexes = new List<ushort> { 1, 0, 2, 2, 3, 1 };

			return new Buffer<ushort>(BufferTarget.ElementArrayBuffer, BufferUsageHint.StaticDraw)
			{
				Data = vertexIndexes.ToArray()
			};
		}

		/// <summary>
		/// Generates a UV coordinate buffer for the four corner vertices.
		/// </summary>
		/// <returns>The native OpenGL ID of the buffer.</returns>
		protected static Buffer<Vector2> GenerateTextureCoordinates()
		{
			// Generate UV coordinates
			List<Vector2> textureCoordinates = new List<Vector2>
			{
				new Vector2(0, 0),
				new Vector2(1, 0),
				new Vector2(0, 1),
				new Vector2(1, 1)
			};

			// Buffer the generated UV coordinates in the GPU
			return new Buffer<Vector2>(BufferTarget.ArrayBuffer, BufferUsageHint.StaticDraw)
			{
				Data = textureCoordinates.ToArray()
			};
		}

		/// <summary>
		/// Throws an <see cref="ObjectDisposedException"/> if the object has been disposed.
		/// </summary>
		/// <exception cref="ObjectDisposedException">Thrown if the object is disposed.</exception>
		private void ThrowIfDisposed()
		{
			if (this.IsDisposed)
			{
				throw new ObjectDisposedException(ToString());
			}
		}

		/// <summary>
		/// Releases all resources used by the <see cref="Everlook.Viewport.Rendering.RenderableBitmap"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="Everlook.Viewport.Rendering.RenderableBitmap"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="Everlook.Viewport.Rendering.RenderableBitmap"/> in an unusable state.
		/// After calling <see cref="Dispose"/>, you must release all references to the
		/// <see cref="Everlook.Viewport.Rendering.RenderableBitmap"/> so the garbage collector can reclaim the memory that the
		/// <see cref="Everlook.Viewport.Rendering.RenderableBitmap"/> was occupying.</remarks>
		public virtual void Dispose()
		{
			this.VertexBuffer.Dispose();
			this.VertexIndexBuffer.Dispose();
			this.UVBuffer.Dispose();
		}
	}
}
