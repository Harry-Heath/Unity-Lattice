using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Heath.Lattice.Editor
{
	[CustomEditor(typeof(Lattice))]
	public class LatticeEditor : UnityEditor.Editor
	{
		private Lattice Lattice => target as Lattice;

		public override void OnInspectorGUI()
		{
			//Lattice.Fix();

			if (Lattice.Handles.Count != Lattice.Resolution.x * Lattice.Resolution.y * Lattice.Resolution.z)
			{
				Lattice.Setup(Lattice.Resolution);
			}

			base.OnInspectorGUI();
		}

		private void OnSceneGUI()
		{
			//Lattice.Fix();

			if (Lattice.Handles.Count != Lattice.Resolution.x * Lattice.Resolution.y * Lattice.Resolution.z)
			{
				Lattice.Setup(Lattice.Resolution);
			}

			DrawGizmos(Lattice, new(1f, 1f, 1f, 0.2f), CompareFunction.Greater);
			DrawGizmos(Lattice, Color.white, CompareFunction.LessEqual);
		}

		[DrawGizmo(GizmoType.InSelectionHierarchy, typeof(Lattice))]
		private static void DrawGizmosCallback(Lattice lattice, GizmoType gizmoType)
		{
			//lattice.Fix();

			if (gizmoType.HasFlag(GizmoType.Active))
			{
				DrawLattice(lattice, new(1f, 1f, 1f, 0.2f), CompareFunction.Always);
				DrawLattice(lattice, new(1f, 1f, 1f, 0.8f), CompareFunction.LessEqual);
			}
			else
			{
				DrawLattice(lattice, new(1f, 1f, 1f, 0.3f), CompareFunction.LessEqual);
			}
		}

		private static void DrawGizmos(Lattice lattice, Color color, CompareFunction compareFunction)
		{
			CompareFunction previousZTest = Handles.zTest;
			Handles.zTest = compareFunction;

			using var scope = new Handles.DrawingScope(color, lattice.transform.localToWorldMatrix);

			for (int i = 0; i < lattice.Resolution.x; i++)
			{
				for (int j = 0; j < lattice.Resolution.y; j++)
				{
					for (int k = 0; k < lattice.Resolution.z; k++)
					{
						float size = HandleUtility.GetHandleSize(lattice.GetHandlePosition(i, j, k)) * 0.05f;

						Vector3 handlePosition = lattice.GetHandlePosition(i, j, k);
						Vector3 newPosition = Handles.FreeMoveHandle(handlePosition, Quaternion.identity, size, Vector3.one * 0.01f, Handles.DotHandleCap);

						if (newPosition != handlePosition)
						{
							Undo.RecordObject(lattice.GetHandle(i, j, k), "Edited lattice handle");
							lattice.SetHandlePosition(i, j, k, newPosition);

							EditorApplication.QueuePlayerLoopUpdate();
						}
					}
				}
			}

			Handles.zTest = previousZTest;
		}

		private static void DrawLattice(Lattice lattice, Color color, CompareFunction compareFunction)
		{
			CompareFunction previousZTest = Handles.zTest;
			Handles.zTest = compareFunction;

			for (int i = 0; i < lattice.Resolution.x; i++)
			{
				for (int j = 0; j < lattice.Resolution.y; j++)
				{
					for (int k = 0; k < lattice.Resolution.z; k++)
					{
						if (i != lattice.Resolution.x - 1) DrawLine(lattice.GetHandlePosition(i, j, k), lattice.GetHandlePosition(i + 1, j, k), lattice, color);
						if (j != lattice.Resolution.y - 1) DrawLine(lattice.GetHandlePosition(i, j, k), lattice.GetHandlePosition(i, j + 1, k), lattice, color);
						if (k != lattice.Resolution.z - 1) DrawLine(lattice.GetHandlePosition(i, j, k), lattice.GetHandlePosition(i, j, k + 1), lattice, color);
					}
				}
			}

			Handles.zTest = previousZTest;
		}

		private static void DrawLine(Vector3 a, Vector3 b, Lattice lattice, Color color)
		{
			float squishStretchFactor = Vector3.Scale(b - a, lattice.Resolution - Vector3Int.one).magnitude - 1f;
			squishStretchFactor = Mathf.Clamp(squishStretchFactor, -1f, 1f);

			Color lineColour = LatticeSettings.Gradient.Evaluate(0.5f * squishStretchFactor + 0.5f);

			float thickness = Mathf.Lerp(
				LatticeSettings.LineThickness, squishStretchFactor < 0 ?
					LatticeSettings.LineThicknessSquish :
					LatticeSettings.LineThicknessStretch,
				Mathf.Abs(squishStretchFactor)
			);

			using var scope = new Handles.DrawingScope(color * lineColour, Matrix4x4.identity);
			
			Handles.DrawLine(
				lattice.transform.localToWorldMatrix.MultiplyPoint(a),
				lattice.transform.localToWorldMatrix.MultiplyPoint(b),
				thickness
			);
		}
	}
}