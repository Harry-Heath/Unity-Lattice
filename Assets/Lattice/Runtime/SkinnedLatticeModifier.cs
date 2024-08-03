using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice
{
	[ExecuteAlways, DisallowMultipleComponent]
	public class SkinnedLatticeModifier : LatticeModifier
	{
		#region Fields

		[SerializeField] private List<Lattice> _skinnedLattices = new();

		private SkinnedMeshRenderer _skinnedMeshRenderer;
		private GraphicsBuffer _skinnedVertexBuffer;
		private Matrix4x4 _skinnedLocalToWorld;

		#endregion

		#region Properties

		/// <summary>
		/// Skinned lattices to apply.
		/// </summary>
		public List<Lattice> SkinnedLattices => _skinnedLattices;

		/// <summary>
		/// Gets the current skinned vertex buffer.
		/// </summary>
		public GraphicsBuffer SkinnedVertexBuffer => _skinnedVertexBuffer;

		/// <summary>
		/// Gets the current skinned local to world matrix.
		/// </summary>
		public Matrix4x4 SkinnedLocalToWorld => _skinnedLocalToWorld;

		/// <inheritdoc cref="LatticeModifier.IsValid"/>
		public bool IsSkinnedValid => IsValid && _skinnedVertexBuffer != null;

		/// <inheritdoc cref="LatticeModifier.Renderer"/>
		protected override Renderer Renderer => MeshRenderer;

		/// <summary>
		/// Retrieves the skinned mesh renderer.
		/// </summary>
		private SkinnedMeshRenderer MeshRenderer => (_skinnedMeshRenderer == null)
			? _skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>()
			: _skinnedMeshRenderer;

		#endregion

		#region Protected Methods

		/// <inheritdoc cref="LatticeModifier.GetMesh"/>
		protected override Mesh GetMesh()
		{
			return MeshRenderer.sharedMesh;
		}

		/// <inheritdoc cref="LatticeModifier.SetMesh"/>
		protected override void SetMesh(Mesh mesh)
		{
			MeshRenderer.sharedMesh = mesh;
		}

		/// <inheritdoc cref="LatticeModifier.Release"/>
		protected override void Release()
		{
			base.Release();

			_skinnedVertexBuffer?.Release();
			_skinnedVertexBuffer = null;
		}

		/// <inheritdoc cref="LatticeModifier.Enqueue"/>
		protected override void Enqueue()
		{
			// Ideally you cache the vertex buffer without releasing it every frame
			// But the skin renderer may swap to a new vertex buffer without disposing the previous
			// So no way to tell if it swapped within code :(
			_skinnedVertexBuffer?.Release();
			_skinnedVertexBuffer = null;

			// Update skinned vertex buffer
			if (MeshRenderer != null)
			{
				_skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
				_skinnedVertexBuffer = _skinnedMeshRenderer.GetVertexBuffer();
			}

			// Update skinned local to world matrix
			if (MeshRenderer != null && MeshRenderer.rootBone != null)
			{
				// Skinning will apply transformations relative to root bone,
				// so we need to create a post transform local to world 
				_skinnedLocalToWorld = Matrix4x4.TRS(MeshRenderer.rootBone.position, 
					MeshRenderer.rootBone.rotation, Vector3.one);
			}
			else
			{
				_skinnedLocalToWorld = transform.localToWorldMatrix;
			}

			LatticeFeature.Enqueue(this);
		}

		#endregion
	}
}