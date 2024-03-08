using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Heath.Lattice.Editor
{
	[CanEditMultipleObjects]
	[CustomEditor(typeof(LatticeHandle))]
	public class LatticeHandleEditor : UnityEditor.Editor
	{
		private LatticeHandle LatticeHandle => target as LatticeHandle;

		private void OnSceneGUI()
		{
			using var scope = new Handles.DrawingScope(Color.white, LatticeHandle.Lattice.transform.localToWorldMatrix);

			float size = HandleUtility.GetHandleSize(LatticeHandle.Position) * 0.05f;
			Handles.FreeMoveHandle(LatticeHandle.Position, Quaternion.identity, size, Vector3.one * 0.01f, Handles.DotHandleCap);
		}

		[DrawGizmo(GizmoType.Active, typeof(LatticeHandle))]
		private static void DrawGizmosCallback(LatticeHandle latticeHandle, GizmoType gizmoType)
		{
			LatticeEditor.DrawLattice(latticeHandle.Lattice, new(1f, 1f, 1f, 0.2f), CompareFunction.Always);
			LatticeEditor.DrawLattice(latticeHandle.Lattice, new(1f, 1f, 1f, 0.8f), CompareFunction.LessEqual);
		}
	}
}