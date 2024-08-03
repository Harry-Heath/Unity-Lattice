using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice
{
	[ExecuteAlways, DisallowMultipleComponent]
	public class LatticeModifier : MonoBehaviour
	{
		private const GraphicsBuffer.Target BufferTargets = GraphicsBuffer.Target.Raw 
			| GraphicsBuffer.Target.CopySource | GraphicsBuffer.Target.CopyDestination;

		#region Fields

		[SerializeField] private Mesh _targetMesh;
		[SerializeField] private bool _highQuality;
		[SerializeField] private List<Lattice> _lattices = new();

		private Mesh _mesh;
		private MeshInfo _meshInfo;
		private MeshFilter _meshFilter;
		private MeshRenderer _meshRenderer;

		private GraphicsBuffer _copyBuffer;
		private GraphicsBuffer _vertexBuffer;
		private bool _ranThisFrame = false;

		#endregion

		#region Properties

		/// <summary>
		/// Lattices to apply to this mesh.
		/// </summary>
		public List<Lattice> Lattices => _lattices;

		/// <summary>
		/// Vertex information about this mesh.
		/// </summary>
		public MeshInfo MeshInfo => _meshInfo;

		/// <summary>
		/// A copy of this mesh's vertex buffer.
		/// </summary>
		public GraphicsBuffer CopyBuffer => _copyBuffer;

		/// <summary>
		/// The mesh's vertex buffer.
		/// </summary>
		public GraphicsBuffer VertexBuffer => _vertexBuffer;

		/// <summary>
		/// The mesh's local to world transform matrix.
		/// </summary>
		public Matrix4x4 LocalToWorld => transform.localToWorldMatrix;

		/// <summary>
		/// Whether the lattices will be applied with high quality tricubic sampling, 
		/// or low quality trilinear sampling.
		/// </summary>
		public bool HighQuality => _highQuality;

		/// <summary>
		/// Whether the component is valid and can be applied without errors.
		/// </summary>
		public bool IsValid => _vertexBuffer != null && _copyBuffer != null;

		/// <summary>
		/// Retrieves the mesh filter on the current object.
		/// </summary>
		private MeshFilter MeshFilter => (_meshFilter == null)
			? _meshFilter = GetComponent<MeshFilter>()
			: _meshFilter;

		/// <summary>
		/// Retrieves the mesh renderer of the current object.
		/// </summary>
		protected virtual Renderer Renderer => (_meshRenderer == null)
			? _meshRenderer = GetComponent<MeshRenderer>()
			: _meshRenderer;

		#endregion


		#region Public Methods

		/// <summary>
		/// Applies the lattice effect mesh to the renderer.
		/// </summary>
		public void ApplyMesh()
		{
			SetMesh(_mesh);
		}

		/// <summary>
		/// Applies the original target mesh to the renderer.
		/// </summary>
		public void ResetMesh()
		{
			SetMesh(_targetMesh);
		}

		#endregion

		#region Protected Methods

		/// <summary>
		/// Releases buffers used for lattice deforming.
		/// </summary>
		protected virtual void Release()
		{
			_copyBuffer?.Release();
			_copyBuffer = null;

			_vertexBuffer?.Release();
			_vertexBuffer = null;

			_mesh = null;
		}

		/// <summary>
		/// Gets the current mesh, either from a mesh filter or skinned mesh renderer.
		/// </summary>
		protected virtual Mesh GetMesh()
		{
			return MeshFilter.sharedMesh;
		}

		/// <summary>
		/// Sets a mesh, either on a mesh filter or skinned mesh renderer.
		/// </summary>
		protected virtual void SetMesh(Mesh mesh)
		{
			MeshFilter.sharedMesh = mesh;
		}

		/// <summary>
		/// Queues the modifier to be run this frame.
		/// </summary>
		protected virtual void Enqueue()
		{
			LatticeFeature.Enqueue(this);
		}

		#endregion

		#region Private Methods

		private void Initialise()
		{
			// Try get target mesh if one is not set
			if (_targetMesh == null)
			{
				_targetMesh = GetMesh();
			}

			// If no target mesh, log warning and exit early
			if (_targetMesh == null)
			{
				Debug.LogWarning("No target mesh set. Can not initialise lattice modifier.", this);
				return;
			}

			// If not readable, log error and exit early
			if (!_targetMesh.isReadable)
			{
				Debug.LogError("Target does not have read/write enabled. Enable it in the model import settings.", _targetMesh);
				return;
			}

			// Create a copy which the lattice will be applied to
			_mesh = Instantiate(_targetMesh);
			_mesh.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
			_mesh.name = _targetMesh.name + " (Lattice)";
			_mesh.vertexBufferTarget |= BufferTargets;

			// Add stretch and squish vertex channel
			Vector2[] stretch = new Vector2[_mesh.vertexCount];
			Array.Fill(stretch, Vector2.one);
			_mesh.SetUVs(3, stretch);

			// Get mesh information
			_meshInfo.VertexCount    = _mesh.vertexCount;
			_meshInfo.BufferStride   = _mesh.GetVertexBufferStride(0);
			_meshInfo.PositionOffset = _mesh.GetVertexAttributeOffset(VertexAttribute.Position);
			_meshInfo.NormalOffset   = _mesh.GetVertexAttributeOffset(VertexAttribute.Normal);
			_meshInfo.TangentOffset  = _mesh.GetVertexAttributeOffset(VertexAttribute.Tangent);
			_meshInfo.StretchOffset  = _mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord3);

			// Get vertex buffer
			_vertexBuffer = _mesh.GetVertexBuffer(0);

			// Create copy of vertex buffer
			// Will be used for resetting to original every frame
			_copyBuffer = new(
				BufferTargets,
				_meshInfo.VertexCount,
				_meshInfo.BufferStride
			);
			Graphics.CopyBuffer(_vertexBuffer, _copyBuffer);
		}

		#endregion

		#region Unity Methods

		private void Update()
		{
			if (_mesh != null && _mesh != GetMesh())
			{
				ApplyMesh();
			}
		}

		private void LateUpdate()
		{
			if (!_ranThisFrame && IsValid && Renderer.isVisible)
			{
				Enqueue();
			}
			_ranThisFrame = false;
		}

		private void OnEnable()
		{
			Initialise();
			ApplyMesh();
			Enqueue();
			_ranThisFrame = true;
		}

		private void OnDisable()
		{
			ResetMesh();
			Release();
		}

		#endregion
	}
}