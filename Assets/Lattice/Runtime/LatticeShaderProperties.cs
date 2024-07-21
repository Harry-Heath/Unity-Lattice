using UnityEngine;

namespace Lattice
{
	public static class LatticeShaderProperties
	{
		public static readonly int VertexCount = Shader.PropertyToID("VertexCount");
		public static readonly int BufferStride = Shader.PropertyToID("BufferStride");
		public static readonly int PositionOffset = Shader.PropertyToID("PositionOffset");
		public static readonly int NormalOffset = Shader.PropertyToID("NormalOffset");
		public static readonly int TangentOffset = Shader.PropertyToID("TangentOffset");
		public static readonly int StretchOffset = Shader.PropertyToID("StretchOffset");
		public static readonly int VertexBuffer = Shader.PropertyToID("VertexBuffer");
		public static readonly int OriginalBuffer = Shader.PropertyToID("OriginalBuffer");
		public static readonly int LatticeBuffer = Shader.PropertyToID("LatticeBuffer");
		public static readonly int ObjectToLattice = Shader.PropertyToID("ObjectToLattice");
		public static readonly int LatticeToObject = Shader.PropertyToID("LatticeToObject");
		public static readonly int LatticeResolution = Shader.PropertyToID("LatticeResolution");
	}
}