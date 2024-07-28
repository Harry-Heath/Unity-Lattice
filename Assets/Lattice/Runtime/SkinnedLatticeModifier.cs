using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lattice
{
	[ExecuteAlways, DisallowMultipleComponent]
	public class SkinnedLatticeModifier : LatticeModifier
	{
		[SerializeField] private List<Lattice> _skinnedLattices = new();

		private SkinnedMeshRenderer _skinnedMeshRenderer;
		private GraphicsBuffer _skinnedVertexBuffer;

		private SkinnedMeshRenderer MeshRenderer
		{
			get
			{
				if (_skinnedMeshRenderer == null)
				{
					_skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
				}
				return _skinnedMeshRenderer;
			}
		}

		private GraphicsBuffer SkinnedVertexBuffer
		{
			get
			{
				if (_skinnedVertexBuffer == null && MeshRenderer != null)
				{
					MeshRenderer.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
					_skinnedVertexBuffer = _skinnedMeshRenderer.GetVertexBuffer();
				}
				return _skinnedVertexBuffer;
			}
		}

		private Matrix4x4 SkinnedLocalToWorld
		{
			get
			{
				if (MeshRenderer && MeshRenderer.rootBone != null)
				{
					return Matrix4x4.TRS(MeshRenderer.rootBone.position, MeshRenderer.rootBone.rotation, Vector3.one);
				}
				else
				{
					return transform.localToWorldMatrix;
				}
			}
		}

		//protected override bool IsValid => base.IsValid && SkinnedVertexBuffer != null;

		//public void ExecuteSkinned(CommandBuffer cmd, ComputeShader compute, ComputeBuffer latticeBuffer)
		//{
		//	if (this == null) return;

		//	if (HighQuality) cmd.EnableShaderKeyword("LATTICE_HIGH_QUALITY");
		//	else cmd.DisableShaderKeyword("LATTICE_HIGH_QUALITY");

		//	cmd.SetComputeIntParam(compute, LatticeShaderProperties.VertexCount, VertexCount);
		//	cmd.SetComputeIntParam(compute, LatticeShaderProperties.BufferStride, BufferStride);
		//	cmd.SetComputeIntParam(compute, LatticeShaderProperties.PositionOffset, PositionOffset);
		//	cmd.SetComputeIntParam(compute, LatticeShaderProperties.NormalOffset, NormalOffset);
		//	cmd.SetComputeIntParam(compute, LatticeShaderProperties.TangentOffset, TangentOffset);

		//	cmd.SetComputeBufferParam(compute, 1, LatticeShaderProperties.VertexBuffer, SkinnedVertexBuffer);
		//	cmd.SetComputeBufferParam(compute, 1, LatticeShaderProperties.OriginalBuffer, CopyBuffer);
		//	cmd.SetComputeBufferParam(compute, 1, LatticeShaderProperties.LatticeBuffer, latticeBuffer);

		//	compute.GetKernelThreadGroupSizes(1, out uint x, out uint _, out uint _);

		//	for (int i = 0; i < _skinnedLattices.Count; i++)
		//	{
		//		Matrix4x4 objectToLattice = _skinnedLattices[i].transform.worldToLocalMatrix * SkinnedLocalToWorld;
		//		Matrix4x4 latticeToObject = objectToLattice.inverse;

		//		cmd.SetComputeMatrixParam(compute, LatticeShaderProperties.ObjectToLattice, objectToLattice);
		//		cmd.SetComputeMatrixParam(compute, LatticeShaderProperties.LatticeToObject, latticeToObject);

		//		_resolution[0] = _skinnedLattices[i]._resolution.x;
		//		_resolution[1] = _skinnedLattices[i]._resolution.y;
		//		_resolution[2] = _skinnedLattices[i]._resolution.z;
		//		cmd.SetComputeIntParams(compute, LatticeShaderProperties.LatticeResolution, _resolution);

		//		cmd.SetBufferData(latticeBuffer, _skinnedLattices[i].Offsets);

		//		cmd.DispatchCompute(compute, 1, VertexCount / (int)x + 1, 1, 1);//
		//	}
		//}

		protected override Mesh GetMesh() => MeshRenderer.sharedMesh;
		protected override void SetMesh(Mesh mesh) => MeshRenderer.sharedMesh = mesh;

		protected override void Release()
		{
			base.Release();

			_skinnedVertexBuffer?.Release();
			_skinnedVertexBuffer = null;
		}

		protected override void Enqueue()
		{
			LatticeFeature.Enqueue(this);
		}
	}
}