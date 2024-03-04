using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Heath.Lattice
{
	[ExecuteAlways, DisallowMultipleComponent]
	public class LatticeModifier : MonoBehaviour
	{
		[SerializeField] private Mesh _targetMesh;
		[SerializeField] private bool _highQuality;
		[SerializeField] private List<Lattice> _lattices = new();

		private MeshFilter _meshFilter;
		private Mesh _mesh;

		private ComputeBuffer _originalBuffer;
		private GraphicsBuffer _vertexBuffer;
		private bool _ranThisFrame = false;

		private MeshFilter MeshFilter
		{
			get
			{
				if (_meshFilter == null)
				{
					_meshFilter = GetComponent<MeshFilter>();
				}
				return _meshFilter;
			}
		}

		protected Mesh Mesh => _mesh;

		protected int VertexCount => Mesh.vertexCount;
		protected int BufferStride => Mesh.GetVertexBufferStride(0);
		protected int PositionOffset => Mesh.GetVertexAttributeOffset(VertexAttribute.Position);
		protected int NormalOffset => Mesh.GetVertexAttributeOffset(VertexAttribute.Normal);
		protected int TangentOffset => Mesh.GetVertexAttributeOffset(VertexAttribute.Tangent);
		protected int StretchOffset => Mesh.GetVertexAttributeOffset(VertexAttribute.TexCoord3);

		protected ComputeBuffer OriginalBuffer
		{
			get
			{
				if (_originalBuffer == null && Mesh != null)
				{
					_originalBuffer = new(Mesh.vertexCount, 9 * sizeof(float));
					CopyVertexBuffer();
				}
				return _originalBuffer;
			}
		}

		protected GraphicsBuffer VertexBuffer
		{
			get
			{
				if (_vertexBuffer == null && Mesh != null)
				{
					Mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
					_vertexBuffer = Mesh.GetVertexBuffer(0);
				}

				return _vertexBuffer;
			}
		}

		protected Matrix4x4 LocalToWorld => transform.localToWorldMatrix;

		protected bool HighQuality => _highQuality;
		protected virtual bool Valid => VertexBuffer != null && OriginalBuffer != null;
		private bool RanThisFrame => _ranThisFrame;

		private void Update()
		{
			if (_mesh != null && _mesh != GetMesh())
			{
				ApplyMesh();
			}

			_ranThisFrame = false;
		}

		private void SetupMesh()
		{
			Mesh sharedMesh = _targetMesh != null ? _targetMesh : GetMesh();

			_targetMesh = sharedMesh;
			_mesh = Instantiate(sharedMesh);
			_mesh.hideFlags |= HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
			_mesh.name = sharedMesh.name + " (Lattice)";

			Vector2[] d = new Vector2[_mesh.vertexCount];
			Array.Fill(d, Vector2.one);
			_mesh.SetUVs(3, d);
		}

		private void CopyVertexBuffer()
		{
			// Copy into original buffer
			LatticeFeature.Compute.SetInt(LatticeShaderProperties.VertexCount, VertexCount);
			LatticeFeature.Compute.SetInt(LatticeShaderProperties.BufferStride, BufferStride);
			LatticeFeature.Compute.SetInt(LatticeShaderProperties.PositionOffset, PositionOffset);
			LatticeFeature.Compute.SetInt(LatticeShaderProperties.NormalOffset, NormalOffset);
			LatticeFeature.Compute.SetInt(LatticeShaderProperties.TangentOffset, TangentOffset);

			LatticeFeature.Compute.SetBuffer(0, LatticeShaderProperties.VertexBuffer, VertexBuffer);
			LatticeFeature.Compute.SetBuffer(0, LatticeShaderProperties.OriginalBuffer, OriginalBuffer);

			LatticeFeature.Compute.GetKernelThreadGroupSizes(0, out uint x, out uint _, out uint _);
			LatticeFeature.Compute.Dispatch(0, VertexCount / (int)x + 1, 1, 1);
		}

		public void Execute(CommandBuffer cmd, ComputeShader compute, ComputeBuffer latticeBuffer)
		{
			if (this == null || _mesh == null) return;

			if (HighQuality) cmd.EnableShaderKeyword("LATTICE_HIGH_QUALITY");
			else cmd.DisableShaderKeyword("LATTICE_HIGH_QUALITY");

			cmd.SetComputeIntParam(compute, LatticeShaderProperties.VertexCount, VertexCount);
			cmd.SetComputeIntParam(compute, LatticeShaderProperties.BufferStride, BufferStride);
			cmd.SetComputeIntParam(compute, LatticeShaderProperties.PositionOffset, PositionOffset);
			cmd.SetComputeIntParam(compute, LatticeShaderProperties.NormalOffset, NormalOffset);
			cmd.SetComputeIntParam(compute, LatticeShaderProperties.TangentOffset, TangentOffset);
			cmd.SetComputeIntParam(compute, LatticeShaderProperties.StretchOffset, StretchOffset);

			{
				cmd.SetComputeBufferParam(compute, 2, LatticeShaderProperties.VertexBuffer, VertexBuffer);
				cmd.SetComputeBufferParam(compute, 2, LatticeShaderProperties.OriginalBuffer, OriginalBuffer);

				compute.GetKernelThreadGroupSizes(2, out uint x2, out uint _, out uint _);
				cmd.DispatchCompute(compute, 2, VertexCount / (int)x2 + 1, 1, 1);
			}

			cmd.SetComputeBufferParam(compute, 1, LatticeShaderProperties.VertexBuffer, VertexBuffer);
			cmd.SetComputeBufferParam(compute, 1, LatticeShaderProperties.OriginalBuffer, OriginalBuffer);
			cmd.SetComputeBufferParam(compute, 1, LatticeShaderProperties.LatticeBuffer, latticeBuffer);

			compute.GetKernelThreadGroupSizes(1, out uint x, out uint _, out uint _);

			for (int i = 0; i < _lattices.Count; i++)
			{
				Matrix4x4 objectToLattice = _lattices[i].transform.worldToLocalMatrix * LocalToWorld;
				Matrix4x4 latticeToObject = objectToLattice.inverse;

				cmd.SetComputeMatrixParam(compute, LatticeShaderProperties.ObjectToLattice, objectToLattice);
				cmd.SetComputeMatrixParam(compute, LatticeShaderProperties.LatticeToObject, latticeToObject);

				_resolution[0] = _lattices[i]._resolution.x;
				_resolution[1] = _lattices[i]._resolution.y;
				_resolution[2] = _lattices[i]._resolution.z;
				cmd.SetComputeIntParams(compute, LatticeShaderProperties.LatticeResolution, _resolution);

				cmd.SetBufferData(latticeBuffer, _lattices[i].Offsets);

				cmd.DispatchCompute(compute, 1, VertexCount / (int)x + 1, 1, 1);
			}
		}

		protected static int[] _resolution = new int[3];

		protected virtual Mesh GetMesh() => MeshFilter.sharedMesh;
		protected virtual void SetMesh(Mesh mesh)
		{
			Debug.Log($"Setting mesh to {mesh.name}");
			MeshFilter.sharedMesh = mesh;
		}

		public void ApplyMesh()
		{
			SetMesh(Mesh);
		}

		public void ResetMesh()
		{
			SetMesh(_targetMesh);
		}

		private void Initialize()
		{
			SetupMesh();
			CopyVertexBuffer();
		}

		protected virtual void Release()
		{
			_originalBuffer?.Release();
			_originalBuffer = null;

			_vertexBuffer?.Release();
			_vertexBuffer = null;

			_mesh = null;
		}

		private void OnEnable()
		{
			Initialize();
			ApplyMesh();
		}

		private void OnDisable()
		{
			ResetMesh();
			Release();
		}

		protected void OnWillRenderObject()
		{
			if (Valid && !RanThisFrame)
			{
				_ranThisFrame = true;
				Enqueue();
			}
		}

		protected virtual void Enqueue()
		{
			LatticeFeature.Enqueue(this);
		}
	}
}