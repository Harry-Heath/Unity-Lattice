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

		#endregion

		#region Properties

		/// <summary>
		/// Skinned lattices to apply.
		/// </summary>
		public List<Lattice> SkinnedLattices => _skinnedLattices;

		/// <summary>
		/// Gets the current skinned vertex buffer.
		/// </summary>
		public GraphicsBuffer SkinnedVertexBuffer
		{
			get
			{
				if (_skinnedVertexBuffer == null && MeshRenderer != null)
				{ 
					_skinnedMeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
					_skinnedVertexBuffer = _skinnedMeshRenderer.GetVertexBuffer();
				}
				return _skinnedVertexBuffer;
			}
		}

		/// <summary>
		/// Gets the current skinned local to world matrix.
		/// </summary>
		public Matrix4x4 SkinnedLocalToWorld
		{
			get
			{
				if (MeshRenderer != null && MeshRenderer.rootBone != null)
				{
					return Matrix4x4.TRS(MeshRenderer.rootBone.position, MeshRenderer.rootBone.rotation, Vector3.one);
				}
				else
				{
					return transform.localToWorldMatrix;
				}
			}
		}

		/// <inheritdoc cref="LatticeModifier.IsValid"/>
		public override bool IsValid => base.IsValid && (SkinnedVertexBuffer != null);

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
			LatticeFeature.Enqueue(this);
		}

		#endregion
	}
}