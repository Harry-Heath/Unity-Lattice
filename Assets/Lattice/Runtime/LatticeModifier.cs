using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice
{
	[ExecuteAlways, DisallowMultipleComponent]
	public class LatticeModifier : MonoBehaviour
	{
		[SerializeField] private Mesh _targetMesh;
		[SerializeField] private bool _highQuality;
		[SerializeField] private List<Lattice> _lattices = new();

		private Mesh _mesh;
		private MeshInfo _meshInfo;
		private MeshFilter _meshFilter;

		private GraphicsBuffer _copyBuffer;
		private GraphicsBuffer _vertexBuffer;
		private bool _ranThisFrame = false;

		public List<Lattice> Lattices => _lattices;
		public MeshInfo MeshInfo => _meshInfo;
		public GraphicsBuffer CopyBuffer => _copyBuffer;
		public GraphicsBuffer VertexBuffer => _vertexBuffer;
		public Matrix4x4 LocalToWorld => transform.localToWorldMatrix;
		public bool HighQuality => _highQuality;
		public virtual bool IsValid => _vertexBuffer != null && _copyBuffer != null;
		private MeshFilter MeshFilter => (_meshFilter == null)
			? _meshFilter = GetComponent<MeshFilter>()
			: _meshFilter;


		#region Public Methods

		public void ApplyMesh()
		{
			SetMesh(_mesh);
		}

		public void ResetMesh()
		{
			SetMesh(_targetMesh);
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
			_mesh.vertexBufferTarget |= (GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource | GraphicsBuffer.Target.CopyDestination);

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
				(GraphicsBuffer.Target.Raw | GraphicsBuffer.Target.CopySource | GraphicsBuffer.Target.CopyDestination),
				_meshInfo.VertexCount,
				_meshInfo.BufferStride
			);
			Graphics.CopyBuffer(_vertexBuffer, _copyBuffer);
		}

		protected virtual void Release()
		{
			_copyBuffer?.Release();
			_copyBuffer = null;

			_vertexBuffer?.Release();
			_vertexBuffer = null;

			_mesh = null;
		}

		protected virtual Mesh GetMesh()
		{
			return MeshFilter.sharedMesh;
		}

		protected virtual void SetMesh(Mesh mesh)
		{
			MeshFilter.sharedMesh = mesh;
		}

		protected virtual void Enqueue()
		{
			LatticeFeature.Enqueue(this);
		}

		#endregion

		#region Unity Methods

		private void Update()
		{
			if (_mesh != null && _mesh != GetMesh())
			{
				ApplyMesh();
			}
			_ranThisFrame = false;
		}

		private void OnEnable()
		{
			Initialise();
			ApplyMesh();
		}

		private void OnDisable()
		{
			ResetMesh();
			Release();
		}

		private void OnWillRenderObject()
		{
			if (IsValid && !_ranThisFrame)
			{
				_ranThisFrame = true;
				Enqueue();
			}
		}

		#endregion
	}
}